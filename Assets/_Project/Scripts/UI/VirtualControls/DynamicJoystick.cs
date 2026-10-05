using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace AuraKnight.UI
{
    /// <summary>
    /// Floating stick: appears under the finger on its zone (the left half of the screen) and feeds
    /// <see cref="VirtualControlPaths.LeftStick"/>, so it drives the Move action like a real stick.
    /// </summary>
    public sealed class DynamicJoystick : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        const int NoPointer = int.MinValue;

        [InputControl(layout = "Vector2")]
        [SerializeField] string stickControlPath = VirtualControlPaths.LeftStick;
        [Tooltip("Touch area; ring and handle are positioned in its local space.")]
        [SerializeField] RectTransform zone;
        [SerializeField] RectTransform ring;
        [SerializeField] RectTransform handle;
        [Tooltip("Canvas units from ring centre to full deflection.")]
        [SerializeField] float radius = 80f;
        [SerializeField, Range(0f, 0.5f)] float deadzone = 0.15f;

        int _pointerId = NoPointer;
        Vector2 _origin;

        /// <summary>Wires the visuals when the control is assembled from code (see <see cref="VirtualControlsBuilder"/>).</summary>
        public void Configure(RectTransform touchZone, RectTransform ringVisual, RectTransform handleVisual)
        {
            zone = touchZone;
            ring = ringVisual;
            handle = handleVisual;
        }

        protected override string controlPathInternal
        {
            get => stickControlPath;
            set => stickControlPath = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Hide();
        }

        protected override void OnDisable()
        {
            _pointerId = NoPointer;
            base.OnDisable();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != NoPointer || !TryLocalPoint(eventData, out var local)) return;
            _pointerId = eventData.pointerId;
            _origin = local;
            if (ring != null) { ring.gameObject.SetActive(true); ring.localPosition = local; }
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId || !TryLocalPoint(eventData, out var local)) return;
            var offset = local - _origin;
            if (handle != null) handle.anchoredPosition = JoystickMath.ClampToRadius(offset, radius);
            SendValueToControl(JoystickMath.Normalize(offset, radius, deadzone));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = NoPointer;
            Hide();
            SendValueToControl(Vector2.zero);
        }

        bool TryLocalPoint(PointerEventData e, out Vector2 local)
        {
            var rect = zone != null ? zone : (RectTransform)transform;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out local);
        }

        void Hide()
        {
            if (ring != null) ring.gameObject.SetActive(false);
        }
    }
}
