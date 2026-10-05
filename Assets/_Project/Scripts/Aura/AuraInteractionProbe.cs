using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>
    /// Circle query that offers an interaction to every <see cref="IAuraInteractable"/> in range.
    /// Shared by the fireball (small radius each step) and the water shield (proximity at cast time).
    /// Reuses its buffer, so it does not allocate per call.
    /// </summary>
    public sealed class AuraInteractionProbe
    {
        readonly List<Collider2D> _results = new List<Collider2D>(16);
        readonly List<IAuraInteractable> _seen = new List<IAuraInteractable>(8);
        // Built on first use: layer lookups are not allowed in MonoBehaviour field initializers (the owners create probes there).
        ContactFilter2D _interactFilter, _solidFilter;
        bool _filtersReady;

        void EnsureFilters()
        {
            if (_filtersReady) return;
            _interactFilter = MakeFilter(PhysicsLayers.ProbeMask);
            _solidFilter = MakeFilter(PhysicsLayers.GroundMask);
            _filtersReady = true;
        }

        static ContactFilter2D MakeFilter(int mask)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            if (mask != 0) filter.SetLayerMask(mask);
            return filter;
        }

        /// <summary>
        /// Returns how many distinct interactables accepted <paramref name="interaction"/>. Looks at Interactable triggers and the
        /// Ground blockers of gates; pass <paramref name="syncTransforms"/> false when the caller already synced this step.
        /// </summary>
        public int Apply(Vector2 center, float radius, AuraInteraction interaction, AuraId source, bool syncTransforms = true)
        {
            EnsureFilters();
            if (syncTransforms) Physics2D.SyncTransforms();
            Physics2D.OverlapCircle(center, radius, _interactFilter, _results);
            _seen.Clear();
            int accepted = 0;
            for (int i = 0; i < _results.Count; i++)
            {
                var collider = _results[i];
                if (collider == null) continue;
                var target = collider.GetComponentInParent<IAuraInteractable>();
                if (target == null || _seen.Contains(target)) continue;
                _seen.Add(target);
                if (target.TryInteract(interaction, source)) accepted++;
            }
            return accepted;
        }

        /// <summary>
        /// First solid Ground collider in range (projectile walls), else null. One-way platforms are ignored so projectiles
        /// fly through them.
        /// </summary>
        public Collider2D FindSolid(Vector2 center, float radius, bool syncTransforms = true)
        {
            EnsureFilters();
            if (syncTransforms) Physics2D.SyncTransforms();
            Physics2D.OverlapCircle(center, radius, _solidFilter, _results);
            for (int i = 0; i < _results.Count; i++)
            {
                var collider = _results[i];
                if (collider == null || collider.isTrigger || collider.usedByEffector) continue;
                if (collider.TryGetComponent<Combat.Hurtbox>(out _)) continue;
                return collider;
            }
            return null;
        }
    }
}
