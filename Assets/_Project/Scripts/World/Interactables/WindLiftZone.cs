using System;
using AuraKnight.Aura;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Passage that is open only while Leo stands inside the trigger with the Wind Aura active (GDD section 7.2).
    /// Presence-driven, not skill-driven, so it never reacts to <see cref="IAuraInteractable"/> offers.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WindLiftZone : MonoBehaviour
    {
        [SerializeField] GateSwitches switches = new GateSwitches();

        bool _playerInside, _applied;

        public AuraId RequiredAura => AuraId.Wind;
        public AuraInteraction Interaction => AuraInteraction.WindLift;
        public bool IsOpen { get; private set; }

        /// <summary>Raised with the new open state.</summary>
        public event Action<bool> StateChanged;

        void Reset()
        {
            if (TryGetComponent<Collider2D>(out var trigger)) trigger.isTrigger = true;
        }

        void Start() => Reevaluate();

        void OnEnable() => EventBus.Subscribe<AuraChanged>(OnAuraChanged);

        void OnDisable() => EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) SetPlayerInside(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) SetPlayerInside(false);
        }

        void OnAuraChanged(AuraChanged evt) => Reevaluate();

        internal void SetPlayerInside(bool inside)
        {
            _playerInside = inside;
            Reevaluate();
        }

        void Reevaluate()
        {
            var manager = AuraManager.Instance;
            var current = manager != null ? manager.Current : AuraId.None;
            bool open = _playerInside && AuraInteractionRules.Accepts(RequiredAura, Interaction, Interaction, current);
            if (IsOpen == open && _applied) return;
            IsOpen = open;
            _applied = true;
            switches.Apply(open);
            StateChanged?.Invoke(open);
        }
    }
}
