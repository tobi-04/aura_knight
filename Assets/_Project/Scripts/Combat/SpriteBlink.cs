using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Pure on/off pattern for the hit blink, derived from the remaining i-frame time.</summary>
    public static class BlinkPattern
    {
        public const float HalfPeriod = 0.06f;

        public static bool IsVisible(float remainingInvulnerability) =>
            remainingInvulnerability <= 0f || ((int)(remainingInvulnerability / HalfPeriod) & 1) == 0;
    }

    /// <summary>Blinks a sprite while <see cref="Health"/> is in its post-hit i-frames (dash i-frames do not blink).</summary>
    public sealed class SpriteBlink : MonoBehaviour
    {
        const float DimAlpha = 0.25f;

        [SerializeField] Health health;
        [SerializeField] SpriteRenderer target;

        void Awake()
        {
            if (health == null) health = GetComponentInParent<Health>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            if (health == null || target == null) enabled = false;
        }

        void OnDisable() => SetAlpha(1f);

        void LateUpdate() => SetAlpha(BlinkPattern.IsVisible(health.Invulnerability.Remaining) ? 1f : DimAlpha);

        void SetAlpha(float alpha)
        {
            if (target == null) return;
            var c = target.color;
            if (Mathf.Approximately(c.a, alpha)) return;
            c.a = alpha;
            target.color = c;
        }
    }
}
