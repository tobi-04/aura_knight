using System;
using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Aura.Skills
{
    /// <summary>
    /// Water shield: absorbs exactly one hit within 6 s (through <see cref="Health.DamageFilter"/>) and, at cast time,
    /// extinguishes fire traps and freezes lava within 2.5 tiles.
    /// </summary>
    public sealed class WaterShieldSkill : AuraSkillBase
    {
        public const float ProximityRadius = 2.5f;

        [SerializeField] GameObject shieldVisual;

        readonly ShieldState _shield = new ShieldState();
        readonly AuraInteractionProbe _probe = new AuraInteractionProbe();
        Health _health;

        public override AuraId Aura => AuraId.Water;
        public override bool CanCast => !_shield.IsActive;
        public bool IsShielded => _shield.IsActive;
        public float Remaining => _shield.Remaining;

        /// <summary>Raised when the shield eats a hit.</summary>
        public event Action<DamageInfo> Absorbed;

        protected override void OnBound()
        {
            Detach();
            _health = OwnerHealth;
            if (_health != null) _health.DamageFilter = Filter;
            SetVisual(false);
        }

        public override void Cast()
        {
            _shield.Activate();
            SetVisual(true);
            _probe.Apply(Origin, ProximityRadius, AuraInteraction.Extinguish, AuraId.Water);
            _probe.Apply(Origin, ProximityRadius, AuraInteraction.Freeze, AuraId.Water);
        }

        void Update() => Tick(Time.deltaTime);

        internal void Tick(float deltaTime)
        {
            if (!_shield.IsActive) return;
            _shield.Tick(deltaTime);
            if (!_shield.IsActive) SetVisual(false);
        }

        bool Filter(DamageInfo info)
        {
            if (!_shield.TryAbsorb()) return false;
            SetVisual(false);
            Absorbed?.Invoke(info);
            return true;
        }

        void SetVisual(bool on)
        {
            if (shieldVisual != null) shieldVisual.SetActive(on);
        }

        void Detach()
        {
            if (_health != null && _health.DamageFilter == (Func<DamageInfo, bool>)Filter) _health.DamageFilter = null;
            _health = null;
        }

        void OnDestroy() => Detach();
    }
}
