using System;
using System.Collections.Generic;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>
    /// Holds Leo's unlocked Auras and the current one, reads the Aura/skill buttons, pays for skills and
    /// persists to <see cref="GameState"/>. Publishes <see cref="AuraChanged"/> and <see cref="AuraUnlocked"/> on the EventBus.
    /// Other phases call <see cref="Unlock"/> (bosses) and <see cref="TrySwitch"/> (HUD ring).
    /// </summary>
    public sealed class AuraManager : MonoBehaviour
    {
        [SerializeField] AuraDefinition[] definitions = Array.Empty<AuraDefinition>();
        [SerializeField] PlayerController controller;
        [SerializeField] PlayerStats stats;
        [Tooltip("Used only when no GameManager exists (test scenes): Auras unlocked at start.")]
        [SerializeField] AuraId[] debugUnlocked = Array.Empty<AuraId>();

        readonly AuraDefinition[] _byId = new AuraDefinition[AuraIds.All.Length];
        readonly AuraSkillBase[] _skills = new AuraSkillBase[AuraIds.All.Length];
        readonly List<string> _exportBuffer = new List<string>(4);
        AuraState _state = new AuraState();

        public static AuraManager Instance { get; private set; }

        public AuraState State => _state;
        public AuraId Current => _state.Current;
        public AuraDefinition CurrentDefinition => _byId[(int)_state.Current];
        public AuraPassives Passives => CurrentDefinition != null ? CurrentDefinition.Passives : AuraPassives.None;
        public bool IsUnlocked(AuraId id) => _state.IsUnlocked(id);
        public AuraDefinition GetDefinition(AuraId id) => (int)id >= 0 && (int)id < _byId.Length ? _byId[(int)id] : null;

        /// <summary>Passives of the live manager, or none when the scene has no Aura system.</summary>
        public static AuraPassives CurrentPassives => Instance != null ? Instance.Passives : AuraPassives.None;

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
            IndexDefinitions();
        }

        void OnEnable() => EventBus.Subscribe<GameStateLoaded>(OnGameStateLoaded);

        void OnDisable() => EventBus.Unsubscribe<GameStateLoaded>(OnGameStateLoaded);

        void OnGameStateLoaded(GameStateLoaded evt)
        {
            LoadFromGameState();
            Publish();
        }

        void Start()
        {
            if (!enabled) return;
            if (controller == null) controller = GetComponent<PlayerController>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            InstantiateSkills();
            LoadFromGameState();
            Publish();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            _state.Tick(Time.deltaTime);
            var input = controller != null ? controller.Input : null;
            if (input == null || !controller.ControlsEnabled) return;
            if (input.AuraWindPressed) TrySwitch(AuraId.Wind);
            else if (input.AuraFirePressed) TrySwitch(AuraId.Fire);
            else if (input.AuraWaterPressed) TrySwitch(AuraId.Water);
            else if (input.AuraNextPressed) TryCycle(1);
            else if (input.AuraPrevPressed) TryCycle(-1);
            if (input.SkillPressed) TryCastSkill();
        }

        /// <summary>Equips an unlocked Aura. False when locked, already current, inside the 0.3 s cooldown, dead or paused.</summary>
        public bool TrySwitch(AuraId id) => CanSwitch && AfterSwitch(_state.TrySwitch(id));

        /// <summary>Next (+1) or previous (-1) unlocked Aura.</summary>
        public bool TryCycle(int direction) => CanSwitch && AfterSwitch(_state.TryCycle(direction));

        bool CanSwitch => controller == null || PlayerActionRules.CanSwitchAura(controller.StateMachine.CurrentId, controller.ControlsEnabled);

        bool CanCast => controller == null || PlayerActionRules.CanCastSkill(controller.StateMachine.CurrentId, controller.ControlsEnabled);

        /// <summary>
        /// Grants an Aura (boss reward) and writes the save immediately. Bosses must call this BEFORE publishing
        /// BossDefeated: that event triggers GameManager's own autosave, which then already contains the reward.
        /// The very first Aura is equipped immediately.
        /// </summary>
        public bool Unlock(AuraId id)
        {
            var result = _state.Unlock(id);
            if (result != UnlockResult.Unlocked && result != UnlockResult.UnlockedAndSwitched) return false;
            SaveToGameState();
            GameManager.Instance?.Save();
            EventBus.Publish(new AuraUnlocked(AuraIds.ToKey(id)));
            if (result == UnlockResult.UnlockedAndSwitched) EventBus.Publish(new AuraChanged(AuraIds.ToKey(id)));
            return true;
        }

        /// <summary>Casts the current Aura's skill: needs the Aura, a cool skill and enough energy.</summary>
        public CastResult TryCastSkill()
        {
            if (!CanCast) return CastResult.Blocked;
            var definition = CurrentDefinition;
            var skill = _skills[(int)_state.Current];
            if (definition == null || skill == null || stats == null) return CastResult.NoAura;
            if (!skill.CanCast) return CastResult.SkillCooldown;
            var result = _state.TryCast(definition.EnergyCost, definition.SkillCooldown, stats.TrySpendEnergy);
            if (result != CastResult.Cast) return result;
            try { skill.Cast(); }
            catch (Exception e) { Debug.LogException(e, this); }
            return result;
        }

        bool AfterSwitch(SwitchResult result)
        {
            if (result != SwitchResult.Switched) return false;
            SaveToGameState();
            Publish();
            return true;
        }

        void Publish() => EventBus.Publish(new AuraChanged(AuraIds.ToKey(_state.Current)));

        void IndexDefinitions()
        {
            foreach (var definition in definitions)
            {
                if (definition == null) continue;
                _byId[(int)definition.Id] = definition;
            }
        }

        void InstantiateSkills()
        {
            var health = stats != null ? stats.Health : null;
            foreach (var definition in definitions)
            {
                if (definition == null || definition.SkillPrefab == null) continue;
                var skill = Instantiate(definition.SkillPrefab, transform);
                skill.name = definition.SkillPrefab.name;
                skill.Bind(controller, health);
                _skills[(int)definition.Id] = skill;
            }
        }

        void LoadFromGameState()
        {
            var gm = GameManager.Instance;
            _state = new AuraState();
            if (gm != null)
            {
                _state.Load(gm.State.unlockedAuras, gm.State.currentAura);
                return;
            }
            var ids = new List<string>(debugUnlocked.Length);
            foreach (var id in debugUnlocked) ids.Add(AuraIds.ToKey(id));
            _state.Load(ids, ids.Count > 0 ? ids[0] : null);
        }

        void SaveToGameState()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            _state.ExportUnlocked(_exportBuffer);
            gm.State.unlockedAuras.Clear();
            gm.State.unlockedAuras.AddRange(_exportBuffer);
            gm.State.currentAura = AuraIds.ToKey(_state.Current);
        }
    }
}
