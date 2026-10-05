using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Serialized-property setters the phase 5 generators need beyond <see cref="PlayerGeneratorUtil"/>.</summary>
    static class AuraSerialized
    {
        public static void SetObjects(Object target, string field, IList<Object> values)
        {
            var so = new SerializedObject(target);
            var property = Find(so, target, field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            Find(so, target, field).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnumArray(Object target, string field, int[] indices)
        {
            var so = new SerializedObject(target);
            var property = Find(so, target, field);
            property.arraySize = indices.Length;
            for (int i = 0; i < indices.Length; i++) property.GetArrayElementAtIndex(i).enumValueIndex = indices[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            Find(so, target, field).colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            Find(so, target, field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sets a nested relative field such as passives.doubleJump.</summary>
        public static void SetNestedBool(Object target, string path, bool value)
        {
            var so = new SerializedObject(target);
            Find(so, target, path).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetNestedFloat(Object target, string path, float value)
        {
            var so = new SerializedObject(target);
            Find(so, target, path).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Find(SerializedObject so, Object target, string path)
        {
            var property = so.FindProperty(path);
            if (property == null) throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{path}'");
            return property;
        }
    }
}
