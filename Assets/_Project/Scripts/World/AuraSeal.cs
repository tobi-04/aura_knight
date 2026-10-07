using System;
using AuraKnight.Aura;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// One of the three seals before the Castle gate. Standing on it while wearing its Aura lights it for good (saved as an opened
    /// gate under its PersistentId, e.g. seal_wind). The Aura has to be unlocked to be worn, so a seal cannot be lit early.
    /// </summary>
    [RequireComponent(typeof(PersistentId), typeof(Collider2D))]
    public sealed class AuraSeal : MonoBehaviour
    {
        [SerializeField] AuraId aura = AuraId.Wind;
        [SerializeField] SpriteRenderer glow;
        [SerializeField] Color litColor = Color.white;

        PersistentId _id;
        bool _playerInside;

        public AuraId Aura => aura;
        public bool IsLit { get; private set; }
        public string SealId => (_id != null ? _id : _id = GetComponent<PersistentId>()).Id;

        /// <summary>Raised once when the seal lights.</summary>
        public event Action<AuraSeal> Lit;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void Start() => RestoreSaved();

        void OnEnable()
        {
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            EventBus.Subscribe<AuraChanged>(OnAuraChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
            EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);
        }

        void OnStateLoaded(GameStateLoaded evt) => RestoreSaved();

        void OnAuraChanged(AuraChanged evt)
        {
            if (_playerInside) TryLight(AuraIds.ParseOrNone(evt.AuraId));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!WorldTags.IsPlayer(other)) return;
            _playerInside = true;
            var manager = AuraManager.Instance;
            if (manager != null) TryLight(manager.Current);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) _playerInside = false;
        }

        /// <summary>Lights the seal if <paramref name="worn"/> is its Aura. Returns whether it is lit afterwards.</summary>
        public bool TryLight(AuraId worn)
        {
            if (IsLit) return true;
            if (!SealRules.Lights(aura, worn)) return false;
            Show();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.State.MarkGateOpened(SealId);
                gm.Save();
            }
            Lit?.Invoke(this);
            return true;
        }

        void RestoreSaved()
        {
            var gm = GameManager.Instance;
            if (gm == null || IsLit || !gm.State.HasOpenedGate(SealId)) return;
            Show();
            Lit?.Invoke(this);
        }

        void Show()
        {
            IsLit = true;
            if (glow != null) glow.color = litColor;
        }
    }
}
