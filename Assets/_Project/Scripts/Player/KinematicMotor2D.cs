using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Kinematic character mover: sweeps the capsule with casts, stops at a skin gap, never penetrates.
    /// Position is tracked internally (MovePosition is deferred) so several moves per step stay consistent.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class KinematicMotor2D : MonoBehaviour
    {
        const float MinMove = 1e-5f;
        const float ProbeDistance = 0.05f;
        const float CornerStep = 0.05f;

        [SerializeField] float skinWidth = 0.02f;
        [Tooltip("Layers that block the motor. Empty = the Ground layer (terrain only; enemies and triggers never block).")]
        [SerializeField] LayerMask collisionMask;
        [SerializeField] Vector2 standingSize = new Vector2(0.8f, 1.9f);
        [SerializeField] float crouchHeight = 0.9f;

        readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
        readonly Collider2D[] _overlaps = new Collider2D[8];
        ContactFilter2D _filter;
        Rigidbody2D _body;
        CapsuleCollider2D _capsule;
        Vector2 _position;
        bool _initialized;

        public Vector2 Position => _position;
        public bool IsCrouched { get; private set; }
        /// <summary>Max sideways nudge (units) used to slip past a ceiling corner when jumping.</summary>
        public float CornerCorrection { get; set; }

        void Awake() => Initialize();

        internal void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _body = GetComponent<Rigidbody2D>();
            _capsule = GetComponent<CapsuleCollider2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _capsule.direction = CapsuleDirection2D.Vertical;
            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(collisionMask.value != 0 ? collisionMask : (LayerMask)PhysicsLayers.GroundMask);
            ApplyColliderShape(false);
            _position = _body.position;
        }

        public void Teleport(Vector2 position)
        {
            _position = position;
            _body.position = position;
        }

        /// <summary>Moves by <paramref name="delta"/>, horizontal axis first, and reports what was hit.</summary>
        public MotorContacts Move(Vector2 delta)
        {
            var contacts = new MotorContacts();
            MoveAxis(new Vector2(Mathf.Sign(delta.x), 0f), Mathf.Abs(delta.x), ref contacts);
            MoveAxis(new Vector2(0f, Mathf.Sign(delta.y)), Mathf.Abs(delta.y), ref contacts);
            _body.MovePosition(_position);
            return contacts;
        }

        public bool CheckGround() => Probe(Vector2.down, ProbeDistance);

        public bool TouchingWall(int direction) => Probe(new Vector2(direction, 0f), skinWidth + ProbeDistance - 0.01f);

        public void SetCrouched(bool crouched)
        {
            IsCrouched = crouched;
            ApplyColliderShape(crouched);
        }

        /// <summary>True when the standing capsule would fit (always true while already standing).</summary>
        public bool CanStand()
        {
            if (!IsCrouched) return true;
            // Exact standing shape so rounded caps never underestimate corner overlap.
            int count = Physics2D.OverlapCapsule(_position, standingSize,
                CapsuleDirection2D.Vertical, 0f, _filter, _overlaps);
            for (int i = 0; i < count; i++)
            {
                var other = _overlaps[i];
                if (other != _capsule && !other.usedByEffector) return false;
            }
            return true;
        }

        /// <summary>Stands up if there is room; otherwise stays crouched and returns false.</summary>
        public bool TrySetStanding()
        {
            if (!CanStand()) return false;
            SetCrouched(false);
            return true;
        }

        void ApplyColliderShape(bool crouched)
        {
            float height = crouched ? crouchHeight : standingSize.y;
            _capsule.size = new Vector2(standingSize.x, height);
            _capsule.offset = new Vector2(0f, (height - standingSize.y) * 0.5f);
        }

        void MoveAxis(Vector2 direction, float distance, ref MotorContacts contacts)
        {
            if (distance < MinMove) return;
            float travel = distance;
            if (Cast(_position, direction, distance + skinWidth, out var hit))
            {
                travel = Mathf.Max(0f, hit.distance - skinWidth);
                if (direction.y > 0f && TryCornerCorrect(distance)) return;
                Flag(direction, ref contacts);
            }
            _position += direction * travel;
        }

        static void Flag(Vector2 direction, ref MotorContacts contacts)
        {
            if (direction.y > 0f) contacts.Above = true;
            else if (direction.y < 0f) contacts.Below = true;
            else if (direction.x > 0f) contacts.Right = true;
            else contacts.Left = true;
        }

        /// <summary>When bonking a ceiling corner, shifts sideways (up to CornerCorrection) and continues upward.</summary>
        bool TryCornerCorrect(float distance)
        {
            for (float offset = CornerStep; offset <= CornerCorrection + 1e-4f; offset += CornerStep)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var sideways = new Vector2(side, 0f);
                    if (Cast(_position, sideways, offset + skinWidth, out _)) continue;
                    var shifted = _position + sideways * offset;
                    if (Cast(shifted, Vector2.up, distance + skinWidth, out _)) continue;
                    _position = shifted + Vector2.up * distance;
                    return true;
                }
            }
            return false;
        }

        bool Probe(Vector2 direction, float distance) => Cast(_position, direction, distance, out _);

        bool Cast(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D nearest)
        {
            nearest = default;
            int count = Physics2D.CapsuleCast(origin + _capsule.offset, _capsule.size, CapsuleDirection2D.Vertical, 0f,
                direction, _filter, _hits, distance);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider == _capsule || !Blocks(hit, direction)) continue;
                if (hit.distance >= best) continue;
                best = hit.distance;
                nearest = hit;
                found = true;
            }
            return found;
        }

        static bool Blocks(RaycastHit2D hit, Vector2 direction)
        {
            if (Vector2.Dot(hit.normal, direction) > -0.5f && hit.distance > 0f) return false;
            if (!hit.collider.usedByEffector) return true;
            // One-way platform: solid only when landing on its top face from above.
            return direction.y < 0f && hit.distance > 0f && hit.normal.y > 0.5f;
        }
    }
}
