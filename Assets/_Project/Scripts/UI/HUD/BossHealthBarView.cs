using AuraKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Boss HP bar at the top of the screen. Appears on BossEncounterStarted, follows BossHealthChanged for that boss id,
    /// disappears on BossEncounterEnded. The bar takes the colour of the region the player is in (RoomEntered).
    /// </summary>
    public sealed class BossHealthBarView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Image fill;
        [SerializeField] ThemedText nameLabel;

        string regionId;

        public bool Visible { get; private set; }
        public string BossId { get; private set; }
        public float Fraction { get; private set; }

        public void Bind(CanvasGroup canvasGroup, Image fillImage, ThemedText label)
        {
            group = canvasGroup;
            fill = fillImage;
            nameLabel = label;
        }

        void OnEnable()
        {
            EventBus.Subscribe<BossEncounterStarted>(OnStarted);
            EventBus.Subscribe<BossEncounterEnded>(OnEnded);
            EventBus.Subscribe<BossHealthChanged>(OnHealth);
            EventBus.Subscribe<RoomEntered>(OnRoom);
            SetVisible(false);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<BossEncounterStarted>(OnStarted);
            EventBus.Unsubscribe<BossEncounterEnded>(OnEnded);
            EventBus.Unsubscribe<BossHealthChanged>(OnHealth);
            EventBus.Unsubscribe<RoomEntered>(OnRoom);
        }

        void OnRoom(RoomEntered e) => regionId = e.RegionId;

        void OnStarted(BossEncounterStarted e)
        {
            BossId = e.BossId;
            Fraction = 1f;
            if (nameLabel != null) nameLabel.SetText(Localization.Format("hud.boss_label", e.DisplayName));
            Paint();
            SetVisible(true);
        }

        void OnEnded(BossEncounterEnded e)
        {
            if (e.BossId == BossId) SetVisible(false);
        }

        void OnHealth(BossHealthChanged e)
        {
            if (!Visible || e.BossId != BossId) return;
            Fraction = e.Max > 0 ? Mathf.Clamp01((float)e.Current / e.Max) : 0f;
            Paint();
        }

        void Paint()
        {
            if (fill == null) return;
            fill.fillAmount = Fraction;
            fill.color = UITheme.Active.RegionColor(regionId);
        }

        void SetVisible(bool visible)
        {
            Visible = visible;
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
        }
    }
}
