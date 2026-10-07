using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds the Rule Tiles of one region: Ground and Wall (3x3 autotile on the 4 cardinal neighbours), one-way Platform (L/M/R),
    /// Spikes. Assets land in Art/Tilesets/&lt;Region&gt;/Rules/. Physics layers and effectors are set by the scenes using them.
    /// </summary>
    public static class RuleTileBuilder
    {
        const int This = RuleTile.TilingRuleOutput.Neighbor.This;
        const int NotThis = RuleTile.TilingRuleOutput.Neighbor.NotThis;

        public static string TilePath(string region, string kind) => $"{ArtPaths.Root}/Tilesets/{region}/Rules/Tile_{region}_{kind}.asset";

        public static void BuildRegion(string region, IReadOnlyDictionary<string, Sprite> sprites)
        {
            Directory.CreateDirectory($"{ArtPaths.Root}/Tilesets/{region}/Rules");
            Autotile(region, "Ground", sprites, Tile.ColliderType.Grid);
            Autotile(region, "Wall", sprites, Tile.ColliderType.Grid);
            Platform(region, sprites);
            Spikes(region, sprites);
            AssetDatabase.SaveAssets();
        }

        static void Autotile(string region, string kind, IReadOnlyDictionary<string, Sprite> sprites, Tile.ColliderType collider)
        {
            var tile = Open(TilePath(region, kind));
            Sprite S(string part) => sprites[$"{region}_{kind}_{part}"];
            tile.m_DefaultSprite = S("C");
            tile.m_DefaultColliderType = collider;
            // Order matters (first match wins): corners, then edges, then the interior fallback.
            tile.m_TilingRules = new List<RuleTile.TilingRule>
            {
                Rule(S("TL"), collider, up: NotThis, left: NotThis), Rule(S("TR"), collider, up: NotThis, right: NotThis),
                Rule(S("BL"), collider, down: NotThis, left: NotThis), Rule(S("BR"), collider, down: NotThis, right: NotThis),
                Rule(S("T"), collider, up: NotThis), Rule(S("B"), collider, down: NotThis),
                Rule(S("L"), collider, left: NotThis), Rule(S("R"), collider, right: NotThis),
                Rule(S("C"), collider, up: This, down: This, left: This, right: This),
            };
            Save(tile, TilePath(region, kind));
        }

        static void Platform(string region, IReadOnlyDictionary<string, Sprite> sprites)
        {
            var tile = Open(TilePath(region, "Platform"));
            tile.m_DefaultSprite = sprites[$"{region}_Platform_M"];
            tile.m_DefaultColliderType = Tile.ColliderType.Sprite;
            tile.m_TilingRules = new List<RuleTile.TilingRule>
            {
                Rule(sprites[$"{region}_Platform_L"], Tile.ColliderType.Sprite, left: NotThis, right: This),
                Rule(sprites[$"{region}_Platform_R"], Tile.ColliderType.Sprite, left: This, right: NotThis),
                Rule(sprites[$"{region}_Platform_M"], Tile.ColliderType.Sprite, left: This, right: This),
            };
            Save(tile, TilePath(region, "Platform"));
        }

        static void Spikes(string region, IReadOnlyDictionary<string, Sprite> sprites)
        {
            var tile = Open(TilePath(region, "Spikes"));
            tile.m_DefaultSprite = sprites[$"{region}_Spikes_Up"];
            tile.m_DefaultColliderType = Tile.ColliderType.Sprite;
            tile.m_TilingRules = new List<RuleTile.TilingRule>
            {
                new RuleTile.TilingRule
                {
                    m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                    m_Sprites = new[] { sprites[$"{region}_Spikes_Up"], sprites[$"{region}_Spikes_UpB"], sprites[$"{region}_Spikes_UpC"] },
                    m_ColliderType = Tile.ColliderType.Sprite,
                },
            };
            Save(tile, TilePath(region, "Spikes"));
        }

        static RuleTile.TilingRule Rule(Sprite sprite, Tile.ColliderType collider, int up = 0, int down = 0, int left = 0, int right = 0)
        {
            var rule = new RuleTile.TilingRule { m_Sprites = new[] { sprite }, m_ColliderType = collider };
            var neighbours = new Dictionary<Vector3Int, int>();
            if (up != 0) neighbours[new Vector3Int(0, 1, 0)] = up;
            if (down != 0) neighbours[new Vector3Int(0, -1, 0)] = down;
            if (left != 0) neighbours[new Vector3Int(-1, 0, 0)] = left;
            if (right != 0) neighbours[new Vector3Int(1, 0, 0)] = right;
            rule.ApplyNeighbors(neighbours);
            return rule;
        }

        static RuleTile Open(string path) => AssetDatabase.LoadAssetAtPath<RuleTile>(path) ?? ScriptableObject.CreateInstance<RuleTile>();

        static void Save(RuleTile tile, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<RuleTile>(path) == null) AssetDatabase.CreateAsset(tile, path);
            EditorUtility.SetDirty(tile);
        }
    }
}
