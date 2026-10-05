using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Player;
using AuraKnight.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Editor
{
    /// <summary>Adds the phase 5 components (manager, binder, oxygen meter, visuals, Light2D) to the Player prefab root.</summary>
    static class AuraPlayerPrefabBuilder
    {
        public static void Attach(GameObject root, PlayerController controller, SpriteRenderer body, AuraDefinition[] definitions)
        {
            body.color = Color.white; // the Aura tint comes from the material property block, not the vertex colour

            var stats = root.GetComponent<PlayerStats>();
            var health = root.GetComponent<Health>();

            var manager = root.AddComponent<AuraManager>();
            AuraSerialized.SetObjects(manager, "definitions", definitions);
            PlayerGeneratorUtil.SetReference(manager, "controller", controller);
            PlayerGeneratorUtil.SetReference(manager, "stats", stats);

            var binder = root.AddComponent<PlayerAuraBinder>();
            PlayerGeneratorUtil.SetReference(binder, "controller", controller);

            var oxygen = root.AddComponent<OxygenMeter>();
            PlayerGeneratorUtil.SetReference(oxygen, "binder", binder);
            PlayerGeneratorUtil.SetReference(oxygen, "health", health);

            var glowObject = new GameObject("AuraGlow");
            glowObject.transform.SetParent(root.transform, false);
            var glow = glowObject.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.pointLightOuterRadius = AuraVisuals.FallbackRadius;
            glow.pointLightInnerRadius = 0f;
            glow.color = AuraVisuals.FallbackColor;
            glow.intensity = 1f;

            var visuals = root.AddComponent<AuraVisuals>();
            PlayerGeneratorUtil.SetReference(visuals, "glow", glow);
            PlayerGeneratorUtil.SetReference(visuals, "body", body);
        }
    }
}
