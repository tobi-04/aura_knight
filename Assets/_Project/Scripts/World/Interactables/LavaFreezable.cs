using System;
using AuraKnight.Aura;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Lava that the Water skill turns into a standable platform for 4 s. While frozen the damaging hitbox object is
    /// off and the platform object is on; afterwards the lava returns. Keep a trigger collider on this root so
    /// the skill's proximity cast finds it even while the lava object is inactive.
    /// </summary>
    public sealed class LavaFreezable : MonoBehaviour, IAuraInteractable
    {
        public const float FreezeSeconds = 4f;

        [Tooltip("Hazard object (Hitbox) active while the lava is liquid.")]
        [SerializeField] GameObject lava;
        [Tooltip("Solid platform active while the lava is frozen.")]
        [SerializeField] GameObject platform;
        [SerializeField, Min(0.1f)] float freezeSeconds = FreezeSeconds;

        readonly Countdown _timer = new Countdown();

        public bool IsFrozen => _timer.IsActive;
        public float Remaining => _timer.Remaining;

        /// <summary>True while frozen = (true), false when it thaws.</summary>
        public event Action<bool> FrozenChanged;

        void Start() => Apply(false);

        void Update() => Tick(Time.deltaTime);

        public bool TryInteract(AuraInteraction interaction, AuraId source)
        {
            if (!AuraInteractionRules.Accepts(AuraId.Water, AuraInteraction.Freeze, interaction, source)) return false;
            bool wasFrozen = IsFrozen;
            _timer.Start(freezeSeconds);
            if (!wasFrozen) Apply(true);
            return true;
        }

        internal void Tick(float deltaTime)
        {
            if (!IsFrozen) return;
            _timer.Tick(deltaTime);
            if (!IsFrozen) Apply(false);
        }

        void Apply(bool frozen)
        {
            if (lava != null) lava.SetActive(!frozen);
            if (platform != null) platform.SetActive(frozen);
            FrozenChanged?.Invoke(frozen);
        }
    }
}
