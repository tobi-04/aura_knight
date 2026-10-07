using System;
using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>One library row: the clips for an id (a random one plays), its base volume and pitch randomisation.</summary>
    [Serializable]
    public sealed class SfxEntry
    {
        public SfxId id;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, AudioMath.MaxPitchVariance)] public float pitchVariance = AudioMath.DefaultPitchVariance;

        public bool HasClips => clips != null && clips.Length > 0;
    }
}
