using AuraKnight.Aura;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Updraft zone (trigger): lifts Leo upward, but only while the Wind aura is active. Hooks the controller's
    /// per-step event so the push survives the state's own gravity.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WindCurrent : MonoBehaviour
    {
        public const float LiftSpeed = 9f;
        public const float LiftAcceleration = 240f;

        [SerializeField, Min(0f)] float liftSpeed = LiftSpeed;
        [SerializeField, Min(0f)] float liftAcceleration = LiftAcceleration;

        PlayerController _rider;

        void Reset()
        {
            if (TryGetComponent<Collider2D>(out var trigger)) trigger.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_rider != null || !WorldTags.IsPlayer(other)) return;
            _rider = other.GetComponentInParent<PlayerController>();
            if (_rider != null) _rider.Stepped += OnStep;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (_rider != null && WorldTags.IsPlayer(other)) Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            if (_rider == null) return;
            _rider.Stepped -= OnStep;
            _rider = null;
        }

        void OnStep(float deltaTime)
        {
            var manager = AuraManager.Instance;
            if (manager == null || manager.Current != AuraId.Wind) return;
            Lift(_rider, deltaTime, liftSpeed, liftAcceleration);
        }

        /// <summary>Accelerates the rider's vertical speed toward <paramref name="speed"/> (never slows an already faster rise).</summary>
        public static void Lift(PlayerController rider, float deltaTime, float speed, float acceleration)
        {
            float vy = rider.Velocity.y;
            if (vy >= speed) return;
            rider.SetVelocityY(Mathf.Min(speed, vy + acceleration * deltaTime));
        }
    }
}
