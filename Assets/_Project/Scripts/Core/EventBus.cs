using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// Lightweight, type-safe static pub/sub. Gameplay publishes struct events; UI/Audio subscribe,
    /// so gameplay never references UI directly (GDD §12.3).
    /// Handlers must unsubscribe in OnDisable/OnDestroy. All subscribers are cleared on play-mode entry.
    /// Subscriber lists are copy-on-write arrays, so Publish never allocates and a handler may
    /// (un)subscribe while an event is being delivered. Trade-off: delivery uses the snapshot taken when Publish
    /// started, so a handler removed mid-publish is still called once for that event, and one added mid-publish
    /// first hears the next event. Handlers must tolerate a late call after unsubscribing (check their own state).
    /// </summary>
    public static class EventBus
    {
        static readonly List<Action> Clearers = new();
        static readonly List<Func<int>> Counters = new();

        /// <summary>Total live subscriptions across every event type (leak detection in tests).</summary>
        public static int SubscriberCount
        {
            get
            {
                int total = 0;
                foreach (var count in Counters) total += count();
                return total;
            }
        }

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            Channel<T>.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            Channel<T>.Remove(handler);
        }

        /// <summary>Delivers to a snapshot of subscribers; one throwing handler never blocks the rest.</summary>
        public static void Publish<T>(T evt) where T : struct
        {
            var snapshot = Channel<T>.Handlers;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i](evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        /// <summary>Removes every subscriber of every event type.</summary>
        public static void Clear()
        {
            foreach (var clear in Clearers) clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeEnter() => Clear();

        static class Channel<T> where T : struct
        {
            public static Action<T>[] Handlers = Array.Empty<Action<T>>();

            static Channel()
            {
                Clearers.Add(() => Handlers = Array.Empty<Action<T>>());
                Counters.Add(() => Handlers.Length);
            }

            public static void Add(Action<T> handler)
            {
                var next = new Action<T>[Handlers.Length + 1];
                Array.Copy(Handlers, next, Handlers.Length);
                next[next.Length - 1] = handler;
                Handlers = next;
            }

            public static void Remove(Action<T> handler)
            {
                int index = Array.LastIndexOf(Handlers, handler);
                if (index < 0) return;
                var next = new Action<T>[Handlers.Length - 1];
                Array.Copy(Handlers, 0, next, 0, index);
                Array.Copy(Handlers, index + 1, next, index, Handlers.Length - index - 1);
                Handlers = next;
            }
        }
    }
}
