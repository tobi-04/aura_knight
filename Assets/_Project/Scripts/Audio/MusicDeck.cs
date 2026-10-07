using UnityEngine;
using UnityEngine.Audio;

namespace AuraKnight.Audio
{
    /// <summary>
    /// One track's pair of looping sources (Explore always, Combat only while its mix is audible). The combat source
    /// joins at the explore source's <c>timeSamples</c> so both layers stay on the same beat.
    /// </summary>
    public sealed class MusicDeck
    {
        readonly AudioSource _explore, _combat;
        RegionMusic _track;

        public CrossfadeValue Gain = new CrossfadeValue(0f);
        public string TrackId => _track != null ? _track.trackId : null;
        public bool IsActive { get; private set; }

        public MusicDeck(Transform parent, string name, AudioMixerGroup group)
        {
            _explore = Create(parent, name + "_Explore", group);
            _combat = Create(parent, name + "_Combat", group);
        }

        static AudioSource Create(Transform parent, string name, AudioMixerGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        public void Begin(RegionMusic track, float startGain)
        {
            Stop();
            _track = track;
            Gain = new CrossfadeValue(startGain);
            _explore.clip = track.explore;
            _explore.volume = 0f; // Apply sets the real level on the next tick; never blip at full volume
            _explore.timeSamples = 0;
            _explore.Play();
            IsActive = true;
        }

        public void Stop()
        {
            _explore.Stop();
            _combat.Stop();
            _explore.clip = null;
            _combat.clip = null;
            _track = null;
            IsActive = false;
        }

        /// <summary>Sets both layer volumes from the deck gain and the shared combat mix; starts or stops the combat layer as needed.</summary>
        public void Apply(float combatMix)
        {
            if (!IsActive) return;
            var (exploreGain, combatGain) = _track.HasCombat ? AudioMath.LayerGains(combatMix) : (1f, 0f);
            _explore.volume = _track.volume * Gain.Value * exploreGain;
            _combat.volume = _track.volume * Gain.Value * combatGain;
            if (!_track.HasCombat) return;
            if (combatMix > 0.001f && _combat.clip == null)
            {
                _combat.clip = _track.combat;
                _combat.timeSamples = _explore.timeSamples; // join on the beat
                _combat.Play();
            }
            else if (combatMix <= 0.001f && _combat.clip != null)
            {
                _combat.Stop();
                _combat.clip = null;
            }
        }

        /// <summary>Pulls the combat layer back onto the explore layer when they drifted apart.</summary>
        public bool Resync()
        {
            if (!IsActive || _combat.clip == null || _explore.clip == null) return false;
            if (!MusicSync.NeedsResync(_explore.timeSamples, _combat.timeSamples, _explore.clip.samples)) return false;
            _combat.timeSamples = _explore.timeSamples;
            return true;
        }
    }
}
