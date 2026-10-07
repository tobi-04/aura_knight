using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Key to text table parsed from a Strings_xx.json file: <c>{"entries":[{"key":"...","value":"..."}]}</c>.</summary>
    public sealed class StringTable
    {
        [Serializable] sealed class Dto { public List<Entry> entries; }
        [Serializable] struct Entry { public string key; public string value; }

        readonly Dictionary<string, string> map = new();
        readonly List<string> duplicates = new();

        public int Count => map.Count;
        public IReadOnlyCollection<string> Keys => map.Keys;
        /// <summary>Keys that appeared more than once (the last value wins); a content bug the tests flag.</summary>
        public IReadOnlyList<string> Duplicates => duplicates;

        public IEnumerable<string> Values => map.Values;

        public bool TryGet(string key, out string value)
        {
            if (key != null) return map.TryGetValue(key, out value);
            value = null;
            return false;
        }

        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (map.ContainsKey(key)) duplicates.Add(key);
            map[key] = value ?? string.Empty;
        }

        /// <summary>Parses JSON; false (and an empty table) on malformed input, never throws.</summary>
        public static bool TryParse(string json, out StringTable table)
        {
            table = new StringTable();
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var dto = JsonUtility.FromJson<Dto>(json);
                if (dto?.entries == null) return false;
                foreach (var e in dto.entries) table.Set(e.key, e.value);
                return table.Count > 0;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
