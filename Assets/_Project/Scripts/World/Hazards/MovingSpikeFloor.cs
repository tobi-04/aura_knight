using UnityEngine;

namespace AuraKnight.World.Hazards
{
    /// <summary>
    /// A spiked slab (Castle) that slides back and forth along the floor around its start position. It has no body of its
    /// own: the Hazard hitbox on the child does the damage, Leo hops over it. Positions come from the physics clock so a
    /// restarted room begins at the same point of the cycle.
    /// </summary>
    public sealed class MovingSpikeFloor : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] float travel = 6f;
        [SerializeField, Min(0.1f)] float speed = 2.5f;

        Vector3 _home;
        float _clock;

        void Awake() => _home = transform.localPosition;

        void OnEnable()
        {
            _clock = 0f;
            Apply();
        }

        void FixedUpdate()
        {
            _clock += Time.fixedDeltaTime;
            Apply();
        }

        void Apply() => transform.localPosition = _home + new Vector3(PingPongPath.Offset(travel, speed, _clock), 0f, 0f);
    }
}
