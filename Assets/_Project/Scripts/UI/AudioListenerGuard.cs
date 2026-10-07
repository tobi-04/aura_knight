using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// No scene of the project carries an AudioListener, so the game would be silent and Unity logs a warning every frame.
    /// Core's UI_Root keeps one alive: if none exists when the scene starts, one is added to the main camera
    /// (or to this object when there is no camera). Never adds a second listener.
    /// </summary>
    public sealed class AudioListenerGuard : MonoBehaviour
    {
        void Start()
        {
            if (FindAnyObjectByType<AudioListener>() != null) return;
            var camera = Camera.main;
            (camera != null ? camera.gameObject : gameObject).AddComponent<AudioListener>();
        }
    }
}
