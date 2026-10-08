using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Aura.Skills
{
    /// <summary>
    /// Pooled fireball: flies straight for 12 tiles, deals 2 damage through its <see cref="Hitbox"/>, ignites
    /// Burn gates it touches and vanishes on hitting a target or a solid wall (one-way platforms are passed through).
    /// </summary>
    public sealed class FireballProjectile : MonoBehaviour
    {
        public const float Range = 12f;
        public const int Damage = 2;
        const float Epsilon = 1e-4f;

        [SerializeField] Hitbox hitbox;
        [SerializeField, Min(1f)] float speed = 20f;
        [SerializeField, Min(0.05f)] float radius = 0.25f;

        readonly AuraInteractionProbe _probe = new AuraInteractionProbe();
        Vector2 _direction;
        float _travelled;

        public bool IsFlying { get; private set; }
        public float Travelled => _travelled;

        /// <summary>
        /// Starts a flight from <paramref name="position"/> moving horizontally toward <paramref name="facing"/> (-1 or +1).
        /// <paramref name="owner"/> (Leo) is reported as the damage source instead of the projectile.
        /// </summary>
        public void Launch(Vector2 position, int facing, Transform owner = null)
        {
            _direction = new Vector2(facing >= 0 ? 1f : -1f, 0f);
            _travelled = 0f;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            IsFlying = true;
            gameObject.SetActive(true);
            if (hitbox == null) return;
            hitbox.Team = Team.Player;
            hitbox.Source = owner;
            hitbox.Kind = DamageKind.Fire;
            hitbox.AutoPoll = false;
            hitbox.Activate(Damage, _direction);
        }

        void FixedUpdate() => Step(Time.fixedDeltaTime);

        /// <summary>Advances one physics step; despawns on range, hit or wall.</summary>
        public void Step(float deltaTime)
        {
            if (!IsFlying) return;
            float move = Mathf.Min(speed * deltaTime, Range - _travelled);
            transform.position += (Vector3)(_direction * move);
            _travelled += move;

            var position = (Vector2)transform.position;
            Physics2D.SyncTransforms(); // once per step; the three queries below reuse it
            _probe.Apply(position, radius, AuraInteraction.Burn, AuraId.Fire, syncTransforms: false);
            bool hitTarget = hitbox != null && hitbox.Poll(syncTransforms: false) > 0;
            bool hitWall = _probe.FindSolid(position, radius, syncTransforms: false) != null;
            if (hitTarget || hitWall || _travelled >= Range - Epsilon) Despawn();
        }

        public void Despawn()
        {
            IsFlying = false;
            if (hitbox != null) hitbox.Deactivate();
            gameObject.SetActive(false);
        }
    }
}
