using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// One-way door: stays blocked from the outside and opens when the player reaches this trigger
    /// from the inside. Opened state persists by PersistentId in GameState.openedShortcuts.
    /// </summary>
    [RequireComponent(typeof(PersistentId), typeof(Collider2D))]
    public sealed class Shortcut : MonoBehaviour
    {
        [Tooltip("Blocks the passage while closed; deactivated on open.")]
        [SerializeField] GameObject doorBlocker;

        PersistentId persistentId;

        public bool IsOpen { get; private set; }

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Awake() => persistentId = GetComponent<PersistentId>();

        void Start() => RestoreSaved();

        void OnEnable() => EventBus.Subscribe<GameStateLoaded>(OnGameStateLoaded);

        void OnDisable() => EventBus.Unsubscribe<GameStateLoaded>(OnGameStateLoaded);

        void OnGameStateLoaded(GameStateLoaded evt) => RestoreSaved();

        void RestoreSaved()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.State.HasOpenedShortcut(persistentId.Id)) ApplyOpen();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOpen && WorldTags.IsPlayer(other)) Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            ApplyOpen();
            var gm = GameManager.Instance;
            if (gm != null) gm.State.MarkShortcutOpened(persistentId.Id);
        }

        void ApplyOpen()
        {
            IsOpen = true;
            if (doorBlocker != null) doorBlocker.SetActive(false);
        }
    }
}
