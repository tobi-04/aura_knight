using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Builds one sprite-swap AnimationClip per manifest row, saved next to the sheet under Clips/.</summary>
    public static class ArtClipBuilder
    {
        static readonly EditorCurveBinding SpriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        public static string ClipPath(string sheetPng, string sheetName, string anim) =>
            $"{Path.GetDirectoryName(sheetPng).Replace('\\', '/')}/Clips/{sheetName}_{anim}.anim";

        /// <summary>Builds every clip of the sheet; returns clips keyed by animation name. Existing clips are rewritten in place (GUIDs stay stable).</summary>
        public static Dictionary<string, AnimationClip> BuildAll(string sheetPng, SheetManifest manifest)
        {
            var sprites = SheetSlicer.LoadSprites(sheetPng);
            var clips = new Dictionary<string, AnimationClip>();
            Directory.CreateDirectory(Path.GetDirectoryName(ClipPath(sheetPng, manifest.name, "x")));
            foreach (var anim in manifest.anims)
                clips[anim.name] = Build(ClipPath(sheetPng, manifest.name, anim.name), manifest, anim, sprites);
            AssetDatabase.SaveAssets();
            return clips;
        }

        static AnimationClip Build(string path, SheetManifest manifest, SheetAnim anim, Dictionary<string, Sprite> sprites)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew) clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
            clip.frameRate = anim.fps;
            var keys = new ObjectReferenceKeyframe[anim.frames + 1];
            for (int i = 0; i < anim.frames; i++)
            {
                string spriteName = SheetManifest.SpriteName(manifest.name, anim.name, i);
                if (!sprites.TryGetValue(spriteName, out var sprite))
                    throw new KeyNotFoundException($"Sprite '{spriteName}' missing from sliced sheet");
                keys[i] = new ObjectReferenceKeyframe { time = i / anim.fps, value = sprite };
            }
            // Closing key holds the last frame for one frame duration so non-looping clips do not cut short.
            keys[anim.frames] = new ObjectReferenceKeyframe { time = anim.frames / anim.fps, value = keys[anim.frames - 1].value };
            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = anim.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
            return clip;
        }
    }
}
