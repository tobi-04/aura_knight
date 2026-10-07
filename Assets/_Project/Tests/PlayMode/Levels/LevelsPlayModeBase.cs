using System.Collections;
using System.Linq;
using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>The real Core and region scenes (generated rooms), with helpers to start anywhere and to walk through doors the way Leo does.</summary>
    public abstract class LevelsPlayModeBase : WorldPlayTestBase
    {
        protected static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include);

        protected static T ById<T>(string id) where T : Component =>
            All<T>().First(c => c.TryGetComponent<PersistentId>(out var pid) && pid.Id == id);

        protected static string CurrentRoomId => RoomManager.Instance.Current.RoomId;

        protected static GameState SaveAt(string altarId, params string[] auras)
        {
            var state = GameState.NewGame();
            state.lastAltarId = altarId;
            state.maxHearts = 9;
            foreach (var aura in auras) state.unlockedAuras.Add(aura);
            state.currentAura = auras.Length > 0 ? auras[0] : GameState.NoAura;
            return state;
        }

        protected IEnumerator NewGame()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok, "new game entered");
            yield return SettlePhysics();
        }

        protected IEnumerator StartAt(string altarId, params string[] auras)
        {
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).Save(SaveAt(altarId, auras)));
            bool ok = false;
            yield return Enter(false, r => ok = r);
            Assert.IsTrue(ok, $"continue at {altarId}");
            yield return SettlePhysics();
        }

        /// <summary>Makes Leo immune for the walk (the traversal tests are about doors, not about surviving the enemies on the way).</summary>
        protected static void MakeInvulnerable() => Player.GetComponent<Health>().InvulnerabilityGate = () => true;

        protected static void Teleport(Vector2 position) => Player.GetComponent<PlayerController>().TeleportTo(position, false);

        /// <summary>Grants an Aura the way a boss does. The unlock popup pauses the game; closing it (Continue) resumes, as a player would.</summary>
        protected static IEnumerator Unlock(AuraId aura)
        {
            Assert.IsTrue(AuraManager.Instance.Unlock(aura), $"{aura} unlocked");
            yield return null;
            UI.PauseController.Instance?.Resume();
            yield return null;
        }

        protected static IEnumerator Seconds(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        /// <summary>Waits for a room's scene to be loaded (a gateway room preloads its neighbour while Leo is in it).</summary>
        protected static IEnumerator RoomLoaded(string roomId) =>
            WaitUntil(() => RoomRegistry.TryGet(roomId, out _), $"room {roomId} loaded", 20f);

        /// <summary>
        /// Where Leo stands when he has just walked into the exit from the room's inside: his edge 0.1 into the trigger. (Standing in the
        /// middle of a door that touches the next room's door would trigger both, which no walking Leo can do.)
        /// </summary>
        static Vector2 JustInside(RoomExit exit, Room room)
        {
            var box = exit.GetComponent<Collider2D>().bounds;
            const float halfWidth = 0.4f, overlap = 0.1f;
            bool leftDoor = box.center.x < room.transform.position.x + room.Bounds.bounds.extents.x;
            float x = leftDoor ? box.max.x + halfWidth - overlap : box.min.x - halfWidth + overlap;
            return new Vector2(x, box.min.y + 1f);
        }

        /// <summary>Steps onto the exit of the current room that leads to <paramref name="target"/> and checks the arrival: right room, at the named spawn, no bounce back.</summary>
        protected IEnumerator Through(string target)
        {
            yield return RoomLoaded(target);
            var from = RoomManager.Instance.Current;
            var exit = All<RoomExit>().FirstOrDefault(e => e.TargetRoomId == target && e.GetComponentInParent<Room>() == from);
            Assert.IsNotNull(exit, $"{from.RoomId} has an exit to {target}");
            var entered = new System.Collections.Generic.List<string>();
            System.Action<RoomEntered> record = e => entered.Add($"{e.RoomId}@{Time.realtimeSinceStartup:F2}");
            EventBus.Subscribe(record);
            Teleport(JustInside(exit, from));
            float deadline = Time.realtimeSinceStartup + 10f;
            while (CurrentRoomId != target && Time.realtimeSinceStartup < deadline) yield return null;
            EventBus.Unsubscribe(record);
            if (CurrentRoomId != target)
                Assert.Fail($"{from.RoomId} -> {target}: still in {CurrentRoomId}; Leo at {PlayerPosition} (exit {exit.GetComponent<Collider2D>().bounds.center}); rooms entered meanwhile: [{string.Join(", ", entered)}]");
            Assert.IsTrue(RoomManager.Instance.Current.TryGetSpawn(exit.TargetSpawnName, out var spawn), $"{target} has spawn {exit.TargetSpawnName}");
            AssertNear(spawn.position, PlayerPosition, "Leo arrives at the spawn", 1.5f);
            yield return Seconds(0.4f);
            Assert.AreEqual(target, CurrentRoomId, "no bounce back through the exit he arrived by");
            Assert.IsFalse(Player.GetComponent<Health>().IsDead);
        }
    }
}
