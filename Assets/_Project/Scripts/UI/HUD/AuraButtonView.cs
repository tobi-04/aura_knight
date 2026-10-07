using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Visual state of one on-screen Aura button (the OnScreenButton on the same object does the input):
    /// aura colour when unlocked, bright when current, grey with a padlock when locked.
    /// </summary>
    public sealed class AuraButtonView : MonoBehaviour
    {
        [SerializeField] string auraId;
        [SerializeField] Image background;
        [SerializeField] TMP_Text label;
        [SerializeField] GameObject padlock;

        public string AuraId => auraId;
        public bool Locked { get; private set; } = true;
        public bool Selected { get; private set; }
        public Color DisplayedColor => background != null ? background.color : Color.clear;
        public bool PadlockVisible => padlock != null && padlock.activeSelf;

        public void Bind(string id, Image back, TMP_Text text, GameObject lockIcon)
        {
            auraId = id;
            background = back;
            label = text;
            padlock = lockIcon;
        }

        public void Apply(bool unlocked, bool selected, float opacity)
        {
            Locked = !unlocked;
            Selected = unlocked && selected;
            var theme = UITheme.Active;
            var color = unlocked ? theme.AuraColor(auraId) : theme.GetColor(UIColorToken.TextMuted);
            color.a = Selected ? opacity : opacity * (unlocked ? 0.55f : 0.7f);
            if (background != null) background.color = color;
            if (label != null) label.gameObject.SetActive(unlocked);
            if (padlock != null) padlock.SetActive(!unlocked);
        }
    }
}
