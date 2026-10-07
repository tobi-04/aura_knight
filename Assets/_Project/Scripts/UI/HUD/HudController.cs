using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Shows the HUD and the virtual controls only while the game is Playing; hides them in menus, loading, cutscenes and
    /// pause (the pause/popup screens sit above on their own canvas). Uses CanvasGroups, never SetActive, so the
    /// on-screen controls keep their virtual gamepad alive. The HUD views listen to the EventBus themselves.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] CanvasGroup[] hud = new CanvasGroup[0];
        [SerializeField] CanvasGroup controls;

        public bool Visible { get; private set; }

        public void Bind(CanvasGroup[] hudGroups, CanvasGroup controlsGroup)
        {
            hud = hudGroups;
            controls = controlsGroup;
        }

        void OnEnable() => Apply(false);

        void Update()
        {
            var gm = GameManager.Instance;
            bool show = gm == null || gm.Mode == GameMode.Playing;
            if (show != Visible) Apply(show);
        }

        void Apply(bool show)
        {
            Visible = show;
            foreach (var group in hud) Set(group, show);
            Set(controls, show);
        }

        static void Set(CanvasGroup group, bool show)
        {
            if (group == null) return;
            group.alpha = show ? 1f : 0f;
            group.blocksRaycasts = show;
            group.interactable = show;
        }
    }
}
