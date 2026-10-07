using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Long-range shadow slash: a dark crescent travels toward Leo. Low slashes are jumped, high slashes are slid under.
    /// <see cref="waves"/> above 1 (the phase 2 variant) sends a low one then a high one, a short beat apart.
    /// </summary>
    public sealed class ShadowSlashAttack : BossAttack
    {
        static readonly Color Shadow = new Color(0.35f, 0.15f, 0.5f);

        [SerializeField, Range(1, 3)] int waves = 1;
        [SerializeField, Min(0.1f)] float waveGap = 0.5f;
        [SerializeField] Vector2 waveSize = new Vector2(0.9f, 1.3f);
        [SerializeField, Min(1f)] float waveSpeed = 12f;
        [SerializeField, Min(0.5f)] float highBottom = 1.15f;

        int _sent;

        protected override void OnTelegraph() => _sent = 0;

        protected override void OnExecute() => Send();

        protected override void OnExecuting(float deltaTime)
        {
            if (_sent < waves && StageElapsed >= _sent * waveGap / Speed) Send();
        }

        protected override void OnEnd(bool cancelled) => _sent = waves;

        void Send()
        {
            bool high = _sent % 2 == 1;
            _sent++;
            var field = Boss.Playfield;
            float speed = waveSpeed * Speed;
            float startX = Boss.transform.position.x + Boss.Facing * 1.8f;
            float bottom = high ? highBottom : 0f;
            float distance = Mathf.Abs(startX - (Boss.Facing > 0 ? field.max.x : field.min.x)) + waveSize.x;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(startX, Boss.FloorY + bottom + waveSize.y * 0.5f),
                Size = waveSize,
                Damage = Damage,
                Lifetime = distance / speed,
                Velocity = new Vector2(Boss.Facing * speed, 0f),
                Color = Shadow,
            });
        }
    }
}
