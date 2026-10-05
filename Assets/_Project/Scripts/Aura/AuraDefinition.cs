using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>
    /// Everything that differs between Auras is data (GDD section 6): colour, light radius, passives, skill and cost.
    /// Code never branches on <see cref="id"/> outside the skill classes.
    /// </summary>
    [CreateAssetMenu(menuName = "Aura Knight/Aura Definition", fileName = "AuraDefinition")]
    public sealed class AuraDefinition : ScriptableObject
    {
        [SerializeField] AuraId id;
        [SerializeField] string displayName = "";
        [SerializeField] Color color = Color.white;
        [Tooltip("Light2D outer radius in tiles.")]
        [SerializeField, Min(0f)] float lightRadius = 3f;
        [SerializeField] AuraPassives passives = new AuraPassives { speedMultiplier = 1f };
        [Tooltip("Prefab with an AuraSkillBase; instantiated once under the player. Empty for None.")]
        [SerializeField] AuraSkillBase skillPrefab;
        [SerializeField, Min(0f)] float energyCost;
        [Tooltip("Seconds before the skill can be cast again.")]
        [SerializeField, Min(0f)] float skillCooldown = 0.5f;
        [Tooltip("Placeholder: played by the audio phase when this Aura is selected. May stay empty.")]
        [SerializeField] AudioClip switchSfx;

        public AuraId Id => id;
        public string DisplayName => displayName;
        public Color Color => color;
        public float LightRadius => lightRadius;
        public AuraPassives Passives => passives;
        public AuraSkillBase SkillPrefab => skillPrefab;
        public float EnergyCost => energyCost;
        public float SkillCooldown => skillCooldown;
        public AudioClip SwitchSfx => switchSfx;

        void OnValidate()
        {
            if (passives.speedMultiplier <= 0f) passives.speedMultiplier = 1f;
        }
    }
}
