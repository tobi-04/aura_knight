namespace AuraKnight.Bosses
{
    /// <summary>The side effects of beating a boss, in the order they must happen. Implemented by <c>BossVictorySteps</c>.</summary>
    public interface IBossVictorySteps
    {
        /// <summary>Grants the reward Aura (persists immediately). True when there is nothing to grant (final boss) or it was granted.</summary>
        bool UnlockReward();
        void MarkDefeated();
        /// <summary>Publishes Core.BossDefeated; GameManager autosaves on it.</summary>
        void PublishDefeated();
        void EndEncounter();
        void ReleaseMusic();
        void PlayEnding();
        void CompleteGame();
    }

    /// <summary>
    /// Victory order: the Aura is unlocked (and saved) BEFORE BossDefeated is published, because BossDefeated triggers the autosave
    /// that must already contain the reward. The final boss plays the ending and publishes GameCompleted instead of releasing the music.
    /// </summary>
    public static class BossVictorySequence
    {
        public static void Run(IBossVictorySteps steps, bool finalBoss)
        {
            // An Aura that could not be granted must not be lost for good: the boss stays undefeated and can be fought again.
            if (steps.UnlockReward())
            {
                steps.MarkDefeated();
                steps.PublishDefeated();
            }
            steps.EndEncounter();
            if (finalBoss)
            {
                steps.PlayEnding();
                steps.CompleteGame();
                return;
            }
            steps.ReleaseMusic();
        }
    }
}
