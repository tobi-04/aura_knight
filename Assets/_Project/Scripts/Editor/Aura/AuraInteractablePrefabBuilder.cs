using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Builds the Aura interactable prefabs under Prefabs/Interactables (placeholder art: coloured squares).</summary>
    static class AuraInteractablePrefabBuilder
    {
        public const string Folder = "Assets/_Project/Prefabs/Interactables";
        public const string BurnGatePath = Folder + "/AuraGate_Burn.prefab";
        public const string ExtinguishGatePath = Folder + "/AuraGate_Extinguish.prefab";
        public const string HeatVentPath = Folder + "/AuraGate_HeatVent.prefab";
        public const string WindCurrentPath = Folder + "/WindCurrent.prefab";
        public const string LavaPath = Folder + "/LavaFreezable.prefab";
        public const string WaterPath = Folder + "/WaterVolume.prefab";

        static readonly Color Wood = new Color32(0x8B, 0x5A, 0x2B, 0xFF);
        static readonly Color Flame = new Color32(0xFF, 0x7A, 0x3D, 0xFF);

        public static void BuildAll(Sprite square)
        {
            PlayerGeneratorUtil.EnsureFolder(Folder);
            BuildBurnGate(square);
            BuildExtinguishGate(square);
            BuildHeatVent(square);
            BuildWindCurrent(square);
            BuildLava(square);
            BuildWater(square);
        }

        static void Save(GameObject root, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }

        const string DeactivateOnOpen = "switches.deactivateOnOpen";

        /// <summary>Root with a trigger collider on the Interactable layer (what skill probes and the player's triggers look for).</summary>
        static GameObject NewInteractable(string name, Vector2 triggerSize)
        {
            var root = new GameObject(name);
            PlayerGeneratorUtil.SetLayer(root, PhysicsLayers.Interactable);
            AuraPrefabParts.AddBox(root, triggerSize, true);
            return root;
        }

        static void BuildBurnGate(Sprite square)
        {
            var root = NewInteractable("AuraGate_Burn", new Vector2(2.4f, 3.4f));
            var gate = root.AddComponent<BurnableGate>();
            root.AddComponent<PersistentId>();
            var blocker = AuraPrefabParts.AddSprite(root.transform, "Blocker", square, Vector2.zero, new Vector2(1.2f, 3f), Wood, 5);
            PlayerGeneratorUtil.SetLayer(blocker, PhysicsLayers.Ground);
            blocker.AddComponent<BoxCollider2D>();
            AuraSerialized.SetObjects(gate, DeactivateOnOpen, new Object[] { blocker });
            Save(root, BurnGatePath);
        }

        static void BuildExtinguishGate(Sprite square)
        {
            var root = NewInteractable("AuraGate_Extinguish", new Vector2(3.5f, 2.5f));
            var gate = root.AddComponent<ExtinguishableGate>();
            root.AddComponent<PersistentId>();
            var flames = AuraPrefabParts.AddSprite(root.transform, "Flames", square, new Vector2(0f, -0.25f), new Vector2(2f, 1.5f), Flame, 5);
            flames.AddComponent<BoxCollider2D>().isTrigger = true;
            var hitbox = flames.AddComponent<Hitbox>();
            hitbox.Team = Team.Hazard;
            hitbox.Damage = 1;
            hitbox.RearmInterval = 0.5f;
            PlayerGeneratorUtil.SetBool(hitbox, "activeOnEnable", true);
            AuraSerialized.SetObjects(gate, DeactivateOnOpen, new Object[] { flames });
            Save(root, ExtinguishGatePath);
        }

        static void BuildHeatVent(Sprite square)
        {
            var root = NewInteractable("AuraGate_HeatVent", new Vector2(2.5f, 4f));
            var vent = root.AddComponent<HeatVent>();
            var steam = AuraPrefabParts.AddSprite(root.transform, "Steam", square, Vector2.zero, new Vector2(1.5f, 3.5f), new Color(1f, 0.6f, 0.4f, 0.6f), 5);
            steam.AddComponent<BoxCollider2D>().isTrigger = true;
            var hitbox = steam.AddComponent<Hitbox>();
            hitbox.Team = Team.Hazard;
            hitbox.Damage = 1;
            hitbox.RearmInterval = 0.5f;
            PlayerGeneratorUtil.SetReference(vent, "ventHazard", hitbox);
            Save(root, HeatVentPath);
        }

        static void BuildWindCurrent(Sprite square)
        {
            var root = NewInteractable("WindCurrent", new Vector2(3f, 12f));
            root.AddComponent<WindCurrent>();
            AuraPrefabParts.AddSprite(root.transform, "Visual", square, Vector2.zero, new Vector2(3f, 12f), new Color(0.15f, 0.83f, 0.55f, 0.2f), 1);
            Save(root, WindCurrentPath);
        }

        static void BuildLava(Sprite square)
        {
            var root = NewInteractable("LavaFreezable", new Vector2(6f, 2f));
            var lavaFreezable = root.AddComponent<LavaFreezable>();
            var lava = AuraPrefabParts.AddSprite(root.transform, "Lava", square, Vector2.zero, new Vector2(6f, 1.5f), new Color32(0xFF, 0x4A, 0x1C, 0xFF), 4);
            lava.AddComponent<BoxCollider2D>().isTrigger = true;
            var hitbox = lava.AddComponent<Hitbox>();
            hitbox.Team = Team.Hazard;
            hitbox.Damage = 1;
            hitbox.RearmInterval = 0.5f;
            PlayerGeneratorUtil.SetBool(hitbox, "activeOnEnable", true);
            var platform = AuraPrefabParts.AddSprite(root.transform, "FrozenPlatform", square, new Vector2(0f, 0.5f), new Vector2(6f, 0.5f), new Color32(0xB8, 0xE8, 0xFF, 0xFF), 4);
            PlayerGeneratorUtil.SetLayer(platform, PhysicsLayers.Ground);
            platform.AddComponent<BoxCollider2D>();
            platform.SetActive(false);
            PlayerGeneratorUtil.SetReference(lavaFreezable, "lava", lava);
            PlayerGeneratorUtil.SetReference(lavaFreezable, "platform", platform);
            Save(root, LavaPath);
        }

        static void BuildWater(Sprite square)
        {
            var root = NewInteractable("WaterVolume", new Vector2(10f, 6f));
            root.AddComponent<WaterVolume>();
            AuraPrefabParts.AddSprite(root.transform, "Visual", square, Vector2.zero, new Vector2(10f, 6f), new Color(0.15f, 0.71f, 0.97f, 0.35f), 8);
            Save(root, WaterPath);
        }
    }
}
