using System;
using AuraKnight.Aura;
using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Player component: runs the <see cref="OxygenTimer"/> while Leo wades without the Water aura and removes hearts
    /// through the normal <see cref="Health"/> damage path (hazard damage, no knockback).
    /// </summary>
    public sealed class OxygenMeter : MonoBehaviour
    {
        [SerializeField] PlayerAuraBinder binder;
        [SerializeField] Health health;

        float _lastFraction = 1f;

        public OxygenTimer Timer { get; } = new OxygenTimer();

        /// <summary>Raised with the new 0..1 fraction whenever it changes (HUD bar).</summary>
        public event Action<float> Changed;

        void Awake()
        {
            if (binder == null) binder = GetComponent<PlayerAuraBinder>();
            if (health == null) health = GetComponent<Health>();
            if (binder == null || health == null)
            {
                Debug.LogError($"{nameof(OxygenMeter)} on '{name}' needs a {nameof(PlayerAuraBinder)} and a {nameof(Health)}.", this);
                enabled = false;
            }
        }

        void Update() => Tick(Time.deltaTime, binder.Modifiers.Drowning);

        internal void Tick(float deltaTime, bool drowning)
        {
            int hearts = Timer.Tick(deltaTime, drowning);
            for (int i = 0; i < hearts; i++)
                health.TakeDamage(new DamageInfo(1, Team.Hazard, null, Vector2.zero, 0f));

            float fraction = Timer.Fraction;
            if (Mathf.Approximately(fraction, _lastFraction)) return;
            _lastFraction = fraction;
            Changed?.Invoke(fraction);
        }
    }
}
