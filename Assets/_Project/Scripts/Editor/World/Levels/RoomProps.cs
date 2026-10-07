using System.Collections.Generic;
using AuraKnight.Aura;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Places the interactive props of a room from its markers: altar (A), chests (C), Sol (N), thorn bushes (T) and the wooden
    /// barricade (Z, both the existing Burnable gate), the Castle seal gate (G) with its seals (Q), shortcut doors (k, j), water (~)
    /// and updrafts (Y). Ids come from the "ids" line in reading order.
    /// </summary>
    static class RoomProps
    {
        const string Interactables = LevelPaths.InteractablesRoot;
        static readonly Color ThornGreen = new Color(0.25f, 0.5f, 0.2f);

        public static void Build(RoomBuild b)
        {
            var f = b.File;
            foreach (var cell in f.Cells('A')) Altar(b, cell);
            Chests(b);
            foreach (var cell in f.Cells('N')) PrefabKit.Place(Interactables + "/NpcSol.prefab", b.Props, new Vector3(cell.x + 0.5f, cell.y, 0f));
            Burnables(b, 'T', ThornGreen);
            Burnables(b, 'Z', new Color32(0x8B, 0x5A, 0x2B, 0xFF));
            Seals(b);
            Shortcuts(b);
            Volumes(b, '~', "WaterVolume", 10f, 6f);
            Volumes(b, 'Y', "WindCurrent", 3f, 12f);
        }

        static void Altar(RoomBuild b, Vector2Int cell)
        {
            var go = PrefabKit.Place(Interactables + "/SunAltar.prefab", b.Props, new Vector3(cell.x + 0.5f, cell.y, 0f));
            PrefabKit.SetString(go.GetComponent<SunAltar>(), "altarId", b.File.AltarId);
        }

        static void Chests(RoomBuild b)
        {
            var ids = b.File.IdsOf('C');
            var cells = b.File.Groups('C');
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i][0];
                var go = PrefabKit.Place(Interactables + "/TreasureChest.prefab", b.Props, new Vector3(cell.x + 0.5f, cell.y, 0f));
                PrefabKit.SetString(go.GetComponent<PersistentId>(), "id", ids[i]);
                if (!b.File.Rewards.TryGetValue(ids[i], out var effect)) continue;
                var chest = go.GetComponent<AuraKnight.World.TreasureChest>();
                var so = new UnityEditor.SerializedObject(chest);
                so.FindProperty("reward.kind").intValue = (int)AuraKnight.Progression.ChestRewardKind.Upgrade;
                var upgrade = so.FindProperty("reward.upgrade");
                int index = System.Array.IndexOf(upgrade.enumNames, effect);
                if (index < 0) throw new System.FormatException($"{b.File.Source}: reward '{effect}' is not a shop effect");
                upgrade.enumValueIndex = index;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>A Burnable gate stretched over the marker group: the Fire skill (fireball) burns it open for good.</summary>
        static void Burnables(RoomBuild b, char marker, Color color)
        {
            var ids = b.File.IdsOf(marker);
            var groups = b.File.Groups(marker);
            for (int i = 0; i < groups.Count; i++)
            {
                var r = RoomGeometry.Bounds(groups[i]);
                var go = PrefabKit.Place(Interactables + "/AuraGate_Burn.prefab", b.Props, RoomBuild.Local(RoomGeometry.Center(r)));
                PrefabKit.SetString(go.GetComponent<PersistentId>(), "id", ids[i]);
                var blocker = go.transform.Find("Blocker");
                PrefabKit.SetScale(blocker, new Vector3(r.width, r.height, 1f));
                PrefabKit.SetPosition(blocker, Vector3.zero);
                PrefabKit.SetColor(blocker.GetComponent<SpriteRenderer>(), "m_Color", color);
                PrefabKit.ResizeBox(go.GetComponent<BoxCollider2D>(), new Vector2(r.width + 1.2f, r.height + 0.4f), Vector2.zero);
            }
        }

        static void Seals(RoomBuild b)
        {
            var ids = b.File.IdsOf('Q');
            var seals = new List<Object>();
            var cells = b.File.Groups('Q');
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i][0];
                var go = PrefabKit.Place(GatePrefabs.AuraSealPath, b.Props, new Vector3(cell.x + 0.5f, cell.y, 0f));
                PrefabKit.SetString(go.GetComponent<PersistentId>(), "id", ids[i]);
                var aura = AuraOfSeal(ids[i], b.File);
                var seal = go.GetComponent<AuraSeal>();
                PrefabKit.SetEnum(seal, "aura", aura.ToString());
                PrefabKit.SetColor(seal, "litColor", GatePrefabs.AuraColor(aura));
                PrefabKit.SetColor(go.transform.Find("Glow").GetComponent<SpriteRenderer>(), "m_Color", GatePrefabs.AuraColor(aura) * 0.35f);
                seals.Add(seal);
            }
            var gateIds = b.File.IdsOf('G');
            var groups = b.File.Groups('G');
            for (int i = 0; i < groups.Count; i++)
            {
                var r = RoomGeometry.Bounds(groups[i]);
                var go = PrefabKit.Place(GatePrefabs.SealGatePath, b.Props, RoomBuild.Local(RoomGeometry.Center(r)));
                PrefabKit.SetString(go.GetComponent<PersistentId>(), "id", gateIds[i]);
                PrefabKit.SetScale(go.transform.Find("Blocker"), new Vector3(r.width, r.height, 1f));
                PrefabKit.SetRefs(go.GetComponent<SealGate>(), "seals", seals.ToArray());
            }
        }

        /// <summary>A seal's id ends with the Aura it answers to (seal_wind, seal_fire, seal_water).</summary>
        public static AuraId AuraOfSeal(string sealId, RoomFile f)
        {
            string tail = sealId.Substring(sealId.LastIndexOf('_') + 1);
            foreach (var aura in SealRules.SealAuras)
                if (string.Equals(aura.ToString(), tail, System.StringComparison.OrdinalIgnoreCase)) return aura;
            throw new System.FormatException($"{f.Source}: seal id '{sealId}' must end with _wind, _fire or _water");
        }

        /// <summary>Shortcut door: k opens from its left side, j from its right (the prefab is mirrored).</summary>
        static void Shortcuts(RoomBuild b)
        {
            foreach (char marker in new[] { 'k', 'j' })
            {
                var ids = b.File.IdsOf(marker);
                var groups = b.File.Groups(marker);
                for (int i = 0; i < groups.Count; i++)
                {
                    var r = RoomGeometry.Bounds(groups[i]);
                    if (r.width != 1 || r.height != 4) throw new System.FormatException($"{b.File.Source}: shortcut door '{marker}' must be 1 wide and 4 tall");
                    var go = PrefabKit.Place(Interactables + "/Shortcut.prefab", b.Props, new Vector3(r.x + 0.5f, r.y, 0f));
                    PrefabKit.SetString(go.GetComponent<PersistentId>(), "id", ids[i]);
                    if (marker == 'j') PrefabKit.SetScale(go.transform, new Vector3(-1f, 1f, 1f));
                }
            }
        }

        /// <summary>Water or updraft volumes: the prefab's trigger is (width x height); the root is scaled to the marker group.</summary>
        static void Volumes(RoomBuild b, char marker, string prefab, float width, float height)
        {
            foreach (var group in b.File.Groups(marker))
            {
                var r = RoomGeometry.Bounds(group);
                var go = PrefabKit.Place($"{Interactables}/{prefab}.prefab", b.Props, RoomBuild.Local(RoomGeometry.Center(r)));
                PrefabKit.SetScale(go.transform, new Vector3(r.width / width, r.height / height, 1f));
            }
        }
    }
}
