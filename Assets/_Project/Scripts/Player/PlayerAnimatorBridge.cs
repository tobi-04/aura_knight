using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Mirrors controller state into Animator parameters (Speed, VelY, Grounded, WallSlide, Dash, Slide) and
    /// flips the visual by facing. Safe without art: when no Animator/controller exists only the flip runs.
    /// </summary>
    public sealed class PlayerAnimatorBridge : MonoBehaviour
    {
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int VelY = Animator.StringToHash("VelY");
        static readonly int Grounded = Animator.StringToHash("Grounded");
        static readonly int WallSlide = Animator.StringToHash("WallSlide");
        static readonly int Dash = Animator.StringToHash("Dash");
        static readonly int Slide = Animator.StringToHash("Slide");

        [SerializeField] PlayerController controller;
        [SerializeField] Animator animator;
        [Tooltip("Child holding the sprite; its X scale is flipped by facing.")]
        [SerializeField] Transform visual;

        bool _animate;

        void Awake()
        {
            if (controller == null) controller = GetComponentInParent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _animate = animator != null && animator.runtimeAnimatorController != null;
            if (controller == null) enabled = false;
        }

        void LateUpdate()
        {
            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * controller.Facing;
                visual.localScale = s;
            }
            if (!_animate) return;

            var id = controller.StateMachine.CurrentId;
            animator.SetFloat(Speed, Mathf.Abs(controller.Velocity.x));
            animator.SetFloat(VelY, controller.Velocity.y);
            animator.SetBool(Grounded, controller.Grounded);
            animator.SetBool(WallSlide, id == PlayerStateId.WallSlide);
            animator.SetBool(Dash, id == PlayerStateId.Dash);
            animator.SetBool(Slide, id == PlayerStateId.Slide);
        }
    }
}
