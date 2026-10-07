using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>
    /// The music of one region (or the boss / ending): an Explore layer and an optional Combat layer. Both layers
    /// must have the same length and tempo so they stay in sync when crossfaded. <see cref="trackId"/> is the RegionId
    /// of the rooms ("hub", "forest", "cave", "city", "castle") or "boss" / "ending".
    /// </summary>
    [CreateAssetMenu(menuName = "Aura Knight/Audio/Region Music", fileName = "RegionMusic")]
    public sealed class RegionMusic : ScriptableObject
    {
        public string trackId;
        public AudioClip explore;
        public AudioClip combat;
        [Range(0f, 1f)] public float volume = 0.7f;

        public bool HasCombat => combat != null;
    }
}
