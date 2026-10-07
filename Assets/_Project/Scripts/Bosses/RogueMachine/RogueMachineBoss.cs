using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Rogue Machine (50 HP). The boiler on its back is a <see cref="WeakPointHurtbox"/> (x2 from any hit, the Fireball is the intended way
    /// to reach it). Pistons, laser and steam are separate attack components.
    /// </summary>
    public sealed class RogueMachineBoss : BossBase
    {
        [SerializeField] WeakPointHurtbox boiler;

        public WeakPointHurtbox Boiler => boiler;
    }
}
