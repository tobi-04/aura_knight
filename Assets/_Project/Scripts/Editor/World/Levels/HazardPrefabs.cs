using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.World.Hazards;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds the hazard prefabs the rooms stamp out (Prefabs/Hazards); the Castle gate pieces are in <see cref="GatePrefabs"/>.
    /// Each is a unit-size stamp the room builder resizes per placement. Steam vents and fire traps reuse the existing
    /// HeatVent and Extinguishable gate prefabs, so there is no second implementation of either.
    /// </summary>
    static class HazardPrefabs
    {
        public const string SpikesPath = LevelPaths.HazardsRoot + "/Spikes.prefab";
        public const string KillZonePath = LevelPaths.HazardsRoot + "/KillZone.prefab";
        public const string CollapsingPath = LevelPaths.HazardsRoot + "/CollapsingPlatform.prefab";
        public const string StalactitePath = LevelPaths.HazardsRoot + "/FallingStalactite.prefab";
        public const string PistonPath = LevelPaths.HazardsRoot + "/Piston.prefab";
        public const string AcidPath = LevelPaths.HazardsRoot + "/AcidPool.prefab";
        public const string MovingSpikesPath = LevelPaths.HazardsRoot + "/MovingSpikeFloor.prefab";

        static readonly Color Metal = new Color(0.55f, 0.58f, 0.66f);
        static readonly Color Danger = new Color(0.85f, 0.3f, 0.3f);
        static readonly Color Acid = new Color(0.45f, 0.9f, 0.2f, 0.75f);

        public static void BuildAll()
        {
            System.IO.Directory.CreateDirectory(LevelPaths.HazardsRoot);
            Save(BuildSpikes(), SpikesPath);
            Save(BuildKillZone(), KillZonePath);
            Save(BuildCollapsing(), CollapsingPath);
            Save(BuildStalactite(), StalactitePath);
            Save(BuildPiston(), PistonPath);
            Save(BuildAcid(), AcidPath);
            Save(BuildMovingSpikes(), MovingSpikesPath);
            GatePrefabs.BuildAll();
        }

        internal static void Save(GameObject root, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>Contact hazard (team Hazard) on the object that already has its trigger collider.</summary>
        static Hitbox AddHazardHitbox(GameObject go, int damage, bool activeOnEnable, float rearm = 0.5f)
        {
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = Team.Hazard;
            PrefabKit.SetInt(hitbox, "damage", damage);
            PrefabKit.SetBool(hitbox, "activeOnEnable", activeOnEnable);
            PrefabKit.SetFloat(hitbox, "rearmInterval", rearm);
            return hitbox;
        }

        /// <summary>One cell of floor spikes: origin at the bottom centre of the cell, trigger 0.55 tall so landing on them hurts.</summary>
        static GameObject BuildSpikes()
        {
            var root = new GameObject("Spikes");
            PrefabKit.OnLayer(root, PhysicsLayers.Hazard);
            PrefabKit.Box(root, new Vector2(0.9f, 0.55f), new Vector2(0f, 0.275f), true);
            AddHazardHitbox(root, 1, true);
            var hurtbox = root.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Hazard;
            root.AddComponent<Spikes>();
            return root;
        }

        /// <summary>Lethal trigger (bottomless pit): origin at the centre, scaled to the pit.</summary>
        static GameObject BuildKillZone()
        {
            var root = new GameObject("KillZone");
            PrefabKit.OnLayer(root, PhysicsLayers.Hazard);
            PrefabKit.Box(root, Vector2.one, Vector2.zero, true);
            AddHazardHitbox(root, Spikes.LethalDamage, true);
            PrefabKit.SetBool(root.AddComponent<Spikes>(), "lethal", true);
            return root;
        }

        /// <summary>Platform whose origin is the bottom centre; Body (solid + sprite) and the sensor above it get their width from the room builder.</summary>
        static GameObject BuildCollapsing()
        {
            var root = new GameObject("CollapsingPlatform");
            PrefabKit.OnLayer(root, PhysicsLayers.Interactable);
            PrefabKit.Box(root, new Vector2(2f, 0.5f), new Vector2(0f, 1.2f), true);
            var body = PrefabKit.Child(root.transform, "Body", new Vector3(0f, 0.5f, 0f));
            PrefabKit.OnLayer(body, PhysicsLayers.Ground);
            PrefabKit.Box(body, new Vector2(2f, 1f));
            // Tiled drawing needs a Full Rect sprite (the art tiles are); without art the platform stays a stretched square.
            var art = TilesetArt.TileSprite("Hub", "Hub_Platform_M");
            var sr = PrefabKit.Sprite(body, art != null ? Color.white : new Color(0.6f, 0.45f, 0.3f), 2, art);
            if (art != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(2f, 1f);
            }

            PrefabKit.SetRef(root.AddComponent<CollapsingPlatform>(), "body", body);
            return root;
        }

        /// <summary>Origin at the ceiling hang point. The sensor (never moves) reaches 9 below; Body (2 tall) shakes, falls and hurts.</summary>
        static GameObject BuildStalactite()
        {
            var root = new GameObject("FallingStalactite");
            PrefabKit.OnLayer(root, PhysicsLayers.Interactable);
            PrefabKit.Box(root, new Vector2(2.4f, 9f), new Vector2(0f, -4.5f), true);
            var body = PrefabKit.Child(root.transform, "Body", new Vector3(0f, -1f, 0f));
            body.transform.localScale = new Vector3(0.9f, 2f, 1f);
            var sr = PrefabKit.Sprite(body, new Color(0.5f, 0.46f, 0.42f), 3);
            var shape = PrefabKit.Box(body, new Vector2(0.8f, 0.95f), Vector2.zero, true);
            var hitbox = AddHazardHitbox(body, 1, false);
            var stalactite = root.AddComponent<FallingStalactite>();
            PrefabKit.SetRef(stalactite, "body", body.transform);
            PrefabKit.SetRef(stalactite, "hitbox", hitbox);
            PrefabKit.SetRef(stalactite, "sprite", sr);
            PrefabKit.SetRef(stalactite, "bodyShape", shape);
            return root;
        }

        /// <summary>Origin at the ceiling cell's top centre; the head rests inside the cell and slides 4 down.</summary>
        static GameObject BuildPiston()
        {
            var root = new GameObject("Piston");
            var head = PrefabKit.Child(root.transform, "Head", new Vector3(0f, -0.5f, 0f));
            head.transform.localScale = new Vector3(1.6f, 1f, 1f);
            PrefabKit.Sprite(head, Metal, 3);
            PrefabKit.Box(head, new Vector2(0.9f, 0.9f), Vector2.zero, true);
            var hitbox = AddHazardHitbox(head, 1, false);
            var piston = root.AddComponent<Piston>();
            PrefabKit.SetRef(piston, "head", head.transform);
            PrefabKit.SetRef(piston, "hitbox", hitbox);
            return root;
        }

        /// <summary>Origin at the centre of the pool; Pool is scaled to the tank.</summary>
        static GameObject BuildAcid()
        {
            var root = new GameObject("AcidPool");
            var pool = PrefabKit.Child(root.transform, "Pool");
            PrefabKit.Sprite(pool, Acid, 4);
            PrefabKit.Box(pool, Vector2.one, Vector2.zero, true);
            var hitbox = AddHazardHitbox(pool, 1, false);
            PrefabKit.SetRef(root.AddComponent<AcidPool>(), "hitbox", hitbox);
            return root;
        }

        /// <summary>Origin at the bottom centre; Slab is scaled to the width of the spiked floor.</summary>
        static GameObject BuildMovingSpikes()
        {
            var root = new GameObject("MovingSpikeFloor");
            var slab = PrefabKit.Child(root.transform, "Slab", new Vector3(0f, 0.4f, 0f));
            slab.transform.localScale = new Vector3(2f, 0.8f, 1f);
            PrefabKit.Sprite(slab, Danger, 4);
            PrefabKit.Box(slab, Vector2.one, Vector2.zero, true);
            AddHazardHitbox(slab, 1, true);
            root.AddComponent<MovingSpikeFloor>();
            return root;
        }
    }
}
