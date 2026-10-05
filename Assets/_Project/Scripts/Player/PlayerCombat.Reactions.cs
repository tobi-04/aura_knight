using System;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Damage, death and respawn reactions of <see cref="PlayerCombat"/>.</summary>
    public sealed partial class PlayerCombat
    {
        bool _awaitingRespawn;

        /// <summary>
        /// Starts moving Leo to a checkpoint; true when the move was started (the handler publishes PlayerRespawned on arrival),
        /// false when no destination can be used yet - Leo then stays in the Dead state and the request is repeated.
        /// Replaceable for tests.
        /// </summary>
        public Func<bool> RespawnHandler { get; set; } = RespawnAtCheckpoint;

        static bool RespawnAtCheckpoint() => CheckpointService.Instance != null && CheckpointService.Instance.Respawn();

        /// <summary>Called by the Dead state once <see cref="RespawnDelay"/> has passed; returns whether a respawn is now under way.</summary>
        public bool RequestRespawn()
        {
            bool started = false;
            try { started = RespawnHandler != null && RespawnHandler(); }
            catch (Exception e) { Debug.LogException(e, this); }
            if (!started) Debug.LogWarning("[PlayerCombat] No respawn destination yet; Leo stays down and will retry.", this);
            return started;
        }

        void OnDamaged(DamageInfo info, int applied)
        {
            LastDamage = info;
            if (feedback != null) feedback.TookHit();
            if (_health.IsDead) return;
            controller.StateMachine.TryChange(PlayerStateId.Hurt);
            Combo.Reset();
        }

        void OnDied(DamageInfo info)
        {
            _awaitingRespawn = true;
            swordHitbox.Deactivate();
            controller.ExternalInvulnerable = true;
            controller.StateMachine.TryChange(PlayerStateId.Dead);
            Combo.Reset();
        }

        void OnRespawned(PlayerRespawned evt)
        {
            if (_awaitingRespawn) ResetToFreshStart();
        }

        /// <summary>Full hearts and energy, alive and idle (respawn, or an existing player re-entering the world from the menu).</summary>
        public void ResetToFreshStart()
        {
            _awaitingRespawn = false;
            swordHitbox.Deactivate();
            stats.RestoreAll();
            controller.ExternalInvulnerable = false;
            controller.SetVelocity(Vector2.zero);
            controller.StateMachine.TryChange(PlayerStateId.Idle);
        }
    }
}
