using System.Collections.Generic;
using System.Linq;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Editor;
using AuraKnight.Enemies;
using AuraKnight.Player;
using AuraKnight.Progression;
using AuraKnight.World;
using AuraKnight.World.Hazards;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Tests.Levels
{
    /// <summary>The generated room prefabs and region scenes agree with the room files (run Aura/Generate Levels, or Regenerate All, first).</summary>
    public sealed class GeneratedLevelTests
    {
        static GameObject Prefab(string roomId)
        {
            var room = LevelTestKit.Room(roomId);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPaths.RoomPrefab(room, LevelRegions.Get(room.Region)));
            Assert.IsNotNull(prefab, $"prefab of {roomId}");
            return prefab;
        }

        static IEnumerable<GameObject> AllPrefabs() => LevelTestKit.Catalog.Select(r => Prefab(r.Id));

        [Test]
        public void ThePrefabValidatorFindsNothing()
        {
            var problems = LevelPrefabValidator.Validate(LevelTestKit.Catalog);
            CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryRoomPrefabCarriesItsIdsBoundsAndEnemyContainer()
        {
            foreach (var file in LevelTestKit.Catalog)
            {
                var room = Prefab(file.Id).GetComponent<Room>();
                Assert.AreEqual(file.Id, room.RoomId);
                Assert.AreEqual(file.Region, room.RegionId);
                Assert.AreEqual(file.PreloadRegion ?? "", room.PreloadRegionId ?? "", file.Id);
                var bounds = room.Bounds.GetPath(0);
                Assert.AreEqual(4, bounds.Length);
                Assert.AreEqual(file.Width, bounds.Max(p => p.x), file.Id);
                Assert.AreEqual(file.Height, bounds.Max(p => p.y), file.Id);
            }
        }

        [Test]
        public void EveryDoorHasAnExitAndEveryExitANamedSpawnInTheTarget()
        {
            foreach (var file in LevelTestKit.Catalog)
            {
                var prefab = Prefab(file.Id);
                var room = prefab.GetComponent<Room>();
                foreach (var door in file.Doors)
                {
                    var exit = prefab.GetComponentsInChildren<RoomExit>(true).FirstOrDefault(e => e.TargetRoomId == door.Target);
                    Assert.IsNotNull(exit, $"{file.Id} -> {door.Target}");
                    Assert.AreEqual(RoomFile.EntrySpawnFor(door.Target, file.Id), exit.TargetSpawnName);
                    Assert.IsTrue(room.TryGetSpawn(RoomFile.SpawnName(door.Target), out var spawn), $"{file.Id} spawn for {door.Target}");
                    var cell = file.SpawnCell(door);
                    Assert.AreEqual(cell.x + 0.5f, spawn.localPosition.x, 1e-4f);
                    Assert.AreEqual(cell.y + 1f, spawn.localPosition.y, 1e-4f);
                }
                Assert.IsTrue(room.TryGetSpawn("default", out _), $"{file.Id} has a default spawn");
            }
        }

        [Test]
        public void AnExitNeverSitsOnTheSpawnItSends()
        {
            foreach (var file in LevelTestKit.Catalog)
                foreach (var door in file.Doors)
                {
                    var cell = file.SpawnCell(door);
                    float leoLeft = cell.x + 0.5f - 0.4f, leoRight = cell.x + 0.5f + 0.4f;
                    if (door.OnLeft) Assert.Greater(leoLeft, door.X + 1f, $"{file.Id} door {door.Digit}: arriving Leo would overlap the exit he arrived through");
                    else Assert.Less(leoRight, door.X, $"{file.Id} door {door.Digit}");
                }
        }

        [Test]
        public void ColliderBoxesFillExactlyTheSolidCells()
        {
            foreach (var file in LevelTestKit.Catalog)
            {
                var solid = Prefab(file.Id).transform.Find("Collision").Cast<Transform>().Where(t => !t.name.StartsWith("OneWay"));
                float area = 0f;
                foreach (var t in solid) area += t.GetComponent<BoxCollider2D>().size.x * t.GetComponent<BoxCollider2D>().size.y;
                int cells = file.Cells('#').Count + file.Cells('W').Count;
                Assert.AreEqual(cells, area, 0.001f, file.Id);
                foreach (Transform t in Prefab(file.Id).transform.Find("Collision"))
                    Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Ground), t.gameObject.layer, $"{file.Id}/{t.name}");
            }
        }

        [Test]
        public void OnlyTheSmoothWallsCarryTheSmoothMarkerAndEveryWallCellHasOne()
        {
            foreach (var file in LevelTestKit.Catalog)
            {
                var boxes = Prefab(file.Id).transform.Find("Collision").Cast<Transform>().ToList();
                var smooth = boxes.Where(t => t.GetComponent<SmoothWall>() != null).ToList();
                Assert.AreEqual(boxes.Count(t => t.name.StartsWith("Smooth_")), smooth.Count, file.Id);
                Assert.AreEqual(file.Cells('W').Count, smooth.Sum(t => t.GetComponent<BoxCollider2D>().size.x * t.GetComponent<BoxCollider2D>().size.y), 0.001f, file.Id);
            }
        }

        [Test]
        public void OneWayPlatformsUseAnEffector()
        {
            foreach (var file in LevelTestKit.Catalog)
                foreach (Transform t in Prefab(file.Id).transform.Find("Collision"))
                {
                    if (!t.name.StartsWith("OneWay")) continue;
                    Assert.IsTrue(t.GetComponent<BoxCollider2D>().usedByEffector, $"{file.Id}/{t.name}");
                    Assert.IsTrue(t.GetComponent<PlatformEffector2D>().useOneWay, $"{file.Id}/{t.name}");
                }
        }

        [Test]
        public void HazardsLiveOnTheHazardLayerAndKillZonesAreLethal()
        {
            int hazard = LayerMask.NameToLayer(PhysicsLayers.Hazard);
            int spikes = 0, lethal = 0;
            foreach (var prefab in AllPrefabs())
                foreach (var s in prefab.GetComponentsInChildren<Spikes>(true))
                {
                    spikes++;
                    Assert.AreEqual(hazard, s.gameObject.layer, s.name);
                    var hitbox = s.GetComponent<Hitbox>();
                    Assert.AreEqual(Team.Hazard, hitbox.Team);
                    if (s.Lethal) { lethal++; Assert.GreaterOrEqual(s.EffectiveDamage, 9, "a pit kills at any heart count"); }
                    else Assert.AreEqual(1, s.EffectiveDamage);
                }
            Assert.Greater(spikes, 20);
            Assert.Greater(lethal, 0);
        }

        [Test]
        public void EveryHazardKindIsPlacedInTheRegionItBelongsTo()
        {
            int Count<T>(string region) where T : Component =>
                LevelTestKit.Catalog.Where(r => r.Region == region).Sum(r => Prefab(r.Id).GetComponentsInChildren<T>(true).Length);
            Assert.Greater(Count<Spikes>("forest"), 0);
            Assert.Greater(Count<CollapsingPlatform>("forest"), 0);
            Assert.Greater(Count<FallingStalactite>("cave"), 0);
            Assert.Greater(Count<HeatVent>("city"), 0, "steam: the existing HeatVent");
            Assert.Greater(Count<Piston>("city"), 0);
            Assert.Greater(Count<AcidPool>("city"), 0);
            Assert.Greater(Count<MovingSpikeFloor>("castle"), 0);
            Assert.Greater(Count<ExtinguishableGate>("castle"), 0, "fire traps: the existing gate");
            Assert.AreEqual(0, Count<Piston>("forest") + Count<AcidPool>("forest") + Count<MovingSpikeFloor>("cave"));
        }

        [Test]
        public void CollapsingPlatformsAreSolidWithATriggerSensorAboveThem()
        {
            foreach (var platform in AllPrefabs().SelectMany(p => p.GetComponentsInChildren<CollapsingPlatform>(true)))
            {
                Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Ground), platform.Body.layer);
                Assert.IsFalse(platform.Body.GetComponent<BoxCollider2D>().isTrigger);
                var sensor = platform.GetComponent<BoxCollider2D>();
                Assert.IsTrue(sensor.isTrigger);
                Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Interactable), platform.gameObject.layer);
                Assert.Greater(sensor.offset.y, 1f, "the sensor sits above the top face");
                Assert.AreEqual(platform.Body.GetComponent<BoxCollider2D>().size.x, sensor.size.x, 1e-4f);
            }
        }

        [Test]
        public void EnemiesAreUnderTheEnemiesContainerOfTheirRoomAndDoNotExceedSix()
        {
            foreach (var prefab in AllPrefabs())
            {
                var enemies = prefab.GetComponentsInChildren<EnemyBase>(true);
                Assert.LessOrEqual(enemies.Length, 6, prefab.name);
                var container = prefab.GetComponent<Room>().GetType().GetField("enemiesContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(prefab.GetComponent<Room>()) as GameObject;
                foreach (var enemy in enemies) Assert.IsTrue(enemy.transform.IsChildOf(container.transform), $"{prefab.name}/{enemy.name}");
            }
        }

        [Test]
        public void EachRegionUsesItsOwnEnemies()
        {
            var expected = new Dictionary<string, string[]>
            {
                ["forest"] = new[] { "BugThorn", "PoisonShroom" }, ["cave"] = new[] { "Bat", "StoneSpider" },
                ["city"] = new[] { "PatrolBot", "ScrapZapper" }, ["castle"] = new[] { "NightKnight", "Ghost" },
            };
            foreach (var pair in expected)
            {
                var names = LevelTestKit.Catalog.Where(r => r.Region == pair.Key).SelectMany(r => Prefab(r.Id).GetComponentsInChildren<EnemyBase>(true))
                    .Select(e => e.name.Replace("(Clone)", "").Trim()).Distinct().ToList();
                foreach (var name in names) CollectionAssert.Contains(pair.Value, name, pair.Key);
                Assert.AreEqual(pair.Value.Length, names.Count, $"{pair.Key} uses both of its enemy kinds");
            }
        }

        [Test]
        public void ChestsAreInPlaceWithTheirIdsAndRewards()
        {
            var seen = new List<string>();
            foreach (var file in LevelTestKit.Catalog)
                foreach (var chest in Prefab(file.Id).GetComponentsInChildren<TreasureChest>(true))
                {
                    seen.Add(chest.ChestId);
                    bool upgrade = file.Rewards.ContainsKey(chest.ChestId);
                    Assert.AreEqual(upgrade ? ChestRewardKind.Upgrade : ChestRewardKind.Coins, chest.Reward.kind, chest.ChestId);
                }
            CollectionAssert.AreEquivalent(new[]
            {
                "chest_forest_01", "chest_forest_02", "chest_cave_01", "chest_cave_02", "chest_city_01", "chest_city_02", "chest_castle_01", "chest_castle_02",
            }, seen);
        }

        [Test]
        public void TheHubGatesAreTheRealGates()
        {
            var hub = Prefab("hub_03");
            var barricade = hub.GetComponentsInChildren<BurnableGate>(true).Single();
            Assert.AreEqual("gate_hub_barricade", barricade.GetComponent<PersistentId>().Id);
            var blocker = barricade.transform.Find("Blocker");
            Assert.AreEqual(new Vector3(2f, 5f, 1f), blocker.localScale, "2 wide, 5 tall: floor to the slab above");
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Ground), blocker.gameObject.layer);

            var gate = hub.GetComponentsInChildren<SealGate>(true).Single();
            Assert.AreEqual("gate_hub_castle", gate.GetComponent<PersistentId>().Id);
            var seals = hub.GetComponentsInChildren<AuraSeal>(true);
            CollectionAssert.AreEquivalent(SealRules.SealAuras, seals.Select(s => s.Aura));
            var wired = new SerializedObject(gate).FindProperty("seals");
            Assert.AreEqual(3, wired.arraySize, "the gate waits for all three seals");
            var zones = hub.GetComponentsInChildren<RegionPreloadZone>(true).Select(z => z.RegionId);
            CollectionAssert.AreEquivalent(new[] { "city", "castle" }, zones);
            Assert.AreEqual("cave", hub.GetComponent<Room>().PreloadRegionId);
        }

        [Test]
        public void TheCaveWallInThePrefabIsOneSixTallSmoothBlock()
        {
            var wall = Prefab("cave_01").transform.Find("Collision").Cast<Transform>().Where(t => t.GetComponent<SmoothWall>() != null).ToList();
            Assert.AreEqual(1, wall.Count);
            var box = wall[0].GetComponent<BoxCollider2D>();
            Assert.AreEqual(6f, box.size.y);
            Assert.AreEqual(1f, wall[0].localPosition.y - box.size.y * 0.5f, 1e-4f, "starts on the floor");
        }

        [Test]
        public void EveryRoomHasTheFourParallaxLayersAtTheGddSpeeds()
        {
            foreach (var prefab in AllPrefabs().Concat(LevelRegions.All.Where(r => r.HasBoss).Select(r => AssetDatabase.LoadAssetAtPath<GameObject>(r.BossPrefabPath))))
            {
                var factors = prefab.GetComponentsInChildren<ParallaxLayer>(true).Select(l => l.Factor).OrderBy(f => f).ToArray();
                CollectionAssert.AreEqual(ParallaxMath.Factors, factors, prefab.name);
            }
        }

        [Test]
        public void BossArenasHaveAWayBackAndNoLeftWall()
        {
            foreach (var region in LevelRegions.All.Where(r => r.HasBoss))
            {
                var arena = AssetDatabase.LoadAssetAtPath<GameObject>(region.BossPrefabPath);
                Assert.IsNull(arena.transform.Find("Greybox/WallLeft"), region.Id);
                var exit = arena.GetComponentsInChildren<RoomExit>(true).Single();
                Assert.AreEqual(region.BossPreviousRoom, exit.TargetRoomId);
                Assert.AreEqual(RoomFile.SpawnName(region.BossRoomId), exit.TargetSpawnName);
                Assert.IsTrue(arena.GetComponent<Room>().TryGetSpawn("default", out _), "the way in is the default spawn");
            }
        }

        [Test]
        public void RegionScenesHoldEveryRoomOnItsSlotAndTheirGlobalLight()
        {
            foreach (var region in LevelRegions.All)
            {
                var scene = EditorSceneManager.OpenScene(region.ScenePath, OpenSceneMode.Additive);
                try
                {
                    var rooms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Room>(true)).ToDictionary(r => r.RoomId);
                    foreach (var file in LevelCatalog.InRegion(LevelTestKit.Catalog, region.Id))
                    {
                        Assert.IsTrue(rooms.TryGetValue(file.Id, out var room), $"{file.Id} in {region.Scene}");
                        var expected = LevelRegions.WorldPosition(file);
                        Assert.AreEqual(expected.x, room.transform.position.x, 1e-3f, file.Id);
                        Assert.AreEqual(expected.y, room.transform.position.y, 1e-3f, file.Id);
                    }
                    Assert.AreEqual(region.HasBoss, rooms.ContainsKey(region.BossRoomId), region.Id);
                    var lighting = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RegionLighting>(true)).Single();
                    Assert.AreEqual(region.Id, lighting.RegionId);
                    var light = lighting.GetComponent<Light2D>();
                    Assert.AreEqual(Light2D.LightType.Global, light.lightType);
                    Assert.AreEqual(RegionLightingTable.IntensityOf(region.Id), light.intensity, 1e-6f);
                    Assert.IsFalse(scene.GetRootGameObjects().Any(r => r.name == "StartRoom"), "no leftover greybox start room");
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        [Test]
        public void RoomsOfDifferentRegionsNeverOverlapInTheWorld()
        {
            var boxes = new List<(string id, Rect rect)>();
            foreach (var file in LevelTestKit.Catalog)
                boxes.Add((file.Id, new Rect(LevelRegions.WorldPosition(file), new Vector2(file.Width, file.Height))));
            foreach (var region in LevelRegions.All.Where(r => r.HasBoss))
                boxes.Add((region.BossRoomId, new Rect(LevelRegions.WorldPosition(region, region.BossSlot, 40), new Vector2(40, 22))));
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                    Assert.IsFalse(boxes[i].rect.Overlaps(boxes[j].rect), $"{boxes[i].id} overlaps {boxes[j].id}");
        }

        [Test]
        public void TheRegionGraphListsEveryAltarOfTheRoomFiles()
        {
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>("Assets/_Project/Data/World/RegionGraph.asset");
            foreach (var region in LevelRegions.All)
            {
                var node = graph.Regions.First(r => r.regionId == region.Id);
                CollectionAssert.AreEqual(LevelCatalog.AltarIds(LevelTestKit.Catalog, region.Id), node.altarIds, region.Id);
            }
        }

        [Test]
        public void TheExistingValidatorsStayClean()
        {
            var errors = RoomIdValidator.Validate(out int rooms);
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            Assert.AreEqual(34, rooms, "30 generated rooms + 4 boss arenas");
            CollectionAssert.IsEmpty(EnemyRoomLimitValidator.Validate());
        }
    }
}
