using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor.Tools
{
    /// <summary>
    /// Renders scenes to PNGs without entering play mode. Batch (needs a graphics device, so no -nographics):
    /// <c>tools/unity-batch.sh shot [-shotScenes a.unity;b.unity] [-shotSizes 1920x1080,2340x1080] [-shotPlayer] [-shotHud]</c>.
    /// Output: Logs/screenshots/&lt;job&gt;_&lt;w&gt;x&lt;h&gt;.png. Jobs run from EditorApplication.update so layout and animators get editor ticks
    /// between scenes. A job that fails (missing scene, blank image) is logged and counted; the process exits 1 if any failed.
    /// Menu: Aura/Screenshots/Capture Default Set.
    /// </summary>
    public static class SceneScreenshot
    {
        public const string OutputFolder = "Logs/screenshots";
        const int SettleTicks = 3;

        static Queue<ShotJob> _jobs;
        static Vector2Int[] _sizes;
        static ShotJob _current;
        static Camera _camera;
        static int _settle, _failures, _written;
        static bool _exitWhenDone;

        [MenuItem("Aura/Screenshots/Capture Default Set")]
        public static void RunFromMenu() => Start(ShotJob.Defaults(), ShotJob.DefaultSizes, false);

        /// <summary>Batch entry point; reads the -shot* arguments from the command line.</summary>
        public static void Run()
        {
            List<ShotJob> jobs;
            Vector2Int[] sizes;
            try { jobs = ShotJob.FromArgs(Environment.GetCommandLineArgs(), out sizes); }
            catch (Exception e)
            {
                Debug.LogError($"[SceneScreenshot] Bad arguments: {e.Message}");
                EditorApplication.Exit(1);
                return;
            }
            Start(jobs, sizes, true);
        }

        static void Start(List<ShotJob> jobs, Vector2Int[] sizes, bool exitWhenDone)
        {
            _jobs = new Queue<ShotJob>(jobs);
            _sizes = sizes;
            _exitWhenDone = exitWhenDone;
            _current = null;
            _failures = _written = 0;
            Directory.CreateDirectory(OutputFolder);
            EditorApplication.update += Tick;
            Debug.Log($"[SceneScreenshot] {jobs.Count} job(s) x {sizes.Length} size(s) -> {OutputFolder}");
        }

        static void Tick()
        {
            try
            {
                if (_current == null)
                {
                    if (_jobs.Count == 0) { Finish(); return; }
                    _current = _jobs.Dequeue();
                    _camera = ScreenshotSetup.Prepare(_current);
                    _settle = SettleTicks;
                    return;
                }
                if (_settle-- > 0) return;
                CaptureCurrent();
                _current = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneScreenshot] Job '{_current?.Name}' failed: {e}");
                _failures++;
                _current = null;
            }
        }

        static void CaptureCurrent()
        {
            foreach (var size in _sizes)
            {
                ScreenshotSetup.FitCanvases(size.x, size.y);
                var image = ScreenshotRender.Render(_camera, size.x, size.y);
                try
                {
                    string path = $"{OutputFolder}/{_current.Name}_{size.x}x{size.y}.png";
                    int colors = ScreenshotRender.DistinctColors(image);
                    if (colors < ScreenshotRender.MinDistinctColors)
                    {
                        _failures++;
                        Debug.LogError($"[SceneScreenshot] {path} is blank ({colors} distinct colour(s)); not written");
                        continue;
                    }
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    _written++;
                    Debug.Log($"[SceneScreenshot] {path} ({colors}+ distinct colours)");
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Debug.Log($"[SceneScreenshot] Done: {_written} written, {_failures} failure(s).");
            if (_exitWhenDone) EditorApplication.Exit(_failures == 0 && _written > 0 ? 0 : 1);
        }
    }
}
