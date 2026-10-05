using System;
using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>One target reached by a <see cref="Hitbox"/>.</summary>
    public readonly struct HitReport
    {
        public readonly Hurtbox Target;
        public readonly HitOutcome Outcome;
        public readonly DamageInfo Info;

        public HitReport(Hurtbox target, HitOutcome outcome, DamageInfo info)
        {
            Target = target;
            Outcome = outcome;
            Info = info;
        }
    }

    /// <summary>
    /// Damage dealer. Owners switch it on with <see cref="Activate"/> for the active frames of an attack;
    /// every overlapping enemy-team <see cref="Hurtbox"/> is hit once per activation. Overlap is polled
    /// (not callback-driven) so timing is deterministic and unit-testable. Needs a trigger collider.
    /// Contact hazards use <c>activeOnEnable</c> plus <see cref="RearmInterval"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] Team team = Team.Enemy;
        [SerializeField, Min(0)] int damage = 1;
        [SerializeField, Min(0f)] float knockbackTiles = Knockback.DefaultTiles;
        [Tooltip("Attacker root reported as DamageInfo.Source; defaults to this transform's root.")]
        [SerializeField] Transform source;
        [SerializeField] bool activeOnEnable;
        [Tooltip("When above 0 an active hitbox may hit the same target again after this many seconds (contact hazards).")]
        [SerializeField, Min(0f)] float rearmInterval;

        readonly HitRegistry<Hurtbox> _registry = new HitRegistry<Hurtbox>();
        readonly List<Collider2D> _results = new List<Collider2D>(16);
        ContactFilter2D _filter = new ContactFilter2D { useTriggers = true };
        Collider2D _collider;
        int _amount;
        Vector2 _direction;
        float _sinceRearm;

        public event Action<HitReport> Hit;

        public bool IsActive { get; private set; }
        /// <summary>When true the hitbox polls itself every physics step; owners that poll in their own tick turn it off.</summary>
        public bool AutoPoll { get; set; } = true;
        /// <summary>Changing the team also moves the object to that team's attack layer and re-aims the overlap query.</summary>
        public Team Team
        {
            get => team;
            set
            {
                team = value;
                ApplyLayer();
            }
        }
        /// <summary>Attacker root reported as DamageInfo.Source (e.g. the player for a fireball); null = this transform's root.</summary>
        public Transform Source { get => source; set => source = value; }
        public int Damage { get => damage; set => damage = Mathf.Max(0, value); }
        public float RearmInterval { get => rearmInterval; set => rearmInterval = Mathf.Max(0f, value); }

        void Awake() => ApplyLayer();

        /// <summary>Puts this object on its team's attack layer and limits the overlap query to the layers that team can hurt.</summary>
        public void ApplyLayer()
        {
            PhysicsLayers.Apply(gameObject, HitMasks.HitboxLayer(team));
            int mask = HitMasks.TargetMask(team);
            _filter = new ContactFilter2D { useTriggers = true };
            if (mask != 0) _filter.SetLayerMask(mask); // an undefined layer set would otherwise hit nothing; fall back to every layer
        }

        void OnEnable()
        {
            if (activeOnEnable) Activate(damage);
        }

        void OnDisable() => Deactivate();

        void FixedUpdate()
        {
            if (!IsActive || !AutoPoll) return;
            AdvanceTime(Time.fixedDeltaTime);
            Poll();
        }

        /// <summary>Arms the hitbox (re-arming clears who was already hit). A zero <paramref name="direction"/> means "away from this hitbox".</summary>
        public void Activate(int amount, Vector2 direction = default)
        {
            _amount = amount;
            _direction = direction;
            _registry.Clear();
            _sinceRearm = 0f;
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
            _registry.Clear();
        }

        /// <summary>Advances the re-arm timer of a continuous hitbox.</summary>
        public void AdvanceTime(float deltaTime)
        {
            if (!IsActive || rearmInterval <= 0f) return;
            _sinceRearm += deltaTime;
            if (_sinceRearm < rearmInterval) return;
            _sinceRearm = 0f;
            _registry.Clear();
        }

        /// <summary>Resizes the collider when it is a box; returns false for other shapes.</summary>
        public bool TrySetBox(Vector2 offset, Vector2 size)
        {
            if (!(Collider is BoxCollider2D box)) return false;
            box.offset = offset;
            box.size = size;
            return true;
        }

        /// <summary>Hits every not-yet-hit overlapping hurtbox. Returns how many were newly reached.</summary>
        public int Poll(bool syncTransforms = true)
        {
            if (!IsActive) return 0;
            if (syncTransforms) Physics2D.SyncTransforms();
            Collider.Overlap(_filter, _results);
            int reached = 0;
            for (int i = 0; i < _results.Count; i++)
            {
                var other = _results[i];
                if (other == null || !other.TryGetComponent<Hurtbox>(out var hurtbox)) continue;
                if (!DamageRules.CanDamage(team, hurtbox.Team) || !_registry.TryRegister(hurtbox)) continue;
                reached++;
                var info = BuildInfo(hurtbox);
                var outcome = hurtbox.Receive(info); // must run even when nobody listens to Hit
                Hit?.Invoke(new HitReport(hurtbox, outcome, info));
            }
            return reached;
        }

        Collider2D Collider
        {
            get
            {
                if (_collider == null) _collider = GetComponent<Collider2D>();
                return _collider;
            }
        }

        DamageInfo BuildInfo(Hurtbox target)
        {
            var direction = _direction;
            if (direction == Vector2.zero)
                direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            var owner = source != null ? source : transform.root;
            return new DamageInfo(_amount, team, owner.gameObject, direction, knockbackTiles);
        }
    }
}
