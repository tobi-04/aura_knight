using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Adds the four parallax layers of the region (GDD 10: background x0.1, midground x0.5, action decor x1.0, foreground x1.2) as
    /// tiled sprites under a "Backdrop" child. The layers hide themselves unless Leo is in this room.
    /// </summary>
    static class RoomBackdrop
    {
        public const string Name = "Backdrop";
        /// <summary>Art file suffix, factor and sorting order of the layers, back to front (tiles sort 0, 10 and -1).</summary>
        static readonly (string layer, float factor, int order)[] Layers =
        {
            ("Bg", ParallaxMath.Background, -30), ("Mid", ParallaxMath.Midground, -20),
            ("Back", ParallaxMath.Action, -10), ("Fg", ParallaxMath.Foreground, 20),
        };
        /// <summary>Layer width: wide enough that the 1.2 layer, which slides against the camera, still covers a 40-wide room.</summary>
        const float LayerWidth = 80f;

        public static void Add(Transform room, LevelRegion region, int width, int height)
        {
            var old = room.Find(Name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var group = PrefabKit.Child(room, Name, new Vector3(width * 0.5f, height * 0.5f, 0f));
            foreach (var (layer, factor, order) in Layers)
            {
                var sprite = TilesetArt.Background(region.Folder, layer);
                if (sprite == null) continue;
                var go = PrefabKit.Child(group.transform, $"Parallax_{layer}");
                var sr = PrefabKit.Sprite(go, Color.white, order, sprite);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(LayerWidth, sprite.bounds.size.y);
                go.AddComponent<ParallaxLayer>().Factor = factor;
            }
        }
    }
}
