using AuraKnight.Aura;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Water zone (trigger). While Leo is inside, <see cref="PlayerAuraBinder"/> either lets him swim (Water aura) or slows
    /// him down and starts the oxygen timer. Keep the collider tight: swimming is limited to this volume.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WaterVolume : MonoBehaviour
    {
        PlayerAuraBinder _binder;

        public bool PlayerInside => _binder != null;

        void Reset()
        {
            if (TryGetComponent<Collider2D>(out var trigger)) trigger.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_binder != null || !WorldTags.IsPlayer(other)) return;
            var binder = other.GetComponentInParent<PlayerAuraBinder>();
            if (binder == null) return;
            _binder = binder;
            _binder.EnterWater();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (_binder == null || !WorldTags.IsPlayer(other)) return;
            Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            if (_binder == null) return;
            _binder.ExitWater();
            _binder = null;
        }
    }
}
