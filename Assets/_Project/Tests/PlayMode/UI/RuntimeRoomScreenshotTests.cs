using System.Collections;
using System.IO;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.UI
{
    /// <summary>
    /// Writes Logs/screenshots/runtime_room_&lt;id&gt;.png: one representative room per region plus a boss arena, rendered from the real
    /// Core camera after Leo was moved into the room (continue at the region's altar, then the room's default spawn) and the follow
    /// camera and parallax had time to settle. Explicit and GPU-only, like <see cref="RuntimeScreenshotTests"/>:
    /// <c>UNITY_GRAPHICS=1 UNITY_TEST_FILTER=AuraKnight.Tests.PlayMode.UI.RuntimeRoomScreenshotTests tools/unity-batch.sh test PlayMode</c>.
    /// </summary>
    [Explicit("Needs a GPU; run through tools/unity-batch.sh with UNITY_GRAPHICS=1"), Category("Screenshot")]
    public sealed class RuntimeRoomScreenshotTests : UiPlayModeBase
    {
        static readonly Vector2Int Size = new Vector2Int(1920, 1080);

        // altar to continue at (loads the region), room to capture.
        static readonly (string altar, string room)[] Targets =
        {
            ("hub_altar_01", "hub_01"),
            ("forest_altar_01", "forest_03"),
            ("cave_altar_01", "cave_01"),
            ("city_altar_01", "city_02"),
            ("castle_altar_01", "castle_03"),
            ("forest_altar_02", "forest_boss"),
            ("castle_altar_02", "castle_boss"),
        };

        [UnityTest]
        public IEnumerator CaptureOneRoomPerRegion()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("No graphics device: set UNITY_GRAPHICS=1");
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "screenshots"));
            Directory.CreateDirectory(dir);
            var save = new SaveSystem(new FileSaveStorage(SavePath));
            foreach (var (altar, room) in Targets)
            {
                var state = GameState.NewGame();
                state.lastAltarId = altar;
                state.maxHearts = 9;
                foreach (var aura in new[] { "Wind", "Fire", "Water" }) state.unlockedAuras.Add(aura);
                state.currentAura = "Wind";
                Assert.IsTrue(save.Save(state));
                bool ok = false;
                yield return Enter(false, r => ok = r);
                Assert.IsTrue(ok, $"continue at {altar}");
                yield return WaitUntil(() => RoomRegistry.TryGet(room, out _), $"room {room} loaded", 20f);
                Assert.IsTrue(RoomManager.Instance.EnterRoom(room, "default", Player.transform), room);
                yield return WaitSeconds(2.5f); // follow camera + confiner blend + parallax settle
                var camera = Camera.main;
                Assert.IsNotNull(camera, "main camera");
                RuntimeScreenshotTests.Render(camera, Size, Path.Combine(dir, $"runtime_room_{room}.png"));
            }
        }
    }
}
