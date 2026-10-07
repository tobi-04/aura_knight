using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraKnight.Editor.Tools
{
    /// <summary>Camera to RenderTexture to Texture2D, plus the blank-image check.</summary>
    static class ScreenshotRender
    {
        /// <summary>A real frame has far more colours than this; a cleared or failed render has one.</summary>
        public const int MinDistinctColors = 8;
        const int ColorCountCap = 4096;

        public static Texture2D Render(Camera camera, int width, int height)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { sRGB = true };
            var target = RenderTexture.GetTemporary(descriptor);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                // Two passes: the first builds canvas meshes and lighting resources, the second is the frame we keep.
                for (int pass = 0; pass < 2; pass++) Submit(camera, target);
                RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                var pixels = image.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) pixels[i].a = 255; // a cleared backbuffer has alpha 0; PNGs should be opaque
                image.SetPixels32(pixels);
                image.Apply(false);
                return image;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        static void Submit(Camera camera, RenderTexture target)
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
            else camera.Render();
        }

        /// <summary>Distinct opaque colours, counted up to a cap (enough to tell a frame from a flat fill).</summary>
        public static int DistinctColors(Texture2D image)
        {
            var seen = new HashSet<int>();
            foreach (var p in image.GetPixels32())
            {
                seen.Add(p.r | (p.g << 8) | (p.b << 16));
                if (seen.Count >= ColorCountCap) break;
            }
            return seen.Count;
        }
    }
}
