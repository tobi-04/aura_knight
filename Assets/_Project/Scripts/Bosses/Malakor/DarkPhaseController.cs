using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Phase 3 of Malakor (P2 scope): when the boss enters the dark phase every Global Light 2D in the loaded scenes dims to
    /// <see cref="darkIntensity"/>, so only Leo's Aura glow lights the room. Lights are restored on reset, death or when the component is disabled.
    /// </summary>
    public sealed class DarkPhaseController : MonoBehaviour
    {
        [SerializeField] MalakorBoss boss;
        [SerializeField, Min(0)] int darkPhaseIndex = 2;
        [SerializeField, Range(0f, 1f)] float darkIntensity = 0.04f;

        readonly Dictionary<Light2D, float> _saved = new Dictionary<Light2D, float>();

        public bool IsDark => _saved.Count > 0;

        void OnEnable()
        {
            if (boss == null) boss = GetComponentInParent<MalakorBoss>(true);
            if (boss == null) return;
            boss.PhaseChanged += OnPhaseChanged;
            boss.Health.Changed += OnHealthChanged;
        }

        void OnDisable()
        {
            if (boss != null)
            {
                boss.PhaseChanged -= OnPhaseChanged;
                boss.Health.Changed -= OnHealthChanged;
            }
            Restore();
        }

        void OnPhaseChanged(int index)
        {
            if (index >= darkPhaseIndex) Darken();
        }

        // Reset refills the HP and dying empties it: both end the dark phase without needing a dedicated event.
        void OnHealthChanged(int current, int max)
        {
            if (current <= 0 || boss.State == BossState.Dormant) Restore();
        }

        void Darken()
        {
            if (IsDark) return;
            foreach (var light in FindObjectsByType<Light2D>())
            {
                if (light.lightType != Light2D.LightType.Global) continue;
                _saved[light] = light.intensity;
                light.intensity = darkIntensity;
            }
        }

        void Restore()
        {
            foreach (var pair in _saved)
                if (pair.Key != null) pair.Key.intensity = pair.Value;
            _saved.Clear();
        }
    }
}
