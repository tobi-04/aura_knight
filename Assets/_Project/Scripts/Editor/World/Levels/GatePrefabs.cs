using AuraKnight.Aura;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Prefabs of the Castle gate (Prefabs/Interactables): the solid <see cref="SealGate"/> and the three <see cref="AuraSeal"/> pedestals.</summary>
    static class GatePrefabs
    {
        public const string SealGatePath = LevelPaths.InteractablesRoot + "/SealGate.prefab";
        public const string AuraSealPath = LevelPaths.InteractablesRoot + "/AuraSeal.prefab";

        public static void BuildAll()
        {
            HazardPrefabs.Save(BuildSealGate(), SealGatePath);
            HazardPrefabs.Save(BuildAuraSeal(), AuraSealPath);
        }

        /// <summary>The Castle gate: origin at the centre of the closed cells, Blocker is scaled to them, the seals are wired in the room.</summary>
        static GameObject BuildSealGate()
        {
            var root = new GameObject("SealGate");
            root.AddComponent<PersistentId>();
            var blocker = PrefabKit.Child(root.transform, "Blocker");
            PrefabKit.OnLayer(blocker, PhysicsLayers.Ground);
            PrefabKit.Sprite(blocker, new Color(0.35f, 0.3f, 0.5f), 5);
            PrefabKit.Box(blocker, Vector2.one);
            PrefabKit.SetRef(root.AddComponent<SealGate>(), "blocker", blocker);
            return root;
        }

        /// <summary>Pedestal: origin on the floor, trigger 2 wide; the Aura and glow colour are set per placement.</summary>
        static GameObject BuildAuraSeal()
        {
            var root = new GameObject("AuraSeal");
            PrefabKit.OnLayer(root, PhysicsLayers.Interactable);
            PrefabKit.Box(root, new Vector2(1.6f, 2.5f), new Vector2(0f, 1.25f), true);
            root.AddComponent<PersistentId>();
            var glow = PrefabKit.Child(root.transform, "Glow", new Vector3(0f, 0.8f, 0f));
            glow.transform.localScale = new Vector3(0.9f, 1.6f, 1f);
            var sr = PrefabKit.Sprite(glow, new Color(0.25f, 0.25f, 0.3f), 5);
            var seal = root.AddComponent<AuraSeal>();
            PrefabKit.SetRef(seal, "glow", sr);
            PrefabKit.SetColor(seal, "litColor", Color.white);
            return root;
        }

        /// <summary>Glow colour of the three seals and their lit state (GDD 6 Aura colours).</summary>
        public static Color AuraColor(AuraId aura)
        {
            switch (aura)
            {
                case AuraId.Wind: return new Color32(0x27, 0xD3, 0x8C, 0xFF);
                case AuraId.Fire: return new Color32(0xFF, 0x5C, 0x57, 0xFF);
                default: return new Color32(0x27, 0xB5, 0xF7, 0xFF);
            }
        }
    }
}
