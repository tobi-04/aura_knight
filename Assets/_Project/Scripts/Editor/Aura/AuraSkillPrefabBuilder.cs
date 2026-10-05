using AuraKnight.Aura;
using AuraKnight.Aura.Skills;
using AuraKnight.Combat;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Builds the skill prefabs under Prefabs/Aura: wind gust, fireball (+ projectile) and water shield.</summary>
    static class AuraSkillPrefabBuilder
    {
        public const string Folder = "Assets/_Project/Prefabs/Aura";
        public const string WindPath = Folder + "/WindGustSkill.prefab";
        public const string FireballPath = Folder + "/FireballSkill.prefab";
        public const string ProjectilePath = Folder + "/FireballProjectile.prefab";
        public const string WaterPath = Folder + "/WaterShieldSkill.prefab";

        public static AuraSkillBase[] BuildAll(Sprite square)
        {
            PlayerGeneratorUtil.EnsureFolder(Folder);
            var skills = new AuraSkillBase[AuraIds.All.Length];
            skills[(int)AuraId.Wind] = BuildWind();
            skills[(int)AuraId.Fire] = BuildFireball(square);
            skills[(int)AuraId.Water] = BuildWater(square);
            return skills;
        }

        static T Save<T>(GameObject root, string path) where T : Component
        {
            try { return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<T>(); }
            finally { Object.DestroyImmediate(root); }
        }

        static AuraSkillBase BuildWind()
        {
            var root = new GameObject("WindGustSkill");
            var skill = root.AddComponent<WindGustSkill>();
            var child = new GameObject("GustHitbox");
            child.transform.SetParent(root.transform, false);
            var circle = child.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = WindGustSkill.Radius;
            var hitbox = child.AddComponent<Hitbox>();
            hitbox.Team = Team.Player;
            hitbox.Damage = WindGustSkill.Damage;
            PlayerGeneratorUtil.SetReference(skill, "hitbox", hitbox);
            return Save<WindGustSkill>(root, WindPath);
        }

        static AuraSkillBase BuildFireball(Sprite square)
        {
            var projectileRoot = new GameObject("FireballProjectile");
            var circle = projectileRoot.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.25f;
            var hitbox = projectileRoot.AddComponent<Hitbox>();
            hitbox.Team = Team.Player;
            hitbox.Damage = FireballProjectile.Damage;
            var projectile = projectileRoot.AddComponent<FireballProjectile>();
            PlayerGeneratorUtil.SetReference(projectile, "hitbox", hitbox);
            AuraPrefabParts.AddSprite(projectileRoot.transform, "Visual", square, Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(0xFF, 0x7A, 0x3D, 0xFF), 12);
            var projectilePrefab = Save<FireballProjectile>(projectileRoot, ProjectilePath);

            var root = new GameObject("FireballSkill");
            var skill = root.AddComponent<FireballSkill>();
            PlayerGeneratorUtil.SetReference(skill, "projectilePrefab", projectilePrefab);
            return Save<FireballSkill>(root, FireballPath);
        }

        static AuraSkillBase BuildWater(Sprite square)
        {
            var root = new GameObject("WaterShieldSkill");
            var skill = root.AddComponent<WaterShieldSkill>();
            var visual = AuraPrefabParts.AddSprite(root.transform, "ShieldVisual", square, Vector2.zero, new Vector2(1.6f, 2.4f),
                new Color(0.15f, 0.71f, 0.97f, 0.35f), 11);
            PlayerGeneratorUtil.SetReference(skill, "shieldVisual", visual);
            return Save<WaterShieldSkill>(root, WaterPath);
        }
    }
}
