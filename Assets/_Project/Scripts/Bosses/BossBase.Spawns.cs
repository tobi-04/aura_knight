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
            if (_spawned.Count >= 48) _spawned.RemoveAll(go => go == null);
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
                go.SetActive(false); // inert right now; Destroy completes at the end of the frame
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _spawned.Clear();
        }
    }
}
