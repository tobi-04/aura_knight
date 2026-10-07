using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Bosses
{
    public abstract partial class BossBase
    {
        readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>Registers an object the boss created (hazard, minion) so a reset or death removes it.</summary>
        public void Track(GameObject spawned)
        {
            if (_spawned.Count >= 48) _spawned.RemoveAll(go => go == null || !go.activeSelf); // released hazards sit inactive in the pool
            _spawned.Add(spawned);
        }

        /// <summary>Tracked objects still alive.</summary>
        public int SpawnedCount
        {
            get
            {
                int alive = 0;
                foreach (var go in _spawned)
                    if (go != null && go.activeInHierarchy) alive++;
                return alive;
            }
        }

        /// <summary>Removes every tracked hazard and minion immediately.</summary>
        public void ClearSpawned()
        {
            foreach (var go in _spawned)
            {
                if (go == null) continue;
                if (go.TryGetComponent<BossHazard>(out var hazard))
                {
                    if (hazard.Owner == this) hazard.Release(); // else it was recycled by another boss in the meantime // pooled (or destroyed when it is a marker); inert right now either way
                    continue;
                }
                go.SetActive(false); // inert right now; Destroy completes at the end of the frame
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _spawned.Clear();
        }
    }
}
