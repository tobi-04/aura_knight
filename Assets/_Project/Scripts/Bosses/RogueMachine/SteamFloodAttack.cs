using AuraKnight.Aura;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Hot steam floods the floor (height 1.3 tiles) for a few seconds: stand on a platform, or wear the Fire Aura, which is heat-immune
    /// (the hazard switches itself off while <see cref="PlayerAuraBinder.HeatImmune"/> is true).
    /// </summary>
    public sealed class SteamFloodAttack : BossAttack
    {
        static readonly Color Steam = new Color(0.95f, 0.85f, 0.75f);

        [SerializeField, Min(0.5f)] float floodHeight = 1.3f;
        [SerializeField, Min(0.5f)] float floodSeconds = 2.2f;

        PlayerAuraBinder _binder;

        protected override void OnTelegraph()
        {
            var field = Boss.Playfield;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(field.center.x, Boss.FloorY + 0.08f),
                Size = new Vector2(field.size.x, 0.16f),
                Telegraph = TelegraphLength,
                Lifetime = 0.05f,
                Color = Steam,
                Harmless = true,
            });
        }

        protected override void OnExecute()
        {
            var field = Boss.Playfield;
            _binder = Boss.Target != null ? Boss.Target.GetComponentInParent<PlayerAuraBinder>() : null;
            var flood = BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(field.center.x, Boss.FloorY + floodHeight * 0.5f),
                Size = new Vector2(field.size.x, floodHeight),
                Damage = Damage,
                Lifetime = floodSeconds,
                Color = Steam,
                RearmSeconds = 0.5f,
            });
            flood.Suppressed = () => _binder != null && _binder.HeatImmune;
        }
    }
}
