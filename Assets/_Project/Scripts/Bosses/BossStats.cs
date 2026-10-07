using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Identity and tunable numbers of one boss (GDD 7.4). One asset per boss under Data/Bosses, written by
    /// <c>AuraKnight.Editor.BossAssetGenerator</c>. Attack numbers live on the attack components of the boss prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "BossStats", menuName = "Aura Knight/Boss Stats")]
    public sealed class BossStats : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id saved in GameState.defeatedBosses and used by the HP bar events (matches the Art/Bosses folder).")]
        public string bossId = "RootTree";
        [Tooltip("Shown in the intro banner: BOSS / DISPLAY NAME.")]
        public string displayName = "BOSS";
        public string regionId = "forest";
        [Tooltip("Aura granted on victory (Wind / Fire / Water); None for the final boss.")]
        public string rewardAura = "None";
        [Tooltip("The final boss ends the game (GameCompleted + ending music) instead of releasing the music override.")]
        public bool finalBoss;

        [Header("Body")]
        [Min(1)] public int maxHp = 30;
        [Min(0)] public int contactDamage = 1;
        [Tooltip("Distance from the boss origin down to its feet; the floor is origin.y - footOffset.")]
        [Min(0f)] public float footOffset = 1.5f;
        public Vector2 bodySize = new Vector2(3f, 3f);

        [Header("Pacing")]
        [Tooltip("Seconds after the fight starts before the first attack (the intro banner plays meanwhile).")]
        [Min(0f)] public float introSeconds = 1.8f;
        [Tooltip("Pause while changing phase (roar), seconds.")]
        [Min(0f)] public float transitionSeconds = 0.9f;
        [Tooltip("Every attack telegraphs at least this long, at any phase speed.")]
        [Min(BossTiming.MinTelegraph)] public float minTelegraph = BossTiming.MinTelegraph;
    }
}
