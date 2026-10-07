using System.Collections.Generic;

namespace AuraKnight.UI
{
    /// <summary>
    /// Pure stack logic behind <see cref="UIRouter"/>: push shows (optionally hiding the screen below), pop hides and
    /// re-shows the screen below if it was hidden, Back lets the top screen handle it first. Unity-free for unit tests.
    /// </summary>
    public sealed class ScreenStack
    {
        struct Entry
        {
            public IRoutedScreen Screen;
            public bool HidBelow;
        }

        readonly List<Entry> entries = new();

        public int Count => entries.Count;
        public IRoutedScreen Top => entries.Count > 0 ? entries[entries.Count - 1].Screen : null;

        public bool Contains(IRoutedScreen screen) => IndexOf(screen) >= 0;

        public bool Push(IRoutedScreen screen, bool hideBelow = false)
        {
            if (screen == null || Contains(screen)) return false;
            bool hide = hideBelow && entries.Count > 0;
            if (hide) Top.Hide();
            entries.Add(new Entry { Screen = screen, HidBelow = hide });
            screen.Show();
            return true;
        }

        /// <summary>Pops the top screen. False when the stack is empty.</summary>
        public bool Pop() => entries.Count > 0 && Remove(entries[entries.Count - 1].Screen);

        /// <summary>Removes a screen wherever it sits in the stack.</summary>
        public bool Remove(IRoutedScreen screen)
        {
            int index = IndexOf(screen);
            if (index < 0) return false;
            var entry = entries[index];
            entries.RemoveAt(index);
            entry.Screen.Hide();
            // Only the screen that is now directly below the top needs waking if this one had hidden it.
            if (entry.HidBelow && index == entries.Count && entries.Count > 0) entries[entries.Count - 1].Screen.Show();
            return true;
        }

        /// <summary>Back key: the top screen may consume it, otherwise it is popped. False when the stack is empty.</summary>
        public bool Back()
        {
            if (entries.Count == 0) return false;
            if (Top.HandleBack()) return true;
            return Pop();
        }

        public void Clear()
        {
            for (int i = entries.Count - 1; i >= 0; i--) entries[i].Screen.Hide(true);
            entries.Clear();
        }

        int IndexOf(IRoutedScreen screen)
        {
            for (int i = 0; i < entries.Count; i++)
                if (ReferenceEquals(entries[i].Screen, screen)) return i;
            return -1;
        }
    }
}
