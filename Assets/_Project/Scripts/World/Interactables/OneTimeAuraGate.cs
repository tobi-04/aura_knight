using System;
using AuraKnight.Aura;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Base of the gates a skill opens for good (Burnable, Extinguishable). Shared here: matching the skill, applying the
    /// <see cref="GateSwitches"/> and persisting the open state in GameState.openedGates by <see cref="PersistentId"/>
    /// (restored on Start and whenever a save is loaded). Needs a trigger collider on the root; solid blockers are
    /// children listed in the switches so skills hitting them reach this component.
    /// </summary>
    public abstract class OneTimeAuraGate : MonoBehaviour, IAuraInteractable
    {
        [SerializeField] GateSwitches switches = new GateSwitches();

        public abstract AuraId RequiredAura { get; }
        public abstract AuraInteraction Interaction { get; }
        public bool IsOpen { get; private set; }

        /// <summary>Raised with the new open state.</summary>
        public event Action<bool> StateChanged;

        void Reset()
        {
            if (TryGetComponent<Collider2D>(out var trigger)) trigger.isTrigger = true;
        }

        void Start() => RestoreSaved();

        void OnEnable() => EventBus.Subscribe<GameStateLoaded>(OnGameStateLoaded);

        void OnDisable() => EventBus.Unsubscribe<GameStateLoaded>(OnGameStateLoaded);

        void OnGameStateLoaded(GameStateLoaded evt) => RestoreSaved();

        /// <summary>Skill-driven opening. Wrong Aura or interaction is rejected.</summary>
        public bool TryInteract(AuraInteraction offered, AuraId source)
        {
            if (!AuraInteractionRules.Accepts(RequiredAura, Interaction, offered, source)) return false;
            if (IsOpen) return true;
            Open();
            var gm = GameManager.Instance;
            if (gm != null) gm.State.MarkGateOpened(SavedId);
            return true;
        }

        void RestoreSaved()
        {
            var gm = GameManager.Instance;
            if (!IsOpen && gm != null && gm.State.HasOpenedGate(SavedId)) Open();
        }

        void Open()
        {
            IsOpen = true;
            switches.Apply(true);
            StateChanged?.Invoke(true);
        }

        string SavedId => TryGetComponent<PersistentId>(out var id) ? id.Id : null;
    }
}
