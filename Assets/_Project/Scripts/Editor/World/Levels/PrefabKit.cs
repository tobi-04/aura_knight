using AuraKnight.Core;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Small helpers shared by the level prefab builders (serialized-field writes, children, colliders, greybox sprites).</summary>
    static class PrefabKit
    {
        static Sprite _square;

        /// <summary>The project's one-unit white square (PPU 32, the placeholder the player generator wrote); the built-in UI sprite is only 0.32 units.</summary>
        public static Sprite Square => _square != null ? _square : (_square = LoadSquare());

        public const string SquarePath = "Assets/_Project/Prefabs/Player/Placeholders/WhiteSquare.png";

        static Sprite LoadSquare() =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath) ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static GameObject Child(Transform parent, string name, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        public static GameObject OnLayer(GameObject go, string layer)
        {
            PhysicsLayers.Apply(go, layer);
            return go;
        }

        public static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 offset = default, bool trigger = false)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = offset;
            box.isTrigger = trigger;
            return box;
        }

        public static SpriteRenderer Sprite(GameObject go, Color color, int order, Sprite sprite = null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : Square;
            sr.color = color;
            sr.sortingOrder = order;
            var lit = AssetDatabase.LoadAssetAtPath<Material>(LevelPaths.LitMaterial);
            if (lit != null) sr.sharedMaterial = lit;
            return sr;
        }

        public static void SetString(Object target, string property, string value) => Edit(target, property, p => p.stringValue = value);
        public static void SetInt(Object target, string property, int value) => Edit(target, property, p => p.intValue = value);
        public static void SetFloat(Object target, string property, float value) => Edit(target, property, p => p.floatValue = value);
        public static void SetBool(Object target, string property, bool value) => Edit(target, property, p => p.boolValue = value);
        public static void SetRef(Object target, string property, Object value) => Edit(target, property, p => p.objectReferenceValue = value);
        public static void SetColor(Object target, string property, Color value) => Edit(target, property, p => p.colorValue = value);
        public static void SetVector2(Object target, string property, Vector2 value) => Edit(target, property, p => p.vector2Value = value);
        public static void SetVector3(Object target, string property, Vector3 value) => Edit(target, property, p => p.vector3Value = value);

        /// <summary>Box size / offset of a collider on a prefab instance (recorded as an override).</summary>
        public static void ResizeBox(BoxCollider2D box, Vector2 size, Vector2 offset)
        {
            SetVector2(box, "m_Size", size);
            SetVector2(box, "m_Offset", offset);
        }

        /// <summary>Scale / position of a child on a prefab instance (recorded as an override).</summary>
        public static void SetScale(Transform t, Vector3 scale) => SetVector3(t, "m_LocalScale", scale);
        public static void SetPosition(Transform t, Vector3 local) => SetVector3(t, "m_LocalPosition", local);

        public static void SetEnum(Object target, string property, string name) => Edit(target, property, p =>
        {
            int index = System.Array.IndexOf(p.enumNames, name);
            if (index < 0) throw new System.ArgumentException($"{target.GetType().Name}.{property} has no value '{name}'");
            p.enumValueIndex = index;
        });

        public static void SetRefs(Object target, string property, Object[] values) => Edit(target, property, p =>
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });

        static void Edit(Object target, string property, System.Action<SerializedProperty> write)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null) throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{property}'");
            write(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.InvalidOperationException($"Missing prefab {path}; run the generators in order (Aura/Regenerate All).");
            return prefab;
        }

        public static GameObject Place(string prefabPath, Transform parent, Vector3 localPosition)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(prefabPath), parent);
            go.transform.localPosition = localPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            return go;
        }
    }
}
