using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Core
{
    /// <summary>Thin additive scene helpers; return null (and log) instead of throwing for scenes missing from the build.</summary>
    public static class SceneLoader
    {
        public static bool IsLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public static AsyncOperation LoadAdditive(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings.");
                return null;
            }
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        public static AsyncOperation Unload(string sceneName)
        {
            return IsLoaded(sceneName) ? SceneManager.UnloadSceneAsync(sceneName) : null;
        }
    }
}
