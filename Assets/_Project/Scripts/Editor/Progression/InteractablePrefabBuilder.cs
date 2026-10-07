using AuraKnight.Core;
using AuraKnight.Progression;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Greybox prefabs of the TreasureChest and Tư Tế Sol (placeholder squares until the art pass). Place them in room prefabs; see docs.</summary>
    static class InteractablePrefabBuilder
    {
        public static void BuildAll()
        {
            BuildChest();
            BuildNpcSol();
        }

        static void BuildChest()
        {
            var root = new GameObject("TreasureChest");
            try
            {
                PhysicsLayers.Apply(root, PhysicsLayers.Interactable);
                var trigger = root.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(1.8f, 1.2f);
                trigger.offset = new Vector2(0f, 0.6f);
                root.AddComponent<PersistentId>(); // the id is set per placed instance: chest_<region>_<nn>
                var sprite = Visual(root.transform, "Sprite", new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 1.1f, 1f), new Color(0.85f, 0.6f, 0.2f));
                var chest = root.AddComponent<TreasureChest>();
                var so = new SerializedObject(chest);
                so.FindProperty("sprite").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ProgressionAssetGenerator.ChestPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void BuildNpcSol()
        {
            var root = new GameObject("NpcSol");
            try
            {
                PhysicsLayers.Apply(root, PhysicsLayers.Interactable);
                var zone = root.AddComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(6f, 3f);
                zone.offset = new Vector2(0f, 1.5f);
                Visual(root.transform, "Sprite", new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 1f), new Color(1f, 0.78f, 0.34f));
                var prompt = WorldPromptBuilder.Create(root.transform, "npc.sol.prompt", new Vector3(0f, 3.4f, 0f));
                var npc = root.AddComponent<NpcSol>();
                var so = new SerializedObject(npc);
                so.FindProperty("dialogue").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogueByProgress>(ProgressionAssetGenerator.DialoguePath);
                so.FindProperty("prompt").objectReferenceValue = prompt;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ProgressionAssetGenerator.NpcSolPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static SpriteRenderer Visual(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = color;
            sr.sortingOrder = 1;
            return sr;
        }
    }
}
