using AuraKnight.UI;
using NUnit.Framework;

namespace AuraKnight.Tests.UI
{
    public sealed class ScreenStackTests
    {
        sealed class Fake : IRoutedScreen
        {
            public bool Visible;
            public bool ConsumesBack;
            public int Backs;
            public void Show(bool instant = false) => Visible = true;
            public void Hide(bool instant = false) => Visible = false;
            public bool HandleBack() { Backs++; return ConsumesBack; }
        }

        [Test]
        public void PushShowsAndPopHides()
        {
            var stack = new ScreenStack();
            var a = new Fake();
            Assert.IsTrue(stack.Push(a));
            Assert.IsTrue(a.Visible);
            Assert.AreEqual(1, stack.Count);
            Assert.IsTrue(stack.Pop());
            Assert.IsFalse(a.Visible);
            Assert.IsFalse(stack.Pop(), "empty stack");
        }

        [Test]
        public void PushIgnoresNullAndDuplicates()
        {
            var stack = new ScreenStack();
            var a = new Fake();
            Assert.IsFalse(stack.Push(null));
            stack.Push(a);
            Assert.IsFalse(stack.Push(a));
            Assert.AreEqual(1, stack.Count);
        }

        [Test]
        public void HideBelowHidesTheScreenUnderneathAndRestoresItOnPop()
        {
            var stack = new ScreenStack();
            var menu = new Fake();
            var settings = new Fake();
            stack.Push(menu);
            stack.Push(settings, hideBelow: true);
            Assert.IsFalse(menu.Visible);
            stack.Pop();
            Assert.IsTrue(menu.Visible);
        }

        [Test]
        public void OverlayWithoutHideBelowLeavesTheScreenUnderneathVisible()
        {
            var stack = new ScreenStack();
            var hud = new Fake();
            var popup = new Fake();
            stack.Push(hud);
            stack.Push(popup);
            Assert.IsTrue(hud.Visible);
            stack.Pop();
            Assert.IsTrue(hud.Visible);
        }

        [Test]
        public void BackPopsUnlessTheTopScreenConsumesIt()
        {
            var stack = new ScreenStack();
            var a = new Fake();
            var b = new Fake { ConsumesBack = true };
            stack.Push(a);
            stack.Push(b);
            Assert.IsTrue(stack.Back());
            Assert.AreEqual(2, stack.Count, "consumed, not popped");
            Assert.AreEqual(1, b.Backs);
            b.ConsumesBack = false;
            Assert.IsTrue(stack.Back());
            Assert.AreEqual(1, stack.Count);
            Assert.IsTrue(a.Visible, "a stays shown");
            Assert.IsTrue(stack.Back());
            Assert.IsFalse(stack.Back(), "nothing left to pop");
        }

        [Test]
        public void RemoveTakesAScreenOutFromTheMiddle()
        {
            var stack = new ScreenStack();
            var a = new Fake();
            var b = new Fake();
            var c = new Fake();
            stack.Push(a);
            stack.Push(b);
            stack.Push(c);
            Assert.IsTrue(stack.Remove(b));
            Assert.IsFalse(b.Visible);
            Assert.AreSame(c, stack.Top);
            Assert.IsFalse(stack.Remove(b));
        }

        [Test]
        public void ClearHidesEverything()
        {
            var stack = new ScreenStack();
            var a = new Fake();
            var b = new Fake();
            stack.Push(a);
            stack.Push(b);
            stack.Clear();
            Assert.AreEqual(0, stack.Count);
            Assert.IsFalse(a.Visible || b.Visible);
        }
    }
}
