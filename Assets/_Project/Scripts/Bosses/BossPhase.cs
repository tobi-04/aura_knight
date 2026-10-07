using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>One entry of a phase's attack pool.</summary>
    [Serializable]
    public struct BossAttackWeight
    {
        public BossAttack attack;
        [Min(0f)] public float weight;
    }

    /// <summary>
    /// A boss phase: when it starts (HP fraction), how fast the boss acts (phase 2 = 1.25) and the weighted pool of attacks.
    /// The variant added in phase 2 is simply one more pool entry (another attack component with different numbers).
    /// </summary>
    [Serializable]
    public sealed class BossPhase
    {
        public string name = "Phase";
        [Range(0.05f, 1f)] public float enterAtHpFraction = 1f;
        [Min(0.1f)] public float speed = 1f;
        [Tooltip("Pause between attacks at speed 1, seconds.")]
        [Min(0f)] public float thinkSeconds = 1.2f;
        public List<BossAttackWeight> attacks = new List<BossAttackWeight>();
    }
}
