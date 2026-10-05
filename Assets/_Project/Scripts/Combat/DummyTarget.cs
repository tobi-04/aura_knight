using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>
    /// Training dummy for test scenes: flashes when hit, logs the damage, and heals itself after a few quiet seconds.
    /// Needs a <see cref="Hurtbox"/> (Enemy team) and a <see cref="Health"/>.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DummyTarget : MonoBehaviour
    {
        const float FlashSeconds = 0.12f;

        [SerializeField, Min(0f)] float healAfterSeconds = 3f;
        [SerializeField] SpriteRenderer sprite;

        Health _health;
        Color _baseColor = Color.white;
        float _flashLeft, _quietFor;

        void Awake()
        {
            _health = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) _baseColor = sprite.color;
        }

        void OnEnable() => _health.Damaged += OnDamaged;
        void OnDisable() => _health.Damaged -= OnDamaged;

        void OnDamaged(DamageInfo info, int applied)
        {
            _flashLeft = FlashSeconds;
            _quietFor = 0f;
            Debug.Log($"[Dummy] -{applied} ({_health.Current}/{_health.Max}) from {(info.Source != null ? info.Source.name : "?")}", this);
        }

        void Update()
        {
            if (sprite != null) sprite.color = _flashLeft > 0f ? Color.white : _baseColor;
            _flashLeft = Mathf.Max(0f, _flashLeft - Time.deltaTime);
            if (_health.Current >= _health.Max) return;
            _quietFor += Time.deltaTime;
            if (_quietFor >= healAfterSeconds) _health.Refill();
        }
    }
}
