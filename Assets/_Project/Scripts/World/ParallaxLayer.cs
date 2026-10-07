using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// One scrolling backdrop layer of a room. Horizontally it follows <see cref="ParallaxMath"/> around the point it was placed
    /// at; vertically it stays on the camera (the art is one screen tall, the room can be taller). Only the layers of the room Leo
    /// is in draw, so neighbouring rooms never paint over each other. Runs late so it sees the camera's final position.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Min(0f)] float factor = ParallaxMath.Action;

        SpriteRenderer _renderer;
        Room _room;
        Transform _camera;
        float _anchorX;

        public float Factor { get => factor; set => factor = Mathf.Max(0f, value); }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _room = GetComponentInParent<Room>();
            _anchorX = transform.position.x;
        }

        void LateUpdate()
        {
            if (_camera == null)
            {
                var main = Camera.main;
                if (main == null) return;
                _camera = main.transform;
            }
            var current = RoomManager.Instance != null ? RoomManager.Instance.Current : null;
            bool show = current == null || _room == null || current == _room;
            if (_renderer.enabled != show) _renderer.enabled = show;
            if (!show) return;
            var position = transform.position;
            position.x = ParallaxMath.PositionX(_anchorX, _camera.position.x, factor);
            position.y = _camera.position.y;
            transform.position = position;
        }
    }
}
