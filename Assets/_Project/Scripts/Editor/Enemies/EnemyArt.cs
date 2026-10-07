using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Where the Art pipeline puts each enemy's sheet, override controller and idle frame. Enemy ids (data, prefabs) and art ids
    /// (folders under Art/Enemies) differ for three variants; <see cref="EnemyVariantSpec.ArtId"/> is the single mapping and is
    /// copied to <c>EnemyStats.artId</c>. Paths are literals because this assembly does not reference AuraKnight.Editor.Art.
    /// </summary>
    static class EnemyArt
    {
        public const string Root = "Assets/_Project/Art/Enemies";
        public const string LitMaterialPath = "Assets/_Project/Art/Materials/Mat_SpriteLit.mat";
        const string IdleSuffix = "_Idle_0";

        public static string SheetPath(string artId) => $"{Root}/{artId}/{artId}.png";

        public static string OverrideControllerPath(string artId) => $"{Root}/{artId}/{artId}.overrideController";

        /// <summary>The first idle frame of the sliced sheet, or null when the sheet is missing or not sliced yet.</summary>
        public static Sprite LoadIdleSprite(string artId)
        {
            if (string.IsNullOrEmpty(artId)) return null;
            string wanted = artId + IdleSuffix;
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(SheetPath(artId)))
                if (asset is Sprite sprite && sprite.name == wanted) return sprite;
            return null;
        }
    }
}
