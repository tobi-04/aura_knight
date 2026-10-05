using System;

namespace AuraKnight.Aura
{
    /// <summary>The Auras Leo can wear. <see cref="None"/> is the grey, always-available starting state.</summary>
    public enum AuraId
    {
        None,
        Wind,
        Fire,
        Water
    }

    /// <summary>Conversions between <see cref="AuraId"/> and the string ids used by save files and EventBus events.</summary>
    public static class AuraIds
    {
        static readonly string[] Keys = Enum.GetNames(typeof(AuraId));

        public static readonly AuraId[] All = (AuraId[])Enum.GetValues(typeof(AuraId));

        /// <summary>Enum name ("Wind"); cached so switching Auras never allocates.</summary>
        public static string ToKey(AuraId id) => Keys[(int)id];

        /// <summary>Parses a save/event id. Unknown or empty strings return false.</summary>
        public static bool TryParse(string key, out AuraId id)
        {
            id = AuraId.None;
            if (string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != key) continue;
                id = (AuraId)i;
                return true;
            }
            return false;
        }

        /// <summary>Parse that falls back to <see cref="AuraId.None"/>.</summary>
        public static AuraId ParseOrNone(string key) => TryParse(key, out var id) ? id : AuraId.None;
    }
}
