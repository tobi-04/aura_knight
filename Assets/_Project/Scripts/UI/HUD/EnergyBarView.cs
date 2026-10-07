using AuraKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>Energy bar coloured by the current Aura (AuraChanged); fill follows EnergyChanged.</summary>
    public sealed class EnergyBarView : MonoBehaviour
    {
        [SerializeField] Image fill;

        string auraId = GameState.NoAura;

        public float Fraction { get; private set; } = 1f;
        public Color FillColor => fill != null ? fill.color : Color.clear;

        public void Bind(Image fillImage) => fill = fillImage;

        void OnEnable()
        {
            EventBus.Subscribe<EnergyChanged>(OnEnergy);
            EventBus.Subscribe<AuraChanged>(OnAura);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            if (GameManager.Instance != null) auraId = GameManager.Instance.State.currentAura;
            Refresh();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<EnergyChanged>(OnEnergy);
            EventBus.Unsubscribe<AuraChanged>(OnAura);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnEnergy(EnergyChanged e)
        {
            Fraction = e.Max > 0f ? Mathf.Clamp01(e.Current / e.Max) : 0f;
            Refresh();
        }

        void OnAura(AuraChanged e)
        {
            auraId = e.AuraId;
            Refresh();
        }

        void OnStateLoaded(GameStateLoaded e)
        {
            if (GameManager.Instance != null) auraId = GameManager.Instance.State.currentAura;
            Refresh();
        }

        void Refresh()
        {
            if (fill == null) return;
            fill.fillAmount = Fraction;
            fill.color = UITheme.Active.EnergyColor(auraId);
        }
    }
}
