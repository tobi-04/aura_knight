using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Rotten Root Tree (30 HP). The core (a <see cref="WeakPointHurtbox"/>, x2) is only reachable while the mouth is open after the branch sweep:
    /// <see cref="SetCoreExposed"/> toggles the Exposed animator state, the glow tint and the core collider.
    /// </summary>
    public sealed class RootTreeBoss : BossBase
    {
        static readonly Color CoreGlow = new Color(1f, 0.95f, 0.5f);

        [SerializeField] GameObject coreWeakPoint;

        public bool CoreExposed { get; private set; }
        public GameObject CoreWeakPoint => coreWeakPoint;

        public void SetCoreExposed(bool exposed)
        {
            CoreExposed = exposed;
            if (coreWeakPoint != null) coreWeakPoint.SetActive(exposed);
            SetExposed(exposed);
            if (exposed) SetTint(CoreGlow);
            else ClearTint();
        }

        protected override void OnReset() => SetCoreExposed(false);

        protected override void OnDeath() => SetCoreExposed(false);
    }
}
