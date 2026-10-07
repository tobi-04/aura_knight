using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Enemies.Modifiers;
using AuraKnight.World.Pickups;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds one variant prefab. Layout: root (Enemy layer: Rigidbody2D, solid body collider, Health, archetype class, modifiers),
    /// child Visual (SpriteRenderer + Animator + bridge), child Hurtbox (Enemy layer trigger), child ContactHitbox (EnemyAttack
    /// layer trigger, always armed, re-hits every 0.5 s), plus ZapHitbox for the static zapper and a Path of waypoints for crawlers.
    /// A Hitbox and a Hurtbox never share an object (different teams).
    /// </summary>
    static class EnemyPrefabBuilder
    {
        const float GravityScale = 3f;
        const float ContactRearmSeconds = 0.5f;
        const float HurtboxPadding = 0.1f;

        public static void Build(EnemyVariantSpec spec, EnemyStats stats, Sprite square, AnimatorController controller, PhysicsMaterial2D material)
        {
            var root = new GameObject(spec.Name);
            try
            {
                PlayerGeneratorUtil.SetLayer(root, PhysicsLayers.Enemy);
                var rb = AddBody(root, spec);
                var body = spec.HasSolidBody ? AddSolidCollider(root, spec, material) : null;
                var health = root.AddComponent<Health>();
                PlayerGeneratorUtil.SetInt(health, "maxHealth", spec.Hp);
                var visual = AddVisual(root.transform, spec, square, controller);
                var hurtbox = AddHurtbox(root.transform, spec, health);
                var contact = AddContactHitbox(root.transform, spec, root.transform);
                var enemy = AddArchetype(root, spec);
                Wire(enemy, stats, body, hurtbox, contact, visual);
                if (spec.Archetype == EnemyArchetype.Crawler || spec.Archetype == EnemyArchetype.Static) AddCrawlerParts(root, spec, enemy);
                AddModifiers(root, spec);
                var bridge = visual.gameObject.AddComponent<EnemyAnimatorBridge>();
                PlayerGeneratorUtil.SetReference(bridge, "enemy", enemy);
                PlayerGeneratorUtil.SetReference(bridge, "animator", visual.GetComponent<Animator>());
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static Rigidbody2D AddBody(GameObject root, EnemyVariantSpec spec)
        {
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = spec.HasSolidBody ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            rb.gravityScale = spec.UsesGravity ? GravityScale : 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            if (spec.HasSolidBody) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return rb;
        }

        static Collider2D AddSolidCollider(GameObject root, EnemyVariantSpec spec, PhysicsMaterial2D material)
        {
            var box = AuraPrefabParts.AddBox(root, spec.Size, false);
            box.sharedMaterial = material;
            return box;
        }

        static SpriteRenderer AddVisual(Transform root, EnemyVariantSpec spec, Sprite square, AnimatorController controller)
        {
            var go = AuraPrefabParts.AddSprite(root, "Visual", square, Vector2.zero, spec.Size, spec.Color, 3);
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = EnemyAnimatorControllerBuilder.OverrideFor(spec.Name) ?? controller;
            return go.GetComponent<SpriteRenderer>();
        }

        static Hurtbox AddHurtbox(Transform root, EnemyVariantSpec spec, Health health)
        {
            var go = new GameObject("Hurtbox");
            go.transform.SetParent(root, false);
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.Enemy);
            AuraPrefabParts.AddBox(go, spec.Size + Vector2.one * HurtboxPadding, true);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            PlayerGeneratorUtil.SetReference(hurtbox, "health", health);
            return hurtbox;
        }

        static Hitbox AddContactHitbox(Transform root, EnemyVariantSpec spec, Transform source)
        {
            var go = new GameObject("ContactHitbox");
            go.transform.SetParent(root, false);
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.EnemyAttack);
            AuraPrefabParts.AddBox(go, spec.Size * 0.9f, true);
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = Team.Enemy;
            PlayerGeneratorUtil.SetInt(hitbox, "damage", 1);
            PlayerGeneratorUtil.SetBool(hitbox, "activeOnEnable", true);
            PlayerGeneratorUtil.SetFloat(hitbox, "rearmInterval", ContactRearmSeconds);
            PlayerGeneratorUtil.SetReference(hitbox, "source", source);
            return hitbox;
        }

        static EnemyBase AddArchetype(GameObject root, EnemyVariantSpec spec)
        {
            switch (spec.Archetype)
            {
                case EnemyArchetype.Walker: return root.AddComponent<WalkerEnemy>();
                case EnemyArchetype.Hopper: return root.AddComponent<HopperEnemy>();
                case EnemyArchetype.Flyer: return root.AddComponent<FlyerEnemy>();
                default: return root.AddComponent<CrawlerEnemy>();
            }
        }

        static void Wire(EnemyBase enemy, EnemyStats stats, Collider2D body, Hurtbox hurtbox, Hitbox contact, SpriteRenderer visual)
        {
            PlayerGeneratorUtil.SetReference(enemy, "stats", stats);
            PlayerGeneratorUtil.SetReference(enemy, "body", body);
            PlayerGeneratorUtil.SetReference(enemy, "hurtbox", hurtbox);
            PlayerGeneratorUtil.SetReference(enemy, "contactHitbox", contact);
            PlayerGeneratorUtil.SetReference(enemy, "sprite", visual);
            PlayerGeneratorUtil.SetReference(enemy, "coinPrefab", AssetDatabase.LoadAssetAtPath<CoinPickup>(EnemyAssetGenerator.CoinPrefabPath));
            PlayerGeneratorUtil.SetReference(enemy, "lightDropPrefab", AssetDatabase.LoadAssetAtPath<LightDropPickup>(EnemyAssetGenerator.LightDropPrefabPath));
        }

        static void AddCrawlerParts(GameObject root, EnemyVariantSpec spec, EnemyBase enemy)
        {
            if (spec.Archetype == EnemyArchetype.Crawler)
            {
                var path = new GameObject("Path").transform;
                path.SetParent(root.transform, false);
                AddWaypoint(path, "P0", Vector2.zero);
                AddWaypoint(path, "P1", new Vector2(4f, 0f));
                PlayerGeneratorUtil.SetReference(enemy, "pathRoot", path);
                return;
            }
            var go = new GameObject("ZapHitbox");
            go.transform.SetParent(root.transform, false);
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.EnemyAttack);
            var ring = go.AddComponent<CircleCollider2D>();
            ring.isTrigger = true;
            ring.radius = 2f;
            var zap = go.AddComponent<Hitbox>();
            zap.Team = Team.Enemy;
            PlayerGeneratorUtil.SetReference(zap, "source", root.transform);
            PlayerGeneratorUtil.SetReference(enemy, "zapHitbox", zap);
        }

        static void AddWaypoint(Transform path, string name, Vector2 local)
        {
            var point = new GameObject(name).transform;
            point.SetParent(path, false);
            point.localPosition = local;
        }

        static void AddModifiers(GameObject root, EnemyVariantSpec spec)
        {
            if (spec.FrontShield) root.AddComponent<FrontShield>();
            if (spec.PhaseThroughWalls) root.AddComponent<PhaseThroughWalls>();
            if (spec.LifeSteal) root.AddComponent<LifeSteal>();
        }
    }
}
