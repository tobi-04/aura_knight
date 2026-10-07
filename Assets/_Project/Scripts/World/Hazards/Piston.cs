using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.World.Hazards
{
    /// <summary>
    /// Piston hanging from the ceiling: the head child slides down by <see cref="travel"/> following a <see cref="PistonCycle"/>
    /// (shake telegraph, slam, retract). The head's Hazard hitbox is armed only while the head is mostly out.
    /// </summary>
    public sealed class Piston : MonoBehaviour
    {
        const float ShakeAmplitude = 0.05f;

        [SerializeField] Transform head;
        [SerializeField] Hitbox hitbox;
        [SerializeField, Min(0.5f)] float travel = 4f;
        [Tooltip("Seconds added to the cycle clock so neighbouring pistons do not slam together.")]
        [SerializeField] float startOffset;

        PistonCycle _cycle;
        Vector3 _home;

        public PistonCycle Cycle => _cycle ??= new PistonCycle(startOffset: startOffset);

        void Awake()
        {
            if (head != null) _home = head.localPosition;
        }

        void OnEnable()
        {
            _cycle = null; // a restarted room starts the pattern over
            if (hitbox != null) hitbox.Deactivate();
        }

        void FixedUpdate() => Advance(Time.fixedDeltaTime);

        internal void Advance(float deltaTime)
        {
            if (head == null) return;
            Cycle.Tick(deltaTime);
            float shake = Cycle.Phase == PistonPhase.Telegraph ? Mathf.Sin(Time.time * 110f) * ShakeAmplitude : 0f;
            head.localPosition = _home + new Vector3(shake, -travel * Cycle.Extension, 0f);
            if (hitbox == null) return;
            if (Cycle.IsDangerous && !hitbox.IsActive) hitbox.Activate(hitbox.Damage);
            else if (!Cycle.IsDangerous && hitbox.IsActive) hitbox.Deactivate();
        }
    }
}
