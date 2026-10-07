using AuraKnight.Combat;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>
    /// Plays Leo's movement and sword sounds by observing the public API of PlayerController (state changes, the
    /// per-step event, grounded and double-jump flags) and the Hitboxes under the player, so Player and Combat code
    /// stays free of audio. Added to the Player prefab by <c>PlayerSfxProbeInstaller</c> (editor, idempotent).
    /// Hurt, death and respawn sounds come from <see cref="AudioEventListener"/> through the EventBus.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSfxProbe : MonoBehaviour
    {
        PlayerController _controller;
        Hitbox[] _hitboxes = System.Array.Empty<Hitbox>();
        readonly FootstepTracker _steps = new FootstepTracker();
        readonly LandingDetector _landing = new LandingDetector();
        bool _bound;
        bool _doubleJumpWasUsed;

        void Start() => Bind();

        void OnDestroy() => Unbind();

        void Bind()
        {
            if (_bound) return;
            _controller = GetComponent<PlayerController>();
            if (_controller == null || _controller.StateMachine == null)
            {
                Debug.LogError($"{nameof(PlayerSfxProbe)} on '{name}' needs an initialized {nameof(PlayerController)}.", this);
                enabled = false;
                return;
            }
            _controller.StateMachine.StateChanged += OnStateChanged;
            _controller.Stepped += OnStep;
            _hitboxes = GetComponentsInChildren<Hitbox>(true);
            foreach (var hitbox in _hitboxes) hitbox.Hit += OnHit;
            _bound = true;
        }

        void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            if (_controller != null)
            {
                _controller.StateMachine.StateChanged -= OnStateChanged;
                _controller.Stepped -= OnStep;
            }
            foreach (var hitbox in _hitboxes) if (hitbox != null) hitbox.Hit -= OnHit;
        }

        void OnStateChanged(PlayerStateId from, PlayerStateId to)
        {
            var id = SfxEventMap.ForPlayerState(to);
            if (id != SfxId.None) Sfx.Play(id, transform.position);
        }

        void OnStep(float deltaTime)
        {
            bool grounded = _controller.Grounded;
            if (_landing.Update(grounded, deltaTime)) Sfx.Play(SfxId.Land, transform.position);

            bool running = grounded && _controller.StateMachine.CurrentId == PlayerStateId.Run;
            if (_steps.Advance(_controller.Velocity.x * deltaTime, running)) Sfx.Play(SfxId.Footstep, transform.position);

            // a double jump re-enters the Jump state without a state change, so watch its flag instead
            bool used = _controller.DoubleJumpUsed;
            if (used && !_doubleJumpWasUsed) Sfx.Play(SfxId.Jump, transform.position);
            _doubleJumpWasUsed = used;
        }

        void OnHit(HitReport report)
        {
            if (report.Outcome.DealtDamage()) Sfx.Play(SfxId.SwordHit, transform.position);
        }
    }
}
