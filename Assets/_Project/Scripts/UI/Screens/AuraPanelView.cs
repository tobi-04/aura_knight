using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// The slide 8-10 layout for one Aura: colour label, big name, four numbered abilities and the key art.
    /// Shared by the Aura info screen (three tabs) and the Aura unlock popup.
    /// </summary>
    public sealed class AuraPanelView : MonoBehaviour
    {
        public const int SkillCount = 4;
        static readonly string[] Ids = { "Wind", "Fire", "Water" };

        [SerializeField] ThemedText tagText;
        [SerializeField] ThemedText title;
        [SerializeField] ThemedAccentBar bar;
        [SerializeField] ThemedText[] numbers = new ThemedText[SkillCount];
        [SerializeField] ThemedText[] skills = new ThemedText[SkillCount];
        [SerializeField] RawImage art;
        [SerializeField] Texture2D[] arts = new Texture2D[3];

        public string AuraId { get; private set; }
        public bool ShowingLocked { get; private set; }
        public string TitleText => title.Text.text;

        public void Bind(ThemedText tagLabel, ThemedText titleText, ThemedAccentBar accent, ThemedText[] numberTexts,
            ThemedText[] skillTexts, RawImage artImage, Texture2D[] artTextures)
        {
            tagText = tagLabel;
            title = titleText;
            bar = accent;
            numbers = numberTexts;
            skills = skillTexts;
            art = artImage;
            arts = artTextures;
        }

        /// <summary>Fills the panel. A locked Aura shows "???" and the hint instead of its abilities.</summary>
        public void Present(string auraId, bool unlocked)
        {
            AuraId = auraId;
            ShowingLocked = !unlocked;
            string prefix = "aura." + auraId.ToLowerInvariant() + ".";
            var color = UITheme.Active.AuraColor(auraId);
            tagText.SetKey(unlocked ? prefix + "tag" : "aura.locked");
            tagText.SetColor(color);
            title.SetKey(unlocked ? prefix + "name" : "aura.unknown");
            bar.SetColor(color);
            for (int i = 0; i < SkillCount; i++)
            {
                numbers[i].gameObject.SetActive(unlocked);
                skills[i].gameObject.SetActive(unlocked);
                if (!unlocked) continue;
                numbers[i].SetText($"{i + 1:00}");
                numbers[i].SetColor(color);
                skills[i].SetKey(prefix + "s" + (i + 1));
            }
            int index = System.Array.IndexOf(Ids, auraId);
            if (art == null) return;
            art.texture = index >= 0 && index < arts.Length ? arts[index] : null;
            art.color = unlocked ? Color.white : new Color(0.25f, 0.25f, 0.3f, 1f);
            if (art.TryGetComponent<CoverFit>(out var fit)) fit.Apply();
        }
    }
}
