using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace AuraKnight.UI
{
    /// <summary>
    /// Swipe down (>= 60 dp within 0.25 s) on the right half of the screen presses the Slide control
    /// for a short pulse. Sits behind the on-screen buttons so they keep priority.
    /// </summary>
    public sealed class SwipeDetector : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        const int NoPointer = int.MinValue;

        [InputControl(layout = "Button")]
        [SerializeField] string slideControlPath = VirtualControlPaths.Slide;
        [SerializeField] float minDistanceDp = 60f;
        [SerializeField] float maxDuration = 0.25f;
        [SerializeField] float pulseDuration = 0.1f;

        SwipeGesture _gesture;
        int _pointerId = NoPointer;
        float _pulseRemaining;

        protected override string controlPathInternal
        {
            get => slideControlPath;
            set => slideControlPath = value;
        }

        protected override void OnDisable()
        {
            _pointerId = NoPointer;
            _pulseRemaining = 0f;
            base.OnDisable();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != NoPointer) return;
            _pointerId = eventData.pointerId;
            _gesture ??= new SwipeGesture(0f, maxDuration); // one instance for the component's lifetime: no allocation per touch
            _gesture.Begin(eventData.position, Time.unscaledTime, UiMetrics.DpToPixels(minDistanceDp));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId || _gesture == null) return;
            if (!_gesture.Update(eventData.position, Time.unscaledTime)) return;
            SendValueToControl(1f);
            _pulseRemaining = pulseDuration;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = NoPointer;
            _gesture?.End();
        }

        void Update()
        {
            if (_pulseRemaining <= 0f) return;
            _pulseRemaining -= Time.unscaledDeltaTime;
            if (_pulseRemaining <= 0f) SendValueToControl(0f);
        }
    }
}
