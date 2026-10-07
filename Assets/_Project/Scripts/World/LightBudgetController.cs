using System.Collections.Generic;
using AuraKnight.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.World
{
    /// <summary>
    /// Core-scene component that keeps the number of lit local Light2Ds within <see cref="LightBudget"/>. Twice a second it ranks every
    /// local light by distance to Leo (the camera when there is no Leo) and switches the far ones off. It only ever re-enables lights
    /// it switched off itself, so lights that other systems turned off stay off; Global lights (region lighting) are never touched.
    /// </summary>
    public sealed class LightBudgetController : MonoBehaviour
    {
        const float RefreshSeconds = 0.5f;

        readonly List<Light2D> _candidates = new List<Light2D>(32);
        readonly List<Vector2> _positions = new List<Vector2>(32);
        readonly List<bool> _allowed = new List<bool>(32);
        readonly HashSet<Light2D> _suppressed = new HashSet<Light2D>();
        float _next;

        /// <summary>Local lights lit right now (the ones the budget kept).</summary>
        public int LitCount { get; private set; }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + RefreshSeconds;
            Refresh();
        }

        void OnDisable() => RestoreAll();

        /// <summary>Re-ranks the lights now (the periodic update calls this; tests call it directly).</summary>
        public void Refresh()
        {
            Gather();
            Vector2 center = Center();
            for (int i = 0; i < _candidates.Count; i++) _positions.Add(_candidates[i].transform.position);
            while (_allowed.Count < _candidates.Count) _allowed.Add(false);
            LightBudget.Select(_positions, center, LightBudget.Limit(GameSettings.PowerSaving), _allowed);
            LitCount = 0;
            for (int i = 0; i < _candidates.Count; i++)
            {
                var light = _candidates[i];
                if (_allowed[i])
                {
                    if (_suppressed.Remove(light)) light.enabled = true;
                    LitCount++;
                }
                else if (light.enabled)
                {
                    light.enabled = false;
                    _suppressed.Add(light);
                }
            }
        }

        void Gather()
        {
            _candidates.Clear();
            _positions.Clear();
            _suppressed.RemoveWhere(light => light == null);
            foreach (var light in FindObjectsByType<Light2D>())
            {
                if (light.lightType == Light2D.LightType.Global) continue;
                if (light.enabled || _suppressed.Contains(light)) _candidates.Add(light);
            }
            foreach (var light in _suppressed)
                if (light.gameObject.activeInHierarchy && !_candidates.Contains(light)) _candidates.Add(light);
        }

        Vector2 Center()
        {
            var player = GameObject.FindGameObjectWithTag(WorldTags.Player);
            if (player != null) return player.transform.position;
            var camera = Camera.main;
            return camera != null ? (Vector2)camera.transform.position : Vector2.zero;
        }

        void RestoreAll()
        {
            foreach (var light in _suppressed)
                if (light != null) light.enabled = true;
            _suppressed.Clear();
        }
    }
}
