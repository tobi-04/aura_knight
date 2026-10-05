using AuraKnight.Core;
using NUnit.Framework;

namespace AuraKnight.Tests.Core
{
    public sealed class EventBusTests
    {
        [SetUp] public void SetUp() => EventBus.Clear();
        [TearDown] public void TearDown() => EventBus.Clear();

        [Test]
        public void Publish_DeliversPayloadToSubscriber()
        {
            RoomEntered received = default;
            EventBus.Subscribe<RoomEntered>(e => received = e);
            EventBus.Publish(new RoomEntered("forest_03", "forest"));
            Assert.AreEqual("forest_03", received.RoomId);
            Assert.AreEqual("forest", received.RegionId);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            System.Action<CoinsChanged> handler = _ => calls++;
            EventBus.Subscribe(handler);
            EventBus.Publish(new CoinsChanged(1));
            EventBus.Unsubscribe(handler);
            EventBus.Publish(new CoinsChanged(2));
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Publish_OnlyReachesMatchingEventType()
        {
            int coins = 0, hearts = 0;
            EventBus.Subscribe<CoinsChanged>(_ => coins++);
            EventBus.Subscribe<HeartsChanged>(_ => hearts++);
            EventBus.Publish(new CoinsChanged(5));
            Assert.AreEqual(1, coins);
            Assert.AreEqual(0, hearts);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new PlayerDied()));
        }

        [Test]
        public void Publish_ThrowingHandlerDoesNotBlockOthers()
        {
            int calls = 0;
            EventBus.Subscribe<GameSaved>(_ => throw new System.InvalidOperationException("boom"));
            EventBus.Subscribe<GameSaved>(_ => calls++);
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception,
                new System.Text.RegularExpressions.Regex("boom"));
            EventBus.Publish(new GameSaved());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Subscribe_DuringPublish_DoesNotAffectCurrentDispatch()
        {
            int late = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => EventBus.Subscribe<PlayerRespawned>(__ => late++));
            EventBus.Publish(new PlayerRespawned());
            Assert.AreEqual(0, late);
        }

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            int calls = 0;
            EventBus.Subscribe<PlayerDied>(_ => calls++);
            EventBus.Clear();
            EventBus.Publish(new PlayerDied());
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void SubscriberCount_TracksSubscriptionsAcrossEventTypes()
        {
            int before = EventBus.SubscriberCount;
            System.Action<CoinsChanged> coins = _ => { };
            System.Action<PlayerDied> died = _ => { };
            EventBus.Subscribe(coins);
            EventBus.Subscribe(died);
            Assert.AreEqual(before + 2, EventBus.SubscriberCount);
            EventBus.Unsubscribe(coins);
            EventBus.Unsubscribe(died);
            Assert.AreEqual(before, EventBus.SubscriberCount);
        }

        [Test]
        public void Unsubscribe_OfAnUnknownHandlerIsHarmless()
        {
            int calls = 0;
            EventBus.Subscribe<PlayerDied>(_ => calls++);
            EventBus.Unsubscribe<PlayerDied>(_ => { });
            EventBus.Publish(new PlayerDied());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Unsubscribe_DuringPublish_DoesNotSkipOrRepeatOthers()
        {
            var order = new System.Collections.Generic.List<int>();
            System.Action<PlayerDied> first = null;
            first = _ => { order.Add(1); EventBus.Unsubscribe(first); };
            EventBus.Subscribe(first);
            EventBus.Subscribe<PlayerDied>(_ => order.Add(2));
            EventBus.Publish(new PlayerDied());
            EventBus.Publish(new PlayerDied());
            CollectionAssert.AreEqual(new[] { 1, 2, 2 }, order);
        }

        [Test]
        public void Publish_DoesNotAllocate()
        {
            EventBus.Subscribe<HeartsChanged>(_ => { });
            EventBus.Subscribe<HeartsChanged>(_ => { });
            EventBus.Publish(new HeartsChanged(1, 5)); // warm-up (JIT, generic channel setup)
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) EventBus.Publish(new HeartsChanged(i, 5));
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.LessOrEqual(allocated, 64, "publishing must not allocate per call");
        }
    }
}
