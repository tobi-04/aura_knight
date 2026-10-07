using AuraKnight.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AuraKnight.UI
{
    /// <summary>
    /// The HUD's MAP and PAUSE buttons are the on-screen buttons of the virtual controls (they drive a virtual gamepad's
    /// Select/Start). This reads the same gamepad paths, so a real gamepad's Start/Select work too: Start toggles pause,
    /// Select publishes <see cref="MapRequested"/> for the phase-12 map screen.
    /// </summary>
    public sealed class HudInput : MonoBehaviour
    {
        [SerializeField] PauseController pause;

        InputAction pauseAction;
        InputAction mapAction;

        public void Bind(PauseController pauseController) => pause = pauseController;

        void OnEnable()
        {
            pauseAction = new InputAction("HudPause", InputActionType.Button, VirtualControlPaths.Pause);
            mapAction = new InputAction("HudMap", InputActionType.Button, VirtualControlPaths.Map);
            pauseAction.performed += OnPause;
            mapAction.performed += OnMap;
            pauseAction.Enable();
            mapAction.Enable();
        }

        void OnDisable()
        {
            pauseAction.performed -= OnPause;
            mapAction.performed -= OnMap;
            pauseAction.Dispose();
            mapAction.Dispose();
        }

        void OnPause(InputAction.CallbackContext ctx) => pause.Toggle();

        void OnMap(InputAction.CallbackContext ctx)
        {
            if (GameManager.ModeAllowsControl) EventBus.Publish(new MapRequested());
        }
    }
}
