using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Training targets for Test_Movement: ground dummy, floating dummy, spike strip (pogo).</summary>
    static class CombatTestObjects
    {
        static readonly Color DummyColor = new Color(0.95f, 0.55f, 0.25f);
        static readonly Color SpikeColor = new Color(0.9f, 0.25f, 0.3f);

        public static void Build(Transform parent, Sprite square)
        {
            Dummy(parent, square, "Dummy_Ground", new Vector2(-6f, 0.9f), new Vector2(1f, 1.8f), 30);
            Dummy(parent, square, "Dummy_Floating", new Vector2(-4f, 4.5f), new Vector2(1.2f, 1.2f), 30);
            Spikes(parent, square, new Vector2(-9f, 0.25f), new Vector2(3f, 0.5f));
        }

        static GameObject Visual(Transform parent, Sprite square, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            var material = PlayerGeneratorUtil.UnlitSpriteMaterial();
            if (material != null) renderer.sharedMaterial = material;
            return go;
        }

        static void Dummy(Transform parent, Sprite square, string name, Vector2 position, Vector2 size, int hp)
        {
            var go = Visual(parent, square, name, position, size, DummyColor);
            go.AddComponent<BoxCollider2D>().isTrigger = true; // no body: Leo walks through it, the sword still finds it
            var health = go.AddComponent<Health>();
            PlayerGeneratorUtil.SetInt(health, "maxHealth", hp);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            hurtbox.AllowsPogo = true;
            PlayerGeneratorUtil.SetReference(hurtbox, "health", health);
            go.AddComponent<DummyTarget>();
        }

        static void Spikes(Transform parent, Sprite square, Vector2 position, Vector2 size)
        {
            var go = Visual(parent, square, "Spikes", position, size, SpikeColor);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Hazard;
            hurtbox.AllowsPogo = true;
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = Team.Hazard;
            hitbox.Damage = 1;
            hitbox.RearmInterval = 0.25f;
            PlayerGeneratorUtil.SetBool(hitbox, "activeOnEnable", true);
        }
    }
}
