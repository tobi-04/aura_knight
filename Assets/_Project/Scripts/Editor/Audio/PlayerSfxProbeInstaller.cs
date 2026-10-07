using AuraKnight.Audio;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Adds <see cref="PlayerSfxProbe"/> to Prefabs/Player/Player.prefab when it is missing and changes nothing else.
    /// Idempotent. Call <see cref="Apply"/> after the Player prefab generator has rewritten the prefab (the generator rebuilds it).
    /// </summary>
    public static class PlayerSfxProbeInstaller
    {
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        [MenuItem("Aura/Audio/Add Sfx Probe To Player Prefab")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                if (root.GetComponent<PlayerSfxProbe>() != null)
                {
                    Debug.Log("[PlayerSfxProbeInstaller] Player prefab already has PlayerSfxProbe.");
                    return;
                }
                root.AddComponent<PlayerSfxProbe>();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("[PlayerSfxProbeInstaller] Added PlayerSfxProbe to the Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
