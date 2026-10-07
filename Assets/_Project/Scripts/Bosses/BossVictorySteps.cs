using AuraKnight.Audio;
using AuraKnight.Aura;
using AuraKnight.Core;
using AuraKnight.UI;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Real implementation of the victory steps: AuraManager, GameManager, EventBus and the music controller.</summary>
    sealed class BossVictorySteps : IBossVictorySteps
    {
        readonly BossStats _stats;

        public BossVictorySteps(BossStats stats) { _stats = stats; }

        public bool UnlockReward()
        {
            if (!AuraIds.TryParse(_stats.rewardAura, out var aura) || aura == AuraId.None) return true;
            var manager = AuraManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[Boss] No AuraManager in the scene; the {_stats.rewardAura} reward of {_stats.bossId} is not granted.");
                return false;
            }
            manager.Unlock(aura); // false only when already owned
            return true;
        }

        public void MarkDefeated() => GameManager.Instance?.State.MarkBossDefeated(_stats.bossId);

        public void PublishDefeated() => EventBus.Publish(new BossDefeated(_stats.bossId));

        public void EndEncounter() => EventBus.Publish(new BossEncounterEnded(_stats.bossId, _stats.displayName));

        public void ReleaseMusic() => MusicLayerController.Instance?.ReleaseOverride();

        public void PlayEnding() => MusicLayerController.Instance?.PlayEnding();

        public void CompleteGame() => EventBus.Publish(new GameCompleted());
    }
}
