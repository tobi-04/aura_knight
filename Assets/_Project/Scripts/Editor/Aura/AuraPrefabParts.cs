using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Placeholder-art helpers shared by the phase 5 prefab and scene builders.</summary>
    static class AuraPrefabParts
    {
        public static GameObject AddSprite(Transform parent, string name, Sprite sprite, Vector2 localPosition, Vector2 size, Color color, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            var material = PlayerGeneratorUtil.UnlitSpriteMaterial();
            if (material != null) renderer.sharedMaterial = material;
            return go;
        }

        public static BoxCollider2D AddBox(GameObject go, Vector2 size, bool trigger, Vector2 offset = default)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = offset;
            box.isTrigger = trigger;
            return box;
        }
    }
}
