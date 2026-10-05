using System;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>The objects an Aura-driven gate toggles: blockers/hazards go off when it opens, passages/effects come on.</summary>
    [Serializable]
    public sealed class GateSwitches
    {
        [Tooltip("Blockers / hazards switched off when the gate opens.")]
        [SerializeField] GameObject[] deactivateOnOpen = Array.Empty<GameObject>();
        [Tooltip("Objects switched on when the gate opens (passages, effects).")]
        [SerializeField] GameObject[] activateOnOpen = Array.Empty<GameObject>();

        public void Apply(bool open)
        {
            foreach (var go in deactivateOnOpen) if (go != null) go.SetActive(!open);
            foreach (var go in activateOnOpen) if (go != null) go.SetActive(open);
        }
    }
}
