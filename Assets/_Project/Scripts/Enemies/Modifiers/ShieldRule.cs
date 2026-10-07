using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Enemies.Modifiers
{
    /// <summary>Pure front-shield rule (Night Knight). <see cref="DamageInfo.Direction"/> points from attacker to victim.</summary>
    public static class ShieldRule
    {
        /// <summary>
        /// True when the hit travels against the facing, meaning the attacker stands in front. Vertical hits (pogo,
        /// no horizontal component) and hits from behind go through. Only Player-team hits are blocked.
        /// </summary>
        public static bool Blocks(int facing, in DamageInfo info)
        {
            if (info.Team != Team.Player) return false;
            float x = info.Direction.x;
            if (Mathf.Abs(x) < 0.01f) return false;
            int heading = x > 0f ? 1 : -1;
            return heading == -(facing >= 0 ? 1 : -1);
        }
    }
}
