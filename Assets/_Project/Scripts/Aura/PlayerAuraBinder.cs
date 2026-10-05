using System;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.Player.States;
using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>
    /// Writes the active Aura's passives into the <see cref="PlayerController"/> hooks (double jump, glide, speed, jump,
    /// swim) and tracks how many water volumes Leo stands in. Registers the Swim state. Exposes heat/acid immunity for hazards.
    /// </summary>
    public sealed class PlayerAuraBinder : MonoBehaviour
    {
        [SerializeField] PlayerController controller;

        int _waterVolumes;
        AuraPassives _passives = AuraPassives.None;
        bool _bound;

        public PlayerModifiers Modifiers { get; private set; } = AuraPassiveResolver.Resolve(AuraPassives.None, false);
        public bool InWater => _waterVolumes > 0;
        public bool HeatImmune => Modifiers.HeatImmune;
        public bool AcidImmune => Modifiers.AcidImmune;

        /// <summary>Raised after the modifiers were recomputed and written to the controller.</summary>
        public event Action<PlayerModifiers> ModifiersChanged;

        void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError($"{nameof(PlayerAuraBinder)} on '{name}' needs a {nameof(PlayerController)}.", this);
                enabled = false;
            }
        }

        /// <summary>Test seam: wires the controller without running Awake.</summary>
        internal void Bind(PlayerController playerController) => controller = playerController;

        void Start()
        {
            if (!enabled) return;
            controller.StateMachine.Register(PlayerStateId.Swim, new SwimState(controller));
            var manager = AuraManager.Instance;
            if (manager != null) SetPassives(manager.Passives);
        }

        void OnEnable()
        {
            if (_bound) return;
            _bound = true;
            EventBus.Subscribe<AuraChanged>(OnAuraChanged);
        }

        void OnDisable() => Unbind();

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);
        }

        /// <summary>Called by a water volume when Leo enters it. Overlapping volumes are counted.</summary>
        public void EnterWater()
        {
            _waterVolumes++;
            Recompute();
        }

        public void ExitWater()
        {
            _waterVolumes = Math.Max(0, _waterVolumes - 1);
            Recompute();
        }

        /// <summary>Applies new passives (the Aura changed) and recomputes everything.</summary>
        public void SetPassives(in AuraPassives passives)
        {
            _passives = passives;
            Recompute();
        }

        void Recompute()
        {
            Modifiers = AuraPassiveResolver.Resolve(_passives, InWater);
            if (controller != null)
            {
                controller.CanDoubleJump = Modifiers.CanDoubleJump;
                controller.CanGlide = Modifiers.CanGlide;
                controller.SpeedMultiplier = Modifiers.SpeedMultiplier;
                controller.JumpMultiplier = Modifiers.JumpMultiplier;
                controller.SwimMode = Modifiers.SwimMode;
            }
            ModifiersChanged?.Invoke(Modifiers);
        }

        void OnAuraChanged(AuraChanged evt)
        {
            var manager = AuraManager.Instance;
            SetPassives(manager != null ? manager.Passives : AuraPassives.None);
        }
    }
}
