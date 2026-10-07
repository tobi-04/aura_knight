using TMPro;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// The one place for design tokens (GDD 9): colours, fonts, accent bar width and motion constants. Every themed
    /// component reads <see cref="Active"/>, so a change here restyles every screen. The asset lives in
    /// Data/UI/Resources/UITheme.asset (Resources so builders that cannot reference assets still find it).
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Aura Knight/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        public const string ResourceName = "UITheme";

        [Header("Colours (GDD 9.1)")]
        [SerializeField] Color night = new Color32(0x07, 0x0D, 0x1F, 0xFF);
        [SerializeField] Color panel = new Color32(0x0E, 0x1A, 0x2C, 0xFF);
        [SerializeField] Color paper = new Color32(0xF1, 0xF0, 0xE5, 0xFF);
        [SerializeField] Color paperAlt = new Color32(0xD9, 0xD3, 0xC7, 0xFF);
        [SerializeField] Color gold = new Color32(0xFF, 0xC8, 0x57, 0xFF);
        [SerializeField] Color wind = new Color32(0x27, 0xD3, 0x8C, 0xFF);
        [SerializeField] Color fire = new Color32(0xFF, 0x5C, 0x57, 0xFF);
        [SerializeField] Color water = new Color32(0x27, 0xB5, 0xF7, 0xFF);
        [SerializeField] Color textPrimary = Color.white;
        [SerializeField] Color textMuted = new Color32(0x8A, 0x93, 0xA6, 0xFF);
        [SerializeField] Color textInk = new Color32(0x0C, 0x18, 0x24, 0xFF);

        [Header("Fonts (TMP dynamic assets)")]
        [SerializeField] TMP_FontAsset display;
        [SerializeField] TMP_FontAsset mono;
        [SerializeField] TMP_FontAsset monoRegular;
        [SerializeField] TMP_FontAsset body;
        [SerializeField] TMP_FontAsset number;

        [Header("Layout and motion")]
        [Min(1f)] [SerializeField] float accentBarWidth = 6f;
        [Min(1f)] [SerializeField] float cardBorderWidth = 4f;
        [Tooltip("TMP character spacing for Mono text (1 unit = 1/100 em, so 15 = +15%).")]
        [SerializeField] float monoSpacing = 15f;
        [Min(0.01f)] [SerializeField] float transitionSeconds = 0.2f;
        [Min(0f)] [SerializeField] float slidePixels = 16f;
        [Range(0.5f, 1f)] [SerializeField] float pressScale = 0.95f;

        static UITheme active;

        public float AccentBarWidth => accentBarWidth;
        public float CardBorderWidth => cardBorderWidth;
        public float MonoSpacing => monoSpacing;
        public float TransitionSeconds => transitionSeconds;
        public float SlidePixels => slidePixels;
        public float PressScale => pressScale;

        /// <summary>The theme asset from Resources; a default-coloured, font-less instance when the asset is missing.</summary>
        public static UITheme Active
        {
            get
            {
                if (active != null) return active;
                active = Resources.Load<UITheme>(ResourceName);
                if (active != null) return active;
                Debug.LogWarning("[UITheme] Resources/UITheme.asset not found; using built-in default colours and no fonts.");
                active = CreateInstance<UITheme>();
                active.hideFlags = HideFlags.HideAndDontSave;
                return active;
            }
        }

        public Color GetColor(UIColorToken token)
        {
            switch (token)
            {
                case UIColorToken.Night: return night;
                case UIColorToken.Panel: return panel;
                case UIColorToken.Paper: return paper;
                case UIColorToken.PaperAlt: return paperAlt;
                case UIColorToken.Gold: return gold;
                case UIColorToken.Wind: return wind;
                case UIColorToken.Fire: return fire;
                case UIColorToken.Water: return water;
                case UIColorToken.TextPrimary: return textPrimary;
                case UIColorToken.TextMuted: return textMuted;
                default: return textInk;
            }
        }

        public TMP_FontAsset GetFont(UIFontRole role)
        {
            switch (role)
            {
                case UIFontRole.Display: return display;
                case UIFontRole.Mono: return mono;
                case UIFontRole.MonoRegular: return monoRegular;
                case UIFontRole.Number: return number;
                default: return body;
            }
        }

        /// <summary>Token for an Aura id ("Wind", "Fire", "Water"); anything else (None) is the muted grey.</summary>
        public static UIColorToken AuraToken(string auraId)
        {
            switch (auraId)
            {
                case "Wind": return UIColorToken.Wind;
                case "Fire": return UIColorToken.Fire;
                case "Water": return UIColorToken.Water;
                default: return UIColorToken.TextMuted;
            }
        }

        /// <summary>Region colour (GDD 9.1): forest = wind, cave = gold, city = water, castle = fire; others gold.</summary>
        public static UIColorToken RegionToken(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return UIColorToken.Gold;
            string id = regionId.ToLowerInvariant();
            if (id.Contains("forest")) return UIColorToken.Wind;
            if (id.Contains("city")) return UIColorToken.Water;
            if (id.Contains("castle")) return UIColorToken.Fire;
            return UIColorToken.Gold;
        }

        /// <summary>Energy bar colour: the Aura colour, or pale gold before the first Aura (GDD 6: Leo glows light gold).</summary>
        public Color EnergyColor(string auraId) =>
            AuraToken(auraId) == UIColorToken.TextMuted ? Color.Lerp(gold, Color.white, 0.35f) : AuraColor(auraId);

        public Color AuraColor(string auraId) => GetColor(AuraToken(auraId));
        public Color RegionColor(string regionId) => GetColor(RegionToken(regionId));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = null;
    }
}
