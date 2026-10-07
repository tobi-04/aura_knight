using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World.Hazards
{
    /// <summary>
    /// Solid platform that falls away 0.6 s after Leo steps on it and returns 3 s later (<see cref="CollapseCycle"/>).
    /// The root carries a trigger sensor just above the top face; <see cref="body"/> is the solid collider + sprite that is
    /// switched off while collapsed. A return is held back while Leo overlaps the body so he is never re-solidified around.
    /// </summary>
    public sealed class CollapsingPlatform : MonoBehaviour
    {
        const float ShakeAmplitude = 0.04f;

        [SerializeField] GameObject body;
        [SerializeField, Min(0.05f)] float delay = CollapseCycle.DefaultDelay;
        [SerializeField, Min(0.05f)] float respawnSeconds = CollapseCycle.DefaultRespawn;

        CollapseCycle _cycle;
        Vector3 _bodyHome;
        bool _waitingForRoom;

        public CollapsePhase Phase => Cycle.Phase;
        public GameObject Body => body;

        CollapseCycle Cycle => _cycle ??= new CollapseCycle(delay, respawnSeconds);

        void Awake()
        {
            if (body != null) _bodyHome = body.transform.localPosition;
        }

        void OnEnable() => Restore();

        void OnTriggerEnter2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) Step();
        }

        /// <summary>The platform was stepped on (also callable from tests).</summary>
        public void Step() => Cycle.Trigger();

        void Update() => Advance(Time.deltaTime);

        internal void Advance(float deltaTime)
        {
            if (body == null) return;
            if (_waitingForRoom)
            {
                if (!PlayerInside()) Restore();
                return;
            }
            bool changed = Cycle.Tick(deltaTime);
            if (Cycle.Phase == CollapsePhase.Shaking)
                body.transform.localPosition = _bodyHome + new Vector3(Mathf.Sin(Cycle.Elapsed * 90f) * ShakeAmplitude, 0f, 0f);
            if (!changed) return;
            if (Cycle.Phase == CollapsePhase.Collapsed) body.SetActive(false);
            else if (Cycle.Phase == CollapsePhase.Solid)
            {
                if (PlayerInside()) _waitingForRoom = true; // stay gone until Leo has moved off the spot
                else Restore();
            }
        }

        void Restore()
        {
            _waitingForRoom = false;
            Cycle.Reset();
            if (body == null) return;
            body.transform.localPosition = _bodyHome;
            body.SetActive(true);
        }

        bool PlayerInside()
        {
            if (!body.TryGetComponent<BoxCollider2D>(out var box)) return false;
            var size = Vector2.Scale(box.size, body.transform.lossyScale);
            var center = body.transform.TransformPoint(box.offset);
            return Physics2D.OverlapBox(center, size, 0f, PhysicsLayers.Mask(PhysicsLayers.Player)) != null;
        }
    }
}
