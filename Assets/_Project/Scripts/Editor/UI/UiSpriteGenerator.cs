using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Writes the few raster UI sprites (pixel heart, sun coin, white pixel, left fade gradient) as PNGs and configures
    /// their importers; also sets the import options of the key art. All drawn in code so nothing here needs an art tool.
    /// </summary>
    public static class UiSpriteGenerator
    {
        static readonly string[] HeartRows =
        {
            "..XXX...XXX..",
            ".XXXXX.XXXXX.",
            "XXXXXXXXXXXXX",
            "XXXXXXXXXXXXX",
            "XXXXXXXXXXXXX",
            ".XXXXXXXXXXX.",
            "..XXXXXXXXX..",
            "...XXXXXXX...",
            "....XXXXX....",
            ".....XXX.....",
            "......X......"
        };

        public static void GenerateAll()
        {
            Directory.CreateDirectory(UiAssetPaths.UiArtDir);
            WriteSprite(UiAssetPaths.HeartSprite, Heart(), FilterMode.Point);
            WriteSprite(UiAssetPaths.SunSprite, Sun(), FilterMode.Point);
            WriteSprite(UiAssetPaths.WhiteSprite, Solid(4, 4), FilterMode.Point);
            WriteSprite(UiAssetPaths.GradientSprite, Gradient(), FilterMode.Bilinear);
            ConfigureKeyArt();
            AssetDatabase.SaveAssets();
        }

        static Texture2D Heart()
        {
            int h = HeartRows.Length, w = HeartRows[0].Length;
            var tex = Blank(w, h);
            for (int row = 0; row < h; row++)
                for (int x = 0; x < w; x++)
                    if (HeartRows[row][x] == 'X') tex.SetPixel(x, h - 1 - row, Color.white);
            return tex;
        }

        static Texture2D Sun()
        {
            const int size = 15, c = 7;
            var tex = Blank(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = x - c, dy = y - c;
                    bool disc = dx * dx + dy * dy <= 13;
                    int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    bool ray = (dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy)) && dist >= 5 && dist <= 6;
                    if (disc || ray) tex.SetPixel(x, y, Color.white);
                }
            return tex;
        }

        static Texture2D Solid(int w, int h)
        {
            var tex = Blank(w, h);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, Color.white);
            return tex;
        }

        /// <summary>White with alpha 1 on the left fading to 0 on the right (tinted to bg/night over the key art).</summary>
        static Texture2D Gradient()
        {
            const int w = 128;
            var tex = Blank(w, 4);
            for (int x = 0; x < w; x++)
            {
                float a = 1f - x / (float)(w - 1);
                for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
            return tex;
        }

        static Texture2D Blank(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color[w * h];
            tex.SetPixels(clear);
            return tex;
        }

        static void WriteSprite(string path, Texture2D tex, FilterMode filter)
        {
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png))
            {
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void ConfigureKeyArt()
        {
            var paths = new[]
            {
                UiAssetPaths.HeroArt, UiAssetPaths.AuraArt("wind"), UiAssetPaths.AuraArt("fire"), UiAssetPaths.AuraArt("water")
            };
            foreach (string path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }
    }
}
