using System;
using System.IO;
using UnityEngine;

namespace AuraKnight.Editor
{
    [Serializable]
    public sealed class SheetAnim
    {
        public string name;
        public int row;
        public int frames;
        public float fps;
        public bool loop;
    }

    /// <summary>Contents of a <c>*.sheet.json</c> written by tools/art: a grid of animation rows, one row per clip.</summary>
    [Serializable]
    public sealed class SheetManifest
    {
        public string name;
        public int cellWidth;
        public int cellHeight;
        public float pivotX;
        public float pivotY;
        public int pixelsPerUnit;
        public SheetAnim[] anims;

        public static string ManifestPathFor(string pngPath) => Path.ChangeExtension(pngPath, ".sheet.json");

        public static SheetManifest Load(string pngPath)
        {
            string path = ManifestPathFor(pngPath);
            if (!File.Exists(path)) throw new FileNotFoundException($"Sheet manifest missing for {pngPath}", path);
            var manifest = JsonUtility.FromJson<SheetManifest>(File.ReadAllText(path));
            if (manifest == null || manifest.anims == null || manifest.anims.Length == 0 || manifest.cellWidth <= 0 || manifest.cellHeight <= 0)
                throw new InvalidDataException($"Sheet manifest {path} is empty or malformed");
            return manifest;
        }

        public static string SpriteName(string sheet, string anim, int frame) => $"{sheet}_{anim}_{frame}";
    }

    /// <summary>Contents of a <c>*.tiles.json</c> written by gen_tilesets.py.</summary>
    [Serializable]
    public sealed class TileManifest
    {
        public string name;
        public int cell;
        public int columns;
        public int rows;
        public TileSpriteEntry[] sprites;

        public static TileManifest Load(string pngPath)
        {
            string path = Path.ChangeExtension(pngPath, ".tiles.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"Tile manifest missing for {pngPath}", path);
            var manifest = JsonUtility.FromJson<TileManifest>(File.ReadAllText(path));
            if (manifest == null || manifest.sprites == null || manifest.sprites.Length == 0 || manifest.cell <= 0)
                throw new InvalidDataException($"Tile manifest {path} is empty or malformed");
            return manifest;
        }
    }

    [Serializable]
    public sealed class TileSpriteEntry
    {
        public string name;
        public int col;
        public int row;
    }
}
