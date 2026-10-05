using System;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Aura energy: +8 per landed sword hit, spent by skills. Pure logic with a change event.</summary>
    public sealed class EnergyPool
    {
        public float Current { get; private set; }
        public float Max { get; private set; }

        /// <summary>(current, max) after any real change.</summary>
        public event Action<float, float> Changed;

        public EnergyPool(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        /// <summary>Returns the amount actually gained (clamped to the max).</summary>
        public float Add(float amount)
        {
            if (!(amount > 0f)) return 0f;
            float gained = Mathf.Min(amount, Max - Current);
            if (gained <= 0f) return 0f;
            Current += gained;
            Changed?.Invoke(Current, Max);
            return gained;
        }

        /// <summary>Spends exactly <paramref name="amount"/> or nothing; false when short or the amount is invalid.</summary>
        public bool TrySpend(float amount)
        {
            if (float.IsNaN(amount) || amount < 0f) return false;
            if (amount == 0f) return true;
            if (amount > Current) return false;
            Current -= amount;
            Changed?.Invoke(Current, Max);
            return true;
        }

        public void Refill()
        {
            if (Current >= Max) return;
            Current = Max;
            Changed?.Invoke(Current, Max);
        }

        public void SetMax(float max, bool refill)
        {
            Max = Mathf.Max(1f, max);
            Current = refill ? Max : Mathf.Min(Current, Max);
            Changed?.Invoke(Current, Max);
        }
    }
}
