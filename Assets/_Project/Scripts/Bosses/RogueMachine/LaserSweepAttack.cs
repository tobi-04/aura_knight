using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// A laser head sweeps across the room at chest height. Its lower edge sits above the height of Leo's slide collider, so sliding passes
    /// underneath (<see cref="ClearsSlide"/> encodes that check). A thin warning line shows the beam's height during the telegraph.
    /// </summary>
    public sealed class LaserSweepAttack : BossAttack
    {
        public const float SlideBodyHeight = 0.9f;
        public const float StandingBodyHeight = 1.9f;
        static readonly Color Beam = new Color(1f, 0.25f, 0.2f);

        [Tooltip("Height of the beam's lower edge above the floor; must exceed Leo's slide height.")]
        [SerializeField, Min(0.95f)] float beamBottom = 1.15f;
        [SerializeField, Min(0.2f)] float beamHeight = 1.3f;
        [SerializeField, Min(0.5f)] float beamWidth = 1.8f;
        [SerializeField, Min(1f)] float beamSpeed = 14f;

        /// <summary>True when a body of <paramref name="bodyHeight"/> standing on the floor does not touch the beam (slide = 0.9, standing = 1.9).</summary>
        public static bool ClearsSlide(float beamBottomEdge, float beamTopEdge, float bodyHeight) =>
            !BossMath.VerticalOverlap(0f, bodyHeight, beamBottomEdge, beamTopEdge);

        public float BeamBottom => beamBottom;
        public float BeamTop => beamBottom + beamHeight;

        Vector2 Origin => new Vector2(Boss.transform.position.x + Boss.Facing * 2.5f, Boss.FloorY + beamBottom + beamHeight * 0.5f);

        protected override void OnTelegraph()
        {
            float width = Boss.Playfield.size.x;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(Origin.x + Boss.Facing * width * 0.5f, Origin.y),
                Size = new Vector2(width, 0.12f),
                Telegraph = TelegraphLength,
                Lifetime = 0.05f,
                Color = Beam,
                Harmless = true,
            });
        }

        protected override void OnExecute()
        {
            var field = Boss.Playfield;
            float speed = beamSpeed * Speed;
            float distance = Mathf.Abs(Origin.x - (Boss.Facing > 0 ? field.max.x : field.min.x)) + beamWidth;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = Origin,
                Size = new Vector2(beamWidth, beamHeight),
                Damage = Damage,
                Lifetime = distance / speed,
                Velocity = new Vector2(Boss.Facing * speed, 0f),
                Color = Beam,
            });
        }
    }
}
