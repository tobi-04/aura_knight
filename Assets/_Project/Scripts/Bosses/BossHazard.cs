using System;
using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Everything a boss hazard needs. Create with an object initialiser and pass to <see cref="BossHazard.Spawn"/>.</summary>
    public struct HazardSpec
    {
        public Vector2 Position;
        public Vector2 Size;
        public int Damage;
        /// <summary>Seconds shown as a faint marker before the hitbox arms.</summary>
        public float Telegraph;
        /// <summary>Seconds the armed hazard lives.</summary>
        public float Lifetime;
        public Vector2 Velocity;
        /// <summary>Downward acceleration (0 = straight line).</summary>
        public float Gravity;
        public Color Color;
        public bool DestroyOnGround;
        /// <summary>No hitbox at all: a pure warning marker (dust, laser line, shadow).</summary>
        public bool Harmless;
        /// <summary>The telegraph marker is this fraction of the height, standing on the hazard's base (dust before a spike).</summary>
        public float MarkerHeightFactor;
        /// <summary>Hit again after this many seconds while overlapping (steam); 0 = once per target.</summary>
        public float RearmSeconds;
    }

    /// <summary>
    /// A runtime-built boss hazard or projectile: faint marker during the telegraph, then an armed <see cref="Hitbox"/> on the EnemyAttack
    /// layer that moves, optionally dies on Ground and expires. Tracked by its boss so a reset removes it.
    /// </summary>
    public sealed class BossHazard : MonoBehaviour
    {
        const float MarkerAlpha = 0.35f, HiddenAlpha = 0.07f;

        Hitbox _hitbox;
        SpriteRenderer _renderer;
        HazardSpec _spec;
        Vector2 _velocity;
        float _age;
        bool _armed;

        public event Action<HitReport> Hit;
        /// <summary>While this returns true the hitbox is off (steam versus a heat-immune Leo).</summary>
        public Func<bool> Suppressed { get; set; }
        /// <summary>When set and false the hazard is drawn almost invisible (Malakor's fire strike is only seen by Fire's light).</summary>
        public Func<bool> Revealed { get; set; }
        public bool IsArmed => _armed;
        public HazardSpec Spec => _spec;

        public static BossHazard Spawn(BossBase boss, in HazardSpec spec)
        {
            var go = new GameObject("BossHazard");
            go.transform.SetParent(boss.SpawnRoot, false);
            go.transform.position = new Vector3(spec.Position.x, spec.Position.y, 0f);
            var hazard = go.AddComponent<BossHazard>();
            hazard.Build(boss, spec);
            boss.Track(go);
            return hazard;
        }

        void Build(BossBase boss, in HazardSpec spec)
        {
            _spec = spec;
            _velocity = spec.Velocity;
            var visual = new GameObject("Marker");
            visual.transform.SetParent(transform, false);
            _renderer = visual.AddComponent<SpriteRenderer>();
            _renderer.sprite = boss.MarkerSprite;
            _renderer.sharedMaterial = boss.MarkerMaterial;
            _renderer.sortingOrder = 6;
            if (!spec.Harmless)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = spec.Size;
                _hitbox = gameObject.AddComponent<Hitbox>();
                _hitbox.Team = Team.Enemy;
                _hitbox.Source = boss.transform;
                _hitbox.RearmInterval = spec.RearmSeconds;
                _hitbox.Hit += report => Hit?.Invoke(report);
            }
            ApplyVisual();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _age += dt;
            if (!_armed && _age >= _spec.Telegraph) Arm();
            if (_armed)
            {
                Move(dt);
                UpdateSuppression();
                if (Revealed != null) ApplyVisual();
            }
            if (_age >= _spec.Telegraph + _spec.Lifetime) Destroy(gameObject);
        }

        void Arm()
        {
            _armed = true;
            if (_hitbox != null) _hitbox.Activate(_spec.Damage);
            ApplyVisual();
        }

        void Move(float dt)
        {
            if (_velocity == Vector2.zero && _spec.Gravity == 0f) return;
            Vector2 position = transform.position;
            BossMath.Step(ref position, ref _velocity, _spec.Gravity, dt);
            transform.position = new Vector3(position.x, position.y, 0f);
            if (!_spec.DestroyOnGround) return;
            if (Physics2D.OverlapBox(position, _spec.Size * 0.8f, 0f, PhysicsLayers.GroundMask) != null) Destroy(gameObject);
        }

        void UpdateSuppression()
        {
            if (_hitbox == null || Suppressed == null) return;
            bool off = Suppressed();
            if (off && _hitbox.IsActive) _hitbox.Deactivate();
            else if (!off && !_hitbox.IsActive) _hitbox.Activate(_spec.Damage);
        }

        void ApplyVisual()
        {
            var tr = _renderer.transform;
            float height = _spec.Size.y;
            bool full = _armed && !_spec.Harmless;
            float factor = full || _spec.MarkerHeightFactor <= 0f ? 1f : _spec.MarkerHeightFactor;
            tr.localScale = new Vector3(_spec.Size.x, height * factor, 1f);
            tr.localPosition = new Vector3(0f, -(1f - factor) * height * 0.5f, 0f);
            var color = _spec.Color;
            if (!full) color.a *= MarkerAlpha;
            else if (Revealed != null && !Revealed()) color.a *= HiddenAlpha;
            _renderer.color = color;
        }
    }
}
