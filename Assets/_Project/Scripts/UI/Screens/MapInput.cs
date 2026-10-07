using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AuraKnight.UI
{
    /// <summary>
    /// Touch and mouse gestures for the map viewport: one-pointer drag, two-finger pinch (Input System touches) and the mouse wheel.
    /// Reports raw screen-space values; <see cref="MapScreen"/> converts them to its own units. Only runs while the screen is active.
    /// </summary>
    public sealed class MapInput : MonoBehaviour, IDragHandler, IScrollHandler
    {
        const float WheelStep = 1.12f;

        float previousDistance;

        /// <summary>Screen-space drag delta of a single pointer (ignored while pinching).</summary>
        public event Action<Vector2> Dragged;
        /// <summary>(previous distance, distance, midpoint) of two fingers in screen pixels.</summary>
        public event Action<float, float, Vector2> Pinched;
        /// <summary>(zoom factor, pointer position) from the mouse wheel.</summary>
        public event Action<float, Vector2> Wheeled;

        public bool IsPinching { get; private set; }

        void OnDisable()
        {
            IsPinching = false;
            previousDistance = 0f;
        }

        void Update()
        {
            var screen = Touchscreen.current;
            if (screen == null) return;
            int first = -1, second = -1;
            for (int i = 0; i < screen.touches.Count; i++)
            {
                if (!screen.touches[i].press.isPressed) continue;
                if (first < 0) first = i;
                else { second = i; break; }
            }
            if (second < 0)
            {
                IsPinching = false;
                previousDistance = 0f;
                return;
            }
            Vector2 a = screen.touches[first].position.ReadValue(), b = screen.touches[second].position.ReadValue();
            float distance = Vector2.Distance(a, b);
            if (IsPinching) Pinched?.Invoke(previousDistance, distance, (a + b) * 0.5f);
            IsPinching = true;
            previousDistance = distance;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsPinching) Dragged?.Invoke(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            float steps = eventData.scrollDelta.y;
            if (Mathf.Approximately(steps, 0f)) return;
            Wheeled?.Invoke(Mathf.Pow(WheelStep, Mathf.Sign(steps)), eventData.position);
        }
    }
}
