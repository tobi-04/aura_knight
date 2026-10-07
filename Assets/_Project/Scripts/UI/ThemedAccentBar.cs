using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// The vertical (or horizontal) accent bar of GDD 9.3: colour from a token, thickness from the theme
    /// (6 px, or the card border 4 px). Thickness only applies on an axis whose anchors are not stretched.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ThemedAccentBar : MonoBehaviour
    {
        [SerializeField] UIColorToken token = UIColorToken.Gold;
        [SerializeField] bool horizontal;
        [SerializeField] bool cardBorder;

        public void Configure(UIColorToken colorToken, bool isHorizontal = false, bool useCardBorder = false)
        {
            token = colorToken;
            horizontal = isHorizontal;
            cardBorder = useCardBorder;
            Apply();
        }

        public void SetColor(UIColorToken colorToken)
        {
            token = colorToken;
            Apply();
        }

        public void SetColor(Color color) => GetComponent<Image>().color = color;

        void OnEnable() => Apply();

        public void Apply()
        {
            var theme = UITheme.Active;
            GetComponent<Image>().color = theme.GetColor(token);
            var rect = (RectTransform)transform;
            float thickness = cardBorder ? theme.CardBorderWidth : theme.AccentBarWidth;
            var size = rect.sizeDelta;
            if (horizontal && Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y)) size.y = thickness;
            else if (!horizontal && Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)) size.x = thickness;
            rect.sizeDelta = size;
        }
    }
}
