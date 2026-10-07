using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace AuraKnight.Editor
{
    /// <summary>Sprite Atlas v2 assets: Atlas_Player (Leo) and one per region (its tileset, its enemies and its boss). Backgrounds stay outside atlases.</summary>
    public static class AtlasBuilder
    {
        static readonly (string region, string[] folders)[] RegionContent =
        {
            ("Hub", new string[0]),
            ("Forest", new[] { "Enemies/ThornBug", "Enemies/MushroomHopper", "Bosses/RootTree" }),
            ("Cave", new[] { "Enemies/Bat", "Enemies/StoneSpider", "Bosses/GiantStoneSpider" }),
            ("City", new[] { "Enemies/PatrolRobot", "Enemies/ScrapZapper", "Bosses/RogueMachine" }),
            ("Castle", new[] { "Enemies/NightKnight", "Enemies/Ghost", "Bosses/Malakor" }),
        };

        public static string AtlasPath(string name) => $"{ArtPaths.Root}/Atlases/Atlas_{name}.spriteatlasv2";

        public static void BuildAll()
        {
            Directory.CreateDirectory($"{ArtPaths.Root}/Atlases");
            Build("Player", new[] { "Characters/Leo" });
            foreach (var (region, folders) in RegionContent)
                Build(region, new[] { "Tilesets/" + region }.Concat(folders).ToArray());
            AssetDatabase.SaveAssets();
        }

        static void Build(string name, string[] relativeFolders)
        {
            string path = AtlasPath(name);
            var asset = new SpriteAtlasAsset();
            asset.Add(relativeFolders.Select(f => (Object)AssetDatabase.LoadAssetAtPath<DefaultAsset>($"{ArtPaths.Root}/{f}"))
                .Where(o => o != null).ToArray());
            asset.SetIncludeInBuild(true);
            SpriteAtlasAsset.Save(asset, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            var packing = importer.packingSettings;
            packing.enableRotation = false;       // rotated pixel art shimmers under the pixel perfect camera
            packing.enableTightPacking = false;
            packing.padding = 4;
            importer.packingSettings = packing;
            var texture = importer.textureSettings;
            texture.filterMode = FilterMode.Point;
            texture.generateMipMaps = false;
            texture.sRGB = true;
            importer.textureSettings = texture;
            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.maxTextureSize = 2048;
            importer.SetPlatformSettings(platform);
            importer.SaveAndReimport();
        }
    }
}
