using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AuraKnight.UI
{
    /// <summary>
    /// Screen stack for one scene plus the Android Back key (Input System maps it to Keyboard.escapeKey).
    /// Back pops the top screen; with an empty stack it raises <see cref="BackOnEmpty"/> (gameplay: open Pause).
    /// </summary>
    public sealed class UIRouter : MonoBehaviour
    {
        readonly ScreenStack stack = new();

        public static UIRouter Instance { get; private set; }

        public int Depth => stack.Count;
        public IRoutedScreen Top => stack.Top;
        public event Action BackOnEmpty;

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
        }

        public bool Push(UIScreen screen, bool hideBelow = false) => stack.Push(screen, hideBelow);
        public bool Pop() => stack.Pop();
        public bool Remove(UIScreen screen) => stack.Remove(screen);
        public bool Contains(UIScreen screen) => stack.Contains(screen);
        public void Clear() => stack.Clear();

        /// <summary>The Back action: the top screen handles it or is popped; otherwise <see cref="BackOnEmpty"/> fires.</summary>
        public void Back()
        {
            if (!stack.Back()) BackOnEmpty?.Invoke();
        }
    }
}
