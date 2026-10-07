using AuraKnight.Bosses;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Prefabs/Rooms/&lt;Region&gt;/Room_Boss_&lt;Region&gt;.prefab from Room_Template (40 x 22): Room id &lt;region&gt;_boss, greybox floor / ceiling / side walls
    /// (replace with tiles; delete the side walls where RoomExits are needed), entry and exit doors (closed only during the fight), the BossArena with its
    /// trigger zone and the boss prefab. Spawn point "default" is inside the entry door. The altar before this room is the level designer's job.
    /// </summary>
    static class BossRoomBuilder
    {
        public const string TemplatePath = "Assets/_Project/Prefabs/Rooms/_Template/Room_Template.prefab";
        const float RoomWidth = 40f, FloorTop = 1f, CeilingY = 22f, DoorHeight = 9f;
        static readonly Color Stone = new Color(0.35f, 0.38f, 0.45f);
        static readonly Color DoorColor = new Color(0.65f, 0.4f, 0.2f);

        public static bool Build(BossSpec spec, Sprite square)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath);
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
            if (template == null || bossPrefab == null)
            {
                Debug.LogError($"[BossRoomBuilder] Missing {(template == null ? TemplatePath : spec.PrefabPath)}; generate world assets / bosses first.");
                return false;
            }
            var room = (GameObject)PrefabUtility.InstantiatePrefab(template);
            try
            {
                PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                room.name = $"Room_Boss_{spec.RegionFolder}";
                SetRoomIds(room, spec);
                MoveSpawn(room);
                AddGreybox(room.transform, square, spec.Platforms);
                var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, room.transform);
                boss.transform.localPosition = new Vector3(spec.HomeX, FloorTop + spec.FootOffset, 0f);
                var doors = new[] { AddDoor(room.transform, square, "DoorEntry", 1.5f), AddDoor(room.transform, square, "DoorExit", RoomWidth - 1.5f) };
                AddArena(room.transform, boss.GetComponent<BossBase>(), doors);
                PlayerGeneratorUtil.EnsureFolder($"{BossSpec.RoomsFolder}/{spec.RegionFolder}");
                PrefabUtility.SaveAsPrefabAsset(room, spec.RoomPath);
            }
            finally { Object.DestroyImmediate(room); }
            return true;
        }

        static void SetRoomIds(GameObject room, BossSpec spec)
        {
            var component = room.GetComponent<Room>();
            PlayerGeneratorUtil.SetString(component, "roomId", spec.RoomId);
            PlayerGeneratorUtil.SetString(component, "regionId", spec.RegionId);
        }

        static void MoveSpawn(GameObject room)
        {
            var spawn = room.transform.Find("SpawnPoints/default");
            if (spawn != null) spawn.localPosition = new Vector3(3.5f, FloorTop + 1f, 0f);
        }

        static void AddGreybox(Transform room, Sprite square, bool platforms)
        {
            var grey = new GameObject("Greybox").transform;
            grey.SetParent(room, false);
            Block(grey, square, "Floor", 0f, 0f, RoomWidth, FloorTop);
            Block(grey, square, "Ceiling", 0f, CeilingY, RoomWidth, CeilingY + 1f);
            Block(grey, square, "WallLeft", -1f, 0f, 0f, CeilingY);
            Block(grey, square, "WallRight", RoomWidth, 0f, RoomWidth + 1f, CeilingY);
            if (!platforms) return;
            OneWay(grey, square, "Platform_A", 11f, 15f);
            OneWay(grey, square, "Platform_B", 19f, 23f);
        }

        static GameObject Block(Transform parent, Sprite square, string name, float left, float bottom, float right, float top)
        {
            var go = AuraPrefabParts.AddSprite(parent, name, square, Vector2.zero, new Vector2(right - left, top - bottom), Stone, 1);
            go.transform.localPosition = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.Ground);
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        /// <summary>A thin one-way platform 3.4 tiles above the floor (steam flood height is 1.3).</summary>
        static void OneWay(Transform parent, Sprite square, string name, float left, float right)
        {
            float top = FloorTop + 3.4f;
            var go = Block(parent, square, name, left, top - 0.4f, right, top);
            var collider = go.GetComponent<BoxCollider2D>();
            collider.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
        }

        static GameObject AddDoor(Transform room, Sprite square, string name, float x)
        {
            var door = Block(room, square, name, x - 0.5f, FloorTop, x + 0.5f, FloorTop + DoorHeight);
            door.GetComponent<SpriteRenderer>().color = DoorColor;
            door.SetActive(false);
            return door;
        }

        static void AddArena(Transform room, BossBase boss, GameObject[] doors)
        {
            var go = new GameObject("BossArena");
            go.transform.SetParent(room, false);
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.Interactable);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(21f, 5f);
            trigger.size = new Vector2(30f, 8f);
            var arena = go.AddComponent<BossArena>();
            PlayerGeneratorUtil.SetReference(arena, "boss", boss);
            var so = new SerializedObject(arena);
            var list = so.FindProperty("doors");
            list.arraySize = doors.Length;
            for (int i = 0; i < doors.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = doors[i];
            so.FindProperty("playfieldMin").vector2Value = new Vector2(2f, FloorTop);
            so.FindProperty("playfieldMax").vector2Value = new Vector2(RoomWidth - 2f, CeilingY - 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
