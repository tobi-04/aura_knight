using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// One entry point for the whole art import: preset, Pixel32 settings, slicing, clips, controllers, rule tiles, atlases, material.
    /// Menu: Aura/Art/Generate All. Batch: tools/unity-batch.sh exec AuraKnight.Editor.ArtPipeline.GenerateAll
    /// Sheets (PNG + JSON) come from tools/art/build_art.py; this step never invents pixels.
    /// </summary>
    public static class ArtPipeline
    {
        [MenuItem("Aura/Art/Generate All")]
        public static void GenerateAll()
        {
            AssetDatabase.Refresh();
            Pixel32Importer.EnsurePreset();
            Pixel32Importer.ApplyToAll();
            ArtMaterials.EnsureSpriteLit();
            Directory.CreateDirectory(ArtPaths.DataFolder);

            BuildLeo();
            var enemyBase = MonsterControllerBuilder.EnsureBase(boss: false);
            foreach (var variant in ArtPaths.Enemies) BuildMonster(ArtPaths.EnemySheet(variant), enemyBase);
            var bossBase = MonsterControllerBuilder.EnsureBase(boss: true);
            foreach (var boss in ArtPaths.Bosses) BuildMonster(ArtPaths.BossSheet(boss), bossBase);
            foreach (var region in ArtPaths.Regions) BuildTileset(region);
            foreach (var region in ArtPaths.Regions) SliceBackgrounds(region);

            AtlasBuilder.BuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtPipeline] Preset, material, Leo, enemies, bosses, tilesets, backgrounds and atlases generated.");
        }

        static void BuildLeo()
        {
            var manifest = SheetSlicer.SliceAnimationSheet(ArtPaths.LeoSheet);
            var clips = ArtClipBuilder.BuildAll(ArtPaths.LeoSheet, manifest);
            LeoControllerBuilder.Build(clips);
        }

        static void BuildMonster(string sheetPng, AnimatorController baseController)
        {
            var manifest = SheetSlicer.SliceAnimationSheet(sheetPng);
            var clips = ArtClipBuilder.BuildAll(sheetPng, manifest);
            string folder = Path.GetDirectoryName(sheetPng).Replace('\\', '/');
            MonsterControllerBuilder.BuildOverride(folder, manifest.name, baseController, clips);
        }

        static void BuildTileset(string region)
        {
            string png = $"{ArtPaths.Root}/Tilesets/{region}/Tiles_{region}.png";
            SheetSlicer.SliceTileSheet(png);
            RuleTileBuilder.BuildRegion(region, SheetSlicer.LoadSprites(png));
        }

        static void SliceBackgrounds(string region)
        {
            foreach (var layer in new[] { "Bg", "Mid", "Back", "Fg" })
            {
                string path = $"{ArtPaths.Root}/Backgrounds/{region}/{region}_{layer}.png";
                if (!File.Exists(path)) throw new FileNotFoundException("Background layer missing; run tools/art/build_art.py", path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Pixel32Importer.Apply(importer);
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Repeat;   // layers tile horizontally for parallax scrolling
                importer.SaveAndReimport();
            }
        }
    }
}
