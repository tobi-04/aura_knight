using System;
using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// The Castle gate: a solid blocker that goes away for good once all three <see cref="AuraSeal"/>s are lit (Wind + Fire + Water,
    /// so all three Auras must have been earned). Needs a PersistentId; the open state is saved as an opened gate.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public sealed class SealGate : MonoBehaviour
    {
        [SerializeField] GameObject blocker;
        [SerializeField] AuraSeal[] seals = Array.Empty<AuraSeal>();

        readonly List<bool> _lit = new List<bool>(3);
        PersistentId _id;

        public bool IsOpen { get; private set; }
        public string GateId => (_id != null ? _id : _id = GetComponent<PersistentId>()).Id;

        /// <summary>Raised with true when the gate opens.</summary>
        public event Action<bool> StateChanged;

        void Start()
        {
            RestoreSaved();
            Evaluate();
        }

        void OnEnable()
        {
            foreach (var seal in seals) if (seal != null) seal.Lit += OnSealLit;
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnDisable()
        {
            foreach (var seal in seals) if (seal != null) seal.Lit -= OnSealLit;
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnStateLoaded(GameStateLoaded evt)
        {
            RestoreSaved();
            Evaluate();
        }

        void OnSealLit(AuraSeal seal) => Evaluate();

        /// <summary>Opens the gate when every seal is lit. Returns whether it is open.</summary>
        public bool Evaluate()
        {
            if (IsOpen) return true;
            _lit.Clear();
            foreach (var seal in seals) _lit.Add(seal != null && seal.IsLit);
            if (!SealRules.AllLit(_lit)) return false;
            Open();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.State.MarkGateOpened(GateId);
                gm.Save();
            }
            return true;
        }

        void RestoreSaved()
        {
            var gm = GameManager.Instance;
            if (!IsOpen && gm != null && gm.State.HasOpenedGate(GateId)) Open();
        }

        void Open()
        {
            IsOpen = true;
            if (blocker != null) blocker.SetActive(false);
            StateChanged?.Invoke(true);
        }
    }
}
