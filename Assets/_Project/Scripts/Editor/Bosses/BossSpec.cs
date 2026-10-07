using AuraKnight.Aura;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Which boss class a spec builds (decides the extra parts and the attack set).</summary>
    enum BossKind { RootTree, StoneSpider, RogueMachine, Malakor }

    /// <summary>One boss of GDD 7.4 as the generators see it: identity, numbers, art sizes and where its room lives.</summary>
    sealed class BossSpec
    {
        public const string DataFolder = "Assets/_Project/Data/Bosses";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Bosses";
        public const string RoomsFolder = "Assets/_Project/Prefabs/Rooms";
        public const string SceneFolder = "Assets/_Project/Scenes/Test";
        public const string ArtFolder = "Assets/_Project/Art/Bosses";

        public BossKind Kind;
        /// <summary>Boss id (saved in defeatedBosses) and art folder name.</summary>
        public string Name;
        public string DisplayName;
        public string RegionId;
        public string RegionFolder;
        public string Reward;
        public bool FinalBoss;
        public int Hp;
        public Vector2 ArtSize;
        public Vector2 BodySize;
        /// <summary>Hurtbox and contact hitbox centre relative to the boss origin.</summary>
        public Vector2 BodyOffset;
        public float FootOffset;
        public float HomeX;
        public bool Platforms;
        /// <summary>Auras the player already owns when reaching this boss; the test scene unlocks them.</summary>
        public AuraId[] AurasBefore;

        public string StatsPath => $"{DataFolder}/{Name}.asset";
        public string PrefabPath => $"{PrefabFolder}/{Name}.prefab";
        public string RoomId => $"{RegionId}_boss";
        public string RoomPath => $"{RoomsFolder}/{RegionFolder}/Room_Boss_{RegionFolder}.prefab";
        public string ScenePath => $"{SceneFolder}/Test_Boss_{Name}.unity";
        public string SheetPath => $"{ArtFolder}/{Name}/{Name}.png";
        public string ControllerPath => $"{ArtFolder}/{Name}/{Name}.overrideController";
    }

    static class BossSpecs
    {
        public static readonly BossSpec[] All =
        {
            new BossSpec
            {
                Kind = BossKind.RootTree, Name = "RootTree", DisplayName = "GỐC CÂY MỤC", RegionId = "forest", RegionFolder = "Forest",
                Reward = "Wind", Hp = 30, ArtSize = new Vector2(3f, 3f), BodySize = new Vector2(2.4f, 1.8f), BodyOffset = new Vector2(0f, -0.6f), FootOffset = 1.5f, HomeX = 34f,
                AurasBefore = new AuraId[0],
            },
            new BossSpec
            {
                Kind = BossKind.StoneSpider, Name = "GiantStoneSpider", DisplayName = "NHỆN ĐÁ", RegionId = "cave", RegionFolder = "Cave",
                Reward = "Fire", Hp = 40, ArtSize = new Vector2(4f, 3f), BodySize = new Vector2(3.6f, 2.4f), FootOffset = 1.5f, HomeX = 31f,
                AurasBefore = new[] { AuraId.Wind },
            },
            new BossSpec
            {
                Kind = BossKind.RogueMachine, Name = "RogueMachine", DisplayName = "CỖ MÁY NỔI LOẠN", RegionId = "city", RegionFolder = "City",
                Reward = "Water", Hp = 50, ArtSize = new Vector2(4f, 4f), BodySize = new Vector2(3f, 2.8f), BodyOffset = new Vector2(-0.2f, -0.4f), FootOffset = 2f, HomeX = 33f,
                Platforms = true, AurasBefore = new[] { AuraId.Wind, AuraId.Fire },
            },
            new BossSpec
            {
                Kind = BossKind.Malakor, Name = "Malakor", DisplayName = "CHÚA TỂ MALAKOR", RegionId = "castle", RegionFolder = "Castle",
                Reward = "None", FinalBoss = true, Hp = 70, ArtSize = new Vector2(3f, 4f), BodySize = new Vector2(1.6f, 3.2f), FootOffset = 2f,
                HomeX = 31f, AurasBefore = new[] { AuraId.Wind, AuraId.Fire, AuraId.Water },
            },
        };
    }
}
