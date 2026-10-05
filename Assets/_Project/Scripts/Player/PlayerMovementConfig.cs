using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Every tunable movement number from GDD section 4. Edit the asset to tune feel on device.
    /// </summary>
    [CreateAssetMenu(menuName = "Aura Knight/Player Movement Config", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Run (u/s, seconds)")]
        [SerializeField] float runSpeed = 8f;
        [SerializeField] float accelerationTime = 0.06f;
        [SerializeField] float decelerationTime = 0.04f;

        [Header("Jump")]
        [SerializeField] float maxJumpHeight = 4.5f;
        [SerializeField] float timeToApex = 0.35f;
        [SerializeField, Range(0f, 1f)] float jumpCutMultiplier = 0.25f;
        [SerializeField] float jumpMinHoldTime = 0.08f;
        [SerializeField] float fallGravityMultiplier = 1.6f;
        [SerializeField] float maxFallSpeed = 20f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float inputBufferTime = 0.12f;
        [SerializeField] float cornerCorrection = 0.2f;

        [Header("Dash")]
        [SerializeField] float dashSpeed = 25f;
        [SerializeField] float dashDuration = 0.2f;
        [SerializeField] float dashInvulnerableTime = 0.1f;
        [SerializeField] float dashCooldown = 0.8f;

        [Header("Slide")]
        [SerializeField] float slideSpeed = 11f;
        [SerializeField] float slideDuration = 0.45f;

        [Header("Wall")]
        [SerializeField] float wallSlideMaxSpeed = 3f;
        [SerializeField] Vector2 wallJumpVelocity = new Vector2(11f, 22f);
        [SerializeField] float wallJumpInputLock = 0.15f;

        [Header("Aura hooks (Wind)")]
        [SerializeField] float doubleJumpVelocity = 20f;
        [SerializeField] float glideMaxFallSpeed = 3.5f;

        public float RunSpeed => runSpeed;
        public float AccelerationTime => accelerationTime;
        public float DecelerationTime => decelerationTime;
        public float MaxJumpHeight => maxJumpHeight;
        public float TimeToApex => timeToApex;
        public float JumpCutMultiplier => jumpCutMultiplier;
        public float JumpMinHoldTime => jumpMinHoldTime;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float MaxFallSpeed => maxFallSpeed;
        public float CoyoteTime => coyoteTime;
        public float InputBufferTime => inputBufferTime;
        public float CornerCorrection => cornerCorrection;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float DashInvulnerableTime => dashInvulnerableTime;
        public float DashCooldown => dashCooldown;
        public float SlideSpeed => slideSpeed;
        public float SlideDuration => slideDuration;
        public float WallSlideMaxSpeed => wallSlideMaxSpeed;
        public Vector2 WallJumpVelocity => wallJumpVelocity;
        public float WallJumpInputLock => wallJumpInputLock;
        public float DoubleJumpVelocity => doubleJumpVelocity;
        public float GlideMaxFallSpeed => glideMaxFallSpeed;

        /// <summary>Rising gravity: g = 2h / t^2 (73.47 u/s^2 for h = 4.5, t = 0.35).</summary>
        public float JumpGravity => 2f * maxJumpHeight / (timeToApex * timeToApex);

        /// <summary>Initial jump velocity: v0 = 2h / t (25.71 u/s for h = 4.5, t = 0.35).</summary>
        public float JumpVelocity => 2f * maxJumpHeight / timeToApex;

        public float FallGravity => JumpGravity * fallGravityMultiplier;

        void OnValidate()
        {
            timeToApex = Mathf.Max(0.05f, timeToApex);
            maxJumpHeight = Mathf.Max(0.5f, maxJumpHeight);
            accelerationTime = Mathf.Max(0.001f, accelerationTime);
            decelerationTime = Mathf.Max(0.001f, decelerationTime);
            dashDuration = Mathf.Max(0.02f, dashDuration);
            slideDuration = Mathf.Max(0.05f, slideDuration);
        }
    }
}
