using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// A low branch sweeps the floor toward Leo (jump it). The recover window of this attack is the open-mouth window of the Root Tree:
    /// the core is exposed from the sweep's end until the attack finishes or is cancelled.
    /// </summary>
    public sealed class BranchSweepAttack : BossAttack
    {
        static readonly Color Wood = new Color(0.4f, 0.28f, 0.16f);

        [SerializeField] Vector2 branchSize = new Vector2(2.4f, 1f);
        [SerializeField, Min(1f)] float sweepSpeed = 11f;

        RootTreeBoss _tree;

        protected override void OnTelegraph()
        {
            var field = Boss.Playfield;
            float length = field.size.x;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(Boss.transform.position.x + Boss.Facing * (branchSize.x * 0.5f + 1.5f), Boss.FloorY + 0.1f),
                Size = new Vector2(Mathf.Min(length, 8f), 0.2f),
                Telegraph = TelegraphLength,
                Lifetime = 0.05f,
                Color = Wood,
                Harmless = true,
            });
        }

        protected override void OnExecute()
        {
            var field = Boss.Playfield;
            float startX = Boss.transform.position.x + Boss.Facing * (branchSize.x * 0.5f + 1.5f);
            float speed = sweepSpeed * Speed;
            float distance = Mathf.Abs(startX - (Boss.Facing > 0 ? field.max.x : field.min.x)) + branchSize.x;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(startX, Boss.FloorY + branchSize.y * 0.5f),
                Size = branchSize,
                Damage = Damage,
                Lifetime = distance / speed,
                Velocity = new Vector2(Boss.Facing * speed, 0f),
                Color = Wood,
            });
        }

        protected override void OnRecover()
        {
            _tree = Boss as RootTreeBoss;
            if (_tree != null) _tree.SetCoreExposed(true);
        }

        protected override void OnEnd(bool cancelled)
        {
            if (_tree != null) _tree.SetCoreExposed(false);
            _tree = null;
        }
    }
}
