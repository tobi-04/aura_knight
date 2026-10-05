using AuraKnight.Combat;
using AuraKnight.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Adds the phase 4 combat components to the generated Player prefab root.</summary>
    static class PlayerCombatPrefabBuilder
    {
        public const int PlayerMaxHearts = 5;
        public const float HurtInvulnerableSeconds = 1f;

        public static void Attach(GameObject root, PlayerController controller, SpriteRenderer visual)
        {
            var health = root.AddComponent<Health>();
            PlayerGeneratorUtil.SetInt(health, "maxHealth", PlayerMaxHearts);
            PlayerGeneratorUtil.SetFloat(health, "invulnerableAfterHit", HurtInvulnerableSeconds);

            var hurtbox = root.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Player;
            hurtbox.AllowsPogo = false;
            PlayerGeneratorUtil.SetReference(hurtbox, "health", health);

            var stats = root.AddComponent<PlayerStats>();
            PlayerGeneratorUtil.SetReference(stats, "health", health);

            var impulse = root.AddComponent<CinemachineImpulseSource>();
            var feedback = root.AddComponent<CombatFeedback>();
            PlayerGeneratorUtil.SetReference(feedback, "impulse", impulse);

            var blink = root.AddComponent<SpriteBlink>();
            PlayerGeneratorUtil.SetReference(blink, "health", health);
            PlayerGeneratorUtil.SetReference(blink, "target", visual);

            var sword = BuildSword(root.transform);
            var combat = root.AddComponent<PlayerCombat>();
            PlayerGeneratorUtil.SetReference(combat, "controller", controller);
            PlayerGeneratorUtil.SetReference(combat, "stats", stats);
            PlayerGeneratorUtil.SetReference(combat, "swordHitbox", sword);
            PlayerGeneratorUtil.SetReference(combat, "feedback", feedback);
        }

        static Hitbox BuildSword(Transform parent)
        {
            var go = new GameObject("SwordHitbox");
            go.transform.SetParent(parent, false);
            var shape = SwordShape.Compute(AttackDirection.Forward, 1);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.offset = shape.Center;
            box.size = shape.Size;
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = Team.Player;
            return hitbox;
        }
    }
}
