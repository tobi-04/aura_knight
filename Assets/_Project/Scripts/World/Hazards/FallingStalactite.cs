using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World.Hazards
{
    /// <summary>
    /// Ceiling spike: the root holds a trigger sensor below it (it never moves). When Leo walks into the sensor the child
    /// <see cref="body"/> shakes for 0.5 s, falls (damaging while it drops) until it touches Ground, shatters, and hangs again
    /// 4 s later. <see cref="Room.Restart"/> style resets happen through OnEnable.
    /// </summary>
    public sealed class FallingStalactite : MonoBehaviour
    {
        const float Gravity = 40f;
        const float MaxFallSpeed = 18f;
        const float ShakeAmplitude = 0.05f;
        const float MaxFallDistance = 40f;

        [SerializeField] Transform body;
        [SerializeField] Hitbox hitbox;
        [SerializeField] SpriteRenderer sprite;
        [Tooltip("Collider of the falling body used to detect the floor.")]
        [SerializeField] BoxCollider2D bodyShape;
        [SerializeField, Min(0.05f)] float shakeSeconds = StalactiteCycle.DefaultShake;
        [SerializeField, Min(0.05f)] float respawnSeconds = StalactiteCycle.DefaultRespawn;

        StalactiteCycle _cycle;
        Vector3 _home;
        float _speed;

        public StalactitePhase Phase => Cycle.Phase;

        StalactiteCycle Cycle => _cycle ??= new StalactiteCycle(shakeSeconds, respawnSeconds);

        void Awake()
        {
            if (body != null) _home = body.localPosition;
        }

        void OnEnable() => ResetBody();

        void OnTriggerEnter2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) Arm();
        }

        /// <summary>Starts the shake (also callable from tests).</summary>
        public void Arm() => Cycle.Arm();

        void FixedUpdate() => Advance(Time.fixedDeltaTime);

        internal void Advance(float deltaTime)
        {
            if (body == null) return;
            switch (Cycle.Phase)
            {
                case StalactitePhase.Shaking:
                    body.localPosition = _home + new Vector3(Mathf.Sin(Cycle.Elapsed * 120f) * ShakeAmplitude, 0f, 0f);
                    if (Cycle.Tick(deltaTime)) BeginFall();
                    break;
                case StalactitePhase.Falling:
                    Fall(deltaTime);
                    break;
                case StalactitePhase.Resting:
                    if (Cycle.Tick(deltaTime)) ResetBody();
                    break;
            }
        }

        void BeginFall()
        {
            _speed = 0f;
            body.localPosition = _home;
            if (hitbox != null) hitbox.Activate(hitbox.Damage);
        }

        void Fall(float deltaTime)
        {
            _speed = Mathf.Min(MaxFallSpeed, _speed + Gravity * deltaTime);
            body.position += Vector3.down * (_speed * deltaTime);
            if (!HitGround() && _home.y - body.localPosition.y < MaxFallDistance) return;
            Shatter();
        }

        bool HitGround()
        {
            if (bodyShape == null) return false;
            var size = Vector2.Scale(bodyShape.size, body.lossyScale);
            var center = body.TransformPoint(bodyShape.offset);
            return Physics2D.OverlapBox(center, size, 0f, PhysicsLayers.GroundMask) != null;
        }

        void Shatter()
        {
            Cycle.Land();
            if (hitbox != null) hitbox.Deactivate();
            if (sprite != null) sprite.enabled = false;
        }

        void ResetBody()
        {
            Cycle.Reset();
            _speed = 0f;
            if (body != null) body.localPosition = _home;
            if (hitbox != null) hitbox.Deactivate();
            if (sprite != null) sprite.enabled = true;
        }
    }
}
