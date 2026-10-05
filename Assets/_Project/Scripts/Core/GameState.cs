using System;
using System.Collections.Generic;

namespace AuraKnight.Core
{
    /// <summary>One shop item and how many times it was bought (JsonUtility cannot serialize dictionaries).</summary>
    [Serializable]
    public struct PurchaseEntry
    {
        public string key;
        public int count;
    }

    /// <summary>
    /// Serializable save payload, field-for-field with GDD §12.4. Stores IDs only, never object positions.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 1;
        public const string NoAura = "None";
        public const string StartAltarId = "hub_altar_01";

        /// <summary>0 until stamped, so a file missing the field is rejected rather than trusted.</summary>
        public int version;
        public string lastAltarId = StartAltarId;
        public int maxHearts = 5;
        public int maxEnergy = 100;
        public int swordLevel = 1;
        public int coins;
        public List<string> unlockedAuras = new();
        public string currentAura = NoAura;
        public List<string> defeatedBosses = new();
        public List<string> visitedRooms = new();
        public List<string> openedChests = new();
        public List<string> openedShortcuts = new();
        public List<string> openedGates = new();
        public List<PurchaseEntry> shopPurchases = new();
        public float playTimeSeconds;

        public static GameState NewGame() => new() { version = CurrentVersion };

        public int GetPurchaseCount(string key)
        {
            int i = IndexOfPurchase(key);
            return i < 0 ? 0 : shopPurchases[i].count;
        }

        public void AddPurchase(string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key) || amount <= 0) return;
            int i = IndexOfPurchase(key);
            if (i < 0) shopPurchases.Add(new PurchaseEntry { key = key, count = amount });
            else shopPurchases[i] = new PurchaseEntry { key = key, count = shopPurchases[i].count + amount };
        }

        public bool MarkRoomVisited(string roomId) => AddUnique(visitedRooms, roomId);
        public bool MarkBossDefeated(string bossId) => AddUnique(defeatedBosses, bossId);
        public bool MarkShortcutOpened(string shortcutId) => AddUnique(openedShortcuts, shortcutId);
        public bool HasOpenedShortcut(string shortcutId) => !string.IsNullOrEmpty(shortcutId) && openedShortcuts.Contains(shortcutId);
        public bool MarkGateOpened(string gateId) => AddUnique(openedGates, gateId);
        public bool HasOpenedGate(string gateId) => !string.IsNullOrEmpty(gateId) && openedGates.Contains(gateId);

        /// <summary>Repairs null collections after deserializing a hand-edited or partial file.</summary>
        public void Normalize()
        {
            unlockedAuras ??= new List<string>();
            defeatedBosses ??= new List<string>();
            visitedRooms ??= new List<string>();
            openedChests ??= new List<string>();
            openedShortcuts ??= new List<string>();
            openedGates ??= new List<string>();
            shopPurchases ??= new List<PurchaseEntry>();
            if (string.IsNullOrEmpty(currentAura)) currentAura = NoAura;
            if (string.IsNullOrEmpty(lastAltarId)) lastAltarId = StartAltarId;
        }

        static bool AddUnique(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id)) return false;
            list.Add(id);
            return true;
        }

        int IndexOfPurchase(string key)
        {
            for (int i = 0; i < shopPurchases.Count; i++)
                if (shopPurchases[i].key == key) return i;
            return -1;
        }
    }
}
