using System;
using AuraKnight.Audio;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Progression;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Secret chest: opens once when Leo touches it, pays 100-150 coins or a free upgrade (per chest), saves, and stays open after a
    /// reload because its PersistentId is stored in <c>GameState.openedChests</c>.
    /// </summary>
    [RequireComponent(typeof(PersistentId), typeof(Collider2D))]
    public sealed class TreasureChest : MonoBehaviour
    {
        [SerializeField] ChestReward reward = ChestReward.Coins();
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Color closedColor = new Color(0.85f, 0.6f, 0.2f);
        [SerializeField] Color openColor = new Color(0.3f, 0.22f, 0.12f);

        PersistentId persistentId;

        public bool IsOpen { get; private set; }
        public string ChestId => PersistentId.Id;
        public ChestReward Reward => reward;

        /// <summary>Raised once when the chest opens (HUD toasts, tests).</summary>
        public event Action<ChestOutcome> Opened;

        PersistentId PersistentId => persistentId != null ? persistentId : (persistentId = GetComponent<PersistentId>());

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Start() => Restore();

        void OnEnable() => EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);

        void OnDisable() => EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);

        void OnStateLoaded(GameStateLoaded evt) => Restore();

        /// <summary>Shows the saved state: open when its id is in the save, closed otherwise (new game).</summary>
        void Restore()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;
            ShowOpen(ChestLogic.IsOpen(manager.State, ChestId));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) TryOpen();
        }

        /// <summary>Opens the chest if it is closed and there is a live game. Returns what it gave (<see cref="ChestOutcome.None"/> if nothing).</summary>
        public ChestOutcome TryOpen()
        {
            var manager = GameManager.Instance;
            if (IsOpen || manager == null) return ChestOutcome.None;
            var outcome = ChestLogic.Open(manager.State, ChestId, reward, UnityRandomSource.Instance);
            if (!outcome.Opened) return outcome;
            ShowOpen(true);
            if (outcome.UpgradeGranted) ShopService.RefreshPlayer();
            if (!manager.Save()) Debug.LogWarning($"[TreasureChest] '{ChestId}' opened but the save could not be written.", this);
            Sfx.Play(SfxId.Chest, transform.position);
            Opened?.Invoke(outcome);
            return outcome;
        }

        void ShowOpen(bool open)
        {
            IsOpen = open;
            if (sprite != null) sprite.color = open ? openColor : closedColor;
        }
    }
}
