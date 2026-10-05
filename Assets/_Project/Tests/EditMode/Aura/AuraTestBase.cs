using System.Collections.Generic;
using AuraKnight.Aura.Skills;
using AuraKnight.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    /// <summary>Tracks scene objects created by a test and removes them afterwards.</summary>
    public abstract class AuraTestBase
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void CleanUp()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
            foreach (var p in Object.FindObjectsByType<FireballProjectile>(FindObjectsInactive.Include))
                if (p != null) Object.DestroyImmediate(p.gameObject);
        }

        protected GameObject Make(string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _created.Add(go);
            return go;
        }

        protected GameObject MakeBox(string name, Vector2 position, Vector2 size, bool trigger)
        {
            var go = Make(name, position);
            PhysicsLayers.Apply(go, trigger ? PhysicsLayers.Interactable : PhysicsLayers.Ground);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = trigger;
            return go;
        }

        protected static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        protected static void SetObjects(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
