using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Places the enemies of a room from their markers, re-skinned per region by the prefab choice: b BugThorn, m PoisonShroom (Forest),
    /// B Bat, S StoneSpider (Cave), p PatrolBot, z ScrapZapper (City), n NightKnight, g Ghost (Castle). Walkers and the hopper stand on
    /// the floor cell, flyers hover mid-cell, a spider run is its crawl path (ceiling when solid ground is above it).
    /// </summary>
    static class RoomEnemies
    {
        /// <summary>Enemy prefab and half its body height (prefab sizes from EnemyVariants), 0 = hovering mid-cell.</summary>
        static readonly (char marker, string prefab, float halfHeight)[] Table =
        {
            ('b', "BugThorn", 0.35f), ('m', "PoisonShroom", 0.4f), ('p', "PatrolBot", 0.5f), ('n', "NightKnight", 0.75f),
            ('z', "ScrapZapper", 0.45f), ('B', "Bat", 0f), ('g', "Ghost", 0f),
        };
        const float SpiderHalfHeight = 0.3f;
        const float FootClearance = 0.05f;

        public static int Count(RoomFile f)
        {
            int count = f.Groups('S').Count;
            foreach (var (marker, _, _) in Table) count += f.Cells(marker).Count;
            return count;
        }

        public static void Build(RoomBuild b)
        {
            var f = b.File;
            foreach (var (marker, prefab, half) in Table)
            {
                foreach (var cell in f.Cells(marker))
                {
                    float y = half > 0f ? cell.y + half + FootClearance : cell.y + 0.5f;
                    PrefabKit.Place($"{LevelPaths.EnemiesRoot}/{prefab}.prefab", b.Enemies, new Vector3(cell.x + 0.5f, y, 0f));
                }
            }
            foreach (var group in f.Groups('S')) Spider(b, group);
        }

        static void Spider(RoomBuild b, System.Collections.Generic.List<Vector2Int> group)
        {
            var r = RoomGeometry.Bounds(group);
            bool ceiling = b.File.At(r.x, r.yMax + 1) == '#';
            float y = ceiling ? r.yMax + 1f - SpiderHalfHeight : r.y + SpiderHalfHeight;
            var go = PrefabKit.Place($"{LevelPaths.EnemiesRoot}/StoneSpider.prefab", b.Enemies, new Vector3(r.x + 0.5f, y, 0f));
            PrefabKit.SetPosition(go.transform.Find("Path/P1"), new Vector3(r.width - 1f, 0f, 0f));
        }
    }
}
