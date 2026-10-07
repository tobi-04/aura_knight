using AuraKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>Aura info (opened from Pause): three tabs Gió / Hỏa / Thủy over the shared <see cref="AuraPanelView"/>.</summary>
    public sealed class AuraInfoScreen : UIScreen
    {
        static readonly string[] Ids = { "Wind", "Fire", "Water" };

        [SerializeField] UIRouter router;
        [SerializeField] AuraPanelView panel;
        [SerializeField] UIButton[] tabs = new UIButton[3];
        [SerializeField] Image[] tabBars = new Image[3];
        [SerializeField] UIButton backButton;

        public int SelectedIndex { get; private set; } = -1;
        public AuraPanelView Panel => panel;

        public void Bind(UIRouter uiRouter, AuraPanelView panelView, UIButton[] tabButtons, Image[] bars, UIButton back)
        {
            router = uiRouter;
            panel = panelView;
            tabs = tabButtons;
            tabBars = bars;
            backButton = back;
        }

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].onClick.AddListener(() => Select(index));
            }
            backButton.onClick.AddListener(() => router.Pop());
        }

        protected override void OnShowing()
        {
            var state = GameManager.Instance != null ? GameManager.Instance.State : null;
            int start = state != null ? System.Array.IndexOf(Ids, state.currentAura) : -1;
            Select(start >= 0 ? start : 0);
        }

        public void Select(int index)
        {
            index = Mathf.Clamp(index, 0, Ids.Length - 1);
            SelectedIndex = index;
            var state = GameManager.Instance != null ? GameManager.Instance.State : null;
            bool unlocked = state != null && state.unlockedAuras.Contains(Ids[index]);
            panel.Present(Ids[index], unlocked);
            var theme = UITheme.Active;
            for (int i = 0; i < tabBars.Length; i++)
                tabBars[i].color = i == index ? theme.AuraColor(Ids[i]) : theme.GetColor(UIColorToken.TextMuted);
        }
    }
}
