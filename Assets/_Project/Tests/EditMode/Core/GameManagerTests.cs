using AuraKnight.Core;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Core
{
    /// <summary>
    /// Start/continue flow of the GameManager without a running player loop (Awake does not run in edit mode;
    /// StartNewGame/Continue/Save are plain methods).
    /// </summary>
    public sealed class GameManagerTests
    {
        sealed class MemoryStorage : ISaveStorage
        {
            public string Text;
            public int Writes;
            public bool Exists() => Text != null;
            public string ReadText() => Text;
            public void WriteText(string text) { Text = text; Writes++; }
            public void Delete() => Text = null;
        }

        GameObject _go;
        GameManager _manager;
        MemoryStorage _storage;
        int _loadedEvents;
        bool _lastWasNewGame;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _go = new GameObject("GameManager");
            _manager = _go.AddComponent<GameManager>();
            _storage = new MemoryStorage();
            _manager.UseSaveSystem(new SaveSystem(_storage));
            _loadedEvents = 0;
            EventBus.Subscribe<GameStateLoaded>(e => { _loadedEvents++; _lastWasNewGame = e.IsNewGame; });
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void StartNewGame_ReplacesTheStateAndAnnouncesIt()
        {
            _manager.State.coins = 99;
            _manager.StartNewGame();
            Assert.AreEqual(0, _manager.State.coins);
            Assert.AreEqual(GameState.StartAltarId, _manager.State.lastAltarId);
            Assert.AreEqual(1, _loadedEvents);
            Assert.IsTrue(_lastWasNewGame);
        }

        [Test]
        public void Continue_LoadsTheSaveAndAnnouncesIt()
        {
            var saved = GameState.NewGame();
            saved.lastAltarId = "forest_altar_01";
            saved.maxHearts = 7;
            Assert.IsTrue(new SaveSystem(_storage).Save(saved));

            Assert.IsTrue(_manager.Continue());
            Assert.AreEqual("forest_altar_01", _manager.State.lastAltarId);
            Assert.AreEqual(7, _manager.State.maxHearts);
            Assert.AreEqual(1, _loadedEvents);
            Assert.IsFalse(_lastWasNewGame);
        }

        [Test]
        public void Continue_WithoutASave_LeavesTheStateAndStaysSilent()
        {
            _manager.State.coins = 5;
            Assert.IsFalse(_manager.Continue());
            Assert.AreEqual(5, _manager.State.coins);
            Assert.AreEqual(0, _loadedEvents);
        }

        [Test]
        public void StartAndContinueDoNotStartTheClockByThemselves()
        {
            _manager.StartNewGame();
            Assert.AreEqual(GameMode.Menu, _manager.Mode, "the world flow sets Playing once the player is placed");
        }

        [Test]
        public void HasSave_RequiresALoadableFile()
        {
            Assert.IsFalse(_manager.HasSave);
            _manager.Save();
            Assert.IsTrue(_manager.HasSave);
        }

        [Test]
        public void ModeAllowsControl_WithoutAManagerIsTrue() => Assert.IsTrue(GameManager.ModeAllowsControl);
    }
}
