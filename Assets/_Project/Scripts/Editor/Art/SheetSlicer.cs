using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Slices generated PNG sheets into named sprites from their JSON manifests. Sprite IDs are reused on re-run so references survive.</summary>
    public static class SheetSlicer
    {
        /// <summary>Animation sheet: sprites are named Sheet_Anim_Frame, pivot from the manifest.</summary>
        public static SheetManifest SliceAnimationSheet(string pngPath)
        {
            var manifest = SheetManifest.Load(pngPath);
            var size = ImportAndMeasure(pngPath);
            var items = new List<(string, Rect)>();
            foreach (var anim in manifest.anims)
            {
                if ((anim.row + 1) * manifest.cellHeight > size.y || anim.frames * manifest.cellWidth > size.x)
                    throw new InvalidOperationException($"{pngPath}: clip '{anim.name}' does not fit the {size.x}x{size.y} texture");
                for (int i = 0; i < anim.frames; i++)
                    items.Add((SheetManifest.SpriteName(manifest.name, anim.name, i),
                        new Rect(i * manifest.cellWidth, size.y - (anim.row + 1) * manifest.cellHeight, manifest.cellWidth, manifest.cellHeight)));
            }
            Apply(pngPath, items, new Vector2(manifest.pivotX, manifest.pivotY));
            return manifest;
        }

        /// <summary>Tileset sheet: one sprite per manifest entry, centre pivot.</summary>
        public static TileManifest SliceTileSheet(string pngPath)
        {
            var manifest = TileManifest.Load(pngPath);
            var size = ImportAndMeasure(pngPath);
            var items = manifest.sprites.Select(s =>
                (s.name, new Rect(s.col * manifest.cell, size.y - (s.row + 1) * manifest.cell, manifest.cell, manifest.cell))).ToList();
            Apply(pngPath, items, new Vector2(0.5f, 0.5f));
            return manifest;
        }

        static Vector2Int ImportAndMeasure(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Pixel32Importer.Apply(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            return new Vector2Int(w, h);
        }

        static void Apply(string path, List<(string name, Rect rect)> items, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First());
            var rects = new List<SpriteRect>(items.Count);
            foreach (var (name, rect) in items)
            {
                if (!existing.TryGetValue(name, out var sprite)) sprite = new SpriteRect { name = name, spriteID = GUID.Generate() };
                sprite.rect = rect;
                sprite.alignment = SpriteAlignment.Custom;
                sprite.pivot = pivot;
                rects.Add(sprite);
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToArray());
            provider.Apply();
            ((AssetImporter)provider.targetObject).SaveAndReimport();
        }

        /// <summary>All sprites of a sliced sheet by name.</summary>
        public static Dictionary<string, Sprite> LoadSprites(string pngPath) =>
            AssetDatabase.LoadAllAssetRepresentationsAtPath(pngPath).OfType<Sprite>().ToDictionary(s => s.name);
    }
}
