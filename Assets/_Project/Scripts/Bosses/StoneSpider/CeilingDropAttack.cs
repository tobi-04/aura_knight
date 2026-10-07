using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// The spider climbs to the ceiling and follows Leo's x (a shadow on the floor shows where), the shadow locks at 70% of the telegraph,
    /// then the spider drops and slams down: contact damage 2 while falling and two shockwaves along the floor on landing.
    /// </summary>
    public sealed class CeilingDropAttack : BossAttack
    {
        static readonly Color Shadow = new Color(0.1f, 0.1f, 0.12f);
        static readonly Color Wave = new Color(0.65f, 0.6f, 0.55f);

        [SerializeField, Min(2f)] float climbHeight = 8f;
        [SerializeField, Min(1f)] float trackSpeed = 9f;
        [SerializeField, Min(1f)] float dropSpeed = 30f;
        [SerializeField, Range(0.3f, 1f)] float lockAt = 0.7f;
        [SerializeField, Min(1f)] float waveSpeed = 10f;

        BossHazard _shadow;
        float _homeY;
        float _lockedX;
        bool _landed;

        protected override void OnTelegraph()
        {
            _homeY = Boss.HomePosition.y;
            _landed = false;
            _lockedX = Boss.transform.position.x;
            _shadow = BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(_lockedX, Boss.FloorY + 0.1f),
                Size = new Vector2(3.4f, 0.2f),
                Telegraph = 10f,
                Lifetime = 1f,
                Color = Shadow,
                Harmless = true,
            });
        }

        protected override void OnTelegraphing(float deltaTime)
        {
            float climbTo = _homeY + climbHeight;
            float y = Mathf.MoveTowards(Boss.transform.position.y, climbTo, climbHeight * 2.5f * deltaTime);
            float x = Boss.transform.position.x;
            if (StageProgress < lockAt && Boss.Target != null)
                x = Mathf.MoveTowards(x, Boss.Target.position.x, trackSpeed * Speed * deltaTime);
            _lockedX = x;
            Boss.SetPosition(new Vector2(x, y));
            if (_shadow != null) _shadow.transform.position = new Vector3(x, Boss.FloorY + 0.1f, 0f);
        }

        protected override void OnExecute()
        {
            Boss.SetContactDamage(Damage + 1);
            if (_shadow != null) Destroy(_shadow.gameObject);
        }

        protected override void OnExecuting(float deltaTime)
        {
            if (_landed) return;
            float y = Mathf.MoveTowards(Boss.transform.position.y, _homeY, dropSpeed * deltaTime);
            Boss.SetPosition(new Vector2(_lockedX, y));
            if (y > _homeY + 0.001f) return;
            _landed = true;
            Boss.SetContactDamage(Boss.Stats.contactDamage);
            SpawnWave(-1);
            SpawnWave(1);
        }

        void SpawnWave(int direction)
        {
            var field = Boss.Playfield;
            float reach = direction > 0 ? field.max.x - _lockedX : _lockedX - field.min.x;
            BossHazard.Spawn(Boss, new HazardSpec
            {
                Position = new Vector2(_lockedX + direction * 2f, Boss.FloorY + 0.5f),
                Size = new Vector2(1.2f, 1f),
                Damage = Damage,
                Lifetime = Mathf.Max(0.2f, reach / waveSpeed),
                Velocity = new Vector2(direction * waveSpeed, 0f),
                Color = Wave,
            });
        }

        protected override void OnEnd(bool cancelled)
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
            _shadow = null;
            Boss.SetContactDamage(Boss.Stats.contactDamage);
            Boss.SetPosition(new Vector2(_lockedX, _homeY)); // never left hanging on the ceiling after a cancel
        }
    }
}
