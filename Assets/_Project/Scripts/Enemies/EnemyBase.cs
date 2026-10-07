using System;
using AuraKnight.Combat;
using AuraKnight.World.Pickups;
using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Shared enemy body: <see cref="Health"/>, a child <see cref="Hurtbox"/> (Enemy layer) and a child contact
    /// <see cref="Hitbox"/> (EnemyAttack layer), stats, the Patrol/Detect/Attack/Cooldown/Hurt/Dead loop and drops.
    /// Archetypes override only the per-state tick methods. Execution order -10 so this runs before modifiers on the same object.
    /// Reset contract: <see cref="OnEnable"/> fully re-initialises (HP, position, state, colliders, hitboxes), so toggling the
    /// room's enemies container (Room.Restart / re-entering a room) revives everything. A dead enemy stays active but inert.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public abstract partial class EnemyBase : MonoBehaviour
    {
        /// <summary>Falling farther than this below the spawn point sends the enemy home (never lost off the map).</summary>
        public const float FallResetDepth = 30f;

        [SerializeField] EnemyStats stats;
        [Tooltip("Solid collider on the root (collides with Ground). Null for enemies that move on authored paths.")]
        [SerializeField] Collider2D body;
        [SerializeField] Hurtbox hurtbox;
        [SerializeField] Hitbox contactHitbox;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] CoinPickup coinPrefab;
        [SerializeField] LightDropPickup lightDropPrefab;
        [SerializeField] int initialFacing = 1;

        readonly EnemyStateMachine _machine = new EnemyStateMachine();
        Health _health;
        Rigidbody2D _rb;
        Transform _spawnParent;
        Vector3 _spawnLocal;
        bool _solidBody = true;
        bool _cached;

        public EnemyStats Stats => stats;
        public EnemyState State => _machine.Current;
        public bool IsAlive => !_machine.IsDead;
        /// <summary>+1 facing right, -1 facing left (drives the sprite flip and the front shield).</summary>
        public int Facing { get; private set; } = 1;
        public Health Health => _health;
        public Hitbox ContactHitbox => contactHitbox;
        public Hurtbox Hurtbox => hurtbox;
        public Vector2 SpawnPosition => _spawnParent != null ? (Vector2)_spawnParent.TransformPoint(_spawnLocal) : (Vector2)_spawnLocal;
        /// <summary>Random source for drops; tests inject a fake.</summary>
        public IRandomSource Random { get; set; } = UnityRandomSource.Instance;

        /// <summary>(from, to) on every state change, including resets.</summary>
        public event Action<EnemyState, EnemyState> StateChanged { add => _machine.Changed += value; remove => _machine.Changed -= value; }
        /// <summary>Raised once per death after drops were spawned.</summary>
        public event Action<EnemyBase> Died;

        /// <summary>False lets a modifier (Ghost) ignore Ground: the root collider stays off.</summary>
        public bool SolidBody
        {
            get => _solidBody;
            set
            {
                _solidBody = value;
                RefreshBody();
            }
        }

        protected EnemyStateMachine Machine => _machine;
        protected Rigidbody2D Rb => _rb;
        protected Collider2D Body => body;
        protected int InitialFacing => initialFacing >= 0 ? 1 : -1;
        /// <summary>Hurt stuns by default; paths that cannot be knocked off (crawlers) only flash.</summary>
        protected virtual bool Interruptible => true;

        protected virtual void OnReset() { }
        protected virtual void OnDeath() { }
        protected virtual void OnHurtEnded() { }
        protected abstract void OnPatrol(float deltaTime);
        protected virtual void OnDetect(float deltaTime) { }
        protected virtual void OnAttack(float deltaTime) { }
        protected virtual void OnCooldown(float deltaTime) { }

        void Awake()
        {
            EnsureCached();
            if (stats != null && hurtbox != null && contactHitbox != null) return;
            Debug.LogError($"{name}: EnemyStats, Hurtbox and contact Hitbox must be assigned; the enemy stays off.", this);
            enabled = false;
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnHealthDied;
            ResetEnemy();
        }

        void OnDisable()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnHealthDied;
        }

        /// <summary>Moves the spawn point (and the enemy) to a world position; used when placing an instance from code.</summary>
        public void SetSpawnPoint(Vector2 world)
        {
            EnsureCached();
            _spawnLocal = _spawnParent != null ? _spawnParent.InverseTransformPoint(world) : (Vector3)world;
            ResetEnemy();
        }

        /// <summary>Full reset to a fresh spawn (HP, position, state, colliders, contact hitbox). Safe to call any time.</summary>
        public void ResetEnemy()
        {
            EnsureCached();
            _health.Initialize(stats.maxHp);
            _machine.Reset(EnemyState.Patrol);
            var home = SpawnPosition;
            transform.position = new Vector3(home.x, home.y, transform.position.z);
            _rb.simulated = true;
            _rb.position = home;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            Face(InitialFacing);
            ResetVisuals();
            hurtbox.gameObject.SetActive(true);
            contactHitbox.Damage = stats.contactDamage;
            contactHitbox.gameObject.SetActive(false); // off/on clears the hit set and re-arms the contact damage
            contactHitbox.gameObject.SetActive(true);
            RefreshBody();
            ResetSensing();
            OnReset();
        }

        void FixedUpdate()
        {
            if (_machine.IsDead) return;
            float dt = Time.fixedDeltaTime;
            _machine.Tick(dt);
            RefreshTarget(dt);
            if (HasFallen()) { ResetEnemy(); return; }
            switch (_machine.Current)
            {
                case EnemyState.Patrol: OnPatrol(dt); break;
                case EnemyState.Detect: OnDetect(dt); break;
                case EnemyState.Attack: OnAttack(dt); break;
                case EnemyState.Cooldown: OnCooldown(dt); break;
                case EnemyState.Hurt: TickHurt(); break;
            }
        }

        bool HasFallen() => transform.position.y < SpawnPosition.y - FallResetDepth;

        void EnsureCached()
        {
            if (_cached) return;
            _cached = true;
            _health = GetComponent<Health>();
            _rb = GetComponent<Rigidbody2D>();
            _spawnParent = transform.parent;
            _spawnLocal = transform.localPosition;
            if (body == null) body = GetComponent<Collider2D>();
            if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>(true);
            if (contactHitbox == null) contactHitbox = GetComponentInChildren<Hitbox>(true);
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>(true);
            if (sprite != null) _baseColor = sprite.color;
        }

        void RefreshBody()
        {
            if (body != null) body.enabled = _solidBody && !_machine.IsDead;
        }

        /// <summary>Changes the facing and mirrors the sprite.</summary>
        protected void Face(int direction)
        {
            Facing = direction >= 0 ? 1 : -1;
            if (sprite != null) sprite.flipX = Facing < 0;
        }

        protected bool Enter(EnemyState state) => _machine.TryEnter(state);
    }
}
