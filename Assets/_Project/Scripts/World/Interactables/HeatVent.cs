using System;
using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Pulsing steam hazard (2 s on / 2 s off, GDD section 7.2) that is harmless once the current Aura grants heat
    /// immunity (Fire). <see cref="IsOpen"/> means "safe because of immunity"; the switches toggle with it.
    /// </summary>
    public sealed class HeatVent : MonoBehaviour
    {
        [SerializeField] GateSwitches switches = new GateSwitches();
        [Tooltip("The damaging hitbox, pulsed on and off.")]
        [SerializeField] Hitbox ventHazard;

        readonly VentCycle _vent = new VentCycle();
        bool _applied;

        public AuraId RequiredAura => AuraId.Fire;
        public AuraInteraction Interaction => AuraInteraction.HeatVent;
        public bool IsOpen { get; private set; }

        /// <summary>Raised with the new immune state.</summary>
        public event Action<bool> StateChanged;

        void Start() => Evaluate();

        void OnEnable() => EventBus.Subscribe<AuraChanged>(OnAuraChanged);

        void OnDisable() => EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);

        void FixedUpdate()
        {
            _vent.Tick(Time.fixedDeltaTime);
            Evaluate();
        }

        void OnAuraChanged(AuraChanged evt) => Evaluate();

        void Evaluate()
        {
            bool immune = AuraManager.CurrentPassives.heatImmune;
            SetImmune(immune);
            if (ventHazard == null) return;
            bool dangerous = _vent.IsOn && !immune;
            if (dangerous && !ventHazard.IsActive) ventHazard.Activate(ventHazard.Damage);
            else if (!dangerous && ventHazard.IsActive) ventHazard.Deactivate();
        }

        void SetImmune(bool immune)
        {
            if (IsOpen == immune && _applied) return;
            IsOpen = immune;
            _applied = true;
            switches.Apply(immune);
            StateChanged?.Invoke(immune);
        }
    }
}
