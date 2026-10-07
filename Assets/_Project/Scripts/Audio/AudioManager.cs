using AuraKnight.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace AuraKnight.Audio
{
    /// <summary>
    /// Plays sound effects from a pool of <see cref="PoolSize"/> sources routed to the mixer's SFX / UI groups.
    /// When every voice is busy the longest-running one is stolen, so new sounds are never dropped.
    /// Other systems call <see cref="Sfx.Play"/>; gameplay events arrive through <see cref="AudioEventListener"/>.
    /// Lives on the "Audio" object of the Core scene.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class AudioManager : MonoBehaviour
    {
        public const int PoolSize = 12;
        const float SpatialBlend = 0.7f, MinDistance = 12f, MaxDistance = 40f;
        const string SfxGroupPath = "Master/SFX", UiGroupPath = "Master/UI", MusicGroupPath = "Master/Music";

        [SerializeField] AudioMixer mixer;
        [SerializeField] SfxLibrary library;

        AudioSource[] _pool;
        float[] _busyUntil, _startedAt;
        int[] _lastClip;
        bool[] _warned;
        SfxThrottle _throttle;
        AudioMixerGroup _sfxGroup, _uiGroup;

        public static AudioManager Instance { get; private set; }

        public AudioMixer Mixer => mixer;
        public AudioMixerGroup MusicGroup { get; private set; }
        public int PlayCount { get; private set; }
        public SfxId LastPlayed { get; private set; }

        /// <summary>Voices whose sound has not finished yet.</summary>
        public int ActiveVoices
        {
            get
            {
                int count = 0;
                float now = Time.unscaledTime;
                foreach (float end in _busyUntil) if (end > now) count++;
                return count;
            }
        }

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
            _throttle = new SfxThrottle();
            _warned = new bool[System.Enum.GetValues(typeof(SfxId)).Length + 8];
            _lastClip = new int[_warned.Length];
            System.Array.Fill(_lastClip, -1);
            _sfxGroup = Group(SfxGroupPath);
            _uiGroup = Group(UiGroupPath);
            MusicGroup = Group(MusicGroupPath);
            BuildPool();
        }

        void Start() => AudioVolumeSettings.Apply();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Plays <paramref name="id"/> once. Returns false when it is silent by design or by accident: id None, no clip
        /// in the library, or the same sound repeated within 40 ms. A position gives distance falloff and panning.
        /// </summary>
        public bool Play(SfxId id, Vector3? position = null)
        {
            if (id == SfxId.None || _pool == null) return false;
            if (library == null || !library.TryGet(id, out var entry) || !entry.HasClips)
            {
                WarnMissing(id);
                return false;
            }
            float now = Time.unscaledTime;
            if (!_throttle.TryAcquire(id, now)) return false;

            int index = (int)id;
            int clipIndex = ClipPicker.Pick(entry.clips.Length, _lastClip[index], Random.value);
            _lastClip[index] = clipIndex;
            var clip = entry.clips[clipIndex];
            if (clip == null) return false;

            int voice = VoiceSelector.Choose(_busyUntil, _startedAt, now);
            var source = _pool[voice];
            float pitch = AudioMath.RandomPitch(entry.pitchVariance, Random.value);
            source.Stop();
            source.clip = clip;
            source.volume = entry.volume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = AudioMath.BusOf(id) == SfxBus.Ui ? _uiGroup : _sfxGroup;
            source.spatialBlend = position.HasValue ? SpatialBlend : 0f;
            source.transform.position = position ?? transform.position;
            source.Play();
            _startedAt[voice] = now;
            _busyUntil[voice] = now + clip.length / pitch;
            PlayCount++;
            LastPlayed = id;
            return true;
        }

        void BuildPool()
        {
            _pool = new AudioSource[PoolSize];
            _busyUntil = new float[PoolSize];
            _startedAt = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject($"SfxVoice_{i:00}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = MinDistance;
                source.maxDistance = MaxDistance;
                _pool[i] = source;
                _busyUntil[i] = float.NegativeInfinity;
                _startedAt[i] = float.NegativeInfinity;
            }
        }

        AudioMixerGroup Group(string path)
        {
            if (mixer == null) return null;
            var groups = mixer.FindMatchingGroups(path);
            if (groups != null && groups.Length > 0) return groups[0];
            Debug.LogWarning($"[{nameof(AudioManager)}] Mixer has no group '{path}'.", this);
            return null;
        }

        void WarnMissing(SfxId id)
        {
            int i = (int)id;
            if (i < 0 || i >= _warned.Length || _warned[i]) return;
            _warned[i] = true;
            Debug.LogWarning($"[{nameof(AudioManager)}] No clip for {id}; it will stay silent.", this);
        }
    }
}
