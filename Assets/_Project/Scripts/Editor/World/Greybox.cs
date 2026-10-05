using AuraKnight.Core;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Placeholder geometry shared by the world scene generators.</summary>
    static class Greybox
    {
        /// <summary>A solid floor strip (top edge at y = 1) on the Ground layer, as a child of the room.</summary>
        public static void AddFloor(Transform room, float width)
        {
            var floor = WorldAssetGenerator.Child(room.gameObject, "GreyboxFloor");
            PhysicsLayers.Apply(floor, PhysicsLayers.Ground);
            floor.transform.localPosition = new Vector3(width / 2f, 0.5f, 0f);
            floor.transform.localScale = new Vector3(width, 1f, 1f);
            floor.AddComponent<BoxCollider2D>().size = Vector2.one;
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(0.45f, 0.45f, 0.5f);
        }
    }
}
