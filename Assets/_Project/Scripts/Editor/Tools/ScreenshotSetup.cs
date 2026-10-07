using System;
using AuraKnight.Player;
using AuraKnight.UI;
using AuraKnight.World;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.Editor.Tools
{
    /// <summary>
    /// Edit-mode scene preparation for <see cref="SceneScreenshot"/>: opens scenes, adds the Player and HUD when asked, frames the camera and
    /// turns Screen Space Overlay canvases into Screen Space Camera ones (an overlay canvas cannot be drawn into a RenderTexture).
    /// Nothing is saved; the scenes are discarded when the process exits.
    /// </summary>
    static class ScreenshotSetup
    {
        const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string HudPrefab = "Assets/_Project/Prefabs/UI/Hud.prefab";
        const int UiSortingBase = 1000;

        public static Camera Prepare(ShotJob job)
        {
            for (int i = 0; i < job.ScenePaths.Length; i++)
                EditorSceneManager.OpenScene(job.ScenePaths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);

            var player = job.AddPlayer ? EnsurePlayer() : UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (job.AddHud && UnityEngine.Object.FindFirstObjectByType<HudController>() == null) Instantiate(HudPrefab);

            if (job.ShowMainMenu)
                foreach (var menu in UnityEngine.Object.FindObjectsByType<MainMenuScreen>(FindObjectsInactive.Include)) menu.Show(true);

            var camera = EnsureCamera();
            foreach (var brain in UnityEngine.Object.FindObjectsByType<CinemachineBrain>()) brain.enabled = false; // else it overrides our framing
            foreach (var follow in camera.GetComponents<SimpleCameraFollow>()) follow.enabled = false;
            var focus = player != null ? (Vector2)player.transform.position : Vector2.zero;
            camera.transform.position = job.Focus.HasValue
                ? new Vector3(job.Focus.Value.x, job.Focus.Value.y, -10f)
                : new Vector3(focus.x + job.FocusOffset.x, focus.y + 2f + job.FocusOffset.y, -10f);
            if (job.OrthoSize > 0f) camera.orthographicSize = job.OrthoSize;
            ConvertOverlayCanvases(camera);
            return camera;
        }

        static PlayerController EnsurePlayer()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (existing != null) return existing;
            var instance = Instantiate(PlayerPrefab);
            var altar = UnityEngine.Object.FindFirstObjectByType<SunAltar>();
            var at = altar != null ? altar.transform.position + new Vector3(-2f, 0f, 0f) : new Vector3(0f, 1f, 0f);
            instance.transform.position = new Vector3(at.x, 0.97f, 0f); // the floor top is y = 0 in every generated scene
            return instance.GetComponent<PlayerController>();
        }

        static GameObject Instantiate(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException($"Prefab missing: {path}");
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        static Camera EnsureCamera()
        {
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                camera = new GameObject("ShotCamera") { tag = "MainCamera" }.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(0x07, 0x0D, 0x1F, 0xFF);
            }
            camera.orthographic = true;
            if (camera.orthographicSize < 1f) camera.orthographicSize = 6f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.enabled = true;
            return camera;
        }

        static void ConvertOverlayCanvases(Camera camera)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.sortingOrder += UiSortingBase;
            }
        }

        /// <summary>
        /// Recomputes every CanvasScaler for the render size. The stock scaler reads the display size, not the RenderTexture, so a
        /// ScaleWithScreenSize scaler is replaced by an equivalent constant scale factor.
        /// </summary>
        public static void FitCanvases(int width, int height)
        {
            foreach (var scaler in UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
            {
                var canvas = scaler.GetComponent<Canvas>();
                if (canvas != null && !canvas.isRootCanvas) continue;
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = ScaleFactor(scaler.referenceResolution, scaler.screenMatchMode, scaler.matchWidthOrHeight, width, height);
                }
            }
            Canvas.ForceUpdateCanvases();
        }

        static float ScaleFactor(Vector2 reference, CanvasScaler.ScreenMatchMode mode, float match, int width, int height)
        {
            float w = width / reference.x, h = height / reference.y;
            switch (mode)
            {
                case CanvasScaler.ScreenMatchMode.Expand: return Mathf.Min(w, h);
                case CanvasScaler.ScreenMatchMode.Shrink: return Mathf.Max(w, h);
                default: return Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(w, 2f), Mathf.Log(h, 2f), match));
            }
        }
    }
}
