using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Malakor fades out and a marker shows where he will reappear beside Leo (fixed at the start of the telegraph, so Leo can walk away).
    /// On execute he teleports there and thrusts (damage 2).
    /// </summary>
    public sealed class TeleportStabAttack : BossAttack
    {
        static readonly Color Dark = new Color(0.4f, 0.1f, 0.55f);

        [SerializeField, Min(1f)] float appearDistance = 2.4f;
        [SerializeField] Vector2 stabSize = new Vector2(2.6f, 1f);
        [SerializeField, Min(0.1f)] float stabSeconds = 0.25f;

        float _targetX;

        protected override void OnTelegraph()
        {
            var field = Boss.Playfield;
            float leo = Boss.Target != null ? Boss.Target.position.x : Boss.HomePosition.x;
            float side = Boss.transform.position.x >= leo ? 1f : -1f;
            _targetX = Mathf.Clamp(leo + side * appearDistance, field.min.x + 1.5f, field.max.x - 1.5f);
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(_targetX, Boss.FloorY + 1.1f),
                Size = new Vector2(1.6f, 2.2f),
                Telegraph = TelegraphLength,
                Lifetime = 0.05f,
                Color = Dark,
                Harmless = true,
            });
        }

        protected override void OnTelegraphing(float deltaTime) => Boss.SetAlpha(Mathf.Lerp(1f, 0.15f, StageProgress));

        protected override void OnExecute()
        {
            Boss.SetAlpha(-1f);
            if (Boss is MalakorBoss malakor) malakor.TeleportTo(_targetX);
            else Boss.SetPosition(new Vector2(_targetX, Boss.HomePosition.y));
            Boss.FaceTarget();
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(Boss.transform.position.x + Boss.Facing * (stabSize.x * 0.5f + 0.4f), Boss.FloorY + 1.2f),
                Size = stabSize,
                Damage = Damage,
                Lifetime = stabSeconds,
                Color = Dark,
            });
        }

        protected override void OnEnd(bool cancelled) => Boss.SetAlpha(-1f);
    }
}
