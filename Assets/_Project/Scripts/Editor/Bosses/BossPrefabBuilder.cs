using AuraKnight.Bosses;
using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds one boss prefab. Layout: root (Enemy layer, kinematic Rigidbody2D, Health, boss class), child Visual (SpriteRenderer + Animator on the
    /// override controller from Art/Bosses, placeholder square when the art is missing), child Hurtbox (Enemy layer trigger), child ContactHitbox
    /// (EnemyAttack layer trigger), boss specific weak points and an Attacks child that carries the attack components.
    /// A Hitbox and a Hurtbox never share an object.
    /// </summary>
    static class BossPrefabBuilder
    {
        const float ContactRearmSeconds = 0.5f;
        const string LitMaterialPath = "Assets/_Project/Art/Materials/Mat_SpriteLit.mat";

        public static void Build(BossSpec spec, BossStats stats, Sprite square)
        {
            var root = new GameObject(spec.Name);
            try
            {
                PlayerGeneratorUtil.SetLayer(root, PhysicsLayers.Enemy);
                var rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                var health = root.AddComponent<Health>();
                PlayerGeneratorUtil.SetInt(health, "maxHealth", spec.Hp);
                var boss = AddBoss(root, spec.Kind);
                var visual = AddVisual(root.transform, spec, square);
                var hurtbox = AddHurtbox(root.transform, spec, health);
                var contact = AddContact(root.transform, spec);
                PlayerGeneratorUtil.SetReference(boss, "stats", stats);
                PlayerGeneratorUtil.SetReference(boss, "hurtbox", hurtbox);
                PlayerGeneratorUtil.SetReference(boss, "contactHitbox", contact);
                PlayerGeneratorUtil.SetReference(boss, "sprite", visual);
                PlayerGeneratorUtil.SetReference(boss, "animator", visual.GetComponent<Animator>());
                PlayerGeneratorUtil.SetReference(boss, "markerSprite", square);
                PlayerGeneratorUtil.SetReference(boss, "markerMaterial", PlayerGeneratorUtil.UnlitSpriteMaterial());
                BossAttackSetup.Build(spec, boss, health);
                PlayerGeneratorUtil.EnsureFolder(BossSpec.PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static BossBase AddBoss(GameObject root, BossKind kind)
        {
            switch (kind)
            {
                case BossKind.RootTree: return root.AddComponent<RootTreeBoss>();
                case BossKind.StoneSpider: return root.AddComponent<StoneSpiderBoss>();
                case BossKind.RogueMachine: return root.AddComponent<RogueMachineBoss>();
                default: return root.AddComponent<MalakorBoss>();
            }
        }

        static SpriteRenderer AddVisual(Transform root, BossSpec spec, Sprite square)
        {
            var art = LoadIdleSprite(spec);
            var go = new GameObject("Visual");
            go.transform.SetParent(root, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 4;
            if (art != null) renderer.sprite = art;
            else
            {
                renderer.sprite = square;
                renderer.color = new Color(0.45f, 0.3f, 0.5f);
                go.transform.localScale = new Vector3(spec.ArtSize.x, spec.ArtSize.y, 1f);
            }
            var lit = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
            renderer.sharedMaterial = art != null && lit != null ? lit : PlayerGeneratorUtil.UnlitSpriteMaterial();
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.ControllerPath);
            return renderer;
        }

        static Sprite LoadIdleSprite(BossSpec spec)
        {
            string wanted = spec.Name + "_Idle_0";
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(spec.SheetPath))
                if (asset is Sprite sprite && sprite.name == wanted) return sprite;
            return null;
        }

        static Hurtbox AddHurtbox(Transform root, BossSpec spec, Health health)
        {
            var go = Child(root, "Hurtbox", PhysicsLayers.Enemy);
            AuraPrefabParts.AddBox(go, spec.BodySize, true, spec.BodyOffset);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            PlayerGeneratorUtil.SetReference(hurtbox, "health", health);
            return hurtbox;
        }

        static Hitbox AddContact(Transform root, BossSpec spec)
        {
            var go = Child(root, "ContactHitbox", PhysicsLayers.EnemyAttack);
            AuraPrefabParts.AddBox(go, spec.BodySize * 0.9f, true, spec.BodyOffset);
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = Team.Enemy;
            PlayerGeneratorUtil.SetInt(hitbox, "damage", 1);
            PlayerGeneratorUtil.SetBool(hitbox, "activeOnEnable", true);
            PlayerGeneratorUtil.SetFloat(hitbox, "rearmInterval", ContactRearmSeconds);
            PlayerGeneratorUtil.SetReference(hitbox, "source", root);
            return hitbox;
        }

        /// <summary>A weak point child: its own Hurtbox (Enemy layer) and stand-in Health, forwarding damage times <paramref name="multiplier"/>.</summary>
        public static WeakPointHurtbox AddWeakPoint(Transform root, string name, Vector2 offset, Vector2 size, float multiplier, Health bossHealth)
        {
            var go = Child(root, name, PhysicsLayers.Enemy);
            go.transform.localPosition = offset;
            AuraPrefabParts.AddBox(go, size, true);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            go.AddComponent<Health>();
            var weak = go.AddComponent<WeakPointHurtbox>();
            PlayerGeneratorUtil.SetReference(weak, "target", bossHealth);
            PlayerGeneratorUtil.SetFloat(weak, "multiplier", multiplier);
            return weak;
        }

        static GameObject Child(Transform parent, string name, string layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            PlayerGeneratorUtil.SetLayer(go, layer);
            return go;
        }
    }
}
