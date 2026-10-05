using UnityEngine;
using UnityEngine.InputSystem;

namespace AuraKnight.Player
{
    /// <summary>
    /// Wraps the "Gameplay" action map (keyboard, gamepad and the on-screen virtual gamepad share it)
    /// and exposes device-independent intents.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        public const string MapName = "Gameplay";
        /// <summary>Stick Y at or below this, combined with DASH, means SLIDE (GDD 3.1).</summary>
        public const float SlideStickThreshold = -0.5f;

        [SerializeField] InputActionAsset actions;

        InputActionAsset _asset;
        InputActionMap _map;
        InputAction _move, _jump, _attack, _dash, _skill, _slide;
        InputAction _wind, _fire, _water, _next, _prev, _pause, _mapAction;

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} on '{name}' has no InputActionAsset assigned.", this);
                enabled = false;
                return;
            }
            _asset = Instantiate(actions); // private copy so enabling state never leaks into the project asset
            _map = _asset.FindActionMap(MapName, true);
            _move = _map.FindAction("Move", true);
            _jump = _map.FindAction("Jump", true);
            _attack = _map.FindAction("Attack", true);
            _dash = _map.FindAction("Dash", true);
            _skill = _map.FindAction("Skill", true);
            _slide = _map.FindAction("Slide", true);
            _wind = _map.FindAction("AuraWind", true);
            _fire = _map.FindAction("AuraFire", true);
            _water = _map.FindAction("AuraWater", true);
            _next = _map.FindAction("AuraNext", true);
            _prev = _map.FindAction("AuraPrev", true);
            _pause = _map.FindAction("Pause", true);
            _mapAction = _map.FindAction("Map", true);
        }

        void OnEnable() => _map?.Enable();
        void OnDisable() => _map?.Disable();

        void OnDestroy()
        {
            if (_asset != null) Destroy(_asset);
        }

        public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpHeld => _jump != null && _jump.IsPressed();
        public bool JumpPressed => _jump != null && _jump.WasPressedThisFrame();
        public bool JumpReleased => _jump != null && _jump.WasReleasedThisFrame();
        public bool AttackPressed => _attack != null && _attack.WasPressedThisFrame();
        public bool SkillPressed => _skill != null && _skill.WasPressedThisFrame();

        bool DashRaw => _dash != null && _dash.WasPressedThisFrame();
        bool StickDown => Move.y <= SlideStickThreshold;

        /// <summary>DASH while the stick is pushed down is a slide, not a dash.</summary>
        public bool DashPressed => DashRaw && !StickDown;

        public bool SlideRequested => (_slide != null && _slide.WasPressedThisFrame()) || (DashRaw && StickDown);

        public bool AuraWindPressed => _wind != null && _wind.WasPressedThisFrame();
        public bool AuraFirePressed => _fire != null && _fire.WasPressedThisFrame();
        public bool AuraWaterPressed => _water != null && _water.WasPressedThisFrame();
        public bool AuraNextPressed => _next != null && _next.WasPressedThisFrame();
        public bool AuraPrevPressed => _prev != null && _prev.WasPressedThisFrame();
        public bool PausePressed => _pause != null && _pause.WasPressedThisFrame();
        public bool MapPressed => _mapAction != null && _mapAction.WasPressedThisFrame();
    }
}
