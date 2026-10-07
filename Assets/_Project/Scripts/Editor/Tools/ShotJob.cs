using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AuraKnight.Editor.Tools
{
    /// <summary>One screenshot target: the scenes to open (first single, the rest additive) and what to add before rendering.</summary>
    public sealed class ShotJob
    {
        public string Name;
        public string[] ScenePaths;
        public bool AddPlayer, AddHud;
        /// <summary>Shows the main menu screen (screens start hidden; in play mode a splash/router does this).</summary>
        public bool ShowMainMenu;
        /// <summary>Camera offset from the player (or scene origin) in world units, to frame what the scene is about.</summary>
        public Vector2 FocusOffset;
        /// <summary>When set, the camera sits at this world position (a room far from the origin) instead of near the player / origin.</summary>
        public Vector2? Focus;
        /// <summary>Orthographic half-height; 0 keeps the scene camera's own size.</summary>
        public float OrthoSize;

        const string Scenes = "Assets/_Project/Scenes/";

        public static readonly Vector2Int[] DefaultSizes =
        {
            new Vector2Int(1920, 1080), new Vector2Int(2340, 1080), new Vector2Int(2520, 1080)
        };

        public static List<ShotJob> Defaults() => new List<ShotJob>
        {
            new ShotJob { Name = "MainMenu", ScenePaths = new[] { Scenes + "MainMenu.unity" }, ShowMainMenu = true },
            new ShotJob { Name = "Core_Region_Hub", ScenePaths = new[] { Scenes + "Core.unity", Scenes + "Region_Hub.unity" }, AddPlayer = true, AddHud = true },
            new ShotJob { Name = "Test_Movement", ScenePaths = new[] { Scenes + "Test/Test_Movement.unity" } },
            new ShotJob { Name = "Test_Aura", ScenePaths = new[] { Scenes + "Test/Test_Aura.unity" } },
            new ShotJob { Name = "Test_Enemies", ScenePaths = new[] { Scenes + "Test/Test_Enemies.unity" }, FocusOffset = new Vector2(12f, 0f) },
        };

        /// <summary>
        /// Command line overrides: <c>-shotScenes a.unity;b.unity</c> (one job per scene, file name as job name),
        /// <c>-shotSizes 1920x1080,2340x1080</c>, <c>-shotPlayer</c>, <c>-shotHud</c>, <c>-shotFocus x,y</c> (camera world position, to frame one room),
        /// <c>-shotOrtho size</c> (half height; 11.5 shows a whole 40 x 22 room). Without -shotScenes the default set runs.
        /// </summary>
        public static List<ShotJob> FromArgs(string[] args, out Vector2Int[] sizes)
        {
            sizes = ParseSizes(ValueOf(args, "-shotSizes")) ?? DefaultSizes;
            string scenes = ValueOf(args, "-shotScenes");
            if (string.IsNullOrEmpty(scenes)) return Defaults();
            bool player = args.Contains("-shotPlayer"), hud = args.Contains("-shotHud");
            var focus = ParseFocus(ValueOf(args, "-shotFocus"));
            float.TryParse(ValueOf(args, "-shotOrtho"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ortho);
            return scenes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(path => new ShotJob
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(path), ScenePaths = new[] { path }, AddPlayer = player, AddHud = hud,
                ShowMainMenu = path.Contains("MainMenu"), Focus = focus, OrthoSize = Mathf.Max(0f, ortho),
            }).ToList();
        }

        /// <summary>"x,y" to a world position; null when absent; throws on a malformed value.</summary>
        public static Vector2? ParseFocus(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var parts = text.Split(',');
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (parts.Length != 2 || !float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out float x)
                || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out float y))
                throw new FormatException($"Bad focus '{text}', expected x,y");
            return new Vector2(x, y);
        }

        static string ValueOf(string[] args, string key)
        {
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        /// <summary>"1920x1080,2340x1080" to sizes; null when absent; throws on a malformed entry.</summary>
        public static Vector2Int[] ParseSizes(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
            {
                var parts = entry.Trim().ToLowerInvariant().Split('x');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int w) || !int.TryParse(parts[1], out int h) || w < 16 || h < 16 || w > 8192 || h > 8192)
                    throw new FormatException($"Bad size '{entry}', expected WxH between 16 and 8192");
                return new Vector2Int(w, h);
            }).ToArray();
        }
    }
}
