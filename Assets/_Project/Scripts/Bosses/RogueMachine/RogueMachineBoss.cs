using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Rogue Machine (50 HP). The boiler on its back is a <see cref="WeakPointHurtbox"/> (x2 from the Fireball only, other hits count
    /// normally). Pistons, laser and steam are separate attack components.
    /// </summary>
    public sealed class RogueMachineBoss : BossBase
    {
        [SerializeField] WeakPointHurtbox boiler;

        public WeakPointHurtbox Boiler => boiler;
    }
}
