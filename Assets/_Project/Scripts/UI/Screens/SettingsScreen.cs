using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Settings: music/SFX volume, haptics, button size and opacity (applied live to the virtual controls through
    /// SettingsChanged), power saving and language. Everything persists through <see cref="GameSettings"/> (PlayerPrefs).
    /// </summary>
    public sealed class SettingsScreen : UIScreen
    {
        [Serializable]
        public struct SliderRow
        {
            public Slider slider;
            public ThemedText value;
        }

        [SerializeField] UIRouter router;
        [SerializeField] SliderRow music, sfx, buttonScale, buttonOpacity;
        [SerializeField] UIButton hapticsButton, powerSavingButton, languageButton, backButton;
        [SerializeField] ThemedText hapticsValue, powerSavingValue, languageValue;

        public Slider MusicSlider => music.slider;
        public Slider ButtonScaleSlider => buttonScale.slider;

        public void Bind(UIRouter uiRouter, SliderRow musicRow, SliderRow sfxRow, SliderRow scaleRow, SliderRow opacityRow,
            UIButton haptics, ThemedText hapticsText, UIButton power, ThemedText powerText, UIButton language, ThemedText languageText, UIButton back)
        {
            router = uiRouter;
            music = musicRow;
            sfx = sfxRow;
            buttonScale = scaleRow;
            buttonOpacity = opacityRow;
            hapticsButton = haptics;
            hapticsValue = hapticsText;
            powerSavingButton = power;
            powerSavingValue = powerText;
            languageButton = language;
            languageValue = languageText;
            backButton = back;
        }

        protected override void Awake()
        {
            base.Awake();
            music.slider.onValueChanged.AddListener(v => { GameSettings.MusicVolume = v; ShowPercent(music); });
            sfx.slider.onValueChanged.AddListener(v => { GameSettings.SfxVolume = v; ShowPercent(sfx); });
            buttonScale.slider.onValueChanged.AddListener(v => { GameSettings.ButtonScale = v; ShowPercent(buttonScale); });
            buttonOpacity.slider.onValueChanged.AddListener(v => { GameSettings.ButtonOpacity = v; ShowPercent(buttonOpacity); });
            hapticsButton.onClick.AddListener(() => { GameSettings.HapticsEnabled = !GameSettings.HapticsEnabled; RefreshToggles(); });
            powerSavingButton.onClick.AddListener(() => { GameSettings.PowerSaving = !GameSettings.PowerSaving; RefreshToggles(); });
            languageButton.onClick.AddListener(CycleLanguage);
            backButton.onClick.AddListener(() => router.Pop());
        }

        protected override void OnShowing()
        {
            music.slider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfx.slider.SetValueWithoutNotify(GameSettings.SfxVolume);
            buttonScale.slider.SetValueWithoutNotify(GameSettings.ButtonScale);
            buttonOpacity.slider.SetValueWithoutNotify(GameSettings.ButtonOpacity);
            ShowPercent(music);
            ShowPercent(sfx);
            ShowPercent(buttonScale);
            ShowPercent(buttonOpacity);
            RefreshToggles();
        }

        void RefreshToggles()
        {
            hapticsValue.SetKey(GameSettings.HapticsEnabled ? "settings.on" : "settings.off");
            powerSavingValue.SetKey(GameSettings.PowerSaving ? "settings.on" : "settings.off");
            languageValue.SetKey("lang." + GameSettings.Language);
        }

        void CycleLanguage()
        {
            var languages = Localization.SupportedLanguages;
            int index = Array.IndexOf(languages, GameSettings.Language);
            GameSettings.Language = languages[(index + 1) % languages.Length];
            RefreshToggles();
        }

        static void ShowPercent(SliderRow row) => row.value.SetText($"{Mathf.RoundToInt(row.slider.value * 100f)}%");
    }
}
