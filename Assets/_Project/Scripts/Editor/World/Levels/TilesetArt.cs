using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AuraKnight.Editor
{
    /// <summary>Loads the region art the room builder uses (rule tiles, tile sprites, parallax layers) by path; a missing asset gives null and a warning once.</summary>
    static class TilesetArt
    {
        static readonly HashSet<string> Warned = new HashSet<string>();
        static readonly Dictionary<string, Sprite[]> Sheets = new Dictionary<string, Sprite[]>();

        public static TileBase Tile(string folder, string kind) =>
            Load<TileBase>($"{LevelPaths.TilesetsRoot}/{folder}/Rules/Tile_{folder}_{kind}.asset");

        public static Sprite TileSprite(string folder, string spriteName)
        {
            string sheet = $"{LevelPaths.TilesetsRoot}/{folder}/Tiles_{folder}.png";
            if (!Sheets.TryGetValue(sheet, out var sprites))
            {
                var all = AssetDatabase.LoadAllAssetsAtPath(sheet);
                var list = new List<Sprite>();
                foreach (var asset in all)
                    if (asset is Sprite sprite) list.Add(sprite);
                Sheets[sheet] = sprites = list.ToArray();
            }
            foreach (var sprite in sprites)
                if (sprite.name == spriteName) return sprite;
            Warn($"{sheet}#{spriteName}");
            return null;
        }

        public static Sprite Background(string folder, string layer) =>
            Load<Sprite>($"{LevelPaths.BackgroundsRoot}/{folder}/{folder}_{layer}.png");

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Warn(path);
            return asset;
        }

        static void Warn(string what)
        {
            if (Warned.Add(what)) Debug.LogWarning($"[Levels] Art asset missing: {what}. The room falls back to greybox; run Aura/Art/Generate All.");
        }

        public static void ClearCache()
        {
            Sheets.Clear();
            Warned.Clear();
        }
    }
}
