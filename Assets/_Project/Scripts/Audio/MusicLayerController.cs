using AuraKnight.Core;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>
    /// Dynamic music. Each region has an Explore and a Combat layer that loop in sync; <see cref="CombatMix"/> glides
    /// between them in <see cref="CrossfadeSeconds"/> (1 s). The mix follows the number of Enemy-layer colliders within
    /// 8 units (scanned every 0.5 s, no allocation) while <see cref="AutoCombat"/> is on, or whatever
    /// <see cref="SetCombatIntensity"/> says when it is off. The region track changes on <c>RoomEntered</c> and
    /// crossfades between the two decks; boss / ending tracks override the region until <see cref="ReleaseOverride"/>.
    /// Lives on the "Audio" object of the Core scene.
    /// </summary>
    public sealed class MusicLayerController : MonoBehaviour
    {
        public const string BossId = "boss", EndingId = "ending";
        public const float ScanInterval = 0.5f, ScanRadius = 8f, ResyncInterval = 1f;

        [SerializeField] RegionMusic[] tracks = System.Array.Empty<RegionMusic>();
        [SerializeField] float crossfadeSeconds = 1f;
        [SerializeField] bool autoCombat = true;

        readonly Collider2D[] _scanBuffer = new Collider2D[16];
        readonly CombatIntensityTracker _intensity = new CombatIntensityTracker();
        CrossfadeValue _combatMix = new CrossfadeValue(0f);
        MusicDeck _current, _previous;
        ContactFilter2D _filter;
        Transform _player;
        string _regionId, _overrideId;
        float _scanTimer, _resyncTimer;
        bool _bound;

        public static MusicLayerController Instance { get; private set; }

        public float CrossfadeSeconds { get => crossfadeSeconds; set => crossfadeSeconds = Mathf.Max(0f, value); }
        public bool AutoCombat { get => autoCombat; set => autoCombat = value; }
        /// <summary>Current Explore(0)..Combat(1) mix.</summary>
        public float CombatMix => _combatMix.Value;
        /// <summary>Id of the track that is (fading) in, or null when silent.</summary>
        public string CurrentTrackId => _current != null && _current.IsActive ? _current.TrackId : null;
        /// <summary>True while an older track is still fading out.</summary>
        public bool IsCrossfading => _previous != null && _previous.IsActive;
        public float CurrentGain => _current != null ? _current.Gain.Value : 0f;

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
        }

        void OnEnable()
        {
            if (_bound || Instance != this) return;
            _bound = true;
            EventBus.Subscribe<RoomEntered>(OnRoomEntered);
        }

        void OnDisable()
        {
            if (!_bound) return;
            _bound = false;
            EventBus.Unsubscribe<RoomEntered>(OnRoomEntered);
        }

        void Start()
        {
            if (Instance != this) return;
            var group = AudioManager.Instance != null ? AudioManager.Instance.MusicGroup : null;
            _current = new MusicDeck(transform, "DeckA", group);
            _previous = new MusicDeck(transform, "DeckB", group);
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = PhysicsLayers.Mask(PhysicsLayers.Enemy), useTriggers = true };
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- API ----

        /// <summary>Manual combat intensity 0..1 (1 = full Combat layer). With <see cref="AutoCombat"/> on, the next scan overrides it.</summary>
        public void SetCombatIntensity(float intensity) => _combatMix.SetTarget(intensity);

        public void PlayBoss() => SetOverride(BossId);

        public void PlayEnding() => SetOverride(EndingId);

        /// <summary>Leaves the boss / ending track and returns to the music of the current region.</summary>
        public void ReleaseOverride()
        {
            _overrideId = null;
            if (_regionId != null) SwitchTo(_regionId);
        }

        /// <summary>Fades the music out (it fades back in with the next region or boss call).</summary>
        public void StopMusic()
        {
            if (_current == null) return;
            _current.Gain.SetTarget(0f);
        }

        void SetOverride(string id)
        {
            _overrideId = id;
            SwitchTo(id);
        }

        void OnRoomEntered(RoomEntered e)
        {
            if (string.IsNullOrEmpty(e.RegionId)) return;
            _regionId = e.RegionId;
            if (_overrideId == null) SwitchTo(_regionId);
        }

        /// <summary>Starts <paramref name="id"/> on the idle deck and fades the playing one out. Unknown ids and the track already playing are ignored.</summary>
        public bool SwitchTo(string id)
        {
            if (_current == null || !TryFind(id, out var track)) return false;
            if (_current.IsActive && _current.TrackId == id && _current.Gain.Target > 0f) return false;
            (_current, _previous) = (_previous, _current); // the old current keeps playing as the fading deck
            _current.Begin(track, 0f);
            _current.Gain.SetTarget(1f);
            if (_previous.IsActive) _previous.Gain.SetTarget(0f);
            return true;
        }

        bool TryFind(string id, out RegionMusic found)
        {
            foreach (var track in tracks)
            {
                if (track == null || track.trackId != id || track.explore == null) continue;
                found = track;
                return true;
            }
            found = null;
            return false;
        }

        // ---- per frame ----

        void Update()
        {
            if (_current == null) return;
            float dt = Time.unscaledDeltaTime;
            if (autoCombat) ScanForEnemies(dt);
            _combatMix.Step(dt, crossfadeSeconds);
            Tick(_current, dt);
            Tick(_previous, dt);
            _resyncTimer += dt;
            if (_resyncTimer >= ResyncInterval)
            {
                _resyncTimer = 0f;
                _current.Resync();
                _previous.Resync();
            }
        }

        void Tick(MusicDeck deck, float dt)
        {
            if (!deck.IsActive) return;
            deck.Gain.Step(dt, crossfadeSeconds);
            if (deck.Gain.Value <= 0f && deck.Gain.Target <= 0f) deck.Stop();
            else deck.Apply(_combatMix.Value);
        }

        void ScanForEnemies(float dt)
        {
            _scanTimer += dt;
            if (_scanTimer < ScanInterval) return;
            _scanTimer = 0f;
            if (_player == null)
            {
                var found = GameObject.FindWithTag(WorldTags.Player);
                if (found == null) return;
                _player = found.transform;
            }
            int count = Physics2D.OverlapCircle(_player.position, ScanRadius, _filter, _scanBuffer);
            _combatMix.SetTarget(_intensity.Update(count, ScanInterval));
        }
    }
}
