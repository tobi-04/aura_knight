using System;
using AuraKnight.Player.States;
using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Leo's brain: owns velocity, timers (coyote, input buffers, dash) and the state machine, and moves the
    /// <see cref="KinematicMotor2D"/> once per physics step. States only decide velocity.
    /// </summary>
    [RequireComponent(typeof(KinematicMotor2D), typeof(PlayerInputReader))]
    public sealed partial class PlayerController : MonoBehaviour, World.ITeleportable
    {
        const float StickDeadzone = 0.3f;
        const float GroundedMaxRiseSpeed = 0.01f;

        [SerializeField] PlayerMovementConfig config;

        float _previousVelocityY;
        bool _controlsFlag = true, _controlsWereEnabled = true;

        /// <summary>Wind aura: one extra jump (v = 20) while airborne.</summary>
        public bool CanDoubleJump { get; set; }
        /// <summary>Wind aura: holding JUMP while falling caps fall speed at 3.5 u/s.</summary>
        public bool CanGlide { get; set; }
        /// <summary>Scales run speed (Fire aura = 1.2).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Scales launch speed of ground and double jumps (wading without the Water aura = 0.6).</summary>
        public float JumpMultiplier { get; set; } = 1f;
        /// <summary>Set by water volumes; the Swim state takes over once registered.</summary>
        public bool SwimMode { get; set; }
        /// <summary>Combat sets this for hurt i-frames; dash i-frames are tracked separately.</summary>
        public bool ExternalInvulnerable { get; set; }
        /// <summary>False when paused/loading (GameManager.Mode != Playing) or switched off: no input latching, steering or actions.</summary>
        public bool ControlsEnabled
        {
            get => _controlsFlag && Core.GameManager.ModeAllowsControl;
            set => _controlsFlag = value;
        }
        public bool IsInvulnerable => ExternalInvulnerable || (Dash != null && Dash.IsInvulnerable);

        // ---- Shared services ----
        public PlayerMovementConfig Config => config;
        public KinematicMotor2D Motor { get; private set; }
        public IPlayerInput Input { get; private set; }
        public PlayerStateMachine StateMachine { get; } = new PlayerStateMachine();
        public DashTracker Dash { get; private set; }
        public JumpCut JumpCut { get; private set; }
        public Countdown Coyote { get; } = new Countdown();
        public Countdown JumpBuffer { get; } = new Countdown();
        public Countdown DashBuffer { get; } = new Countdown();
        public Countdown SlideBuffer { get; } = new Countdown();
        /// <summary>Latched ATK presses for the combat phase to consume.</summary>
        public Countdown AttackBuffer { get; } = new Countdown();

        /// <summary>Raised once per physics step after timers tick and before the state runs (combat starts attacks here).</summary>
        public event Action<float> Stepped;

        // ---- Runtime state ----
        public Vector2 Velocity { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool Grounded { get; private set; }
        public bool DoubleJumpUsed { get; set; }
        public int WallJumpDirection { get; set; }
        public MotorContacts LastContacts { get; private set; }
        public float Dt { get; private set; }

        public int MoveDirection
        {
            get
            {
                if (!ControlsEnabled) return 0; // paused / loading: the stick must not steer
                float x = Input.Move.x;
                return Mathf.Abs(x) < StickDeadzone ? 0 : (x > 0f ? 1 : -1);
            }
        }

        public float RunSpeed => config.RunSpeed * SpeedMultiplier;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(PlayerController)} on '{name}' has no PlayerMovementConfig assigned.", this);
                enabled = false;
                return;
            }
            Initialize(config, GetComponent<PlayerInputReader>(), GetComponent<KinematicMotor2D>());
        }

        internal void Initialize(PlayerMovementConfig movementConfig, IPlayerInput input, KinematicMotor2D motor)
        {
            config = movementConfig;
            Input = input;
            Motor = motor;
            Motor.Initialize();
            Motor.CornerCorrection = config.CornerCorrection;
            Dash = new DashTracker(config);
            JumpCut = new JumpCut(config);
            RegisterStates();
            StateMachine.TryChange(PlayerStateId.Fall);
        }

        void RegisterStates()
        {
            StateMachine.Register(PlayerStateId.Idle, new IdleState(this));
            StateMachine.Register(PlayerStateId.Run, new RunState(this));
            StateMachine.Register(PlayerStateId.Jump, new JumpState(this));
            StateMachine.Register(PlayerStateId.Fall, new FallState(this));
            StateMachine.Register(PlayerStateId.WallSlide, new WallSlideState(this));
            StateMachine.Register(PlayerStateId.WallJump, new WallJumpState(this));
            StateMachine.Register(PlayerStateId.Dash, new DashState(this));
            StateMachine.Register(PlayerStateId.Slide, new SlideState(this));
        }

        void Update()
        {
            ReadInput();
            StateMachine.Current.Tick();
        }

        void FixedUpdate() => Step(Time.fixedDeltaTime);

        /// <summary>Latches edge-triggered intents into the input buffers (0.12 s).</summary>
        internal void ReadInput()
        {
            if (!ControlsEnabled)
            {
                if (_controlsWereEnabled) CancelBuffers(); // a press latched just before the pause must not fire after it
                _controlsWereEnabled = false;
                return;
            }
            _controlsWereEnabled = true;
            float window = config.InputBufferTime;
            if (Input.JumpPressed) JumpBuffer.Start(window);
            if (Input.DashPressed) DashBuffer.Start(window);
            if (Input.AttackPressed) AttackBuffer.Start(window);
            if (Input.SlideRequested) SlideBuffer.Start(window);
        }

        void CancelBuffers()
        {
            JumpBuffer.Cancel();
            DashBuffer.Cancel();
            AttackBuffer.Cancel();
            SlideBuffer.Cancel();
        }

        /// <summary>One physics step: refresh ground, tick timers, run the state, move the motor.</summary>
        internal void Step(float deltaTime)
        {
            Dt = deltaTime;
            Grounded = Velocity.y <= GroundedMaxRiseSpeed && Motor.CheckGround();
            if (Grounded)
            {
                Coyote.Start(config.CoyoteTime);
                DoubleJumpUsed = false;
                Dash.ResetAir();
            }
            else Coyote.Tick(deltaTime);

            JumpBuffer.Tick(deltaTime);
            DashBuffer.Tick(deltaTime);
            SlideBuffer.Tick(deltaTime);
            AttackBuffer.Tick(deltaTime);
            Dash.Tick(deltaTime);
            Stepped?.Invoke(deltaTime);

            if (Motor.IsCrouched && StateMachine.CurrentId != PlayerStateId.Slide) Motor.TrySetStanding();
            SyncSwimState();
            StateMachine.Current.FixedTick();
            Integrate(deltaTime);
        }

        void Integrate(float deltaTime)
        {
            var v = Velocity;
            var delta = new Vector2(v.x * deltaTime, AirPhysics.Displacement(_previousVelocityY, v.y, deltaTime));
            var contacts = Motor.Move(delta);
            if (contacts.Below && v.y < 0f) v.y = 0f;
            if (contacts.Above && v.y > 0f) v.y = 0f;
            if ((contacts.Left && v.x < 0f) || (contacts.Right && v.x > 0f)) v.x = 0f;
            Velocity = v;
            _previousVelocityY = v.y;
            LastContacts = contacts;
        }

        void SyncSwimState()
        {
            // Hurt, Dead and an air swing own the body: water must not pull Leo out of them (a swim swing is an AirAttack).
            var id = StateMachine.CurrentId;
            if (id == PlayerStateId.Hurt || id == PlayerStateId.Dead || id == PlayerStateId.AirAttack) return;
            bool swimming = StateMachine.CurrentId == PlayerStateId.Swim;
            if (SwimMode && !swimming) StateMachine.TryChange(PlayerStateId.Swim);
            else if (!SwimMode && swimming) StateMachine.TryChange(PlayerStateId.Fall);
        }
    }
}
