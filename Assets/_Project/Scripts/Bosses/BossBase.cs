using System;
using System.Collections.Generic;
using AuraKnight.Combat;
using AuraKnight.Enemies;
using UnityEngine;

namespace AuraKnight.Bosses
{
    public enum BossState { Dormant, Idle, Attacking, Transition, Dead }

    /// <summary>
    /// Shared boss body: <see cref="Health"/>, a child Hurtbox (Enemy layer), a child contact Hitbox (EnemyAttack layer), phases with weighted
    /// attack pools, and the Idle / Attacking / Transition / Dead loop. A boss sleeps (<see cref="BossState.Dormant"/>, untouchable) until
    /// <see cref="BossArena"/> engages it. <see cref="ResetBoss"/> restores the fresh state (HP, position, phase, hazards, summons, colliders).
    /// The Hitbox and Hurtbox are on separate child objects of different teams. Subclasses add only setup and boss-specific parts.
    /// Execution order -10 so the reset runs before attack components on the same prefab.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(Health))]
    public abstract partial class BossBase : MonoBehaviour
    {
        [SerializeField] BossStats stats;
        [SerializeField] Hurtbox hurtbox;
        [SerializeField] Hitbox contactHitbox;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Animator animator;
        [Tooltip("Placeholder sprite and material for hazards and warning markers.")]
        [SerializeField] Sprite markerSprite;
        [SerializeField] Material markerMaterial;
        [SerializeField] int initialFacing = -1;
        [SerializeField] List<BossPhase> phases = new List<BossPhase>();

        Health _health;
        Transform _target;
        Transform _homeParent;
        Vector3 _homeLocal;
        BossArena _arena;
        BossAttack _current;
        float[] _weights = Array.Empty<float>();
        float[] _fractions = Array.Empty<float>();
        float _think;
        float _transitionLeft;
        int _lastPick = -1;
        bool _cached;
        bool _present = true;

        public BossStats Stats => stats;
        public Health Health => _health;
        public BossState State { get; private set; } = BossState.Dormant;
        public int PhaseIndex { get; private set; }
        /// <summary>Leo: the engaged target, or the object tagged Player while the boss sleeps (attacks driven by tests or the debug tool).</summary>
        public Transform Target
        {
            get
            {
                if (_target == null) _target = GameObject.FindGameObjectWithTag(World.WorldTags.Player)?.transform;
                return _target;
            }
            private set => _target = value;
        }
        public int Facing { get; private set; } = -1;
        public bool IsAlive => State != BossState.Dead && _present;
        /// <summary>False after the boss was beaten in a loaded save: it is invisible and untouchable.</summary>
        public bool IsPresent => _present;
        public Hitbox ContactHitbox => contactHitbox;
        public Hurtbox Hurtbox => hurtbox;
        public Sprite MarkerSprite => markerSprite;
        public Material MarkerMaterial => markerMaterial;
        public IReadOnlyList<BossPhase> Phases => phases;
        public BossAttack CurrentAttack => _current;
        public BossPhase Phase => phases[Mathf.Clamp(PhaseIndex, 0, phases.Count - 1)];
        /// <summary>Phase speed (1, then 1.25 in phase 2): shortens think, execute and recover times and speeds up the animator.</summary>
        public float Speed => phases.Count == 0 ? 1f : Phase.speed;
        /// <summary>Random source for attack picks; tests inject a fake.</summary>
        public IRandomSource Random { get; set; } = UnityRandomSource.Instance;
        public Vector2 HomePosition => _homeParent != null ? (Vector2)_homeParent.TransformPoint(_homeLocal) : (Vector2)_homeLocal;
        public float FloorY => HomePosition.y - stats.footOffset;
        public Transform SpawnRoot => transform.parent;
        /// <summary>The room controller that engages and resets this boss (a sibling object, it binds itself in its Awake).</summary>
        public BossArena Arena => _arena;
        public Bounds Playfield => _arena != null ? _arena.Playfield : new Bounds(HomePosition + Vector2.up * 5f, new Vector3(28f, 14f, 1f));

        public event Action<BossBase> Defeated;
        public event Action<int> PhaseChanged;

        protected virtual void OnReset() { }
        protected virtual void OnEngaged() { }
        protected virtual void OnPhaseEntered(int phaseIndex) { }
        protected virtual void OnIdle(float deltaTime) { }
        protected virtual void OnDeath() { }

        void Awake()
        {
            Cache();
            string problem = Validate();
            if (problem == null) return;
            Debug.LogError($"{name}: {problem}; the boss stays off.", this);
            enabled = false;
        }

        internal void BindArena(BossArena arena) => _arena = arena;

        void Cache()
        {
            if (_cached) return;
            _cached = true;
            _health = GetComponent<Health>();
            _homeParent = transform.parent;
            _homeLocal = transform.localPosition;
            if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>(true);
            if (contactHitbox == null) contactHitbox = GetComponentInChildren<Hitbox>(true);
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>(true);
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            _fractions = new float[phases.Count];
            for (int i = 0; i < phases.Count; i++) _fractions[i] = phases[i].enterAtHpFraction;
            CacheVisuals();
        }

        string Validate()
        {
            if (stats == null || hurtbox == null || contactHitbox == null) return "BossStats, Hurtbox and contact Hitbox must be assigned";
            if (phases.Count == 0) return "at least one phase is required";
            foreach (var phase in phases)
                if (phase.attacks.Count == 0) return $"phase '{phase.name}' has no attacks";
            return null;
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Changed += OnHealthChanged;
            _health.Died += OnHealthDied;
            ResetBoss();
        }

        void OnDisable()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Changed -= OnHealthChanged;
            _health.Died -= OnHealthDied;
            CancelAttack();
            ClearSpawned();
        }

        /// <summary>Back to a fresh, dormant boss: full HP, home position, phase 0, no hazards or summons, hurt/hit boxes off.</summary>
        public void ResetBoss()
        {
            Cache();
            CancelAttack();
            ClearSpawned();
            State = BossState.Dormant;
            PhaseIndex = 0;
            Target = null;
            _lastPick = -1;
            _think = 0f;
            _present = !BossProgress.IsDefeated(stats.bossId);
            SetPosition(HomePosition);
            _health.Initialize(stats.maxHp);
            hurtbox.gameObject.SetActive(false);
            contactHitbox.gameObject.SetActive(false);
            Face(initialFacing);
            ResetVisuals();
            OnReset();
        }

        /// <summary>Starts the fight against <paramref name="target"/>. Returns false when the boss is absent, dead or already fighting.</summary>
        public bool Engage(Transform target)
        {
            if (!_present || State != BossState.Dormant || !isActiveAndEnabled) return false;
            Target = target;
            State = BossState.Idle;
            _think = stats.introSeconds;
            hurtbox.gameObject.SetActive(true);
            SetContactDamage(stats.contactDamage);
            contactHitbox.gameObject.SetActive(true);
            FaceTarget();
            ApplyAnimatorSpeed();
            PublishHealth();
            OnEngaged();
            return true;
        }

        /// <summary>Faces the target (the art faces right; left flips the sprite).</summary>
        public void FaceTarget()
        {
            if (Target != null) Face(Target.position.x >= transform.position.x ? 1 : -1);
        }

        public void Face(int direction)
        {
            Facing = direction >= 0 ? 1 : -1;
            if (sprite != null) sprite.flipX = Facing < 0;
        }

        /// <summary>Moves the boss root (kinematic body: attacks drive the position directly).</summary>
        public void SetPosition(Vector2 world) => transform.position = new Vector3(world.x, world.y, transform.position.z);

        /// <summary>Sets the contact damage and re-arms the contact hitbox (the dropping spider hits for 2).</summary>
        public void SetContactDamage(int amount)
        {
            contactHitbox.Damage = amount;
            if (contactHitbox.gameObject.activeSelf) contactHitbox.Activate(amount);
        }
    }
}
