using AuraKnight.Aura;
using AuraKnight.Core;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Timed movement slow on Leo (the spider's web: 50%). It multiplies <see cref="PlayerController.SpeedMultiplier"/> instead of
    /// replacing it, through <see cref="SlowSpeedLayer"/>, because <see cref="PlayerAuraBinder"/> rewrites that property on every Aura
    /// or water change. The binder's <c>ModifiersChanged</c> (raised right after its write) and <c>LateUpdate</c> re-apply the slow.
    /// Added to the player at runtime by <see cref="ApplyTo"/>; no Player prefab change.
    /// </summary>
    public sealed class PlayerSlowStatus : MonoBehaviour
    {
        readonly SlowSpeedLayer _layer = new SlowSpeedLayer();
        PlayerController _controller;
        PlayerAuraBinder _binder;
        float _factor = 1f;
        float _remaining;
        bool _applied;
        bool _bound;

        public bool IsSlowed => _remaining > 0f;
        public float Factor => _factor;

        /// <summary>Slows the player that owns <paramref name="anyPlayerPart"/> to <paramref name="factor"/> for <paramref name="seconds"/> (stronger / longer wins).</summary>
        public static PlayerSlowStatus ApplyTo(Component anyPlayerPart, float factor, float seconds)
        {
            var controller = anyPlayerPart != null ? anyPlayerPart.GetComponentInParent<PlayerController>() : null;
            if (controller == null) return null;
            if (!controller.TryGetComponent<PlayerSlowStatus>(out var status)) status = controller.gameObject.AddComponent<PlayerSlowStatus>();
            status.Begin(factor, seconds);
            return status;
        }

        /// <summary>Removes any slow from the player (boss reset, player death).</summary>
        public static void ClearOn(Component anyPlayerPart)
        {
            var controller = anyPlayerPart != null ? anyPlayerPart.GetComponentInParent<PlayerController>() : null;
            if (controller != null && controller.TryGetComponent<PlayerSlowStatus>(out var status)) status.Clear();
        }

        void Begin(float factor, float seconds)
        {
            Bind();
            _factor = _remaining > 0f ? Mathf.Min(_factor, factor) : factor;
            _remaining = Mathf.Max(_remaining, seconds);
            Refresh();
        }

        public void Clear()
        {
            _remaining = 0f;
            Refresh();
        }

        void Bind()
        {
            if (_bound) return;
            _controller = GetComponent<PlayerController>();
            _binder = GetComponent<PlayerAuraBinder>();
            if (_binder != null) _binder.ModifiersChanged += OnModifiersChanged;
            EventBus.Subscribe<PlayerDied>(OnPlayerDied);
            _bound = true;
        }

        void OnDisable() => Unbind();

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            _remaining = 0f;
            Refresh();
            if (_binder != null) _binder.ModifiersChanged -= OnModifiersChanged;
            EventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
        }

        void OnModifiersChanged(PlayerModifiers modifiers) => Refresh();

        void OnPlayerDied(PlayerDied evt) => Clear();

        void Update()
        {
            if (_remaining <= 0f) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) Refresh();
        }

        void LateUpdate()
        {
            if (_applied || _remaining > 0f) Refresh();
        }

        /// <summary>The Aura binder's own speed (Aura passive times wading), the authoritative base; null without a binder.</summary>
        float? KnownBase() => _binder != null ? _binder.Modifiers.SpeedMultiplier : (float?)null;

        void Refresh()
        {
            if (_controller == null) return;
            if (_remaining > 0f)
            {
                _controller.SpeedMultiplier = _layer.Apply(_controller.SpeedMultiplier, _factor, KnownBase());
                _applied = true;
            }
            else if (_applied)
            {
                _controller.SpeedMultiplier = _layer.Release(_controller.SpeedMultiplier, KnownBase());
                _applied = false;
            }
        }
    }
}
