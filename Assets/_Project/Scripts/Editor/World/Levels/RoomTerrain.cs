using AuraKnight.Core;
using AuraKnight.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Terrain of a room: the visual tiles (Ground, Wall, Platform and Spikes rule tiles of the region, painted in the Ground tilemap)
    /// and the collision as merged box colliders on the Ground layer. Collision is explicit boxes, not tile colliders, so the
    /// physics match the grid the reachability tests reason about. Smooth wall boxes carry <see cref="SmoothWall"/>; one-way
    /// platforms are thin boxes with a PlatformEffector2D.
    /// </summary>
    static class RoomTerrain
    {
        const float OneWayThickness = 0.4f;

        public static void Build(RoomBuild b)
        {
            var ground = b.Grid.Find("Ground");
            var collider = ground.GetComponent<TilemapCollider2D>();
            if (collider != null) Object.DestroyImmediate(collider); // the boxes below are the collision
            var lit = AssetDatabase.LoadAssetAtPath<Material>(LevelPaths.LitMaterial);
            foreach (Transform layer in b.Grid)
                if (lit != null && layer.TryGetComponent<TilemapRenderer>(out var renderer)) renderer.sharedMaterial = lit;
            Paint(b, ground.GetComponent<Tilemap>());
            Collision(b);
        }

        static void Paint(RoomBuild b, Tilemap map)
        {
            var f = b.File;
            var kinds = new[] { ('#', "Ground"), ('W', "Wall"), ('=', "Platform"), ('^', "Spikes") };
            foreach (var (marker, kind) in kinds)
            {
                var tile = TilesetArt.Tile(b.Region.Folder, kind);
                if (tile == null) continue;
                foreach (var cell in f.Cells(marker)) map.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
            }
            map.RefreshAllTiles();
        }

        static void Collision(RoomBuild b)
        {
            var f = b.File;
            int n = 0;
            foreach (var r in RoomGeometry.MergeRects(f.Width, f.Height, (x, y) => f.At(x, y) == '#'))
                Solid(b.Collision, $"Solid_{n++}", r, smooth: false);
            n = 0;
            foreach (var r in RoomGeometry.MergeRects(f.Width, f.Height, (x, y) => f.At(x, y) == 'W'))
                Solid(b.Collision, $"Smooth_{n++}", r, smooth: true);
            n = 0;
            foreach (var group in f.Groups('='))
            {
                // One object per horizontal run: Groups are 4-connected so stacked platforms would merge; split by row.
                foreach (var run in Runs(group)) OneWay(b.Collision, $"OneWay_{n++}", run);
            }
        }

        static void Solid(Transform parent, string name, RectInt r, bool smooth)
        {
            var go = PrefabKit.OnLayer(PrefabKit.Child(parent, name, RoomBuild.Local(RoomGeometry.Center(r))), PhysicsLayers.Ground);
            PrefabKit.Box(go, new Vector2(r.width, r.height));
            if (smooth) go.AddComponent<SmoothWall>();
        }

        static void OneWay(Transform parent, string name, RectInt run)
        {
            float top = run.y + 1f;
            var go = PrefabKit.OnLayer(PrefabKit.Child(parent, name, new Vector3(run.x + run.width * 0.5f, top - OneWayThickness * 0.5f, 0f)),
                PhysicsLayers.Ground);
            var box = PrefabKit.Box(go, new Vector2(run.width, OneWayThickness));
            box.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
        }

        /// <summary>Splits a connected group of cells into maximal horizontal runs (a row's contiguous cells).</summary>
        public static System.Collections.Generic.List<RectInt> Runs(System.Collections.Generic.IReadOnlyList<Vector2Int> cells)
        {
            var set = new System.Collections.Generic.HashSet<Vector2Int>(cells);
            var runs = new System.Collections.Generic.List<RectInt>();
            foreach (var c in cells)
            {
                if (set.Contains(c + Vector2Int.left)) continue;
                int length = 1;
                while (set.Contains(new Vector2Int(c.x + length, c.y))) length++;
                runs.Add(new RectInt(c.x, c.y, length, 1));
            }
            return runs;
        }
    }
}
