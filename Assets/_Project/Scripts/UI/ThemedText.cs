using TMPro;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Applies the theme (font, colour, mono tracking) to a TMP text and optionally fills it from a Localization key.
    /// Re-applies when the language changes. Use <see cref="SetKey"/>/<see cref="SetText"/> instead of writing TMP text
    /// directly so the string keeps flowing through Localization.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ThemedText : MonoBehaviour
    {
        [SerializeField] UIFontRole role = UIFontRole.Body;
        [SerializeField] UIColorToken color = UIColorToken.TextPrimary;
        [SerializeField] string locKey;
        [SerializeField] bool uppercase;

        TMP_Text cached;
        bool hasOverride;
        Color overrideColor;

        public TMP_Text Text => cached != null ? cached : (cached = GetComponent<TMP_Text>());
        public string LocKey => locKey;
        public UIFontRole Role => role;

        /// <summary>Sets role, colour and key in one go (used by the generators). Uppercase defaults on for Display and Mono.</summary>
        public void Configure(UIFontRole fontRole, UIColorToken colorToken, string key = null)
        {
            role = fontRole;
            color = colorToken;
            locKey = key;
            uppercase = fontRole == UIFontRole.Display || fontRole == UIFontRole.Mono;
            Apply();
        }

        void OnEnable()
        {
            Localization.LanguageChanged += Apply;
            Apply();
        }

        void OnDisable() => Localization.LanguageChanged -= Apply;

        public void SetKey(string key)
        {
            locKey = key;
            Apply();
        }

        /// <summary>Shows literal text (numbers, names from data); clears the localization key.</summary>
        public void SetText(string text)
        {
            locKey = null;
            Text.text = text;
        }

        /// <summary>Titles in the slides are mixed case ("Aura Gió"); only labels and big display words are UPPERCASE.</summary>
        public void SetUppercase(bool value)
        {
            uppercase = value;
            Apply();
        }

        public void SetColor(UIColorToken token)
        {
            color = token;
            hasOverride = false;
            Apply();
        }

        /// <summary>A runtime colour that is not a token (Aura/region colour from data).</summary>
        public void SetColor(Color value)
        {
            hasOverride = true;
            overrideColor = value;
            Text.color = value;
        }

        public void Apply()
        {
            var theme = UITheme.Active;
            var tmp = Text;
            var font = theme.GetFont(role);
            if (font != null && tmp.font != font) tmp.font = font;
            tmp.color = hasOverride ? overrideColor : theme.GetColor(color);
            tmp.characterSpacing = role == UIFontRole.Mono ? theme.MonoSpacing : 0f;
            tmp.fontStyle = uppercase ? tmp.fontStyle | FontStyles.UpperCase : tmp.fontStyle & ~FontStyles.UpperCase;
            if (!string.IsNullOrEmpty(locKey)) tmp.text = Localization.Get(locKey);
        }
    }
}
