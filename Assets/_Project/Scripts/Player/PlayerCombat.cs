using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player.States;
using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Leo's fighting brain: starts sword swings from the attack buffer, times the sword hitbox in code,
    /// converts landed hits into energy / pogo / feedback. Damage, death and respawn reactions live in PlayerCombat.Reactions.cs.
    /// Registers the Attack, AirAttack, Hurt and Dead states on the controller.
    /// </summary>
    public sealed partial class PlayerCombat : MonoBehaviour
    {
        /// <summary>Seconds in the Dead state before the checkpoint respawn is requested.</summary>
        public const float RespawnDelay = 1.2f;

        [SerializeField] PlayerController controller;
        [SerializeField] PlayerStats stats;
        [SerializeField] Hitbox swordHitbox;
        [SerializeField] CombatFeedback feedback;

        Health _health;
        float _swingElapsed;
        bool _bound;

        public ComboTracker Combo { get; } = new ComboTracker();
        public AttackDirection Aim { get; private set; }
        /// <summary>The hit that caused the current Hurt state (knockback direction and distance).</summary>
        public DamageInfo LastDamage { get; private set; }

        void Start()
        {
            if (_bound) return;
            if (controller == null) controller = GetComponent<PlayerController>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (feedback == null) feedback = GetComponent<CombatFeedback>();
            if (controller == null || stats == null || swordHitbox == null)
            {
                Debug.LogError($"{nameof(PlayerCombat)} on '{name}' is missing its controller, stats or sword hitbox.", this);
                enabled = false;
                return;
            }
            Initialize(controller, stats, swordHitbox, feedback);
        }

        void OnDestroy() => Unbind();

        public void Initialize(PlayerController playerController, PlayerStats playerStats, Hitbox sword, CombatFeedback juice = null)
        {
            Unbind();
            controller = playerController;
            stats = playerStats;
            swordHitbox = sword;
            feedback = juice;
            _health = stats.Health;
            swordHitbox.Team = Team.Player;
            swordHitbox.AutoPoll = false;
            swordHitbox.Deactivate();
            _health.InvulnerabilityGate = () => controller.IsInvulnerable;
            _health.SelfTicking = false;

            var machine = controller.StateMachine;
            machine.Register(PlayerStateId.Attack, new AttackState(controller, this));
            machine.Register(PlayerStateId.AirAttack, new AirAttackState(controller, this));
            machine.Register(PlayerStateId.Hurt, new HurtState(controller, this));
            machine.Register(PlayerStateId.Dead, new DeadState(controller, this));

            controller.Stepped += OnStep;
            swordHitbox.Hit += OnSwordHit;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            EventBus.Subscribe<PlayerRespawned>(OnRespawned);
            _bound = true;
        }

        public void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            controller.Stepped -= OnStep;
            swordHitbox.Hit -= OnSwordHit;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            EventBus.Unsubscribe<PlayerRespawned>(OnRespawned);
        }

        // ---- swing, driven by Attack / AirAttack states ----

        public void BeginSwing()
        {
            controller.Face(controller.MoveDirection);
            Aim = AttackAim.From(controller.Input.Move, controller.Grounded);
            if (Aim == AttackDirection.Forward) Combo.Begin();
            else Combo.Reset();
            _swingElapsed = 0f;
            var shape = SwordShape.Compute(Aim, controller.Facing);
            swordHitbox.TrySetBox(shape.Center, shape.Size);
        }

        /// <summary>Advances the swing, arms/disarms the hitbox and polls it. Returns true when the swing is over.</summary>
        public bool TickSwing(float deltaTime)
        {
            _swingElapsed += deltaTime;
            bool active = SwordTiming.IsHitboxActive(_swingElapsed);
            if (active && !swordHitbox.IsActive)
                swordHitbox.Activate(stats.SwordLevel, new Vector2(controller.Facing, 0f));
            else if (!active && swordHitbox.IsActive)
                swordHitbox.Deactivate();
            if (swordHitbox.IsActive)
            {
                swordHitbox.transform.position = new Vector3(controller.Motor.Position.x, controller.Motor.Position.y, swordHitbox.transform.position.z);
                swordHitbox.Poll();
            }
            return _swingElapsed >= SwordTiming.SwingDuration - 1e-4f;
        }

        public void EndSwing()
        {
            swordHitbox.Deactivate();
            if (Aim == AttackDirection.Forward) Combo.EndSwing();
            else Combo.Reset();
        }

        // ---- sword ----

        void OnStep(float deltaTime)
        {
            Combo.Tick(deltaTime);
            _health.Invulnerability.Tick(deltaTime);
            controller.ExternalInvulnerable = _health.Invulnerability.IsActive || _health.IsDead;
            TryStartAttack();
        }

        void TryStartAttack()
        {
            if (_health.IsDead || !controller.AttackBuffer.IsActive) return;
            var id = controller.StateMachine.CurrentId;
            if (!PlayerActionRules.CanStartAttack(id, controller.ControlsEnabled)) return;
            if (controller.StateMachine.TryChange(PlayerActionRules.AttackStateFor(id, controller.Grounded)))
                controller.AttackBuffer.Cancel();
        }

        void OnSwordHit(HitReport report)
        {
            if (report.Outcome.DealtDamage())
            {
                stats.GainEnergyFromHit();
                if (feedback != null) feedback.LandedHit();
            }
            bool pogo = Aim == AttackDirection.Down && report.Target.AllowsPogo && report.Outcome != HitOutcome.Ignored;
            if (pogo) controller.SetVelocityY(SwordTiming.PogoSpeed(controller.Config.JumpGravity));
        }
    }
}
