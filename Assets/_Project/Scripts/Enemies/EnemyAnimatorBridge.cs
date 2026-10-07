using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Optional animation hook: mirrors the enemy state onto Animator parameters (Moving bool, Attack and Hurt triggers,
    /// Dead bool). The shared controller Data/Enemies/EnemyAnimator.controller has the states Idle, Move, Attack, Hurt, Death;
    /// per-variant clips come through an AnimatorOverrideController. Without an Animator or controller this does nothing.
    /// </summary>
    public sealed class EnemyAnimatorBridge : MonoBehaviour
    {
        static readonly int Moving = Animator.StringToHash("Moving");
        static readonly int AttackTrigger = Animator.StringToHash("Attack");
        static readonly int HurtTrigger = Animator.StringToHash("Hurt");
        static readonly int Dead = Animator.StringToHash("Dead");
        const float MovingThreshold = 0.1f;

        [SerializeField] EnemyBase enemy;
        [SerializeField] Animator animator;

        Vector2 _lastPosition;
        bool _wasMoving;

        bool Usable => animator != null && animator.runtimeAnimatorController != null;

        void Awake()
        {
            if (enemy == null) enemy = GetComponentInParent<EnemyBase>();
        }

        void OnEnable()
        {
            if (enemy == null) return;
            enemy.StateChanged += OnStateChanged;
            if (!Usable) return;
            animator.Rebind();
            animator.SetBool(Dead, !enemy.IsAlive);
            _wasMoving = false;
            _lastPosition = enemy.transform.position;
        }

        void OnDisable()
        {
            if (enemy != null) enemy.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(EnemyState from, EnemyState to)
        {
            if (!Usable) return;
            animator.SetBool(Dead, to == EnemyState.Dead);
            if (to == EnemyState.Attack) animator.SetTrigger(AttackTrigger);
            else if (to == EnemyState.Hurt) animator.SetTrigger(HurtTrigger);
        }

        void Update()
        {
            if (!Usable || enemy == null || Time.deltaTime <= 0f) return;
            var position = (Vector2)enemy.transform.position;
            float speed = (position - _lastPosition).magnitude / Time.deltaTime; // position delta also covers kinematic crawlers
            _lastPosition = position;
            bool moving = enemy.IsAlive && speed > MovingThreshold;
            if (moving == _wasMoving) return;
            _wasMoving = moving;
            animator.SetBool(Moving, moving);
        }
    }
}
