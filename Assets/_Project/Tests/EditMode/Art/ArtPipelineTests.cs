using System.IO;
using System.Linq;
using AuraKnight.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AuraKnight.Tests.Art
{
    /// <summary>Checks the output of Aura/Art/Generate All: import settings, sliced sprites, clips, controllers, rule tiles, atlases, licences.</summary>
    public sealed class ArtPipelineTests
    {
        static readonly string[] BridgeParameters =
            { "Speed", "VelY", "Grounded", "WallSlide", "Dash", "Slide", "Swim", "Attack", "AirAttack", "Hurt", "Dead", "ComboStep", "AimDir" };

        [Test]
        public void EveryPixelArtTextureUsesPixel32Settings()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", ArtPaths.PixelFolders).Select(AssetDatabase.GUIDToAssetPath).Where(Pixel32Importer.IsPixelArt).ToList();
            Assert.Greater(paths.Count, 30, "art not generated; run Aura/Art/Generate All");
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(32f, importer.spritePixelsPerUnit, path);
                Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
                Assert.IsFalse(importer.mipmapEnabled, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);
            }
        }

        [Test]
        public void PresetAndMaterialExist()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(ArtPaths.PresetPath));
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtPaths.MaterialPath);
            Assert.AreEqual("Universal Render Pipeline/2D/Sprite-Lit-Default", material.shader.name);
        }

        [Test]
        public void LeoSheetHasAllRequiredClips()
        {
            var manifest = SheetManifest.Load(ArtPaths.LeoSheet);
            foreach (var name in new[] { "Idle", "Run", "Jump", "Fall", "Attack1", "Attack2", "AirAttack", "AirAttackUp", "AirAttackDown",
                         "Dash", "Slide", "WallSlide", "Hurt", "Death", "Swim" })
                Assert.IsTrue(manifest.anims.Any(a => a.name == name), name);
            var sprites = SheetSlicer.LoadSprites(ArtPaths.LeoSheet);
            foreach (var anim in manifest.anims)
            {
                for (int i = 0; i < anim.frames; i++) Assert.IsTrue(sprites.ContainsKey(SheetManifest.SpriteName("Leo", anim.name, i)));
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ArtClipBuilder.ClipPath(ArtPaths.LeoSheet, "Leo", anim.name));
                Assert.IsNotNull(clip, anim.name);
                Assert.AreEqual(anim.loop, clip.isLooping, anim.name);
                Assert.AreEqual(anim.frames + 1, AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)[0]).Length);
            }
            Assert.AreEqual(new Vector2(64, 64), sprites["Leo_Idle_0"].rect.size);
            Assert.AreEqual(32f, sprites["Leo_Idle_0"].pixelsPerUnit);
        }

        [Test]
        public void LeoControllerExposesTheBridgeParametersAndStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ArtPaths.LeoController);
            var names = controller.parameters.Select(p => p.name).ToList();
            foreach (var p in BridgeParameters) CollectionAssert.Contains(names, p);
            var states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (var s in new[] { "Idle", "Run", "Jump", "Fall", "Attack1", "Attack2", "AirAttack", "Hurt", "Death", "Swim", "Dash", "Slide", "WallSlide" })
                CollectionAssert.Contains(states, s);
            Assert.AreEqual("Idle", controller.layers[0].stateMachine.defaultState.name);
            Assert.IsTrue(controller.layers[0].stateMachine.states.All(s => s.state.motion != null));
        }

        [Test]
        public void EveryEnemyAndBossHasAnOverrideControllerWithRealClips()
        {
            foreach (var enemy in ArtPaths.Enemies) AssertOverride(ArtPaths.EnemySheet(enemy), new[] { "Idle", "Move", "Attack", "Hurt", "Death" });
            foreach (var boss in ArtPaths.Bosses) AssertOverride(ArtPaths.BossSheet(boss), new[] { "Idle", "Move", "Attack1", "Attack2", "Attack3", "Hurt", "Death" });
        }

        static void AssertOverride(string sheetPng, string[] clipNames)
        {
            string name = Path.GetFileNameWithoutExtension(sheetPng);
            var over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{Path.GetDirectoryName(sheetPng).Replace('\\', '/')}/{name}.overrideController");
            Assert.IsNotNull(over, name);
            foreach (var clip in clipNames)
            {
                var mapped = over[clip];
                Assert.IsNotNull(mapped, $"{name}/{clip}");
                Assert.AreEqual($"{name}_{clip}", mapped.name);
            }
        }

        [Test]
        public void EveryRegionHasRuleTilesAndAnAtlas()
        {
            foreach (var region in ArtPaths.Regions)
            {
                foreach (var kind in new[] { "Ground", "Wall", "Platform", "Spikes" })
                {
                    var tile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTileBuilder.TilePath(region, kind));
                    Assert.IsNotNull(tile, $"{region} {kind}");
                    Assert.IsNotNull(tile.m_DefaultSprite);
                    Assert.IsTrue(tile.m_TilingRules.Count > 0);
                    Assert.IsTrue(tile.m_TilingRules.All(r => r.m_Sprites.All(s => s != null)));
                }
                Assert.IsTrue(File.Exists(AtlasBuilder.AtlasPath(region)), region);
            }
            Assert.IsTrue(File.Exists(AtlasBuilder.AtlasPath("Player")));
        }

        [Test]
        public void GroundRuleTilePicksEdgeSpritesByNeighbours()
        {
            var tile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTileBuilder.TilePath("Forest", "Ground"));
            var top = tile.m_TilingRules.Single(r => r.m_Sprites[0].name == "Forest_Ground_T");
            var neighbours = top.GetNeighbors();
            Assert.AreEqual(2, neighbours[new Vector3Int(0, 1, 0)], "top edge needs empty space above");
            Assert.AreEqual(1, tile.m_TilingRules.Count(r => r.m_Sprites[0].name == "Forest_Ground_C"));
        }

        [Test]
        public void LicencesListEveryArtSource()
        {
            string text = File.ReadAllText(ArtPaths.Root + "/LICENSES.md");
            foreach (var needle in new[] { "Kenney", "Tiny Dungeon", "CC0", "generated, project-owned", "Characters/Leo", "Enemies", "Bosses", "Tilesets", "Backgrounds" })
                StringAssert.Contains(needle, text);
        }
    }
}
