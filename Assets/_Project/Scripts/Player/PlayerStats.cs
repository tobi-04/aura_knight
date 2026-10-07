using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Leo's hearts, energy and sword level. Hearts live in <see cref="Health"/>; this component seeds them
    /// from the save, publishes HeartsChanged / EnergyChanged / PlayerDamaged / PlayerDied on the EventBus and
    /// refills on altar checkpoints. Call <see cref="Initialize"/> or <see cref="RefreshFromGameState"/> again after shop upgrades.
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] Health health;

        bool _initialized, _bound;

        public Health Health { get { ResolveHealth(); return health; } }
        public EnergyPool Energy { get; } = new EnergyPool(PlayerStatsSeed.Defaults.MaxEnergy);
        /// <summary>Sword damage per hit (1-3).</summary>
        public int SwordLevel { get; private set; } = 1;

        void Awake() => RefreshFromGameState();

        void OnEnable()
        {
            if (_initialized) Bind();
        }

        void OnDisable() => Unbind();

        void OnDestroy() => Unbind();

        public void RefreshFromGameState()
        {
            var manager = GameManager.Instance;
            Initialize(PlayerStatsSeed.From(manager != null ? manager.State : null));
        }

        /// <summary>Applies the seed, restores everything to full and announces the values.</summary>
        public void Initialize(PlayerStatsSeed seed)
        {
            ResolveHealth();
            Unbind();
            health.Initialize(seed.MaxHearts);
            Energy.SetMax(seed.MaxEnergy, true);
            SwordLevel = seed.SwordLevel;
            _initialized = true;
            Bind();
            EventBus.Publish(new HeartsChanged(health.Current, health.Max));
            EventBus.Publish(new EnergyChanged(Energy.Current, Energy.Max));
        }

        /// <summary>
        /// After a shop purchase or free chest upgrade: takes the new maxima and sword level from the save but keeps the current
        /// hearts/energy, adding what the upgrade gained (a new heart arrives full). Does nothing while dead.
        /// </summary>
        public void ApplyUpgrades()
        {
            ResolveHealth();
            var manager = GameManager.Instance;
            var seed = PlayerStatsSeed.From(manager != null ? manager.State : null);
            SwordLevel = seed.SwordLevel;
            if (health.IsDead) return;
            int heartsGained = Mathf.Max(0, seed.MaxHearts - health.Max);
            health.Initialize(seed.MaxHearts, health.Current + heartsGained);
            float energyGained = Mathf.Max(0f, seed.MaxEnergy - Energy.Max);
            Energy.SetMax(seed.MaxEnergy, false);
            Energy.Add(energyGained);
        }

        /// <summary>Spends energy for an Aura skill; false (nothing spent) when there is not enough.</summary>
        public bool TrySpendEnergy(float amount) => Energy.TrySpend(amount);

        public float AddEnergy(float amount) => Energy.Add(amount);

        /// <summary>+8 energy for a landed sword hit.</summary>
        public void GainEnergyFromHit() => Energy.Add(SwordTiming.EnergyPerHit);

        /// <summary>Heals hearts; returns how many were restored.</summary>
        public int Heal(int hearts) => Health.Heal(hearts);

        /// <summary>Full hearts and energy (also revives).</summary>
        public void RestoreAll()
        {
            Health.Refill();
            Energy.Refill();
        }

        public void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            EventBus.Unsubscribe<CheckpointReached>(OnCheckpointReached);
            EventBus.Unsubscribe<GameStateLoaded>(OnGameStateLoaded);
            if (health == null) return;
            health.Changed -= OnHeartsChanged;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            Energy.Changed -= OnEnergyChanged;
        }

        void Bind()
        {
            if (_bound) return;
            _bound = true;
            health.Changed += OnHeartsChanged;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            Energy.Changed += OnEnergyChanged;
            EventBus.Subscribe<CheckpointReached>(OnCheckpointReached);
            EventBus.Subscribe<GameStateLoaded>(OnGameStateLoaded);
        }

        void ResolveHealth()
        {
            if (health == null) health = GetComponent<Health>();
            if (health == null) throw new System.InvalidOperationException($"{nameof(PlayerStats)} on '{name}' needs a {nameof(Health)} component.");
        }

        void OnHeartsChanged(int current, int max) => EventBus.Publish(new HeartsChanged(current, max));
        void OnDamaged(DamageInfo info, int applied) => EventBus.Publish(new PlayerDamaged(applied, health.Current));
        void OnDied(DamageInfo info) => EventBus.Publish(new PlayerDied());
        void OnEnergyChanged(float current, float max) => EventBus.Publish(new EnergyChanged(current, max));

        /// <summary>New game / continue replaced the state: take hearts, energy and sword level from it again.</summary>
        void OnGameStateLoaded(GameStateLoaded evt) => RefreshFromGameState();

        void OnCheckpointReached(CheckpointReached evt)
        {
            if (!health.IsDead) RestoreAll();
        }
    }
}
