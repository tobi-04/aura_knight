using System;
using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>One line of the priest and what Leo must have achieved for it to be the current one.</summary>
    [Serializable]
    public struct DialogueEntry
    {
        [Tooltip("Localization key of the line (Strings_vi.json).")]
        public string lineKey;
        [Tooltip("Aura ids ('Wind', 'Fire', 'Water') that must be unlocked.")]
        public List<string> requiredAuras;
        [Tooltip("Boss id that must be defeated (empty = none).")]
        public string requiredBoss;
    }

    /// <summary>
    /// What Tư Tế Sol says, picked by progress (GDD §7.1-7.2): entries are ordered from the start of the game to the end and the last
    /// one whose requirements are all met wins, so it always hints at the next region to visit.
    /// </summary>
    [CreateAssetMenu(menuName = "Aura/Dialogue By Progress", fileName = "DialogueByProgress")]
    public sealed class DialogueByProgress : ScriptableObject
    {
        [SerializeField] List<DialogueEntry> entries = new();

        public IReadOnlyList<DialogueEntry> Entries => entries;

        public DialogueByProgress SetEntries(IEnumerable<DialogueEntry> list)
        {
            entries = new List<DialogueEntry>(list ?? Array.Empty<DialogueEntry>());
            return this;
        }

        /// <summary>Line key for the save's progress; the first entry when nothing is met; empty when there are no entries.</summary>
        public string Select(GameState state)
        {
            string chosen = entries.Count > 0 ? entries[0].lineKey : string.Empty;
            if (state == null) return chosen;
            foreach (var entry in entries)
                if (IsMet(entry, state)) chosen = entry.lineKey;
            return chosen;
        }

        static bool IsMet(DialogueEntry entry, GameState state)
        {
            if (!string.IsNullOrEmpty(entry.requiredBoss) && !state.defeatedBosses.Contains(entry.requiredBoss)) return false;
            if (entry.requiredAuras == null) return true;
            foreach (var aura in entry.requiredAuras)
                if (!state.unlockedAuras.Contains(aura)) return false;
            return true;
        }
    }
}
