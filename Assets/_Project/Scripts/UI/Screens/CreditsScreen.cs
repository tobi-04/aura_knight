using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Credits on the light paper background (slide 12 style). The text is the build-time aggregation of every
    /// LICENSES.md (Resources/CreditsText). In ending mode it is the end of the game: closing returns to the main menu.
    /// </summary>
    public sealed class CreditsScreen : UIScreen
    {
        public const string ResourceName = "CreditsText";
        const float AutoScrollPixelsPerSecond = 90f;

        [SerializeField] UIRouter router;
        [SerializeField] ScrollRect scroll;
        [SerializeField] ThemedText title;
        [SerializeField] ThemedText body;
        [SerializeField] CreditsDragProbe drag;
        [SerializeField] UIButton closeButton;
        [SerializeField] bool endingMode;

        public bool EndingMode => endingMode;
        public string BodyText => body != null ? body.Text.text : string.Empty;

        public void Bind(UIRouter uiRouter, ScrollRect scrollRect, ThemedText titleText, ThemedText bodyText,
            CreditsDragProbe dragProbe, UIButton close, bool ending)
        {
            router = uiRouter;
            scroll = scrollRect;
            title = titleText;
            body = bodyText;
            drag = dragProbe;
            closeButton = close;
            endingMode = ending;
        }

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }

        protected override void OnShowing()
        {
            title.SetKey(endingMode ? "credits.title_ending" : "credits.title");
            var asset = Resources.Load<TextAsset>(ResourceName);
            body.SetText(asset != null ? asset.text : Localization.Get("credits.missing"));
            scroll.verticalNormalizedPosition = 1f;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible || drag.Dragging || scroll.content == null) return;
            float travel = scroll.content.rect.height - scroll.viewport.rect.height;
            if (travel <= 0f) return;
            var position = scroll.content.anchoredPosition;
            position.y = Mathf.Min(travel, position.y + AutoScrollPixelsPerSecond * Time.unscaledDeltaTime);
            scroll.content.anchoredPosition = position;
        }

        void Close()
        {
            if (endingMode) GameLauncher.ReturnToMenu();
            else router.Remove(this);
        }

        public override bool HandleBack()
        {
            if (!endingMode) return false;
            Close();
            return true;
        }
    }
}
