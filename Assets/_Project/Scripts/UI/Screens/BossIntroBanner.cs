using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Slide-13 style horizontal bar: BOSS / NAME in the region colour; fades in, holds, fades out. Never pauses.</summary>
    public sealed class BossIntroBanner : UIScreen
    {
        [SerializeField] ThemedText label;
        [SerializeField] ThemedText nameText;
        [SerializeField] ThemedAccentBar bar;
        [Min(0.5f)] [SerializeField] float holdSeconds = 2.6f;

        float remaining;

        protected override bool BlocksTouches => false;

        public string DisplayedName => nameText != null ? nameText.Text.text : string.Empty;

        public void Bind(ThemedText boss, ThemedText bossName, ThemedAccentBar accent)
        {
            label = boss;
            nameText = bossName;
            bar = accent;
        }

        public void Present(string displayName, Color regionColor)
        {
            label.SetColor(regionColor);
            bar.SetColor(regionColor);
            nameText.SetText(displayName);
            remaining = holdSeconds;
            Show();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible) return;
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f) Hide();
        }
    }
}
