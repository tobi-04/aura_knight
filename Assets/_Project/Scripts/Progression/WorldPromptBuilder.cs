using AuraKnight.UI;
using TMPro;
using UnityEngine;

namespace AuraKnight.Progression
{
    /// <summary>Builds the small world-space prompt text above an interactable ("VUỐT LÊN ĐỂ NÓI CHUYỆN"). Runtime code so editor generators and tests share it.</summary>
    public static class WorldPromptBuilder
    {
        /// <summary>Creates an inactive child with a themed mono gold <see cref="TextMeshPro"/>; the owner activates it while the player is near.</summary>
        public static GameObject Create(Transform parent, string locKey, Vector3 localPosition)
        {
            var go = new GameObject("Prompt");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var tmp = go.AddComponent<TextMeshPro>();
            var font = UITheme.Active.GetFont(UIFontRole.Mono);
            if (font != null) tmp.font = font;
            tmp.fontSize = 5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(12f, 1f);
            tmp.sortingOrder = 20;
            go.AddComponent<ThemedText>().Configure(UIFontRole.Mono, UIColorToken.Gold, locKey);
            return go;
        }
    }
}
