using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AuraKnight.Tests.PlayMode.UI
{
    /// <summary>
    /// Writes Logs/screenshots/runtime_hud_WxH.png from the real game flow (Core + Region_Hub through WorldEntry.StartNewGame).
    /// Explicit: skipped by every normal run, and it needs a GPU, so run it with
    /// <c>UNITY_GRAPHICS=1 UNITY_TEST_FILTER=AuraKnight.Tests.PlayMode.UI.RuntimeScreenshotTests tools/unity-batch.sh test PlayMode</c>.
    /// Batch mode never fires WaitForEndOfFrame, so the main camera is rendered by hand into a RenderTexture, with the overlay
    /// canvases switched to that camera and their scale computed for the texture size (the stock scaler reads the display size).
    /// </summary>
    [Explicit("Needs a GPU; run through tools/unity-batch.sh with UNITY_GRAPHICS=1"), Category("Screenshot")]
    public sealed class RuntimeScreenshotTests : UiPlayModeBase
    {
        static readonly Vector2Int[] Sizes = { new Vector2Int(1920, 1080), new Vector2Int(2520, 1080) };

        [UnityTest]
        public IEnumerator CaptureHudAfterANewGame()
        {
            // A -nographics run (e.g. a namespace filter that pulls this class in) renders flat grey and would overwrite good shots.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device: set UNITY_GRAPHICS=1");
            Debug.Log("[RuntimeScreenshot] entering the world");
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Debug.Log("[RuntimeScreenshot] world entered");
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "screenshots"));
            Directory.CreateDirectory(dir);
            var camera = Camera.main;
            Assert.IsNotNull(camera, "main camera");
            foreach (var size in Sizes)
            {
                FitCanvases(camera, size);
                yield return WaitFrames(3);
                string path = Path.Combine(dir, $"runtime_hud_{size.x}x{size.y}.png");
                Render(camera, size, path);
            }
            // All three Auras unlocked, Fire current, energy half full: the coloured ring and the highlighted current Aura.
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.AuraUnlocked("Wind"));
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.AuraUnlocked("Fire"));
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.AuraUnlocked("Water"));
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.AuraChanged("Fire"));
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.EnergyChanged(50f, 100f));
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.HeartsChanged(3, 5));
            AuraKnight.Core.EventBus.Publish(new AuraKnight.Core.CoinsChanged(120));
            var popup = Find<AuraKnight.UI.AuraUnlockPopup>();
            Render(camera, Sizes[0], Path.Combine(dir, "runtime_popup_1920x1080.png")); // the unlock popup itself
            for (int i = 0; i < 3 && popup.IsVisible; i++) Button(popup, "Continue").onClick.Invoke();
            yield return WaitSeconds(0.4f);
            FitCanvases(camera, Sizes[0]);
            yield return WaitFrames(3);
            Render(camera, Sizes[0], Path.Combine(dir, "runtime_hud_auras_1920x1080.png"));
            camera.targetTexture = null;
        }

        internal static void FitCanvases(Camera camera, Vector2Int size)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                scaler.enabled = false;
                var reference = scaler.referenceResolution;
                float factor = Mathf.Pow(size.x / reference.x, 1f - scaler.matchWidthOrHeight) * Mathf.Pow(size.y / reference.y, scaler.matchWidthOrHeight);
                canvas.scaleFactor = factor;
            }
        }

        internal static void Render(Camera camera, Vector2Int size, string path)
        {
            var rt = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[RuntimeScreenshot] {path}");
            Object.Destroy(tex);
            Object.Destroy(rt);
        }
    }
}
