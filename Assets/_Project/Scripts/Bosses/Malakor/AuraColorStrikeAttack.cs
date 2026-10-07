using AuraKnight.Aura;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Phase 3 strike, coloured like one of Leo's Auras (GDD 7.4): the colour is shown on Malakor during the telegraph and Leo must answer with the matching Aura.
    /// Water (blue orb): harmless while Leo wears Water (the shield blocks it). Fire (dark orb): nearly invisible except in Fire's light.
    /// Wind (green wall, 5 tiles tall): cannot be jumped without Wind's double jump, so Leo must switch to Wind and go over it.
    /// </summary>
    public sealed class AuraColorStrikeAttack : BossAttack
    {
        public enum Strike { Water, Fire, Wind }

        static readonly Color WaterColor = new Color32(0x27, 0xB5, 0xF7, 0xFF);
        static readonly Color FireColor = new Color32(0xFF, 0x5C, 0x57, 0xFF);
        static readonly Color WindColor = new Color32(0x27, 0xD3, 0x8C, 0xFF);

        [SerializeField, Min(1f)] float orbSpeed = 9f;
        [SerializeField] Vector2 orbSize = new Vector2(1f, 1f);
        [SerializeField] Vector2 windWallSize = new Vector2(0.8f, 5f);

        public Strike Current { get; private set; }

        public static Color ColorOf(Strike strike) => strike == Strike.Water ? WaterColor : strike == Strike.Fire ? FireColor : WindColor;

        protected override void OnTelegraph()
        {
            Current = (Strike)Boss.Random.Range(0, 3);
            Boss.SetTint(ColorOf(Current));
        }

        protected override void OnExecute()
        {
            Boss.ClearTint();
            var field = Boss.Playfield;
            var strike = Current;
            Vector2 from = (Vector2)Boss.transform.position + new Vector2(Boss.Facing * 1.6f, 0.8f);
            var spec = new HazardSpec { Damage = Damage, Color = ColorOf(strike), Lifetime = 4f };
            if (strike == Strike.Wind)
            {
                float startX = Boss.transform.position.x + Boss.Facing * 1.8f;
                float distance = Mathf.Abs(startX - (Boss.Facing > 0 ? field.max.x : field.min.x)) + windWallSize.x;
                spec.Position = new Vector2(startX, Boss.FloorY + windWallSize.y * 0.5f);
                spec.Size = windWallSize;
                spec.Velocity = new Vector2(Boss.Facing * orbSpeed, 0f);
                spec.Lifetime = distance / orbSpeed;
                BossHazard.Spawn(Boss, spec);
                return;
            }
            Vector2 aim = Boss.Target != null ? (Vector2)Boss.Target.position - from : new Vector2(Boss.Facing, 0f);
            spec.Position = from;
            spec.Size = orbSize;
            spec.Velocity = aim.normalized * orbSpeed;
            spec.DestroyOnGround = true;
            var orb = BossHazard.Spawn(Boss, spec);
            if (strike == Strike.Water) orb.Suppressed = () => Wearing(AuraId.Water);
            else orb.Revealed = () => Wearing(AuraId.Fire);
        }

        static bool Wearing(AuraId aura) => AuraManager.Instance != null && AuraManager.Instance.Current == aura;

        protected override void OnEnd(bool cancelled) => Boss.ClearTint();
    }
}
