using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Crawler: walks a path of authored waypoints along walls and ceilings (Stone Spider), speeding up when Leo is near.
    /// Static zapper mode (archetype Static, Scrap Zapper): stands still and discharges a 2-tile ring every 3 s after a
    /// warning flash. Waypoints are child transforms of the object named by <c>pathRoot</c>; their positions relative to the
    /// root at Awake become offsets from the spawn point, so the path moves with the room. Never knocked back or stunned.
    /// </summary>
    public sealed class CrawlerEnemy : EnemyBase
    {
        static readonly Color TelegraphColor = new Color(1f, 0.95f, 0.35f);

        [Tooltip("Child whose children are the waypoints, in order (first one normally at the root's own position).")]
        [SerializeField] Transform pathRoot;
        [SerializeField] bool loopPath;
        [Tooltip("Static mode only: trigger ring that deals the zap damage.")]
        [SerializeField] Hitbox zapHitbox;

        Vector2[] _offsets = new Vector2[0];
        WaypointPath _path = new WaypointPath(null, true);
        ZapCycle _zap;
        float _zapActiveLeft;

        protected override bool Interruptible => false;
        bool IsStatic => Stats.archetype == EnemyArchetype.Static;

        protected override void OnReset()
        {
            if (IsStatic) ResetZap();
            else ResetPath();
        }

        void ResetPath()
        {
            if (_offsets.Length == 0) _offsets = ReadOffsets();
            var spawn = SpawnPosition;
            var points = new Vector2[_offsets.Length];
            for (int i = 0; i < points.Length; i++) points[i] = spawn + _offsets[i];
            _path = new WaypointPath(points, loopPath);
        }

        Vector2[] ReadOffsets()
        {
            if (pathRoot == null) return new Vector2[0];
            var origin = (Vector2)transform.position;
            var offsets = new Vector2[pathRoot.childCount];
            for (int i = 0; i < offsets.Length; i++) offsets[i] = (Vector2)pathRoot.GetChild(i).position - origin;
            return offsets;
        }

        void ResetZap()
        {
            if (_zap != null) _zap.Fired -= OnZap;
            _zap = new ZapCycle(Stats.zapInterval, Stats.zapTelegraphSeconds, Stats.zapActiveSeconds);
            _zap.Fired += OnZap;
            _zapActiveLeft = 0f;
            if (zapHitbox == null) return;
            zapHitbox.Team = Team.Enemy;
            zapHitbox.Damage = Stats.contactDamage;
            zapHitbox.Deactivate();
            if (zapHitbox.TryGetComponent<CircleCollider2D>(out var ring)) ring.radius = Stats.zapRadius;
        }

        protected override void OnPatrol(float deltaTime)
        {
            if (IsStatic) { TickStatic(deltaTime); return; }
            if (TargetInRange(Stats.detectRange)) Machine.Reset(EnemyState.Detect);
            Crawl(deltaTime, Stats.moveSpeed);
        }

        protected override void OnDetect(float deltaTime)
        {
            if (IsStatic) { TickStatic(deltaTime); return; }
            if (!TargetInRange(Stats.detectRange * LoseRangeFactor)) Machine.Reset(EnemyState.Patrol);
            Crawl(deltaTime, Stats.chargeSpeed);
        }

        protected override void OnAttack(float deltaTime)
        {
            if (IsStatic) TickStatic(deltaTime);
        }

        protected override void OnCooldown(float deltaTime)
        {
            if (IsStatic) TickStatic(deltaTime);
        }

        void Crawl(float deltaTime, float speed)
        {
            var next = _path.Advance(Position, speed, deltaTime);
            if (Mathf.Abs(next.x - Position.x) > 0.0001f) Face(next.x > Position.x ? 1 : -1);
            Rb.MovePosition(next);
        }

        // The machine state is only a label here: it mirrors the zap phase so animation and audio can follow it.
        void TickStatic(float deltaTime)
        {
            _zap.Tick(deltaTime);
            if (_zapActiveLeft > 0f)
            {
                _zapActiveLeft -= deltaTime;
                if (_zapActiveLeft <= 0f && zapHitbox != null) zapHitbox.Deactivate();
            }
            switch (_zap.Phase)
            {
                case ZapPhase.Telegraph: SetTint(TelegraphColor); Machine.Reset(EnemyState.Detect); break;
                case ZapPhase.Fire: ClearTint(); Machine.Reset(EnemyState.Attack); break;
                default: ClearTint(); Machine.Reset(EnemyState.Patrol); break;
            }
        }

        void OnZap()
        {
            if (zapHitbox == null) return;
            zapHitbox.Activate(Stats.contactDamage);
            _zapActiveLeft = Stats.zapActiveSeconds;
        }

        protected override void OnDeath()
        {
            if (zapHitbox != null) zapHitbox.Deactivate();
        }

        void OnDrawGizmosSelected()
        {
            if (pathRoot == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathRoot.childCount; i++)
            {
                var a = pathRoot.GetChild(i).position;
                Gizmos.DrawWireSphere(a, 0.15f);
                if (i + 1 < pathRoot.childCount) Gizmos.DrawLine(a, pathRoot.GetChild(i + 1).position);
            }
        }
    }
}
