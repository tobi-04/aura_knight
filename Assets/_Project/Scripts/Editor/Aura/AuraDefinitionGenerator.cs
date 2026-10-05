using AuraKnight.Aura;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Creates the four AuraDefinition assets with the authoritative GDD section 6 numbers.</summary>
    static class AuraDefinitionGenerator
    {
        public const string Folder = "Assets/_Project/Data/Auras";

        struct Spec
        {
            public AuraId Id; public string Name; public Color Color; public float Light; public float Cost; public float Cooldown;
            public bool DoubleJump, Glide, Swim, Heat, Acid; public float Speed;
        }

        static readonly Spec[] Specs =
        {
            new Spec { Id = AuraId.None, Name = "Không Aura", Color = new Color32(0xFF, 0xEB, 0x9E, 0xFF), Light = 3f, Speed = 1f },
            new Spec { Id = AuraId.Wind, Name = "Gió", Color = new Color32(0x27, 0xD3, 0x8C, 0xFF), Light = 4f, Cost = 25f, Cooldown = 0.5f, Speed = 1f, DoubleJump = true, Glide = true },
            new Spec { Id = AuraId.Fire, Name = "Hỏa", Color = new Color32(0xFF, 0x5C, 0x57, 0xFF), Light = 5f, Cost = 30f, Cooldown = 0.4f, Speed = 1.2f, Heat = true },
            new Spec { Id = AuraId.Water, Name = "Thủy", Color = new Color32(0x27, 0xB5, 0xF7, 0xFF), Light = 4f, Cost = 35f, Cooldown = 0.5f, Speed = 1f, Swim = true, Acid = true },
        };

        public static string PathFor(AuraId id) => $"{Folder}/{id}.asset";

        /// <summary>Creates or refreshes every definition; <paramref name="skillPrefabs"/> is indexed by <see cref="AuraId"/>.</summary>
        public static AuraDefinition[] Generate(AuraSkillBase[] skillPrefabs)
        {
            PlayerGeneratorUtil.EnsureFolder(Folder);
            var result = new AuraDefinition[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                var spec = Specs[i];
                var path = PathFor(spec.Id);
                var asset = AssetDatabase.LoadAssetAtPath<AuraDefinition>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<AuraDefinition>();
                    AssetDatabase.CreateAsset(asset, path);
                }
                Apply(asset, spec, skillPrefabs != null && (int)spec.Id < skillPrefabs.Length ? skillPrefabs[(int)spec.Id] : null);
                result[i] = asset;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        static void Apply(AuraDefinition asset, Spec spec, AuraSkillBase skill)
        {
            AuraSerialized.SetEnum(asset, "id", (int)spec.Id);
            AuraSerialized.SetString(asset, "displayName", spec.Name);
            AuraSerialized.SetColor(asset, "color", spec.Color);
            PlayerGeneratorUtil.SetFloat(asset, "lightRadius", spec.Light);
            PlayerGeneratorUtil.SetFloat(asset, "energyCost", spec.Cost);
            PlayerGeneratorUtil.SetFloat(asset, "skillCooldown", spec.Cooldown);
            AuraSerialized.SetNestedBool(asset, "passives.doubleJump", spec.DoubleJump);
            AuraSerialized.SetNestedBool(asset, "passives.glide", spec.Glide);
            AuraSerialized.SetNestedFloat(asset, "passives.speedMultiplier", spec.Speed);
            AuraSerialized.SetNestedBool(asset, "passives.swim", spec.Swim);
            AuraSerialized.SetNestedBool(asset, "passives.heatImmune", spec.Heat);
            AuraSerialized.SetNestedBool(asset, "passives.acidImmune", spec.Acid);
            PlayerGeneratorUtil.SetReference(asset, "skillPrefab", skill);
            EditorUtility.SetDirty(asset);
        }
    }
}
