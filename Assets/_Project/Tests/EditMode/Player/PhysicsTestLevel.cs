using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    /// <summary>Builds static box colliders for EditMode physics-query tests and cleans them up.</summary>
    public sealed class PhysicsTestLevel
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        /// <summary>Box with the given centre and size (units).</summary>
        public GameObject Box(float cx, float cy, float w, float h, string name = "Box")
        {
            var go = new GameObject(name);
            PhysicsLayers.Apply(go, PhysicsLayers.Ground); // the motor only collides with the Ground layer
            go.transform.position = new Vector3(cx, cy, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
            _objects.Add(go);
            return go;
        }

        /// <summary>Box defined by its edges.</summary>
        public GameObject Rect(float left, float bottom, float right, float top, string name = "Box") =>
            Box((left + right) * 0.5f, (bottom + top) * 0.5f, right - left, top - bottom, name);

        public GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        public void Sync() => Physics2D.SyncTransforms();

        public void Dispose()
        {
            foreach (var go in _objects)
                if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }
    }
}
