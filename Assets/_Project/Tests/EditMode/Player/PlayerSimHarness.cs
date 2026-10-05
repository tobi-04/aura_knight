using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    /// <summary>Scriptable input: edge flags are consumed by one <see cref="PlayerSimHarness.Frame"/>.</summary>
    public sealed class FakeInput : IPlayerInput
    {
        public Vector2 Move { get; set; }
        public bool JumpHeld { get; set; }
        public bool JumpPressed { get; set; }
        public bool JumpReleased { get; set; }
        public bool DashPressed { get; set; }
        public bool AttackPressed { get; set; }
        public bool SkillPressed { get; set; }
        public bool SlideRequested { get; set; }
        public bool AuraWindPressed { get; set; }
        public bool AuraFirePressed { get; set; }
        public bool AuraWaterPressed { get; set; }
        public bool AuraNextPressed { get; set; }
        public bool AuraPrevPressed { get; set; }
        public bool PausePressed { get; set; }
        public bool MapPressed { get; set; }

        public void ClearEdges()
        {
            JumpPressed = JumpReleased = DashPressed = AttackPressed = SkillPressed = SlideRequested = false;
            AuraWindPressed = AuraFirePressed = AuraWaterPressed = AuraNextPressed = AuraPrevPressed = false;
            PausePressed = MapPressed = false;
        }
    }

    /// <summary>Runs a real PlayerController + KinematicMotor2D against static test colliders, one fixed step at a time.</summary>
    public sealed class PlayerSimHarness
    {
        public const float Dt = 0.02f;
        public readonly PhysicsTestLevel Level = new PhysicsTestLevel();
        public readonly FakeInput Input = new FakeInput();
        public PlayerController Player { get; }
        public PlayerMovementConfig Config { get; }
        public float Time { get; private set; }

        public PlayerSimHarness()
        {
            Config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            Level.Rect(-200f, -1f, 200f, 0f, "Floor");
            var go = new GameObject("Player");
            go.SetActive(false); // Awake must not run in edit mode; we initialise explicitly
            go.transform.position = new Vector3(0f, 0.97f, 0f);
            Level.Track(go);
            var motor = go.AddComponent<KinematicMotor2D>();
            Player = go.AddComponent<PlayerController>();
            Level.Sync();
            Player.Initialize(Config, Input, motor);
        }

        public Vector2 Position => Player.Motor.Position;
        public PlayerStateId State => Player.StateMachine.CurrentId;

        public void Teleport(float x, float y) => Player.Teleport(new Vector2(x, y));

        /// <summary>One Update + one FixedUpdate, then clears edge-triggered input.</summary>
        public void Frame()
        {
            Player.ReadInput();
            Player.Step(Dt);
            Input.ClearEdges();
            Time += Dt;
        }

        public void Run(float seconds)
        {
            int n = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < n; i++) Frame();
        }

        public void Settle() => Run(0.3f);

        public void Dispose()
        {
            Level.Dispose();
            Object.DestroyImmediate(Config);
        }
    }
}
