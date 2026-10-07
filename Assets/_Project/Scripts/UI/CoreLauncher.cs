using System.Collections;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Lives in the Core scene. When the menu asked for a new game or a continue, starts the world entry once the
    /// managers exist; if that fails (no loadable save, entry busy) it goes back to the menu instead of leaving a dead scene.
    /// </summary>
    public sealed class CoreLauncher : MonoBehaviour
    {
        const float WaitSeconds = 5f;

        IEnumerator Start()
        {
            var request = GameLauncher.Consume();
            if (request == GameLauncher.Request.None) yield break;

            float deadline = Time.realtimeSinceStartup + WaitSeconds;
            while (WorldEntry.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            var entry = WorldEntry.Instance;
            bool started = entry != null &&
                (request == GameLauncher.Request.NewGame ? entry.StartNewGame() : entry.Continue());
            if (started) yield break;
            Debug.LogError($"[CoreLauncher] Could not start '{request}'; returning to the menu.");
            GameLauncher.ReturnToMenu();
        }
    }
}
