using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Flat themed button: tints its background by state and greys the label when disabled. The background colour comes
    /// from a ThemedImage; the tints multiply on top of it.
    /// </summary>
    public sealed class UIButton : Button
    {
        [SerializeField] ThemedText label;
        [SerializeField] UIColorToken enabledColor = UIColorToken.TextPrimary;

        public ThemedText Label => label;

        public void Bind(ThemedText text, Graphic background, UIColorToken labelColor = UIColorToken.TextPrimary)
        {
            label = text;
            enabledColor = labelColor;
            targetGraphic = background;
            transition = Transition.ColorTint;
            colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f),
                pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.45f, 0.45f, 0.45f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            navigation = new Navigation { mode = Navigation.Mode.None };
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (label == null) return;
            if (state == SelectionState.Disabled) label.SetColor(UIColorToken.TextMuted);
            else label.SetColor(enabledColor);
        }
    }
}
