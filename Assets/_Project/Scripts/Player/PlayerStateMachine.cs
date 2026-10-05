using System;
using System.Collections.Generic;

namespace AuraKnight.Player
{
    /// <summary>Holds the registered states and switches between them (Exit, then Enter).</summary>
    public sealed class PlayerStateMachine
    {
        readonly Dictionary<PlayerStateId, IPlayerState> _states = new Dictionary<PlayerStateId, IPlayerState>();

        public IPlayerState Current { get; private set; }
        public PlayerStateId CurrentId { get; private set; }
        public PlayerStateId PreviousId { get; private set; }

        /// <summary>Raised with (from, to) just before the new state's Enter.</summary>
        public event Action<PlayerStateId, PlayerStateId> StateChanged;

        /// <summary>Adds or replaces the state behind <paramref name="id"/>. Later phases use this for Attack/Hurt/Dead/Swim.</summary>
        public void Register(PlayerStateId id, IPlayerState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _states[id] = state;
        }

        public bool IsRegistered(PlayerStateId id) => _states.ContainsKey(id);

        /// <summary>Returns false (and stays put) when the state is not registered.</summary>
        public bool TryChange(PlayerStateId id)
        {
            if (!_states.TryGetValue(id, out var next)) return false;
            if (Current == next) return true;

            var from = CurrentId;
            Current?.Exit();
            PreviousId = from;
            CurrentId = id;
            Current = next;
            StateChanged?.Invoke(from, id);
            next.Enter();
            return true;
        }
    }
}
