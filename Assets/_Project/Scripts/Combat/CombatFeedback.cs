using AuraKnight.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>
    /// Juice for combat: hit stop, haptics and a Cinemachine impulse (camera shake). Every piece is optional;
    /// the impulse needs a <see cref="CinemachineImpulseSource"/> here and a CinemachineImpulseListener on the camera.
    /// </summary>
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] CinemachineImpulseSource impulse;
        [SerializeField, Min(0f)] float hitStopSeconds = HitStop.DefaultDuration;
        [SerializeField, Min(0f)] float landedHitShake = 0.15f;
        [SerializeField, Min(0f)] float damagedShake = 0.5f;

        void Awake()
        {
            if (impulse == null) impulse = GetComponent<CinemachineImpulseSource>();
        }

        /// <summary>The player's sword connected.</summary>
        public void LandedHit() => Play(landedHitShake);

        /// <summary>The player was hurt.</summary>
        public void TookHit() => Play(damagedShake);

        void Play(float shake)
        {
            HitStop.Request(hitStopSeconds);
            Haptics.Pulse();
            if (impulse != null && shake > 0f) impulse.GenerateImpulseWithForce(shake);
        }
    }
}
