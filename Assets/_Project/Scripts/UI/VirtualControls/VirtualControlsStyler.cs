using AuraKnight.Core;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Applies the personal button settings (size 80-130%, opacity 30-100%) to the on-screen buttons and reacts to
    /// SettingsChanged. Aura buttons keep their own colour logic (<see cref="AuraButtonView"/>) and only get the scale.
    /// </summary>
    public sealed class VirtualControlsStyler : MonoBehaviour
    {
        OnScreenButton[] buttons;

        void OnEnable()
        {
            buttons = GetComponentsInChildren<OnScreenButton>(true);
            EventBus.Subscribe<SettingsChanged>(OnSettings);
            Apply();
        }

        void OnDisable() => EventBus.Unsubscribe<SettingsChanged>(OnSettings);

        void OnSettings(SettingsChanged e)
        {
            if (e.Key == SettingsKeys.ButtonScale || e.Key == SettingsKeys.ButtonOpacity) Apply();
        }

        public void Apply()
        {
            float scale = GameSettings.ButtonScale;
            float opacity = GameSettings.ButtonOpacity;
            foreach (var button in buttons)
            {
                if (button == null) continue;
                button.transform.localScale = Vector3.one * scale;
                if (button.GetComponent<AuraButtonView>() != null) continue;
                var image = button.GetComponent<Image>();
                if (image == null) continue;
                var c = image.color;
                c.a = opacity;
                image.color = c;
            }
        }
    }
}
