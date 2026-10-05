using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Aura.Skills
{
    /// <summary>
    /// Whirlwind: a 2.5-tile ring around Leo that deals 1 damage and pushes enemies away (GDD section 6).
    /// The damage is a normal <see cref="Hitbox"/> armed for a short burst.
    /// </summary>
    public sealed class WindGustSkill : AuraSkillBase
    {
        public const float Radius = 2.5f;
        public const int Damage = 1;

        [SerializeField] Hitbox hitbox;
        [SerializeField, Min(0.02f)] float activeSeconds = 0.15f;

        float _remaining;

        public override AuraId Aura => AuraId.Wind;
        public bool IsActive => hitbox != null && hitbox.IsActive;

        protected override void OnBound() => Setup(hitbox);

        internal void Setup(Hitbox gustHitbox)
        {
            hitbox = gustHitbox;
            if (hitbox == null) return;
            hitbox.Team = Team.Player;
            hitbox.Damage = Damage;
            if (hitbox.TryGetComponent<CircleCollider2D>(out var circle)) circle.radius = Radius;
            hitbox.Deactivate();
        }

        public override void Cast()
        {
            if (hitbox == null) return;
            hitbox.transform.position = new Vector3(Origin.x, Origin.y, hitbox.transform.position.z);
            hitbox.Activate(Damage);
            _remaining = activeSeconds;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        internal void Tick(float deltaTime)
        {
            if (_remaining <= 0f) return;
            _remaining -= deltaTime;
            if (_remaining > 0f) return;
            _remaining = 0f;
            if (hitbox != null) hitbox.Deactivate();
        }
    }
}
