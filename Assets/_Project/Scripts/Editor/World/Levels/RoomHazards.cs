using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Places the hazards of a room from its markers: spikes (^), bottomless pits (K), collapsing platforms (c), stalactites (s),
    /// steam vents (h, the existing HeatVent), pistons (P), acid (a), moving spike floors (M) and fire traps (f, the existing
    /// extinguishable gate). Prefab stamps are resized per placement through serialized overrides.
    /// </summary>
    static class RoomHazards
    {
        /// <summary>How far a pit's kill zone reaches below its cells (the fall is lethal long before the room edge).</summary>
        const float PitDepth = 4f;

        public static void Build(RoomBuild b)
        {
            var f = b.File;
            foreach (var cell in f.Cells('^')) Stamp(b, HazardPrefabs.SpikesPath, new Vector2(cell.x + 0.5f, cell.y));
            foreach (var group in f.Groups('K')) Pit(b, RoomGeometry.Bounds(group));
            foreach (var group in f.Groups('c')) foreach (var run in RoomTerrain.Runs(group)) Collapsing(b, run);
            foreach (var cell in f.Cells('s')) Stamp(b, HazardPrefabs.StalactitePath, new Vector2(cell.x + 0.5f, cell.y + 1f));
            foreach (var cell in f.Cells('h')) Stamp(b, LevelPaths.InteractablesRoot + "/AuraGate_HeatVent.prefab", new Vector2(cell.x + 0.5f, cell.y + 1.9f));
            foreach (var cell in f.Cells('P')) Piston(b, cell);
            foreach (var group in f.Groups('a')) Acid(b, RoomGeometry.Bounds(group));
            foreach (var group in f.Groups('M')) MovingSpikes(b, RoomGeometry.Bounds(group));
            Traps(b);
        }

        static GameObject Stamp(RoomBuild b, string path, Vector2 position) => PrefabKit.Place(path, b.Hazards, RoomBuild.Local(position));

        static void Pit(RoomBuild b, RectInt r)
        {
            var go = Stamp(b, HazardPrefabs.KillZonePath, new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f - PitDepth * 0.5f));
            PrefabKit.SetScale(go.transform, new Vector3(r.width, r.height + PitDepth, 1f));
        }

        static void Collapsing(RoomBuild b, RectInt run)
        {
            var go = Stamp(b, HazardPrefabs.CollapsingPath, new Vector2(run.x + run.width * 0.5f, run.y));
            var body = go.transform.Find("Body");
            var sr = body.GetComponent<SpriteRenderer>();
            var sprite = TilesetArt.TileSprite(b.Region.Folder, $"{b.Region.Folder}_Platform_M");
            if (sprite != null)
            {
                PrefabKit.ResizeBox(body.GetComponent<BoxCollider2D>(), new Vector2(run.width, 1f), Vector2.zero);
                PrefabKit.SetVector2(sr, "m_Size", new Vector2(run.width, 1f));
                PrefabKit.SetRef(sr, "m_Sprite", sprite);
            }
            else
            {
                // No art: the square sprite is stretched by the transform, so the collider stays one unit.
                PrefabKit.ResizeBox(body.GetComponent<BoxCollider2D>(), Vector2.one, Vector2.zero);
                PrefabKit.SetScale(body, new Vector3(run.width, 1f, 1f));
            }
            PrefabKit.ResizeBox(go.GetComponent<BoxCollider2D>(), new Vector2(run.width, 0.5f), new Vector2(0f, 1.2f));
        }

        static void Piston(RoomBuild b, Vector2Int cell)
        {
            var go = Stamp(b, HazardPrefabs.PistonPath, new Vector2(cell.x + 0.5f, cell.y + 1f));
            // Neighbouring pistons start at different points of the 2.85 s cycle so the hall is never one solid wall of slams.
            PrefabKit.SetFloat(go.GetComponent<AuraKnight.World.Hazards.Piston>(), "startOffset", (cell.x * 0.37f) % 2.85f);
        }

        static void Acid(RoomBuild b, RectInt r)
        {
            var go = Stamp(b, HazardPrefabs.AcidPath, new Vector2(r.x + r.width * 0.5f, r.y + 0.4f));
            PrefabKit.SetScale(go.transform.Find("Pool"), new Vector3(r.width, 0.8f, 1f));
        }

        static void MovingSpikes(RoomBuild b, RectInt r)
        {
            var go = Stamp(b, HazardPrefabs.MovingSpikesPath, new Vector2(r.x + r.width * 0.5f, r.y));
            PrefabKit.SetScale(go.transform.Find("Slab"), new Vector3(r.width, 0.8f, 1f));
        }

        /// <summary>Fire traps: the existing extinguishable gate, its flames stretched over the closed cells so Water is the way through.</summary>
        static void Traps(RoomBuild b)
        {
            var ids = b.File.IdsOf('f');
            var groups = b.File.Groups('f');
            for (int i = 0; i < groups.Count; i++)
            {
                var r = RoomGeometry.Bounds(groups[i]);
                var go = Stamp(b, LevelPaths.InteractablesRoot + "/AuraGate_Extinguish.prefab", RoomGeometry.Center(r));
                PrefabKit.SetString(go.GetComponent<AuraKnight.World.PersistentId>(), "id", ids[i]);
                var flames = go.transform.Find("Flames");
                PrefabKit.SetScale(flames, new Vector3(r.width, r.height, 1f));
                PrefabKit.SetPosition(flames, Vector3.zero);
                PrefabKit.ResizeBox(go.GetComponent<BoxCollider2D>(), new Vector2(r.width + 1.5f, r.height + 1f), Vector2.zero);
            }
        }
    }
}
