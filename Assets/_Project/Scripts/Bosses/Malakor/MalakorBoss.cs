using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Lord Malakor (70 HP, final boss). Phases 1 and 2 use the shadow slash and the teleport stab (phase 2: 25% faster plus a double slash);
    /// phase 3 (at 25% HP, P2 scope) switches the lights off through <see cref="DarkPhaseController"/> and adds the Aura-coloured strikes.
    /// Provides the teleport helper used by <see cref="TeleportStabAttack"/>.
    /// </summary>
    public sealed class MalakorBoss : BossBase
    {
        /// <summary>Moves Malakor to <paramref name="x"/> on his floor and turns him toward Leo.</summary>
        public void TeleportTo(float x)
        {
            var field = Playfield;
            SetPosition(new Vector2(Mathf.Clamp(x, field.min.x + 1.5f, field.max.x - 1.5f), HomePosition.y));
            FaceTarget();
        }
    }
}
