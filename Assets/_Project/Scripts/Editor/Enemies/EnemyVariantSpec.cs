using AuraKnight.Enemies;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Everything the generators need to build one variant: the GDD 7.3 numbers plus placeholder look and modifier flags.</summary>
    sealed class EnemyVariantSpec
    {
        public string Name;
        public EnemyArchetype Archetype;
        public int Hp;
        public int CoinsMin, CoinsMax;
        public Color Color;
        public Vector2 Size;           // sprite and body size in tiles
        public float MoveSpeed, ChargeSpeed;
        public float DetectRange = 6f;
        public float VerticalTolerance = 2.5f;
        public float TurnDelay;
        public float KnockbackScale = 1f;
        public float DiveSpeed = 7f;
        public float WindupSeconds = 0.5f;
        public bool FrontShield, PhaseThroughWalls, LifeSteal;

        public bool UsesGravity => Archetype == EnemyArchetype.Walker || Archetype == EnemyArchetype.Hopper;
        public bool HasSolidBody => Archetype != EnemyArchetype.Crawler && Archetype != EnemyArchetype.Static;
        public string StatsPath => EnemyAssetGenerator.DataFolder + "/" + Name + ".asset";
        public string PrefabPath => EnemyAssetGenerator.PrefabFolder + "/" + Name + ".prefab";
    }

    /// <summary>The eight variants of GDD 7.3 (HP, damage and coin numbers are authoritative there).</summary>
    static class EnemyVariants
    {
        public static readonly EnemyVariantSpec[] All =
        {
            new EnemyVariantSpec { Name = "BugThorn", Archetype = EnemyArchetype.Walker, Hp = 2, CoinsMin = 3, CoinsMax = 5,
                Color = new Color(0.35f, 0.75f, 0.30f), Size = new Vector2(0.9f, 0.7f), MoveSpeed = 1.5f, ChargeSpeed = 3.5f },
            new EnemyVariantSpec { Name = "PatrolBot", Archetype = EnemyArchetype.Walker, Hp = 4, CoinsMin = 3, CoinsMax = 5,
                Color = new Color(0.45f, 0.60f, 0.78f), Size = new Vector2(1f, 1f), MoveSpeed = 1.8f, ChargeSpeed = 3.8f },
            new EnemyVariantSpec { Name = "NightKnight", Archetype = EnemyArchetype.Walker, Hp = 6, CoinsMin = 3, CoinsMax = 5,
                Color = new Color(0.22f, 0.22f, 0.40f), Size = new Vector2(1f, 1.5f), MoveSpeed = 1.3f, ChargeSpeed = 3.0f,
                TurnDelay = 0.7f, KnockbackScale = 0.5f, FrontShield = true },
            new EnemyVariantSpec { Name = "PoisonShroom", Archetype = EnemyArchetype.Hopper, Hp = 2, CoinsMin = 3, CoinsMax = 3,
                Color = new Color(0.62f, 0.36f, 0.82f), Size = new Vector2(0.8f, 0.8f), MoveSpeed = 0f, ChargeSpeed = 0f, VerticalTolerance = 3f },
            new EnemyVariantSpec { Name = "Bat", Archetype = EnemyArchetype.Flyer, Hp = 2, CoinsMin = 4, CoinsMax = 6,
                Color = new Color(0.60f, 0.22f, 0.32f), Size = new Vector2(0.8f, 0.5f), MoveSpeed = 2f, ChargeSpeed = 2f,
                VerticalTolerance = 0f, LifeSteal = true },
            new EnemyVariantSpec { Name = "Ghost", Archetype = EnemyArchetype.Flyer, Hp = 4, CoinsMin = 4, CoinsMax = 6,
                Color = new Color(0.80f, 0.86f, 1f, 0.75f), Size = new Vector2(0.9f, 1.2f), MoveSpeed = 1.6f, ChargeSpeed = 1.6f,
                VerticalTolerance = 0f, DiveSpeed = 6f, WindupSeconds = 0.6f, PhaseThroughWalls = true },
            new EnemyVariantSpec { Name = "StoneSpider", Archetype = EnemyArchetype.Crawler, Hp = 3, CoinsMin = 4, CoinsMax = 4,
                Color = new Color(0.55f, 0.48f, 0.42f), Size = new Vector2(1f, 0.6f), MoveSpeed = 1.5f, ChargeSpeed = 2.5f,
                DetectRange = 5f, VerticalTolerance = 0f, KnockbackScale = 0f },
            new EnemyVariantSpec { Name = "ScrapZapper", Archetype = EnemyArchetype.Static, Hp = 4, CoinsMin = 4, CoinsMax = 4,
                Color = new Color(0.92f, 0.70f, 0.20f), Size = new Vector2(0.9f, 0.9f), MoveSpeed = 0f, ChargeSpeed = 0f,
                VerticalTolerance = 0f, KnockbackScale = 0f },
        };
    }
}
