using AuraKnight.Core;

using UnityEngine;

namespace AuraKnight.World.Pickups
{
    /// <summary>
    /// One sun coin: pops out of an enemy, falls to the floor, is pulled to Leo within 2 tiles and credited through
    /// <see cref="CoinCollector"/>. Moves itself (no Rigidbody): a downward ground ray stops the fall. Leaves after
    /// <see cref="LifetimeSeconds"/> so stray coins never pile up. Reused through <see cref="CoinPickupPool"/>.
    /// </summary>
    public sealed class CoinPickup : MonoBehaviour
    {
        public const float LifetimeSeconds = 25f;
        const float Gravity = 22f;
        const float MagnetSpeed = 9f;
        const float Radius = 0.15f;
        const float PlayerLookupInterval = 0.5f;

        static Transform _player;
        static float _nextLookup;

        [SerializeField, Min(1)] int value = 1;

        Vector2 _velocity;
        float _age;
        bool _resting;
        bool _collected;

        public int Value => value;
        public bool IsResting => _resting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeEnter()
        {
            _player = null;
            _nextLookup = 0f;
        }

        /// <summary>(Re)places the coin and gives it a pop velocity.</summary>
        public void Launch(Vector2 position, Vector2 velocity)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            _velocity = velocity;
            _age = 0f;
            _resting = false;
            _collected = false;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_age >= LifetimeSeconds) { Despawn(); return; }
            var position = (Vector2)transform.position;
            var player = FindPlayer();
            if (player != null && CoinMagnet.InRange(position, player.position))
            {
                _resting = false;
                _velocity = Vector2.zero;
                position = CoinMagnet.Pull(position, player.position, MagnetSpeed, dt);
                transform.position = new Vector3(position.x, position.y, transform.position.z);
                if (CoinMagnet.Reached(position, player.position)) Collect();
                return;
            }
            if (!_resting) Fall(position, dt);
        }

        void Fall(Vector2 position, float dt)
        {
            _velocity.y -= Gravity * dt;
            var step = _velocity * dt;
            if (step.y < 0f)
            {
                var hit = Physics2D.Raycast(position, Vector2.down, -step.y + Radius, PhysicsLayers.GroundMask);
                if (hit.collider != null)
                {
                    position = new Vector2(position.x + step.x, hit.point.y + Radius);
                    _velocity = Vector2.zero;
                    _resting = true;
                    transform.position = new Vector3(position.x, position.y, transform.position.z);
                    return;
                }
            }
            position += step;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        void Collect()
        {
            if (_collected) return;
            _collected = true;
            CoinCollector.Collect(value);
            Despawn();
        }

        void Despawn()
        {
            if (Application.isPlaying) CoinPickupPool.Release(this);
            else gameObject.SetActive(false);
        }

        static Transform FindPlayer()
        {
            if (_player != null) return _player;
            if (Time.time < _nextLookup) return null;
            _nextLookup = Time.time + PlayerLookupInterval;
            var go = GameObject.FindGameObjectWithTag(WorldTags.Player);
            _player = go != null ? go.transform : null;
            return _player;
        }
    }
}
