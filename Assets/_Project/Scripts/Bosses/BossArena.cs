using AuraKnight.Audio;
using AuraKnight.Core;
using AuraKnight.UI;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// The boss room's controller. The trigger zone (Interactable layer, inside the room, past the entry door) starts the fight: doors close, the
    /// boss engages, the intro banner / HP bar events fire (BossEncounterStarted) and the boss music and roar play. Leo's death resets the boss
    /// completely and reopens the doors (BossEncounterEnded). Victory runs <see cref="BossVictorySequence"/>. A boss already in
    /// <c>GameState.defeatedBosses</c> stays down: doors open, boss absent, trigger inert. Altar placement: see <see cref="DesignerNote"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BossArena : MonoBehaviour
    {
        public enum ArenaState { Idle, Fighting, Defeated }

        [SerializeField] BossBase boss;
        [Tooltip("Blockers closed while the fight runs (Ground layer colliders).")]
        [SerializeField] GameObject[] doors = new GameObject[0];
        [Tooltip("Floor-level playfield in this object's local space; attacks keep their hazards inside it.")]
        [SerializeField] Vector2 playfieldMin = new Vector2(3f, 1f);
        [SerializeField] Vector2 playfieldMax = new Vector2(37f, 21f);
        [TextArea(2, 5)]
        [SerializeField] string designerNote = "Level design: put a Sun Altar in the room BEFORE this one (GDD 5, altar before the boss room), " +
            "RoomExit the entry to spawn 'default' (inside the entry door), and keep the exit door side free for the way back.";

        public ArenaState State { get; private set; } = ArenaState.Idle;
        public BossBase Boss => boss;
        public string DesignerNote => designerNote;
        public bool DoorsClosed { get; private set; }
        public Bounds Playfield
        {
            get
            {
                Vector2 min = transform.TransformPoint(playfieldMin), max = transform.TransformPoint(playfieldMax);
                var bounds = new Bounds();
                bounds.SetMinMax(new Vector3(min.x, min.y, -1f), new Vector3(max.x, max.y, 1f));
                return bounds;
            }
        }

        void Awake()
        {
            if (boss != null)
            {
                boss.BindArena(this);
                return;
            }
            Debug.LogError($"{name}: BossArena has no boss assigned.", this);
            enabled = false;
        }

        void OnEnable()
        {
            EventBus.Subscribe<PlayerDied>(OnPlayerDied);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            boss.Defeated += OnBossDefeated;
            Refresh();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
            if (boss != null) boss.Defeated -= OnBossDefeated;
            if (State == ArenaState.Fighting) ResetEncounter();
        }

        /// <summary>Re-reads the save: a defeated boss opens the doors and stays away; otherwise the arena is armed.</summary>
        public void Refresh()
        {
            bool defeated = BossProgress.IsDefeated(boss.Stats.bossId);
            if (State == ArenaState.Fighting) ResetEncounter();
            State = defeated ? ArenaState.Defeated : ArenaState.Idle;
            SetDoors(false);
            boss.ResetBoss();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (State != ArenaState.Idle || !WorldTags.IsPlayer(other)) return;
            Engage(other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform);
        }

        /// <summary>Closes the doors and starts the fight against <paramref name="player"/>.</summary>
        public bool Engage(Transform player)
        {
            if (State != ArenaState.Idle || !boss.Engage(player)) return false;
            State = ArenaState.Fighting;
            SetDoors(true);
            var stats = boss.Stats;
            EventBus.Publish(new BossEncounterStarted(stats.bossId, stats.displayName));
            MusicLayerController.Instance?.PlayBoss();
            Sfx.Play(SfxId.BossRoar, boss.transform.position);
            return true;
        }

        /// <summary>Leo died (or the room unloads): the boss resets completely, doors reopen, the HP bar hides, the room music returns.</summary>
        public void ResetEncounter()
        {
            if (State != ArenaState.Fighting || boss == null) return;
            var player = boss.Target;
            boss.ResetBoss();
            PlayerSlowStatus.ClearOn(player);
            State = ArenaState.Idle;
            SetDoors(false);
            var stats = boss.Stats;
            EventBus.Publish(new BossEncounterEnded(stats.bossId, stats.displayName));
            MusicLayerController.Instance?.ReleaseOverride();
        }

        void OnBossDefeated(BossBase defeated)
        {
            if (State != ArenaState.Fighting) return;
            State = ArenaState.Defeated;
            SetDoors(false);
            PlayerSlowStatus.ClearOn(defeated.Target);
            BossVictorySequence.Run(new BossVictorySteps(defeated.Stats), defeated.Stats.finalBoss);
        }

        void OnPlayerDied(PlayerDied evt) => ResetEncounter();

        void OnStateLoaded(GameStateLoaded evt) => Refresh();

        void SetDoors(bool closed)
        {
            DoorsClosed = closed;
            foreach (var door in doors)
                if (door != null) door.SetActive(closed);
        }
    }
}
