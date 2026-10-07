using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Every visible string goes through here. Tables load from <c>Resources/Strings_{code}</c> (Data/UI/Resources).
    /// Vietnamese is the default; English (P2) only needs a <c>Strings_en.json</c> and its code added to
    /// <see cref="SupportedLanguages"/>. A missing key shows as <c>!key!</c> and logs once, so gaps are visible, never blank.
    /// </summary>
    public static class Localization
    {
        public const string DefaultLanguage = "vi";
        public static readonly string[] SupportedLanguages = { "vi" };

        static StringTable table;
        static string loadedCode;
        static readonly HashSet<string> Reported = new();

        public static event Action LanguageChanged;

        public static string CurrentLanguage => loadedCode ?? DefaultLanguage;

        public static bool IsSupported(string code) => Array.IndexOf(SupportedLanguages, code) >= 0;

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            Ensure();
            if (table.TryGet(key, out var value)) return value;
            if (Reported.Add(key)) Debug.LogWarning($"[Localization] Missing string key '{key}' ({CurrentLanguage}).");
            return $"!{key}!";
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            try { return string.Format(pattern, args); }
            catch (FormatException) { return pattern; }
        }

        public static bool Has(string key)
        {
            Ensure();
            return table.TryGet(key, out _);
        }

        public static void SetLanguage(string code)
        {
            if (!IsSupported(code)) code = DefaultLanguage;
            if (table != null && code == loadedCode) return;
            Load(code);
            LanguageChanged?.Invoke();
        }

        /// <summary>Re-reads the table of the stored language from Resources (drops anything installed with <see cref="UseTable"/>).</summary>
        public static void Reload()
        {
            Load(GameSettings.Language);
            LanguageChanged?.Invoke();
        }

        /// <summary>Installs a table directly (tests, tools).</summary>
        public static void UseTable(StringTable newTable, string code)
        {
            table = newTable ?? new StringTable();
            loadedCode = code;
            LanguageChanged?.Invoke();
        }

        static void Ensure()
        {
            if (table == null) Load(GameSettings.Language);
        }

        static void Load(string code)
        {
            var asset = Resources.Load<TextAsset>("Strings_" + code);
            if (asset != null && StringTable.TryParse(asset.text, out var parsed))
            {
                table = parsed;
                loadedCode = code;
                return;
            }
            Debug.LogError($"[Localization] Could not load Resources/Strings_{code}.json; strings will show as !key!.");
            table = new StringTable();
            loadedCode = code;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            table = null;
            loadedCode = null;
            Reported.Clear();
            LanguageChanged = null;
        }
    }
}
