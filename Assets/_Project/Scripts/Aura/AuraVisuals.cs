using AuraKnight.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Aura
{
    /// <summary>
    /// Aura glow: sets the Light2D colour and radius and tints the player sprite (material property block) from the
    /// current <see cref="AuraDefinition"/>, with a 0.2 s flash on every switch. No per-frame allocations.
    /// </summary>
    public sealed class AuraVisuals : MonoBehaviour
    {
        /// <summary>Pale gold glow of the Aura-less state, used when no None definition is assigned.</summary>
        public static readonly Color FallbackColor = new Color(1f, 0.92f, 0.62f, 1f);
        public const float FallbackRadius = 3f;

        [SerializeField] Light2D glow;
        [SerializeField] SpriteRenderer body;
        [Tooltip("Colour property the sprite material exposes (Sprites-Default and URP sprite shaders use _Color).")]
        [SerializeField] string tintProperty = "_Color";
        [SerializeField, Min(0f)] float baseIntensity = 1f;

        MaterialPropertyBlock _block;
        int _tintId;
        Color _auraColor = FallbackColor;
        float _flashElapsed = AuraFlash.Duration;
        bool _bound;

        public Color CurrentColor => _auraColor;
        public float FlashRemaining => Mathf.Max(0f, AuraFlash.Duration - _flashElapsed);

        void Awake() => EnsureInitialized();

        void EnsureInitialized()
        {
            if (_block != null) return;
            _block = new MaterialPropertyBlock();
            _tintId = Shader.PropertyToID(tintProperty);
            if (glow == null) glow = GetComponentInChildren<Light2D>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            if (_bound) return;
            _bound = true;
            EventBus.Subscribe<AuraChanged>(OnAuraChanged);
            var manager = AuraManager.Instance;
            Apply(manager != null ? manager.CurrentDefinition : null, flash: false);
        }

        void OnDisable() => Unbind();

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);
        }

        void Update() => Advance(Time.deltaTime);

        internal void Advance(float deltaTime)
        {
            if (_flashElapsed >= AuraFlash.Duration) return;
            _flashElapsed += deltaTime;
            Render(AuraFlash.Strength(_flashElapsed));
        }

        void OnAuraChanged(AuraChanged evt)
        {
            var manager = AuraManager.Instance;
            AuraDefinition definition = null;
            if (manager != null && AuraIds.TryParse(evt.AuraId, out var id)) definition = manager.GetDefinition(id);
            Apply(definition, flash: true);
        }

        internal void Apply(AuraDefinition definition, bool flash)
        {
            EnsureInitialized();
            _auraColor = definition != null ? definition.Color : FallbackColor;
            float radius = definition != null ? definition.LightRadius : FallbackRadius;
            if (glow != null)
            {
                glow.pointLightOuterRadius = radius;
                glow.pointLightInnerRadius = 0f;
            }
            _flashElapsed = flash ? 0f : AuraFlash.Duration;
            Render(flash ? 1f : 0f);
        }

        void Render(float flash)
        {
            if (glow != null)
            {
                glow.color = _auraColor;
                glow.intensity = baseIntensity + flash * AuraFlash.PeakIntensityBoost;
            }
            if (body == null) return;
            body.GetPropertyBlock(_block);
            _block.SetColor(_tintId, Color.Lerp(_auraColor, Color.white, flash));
            body.SetPropertyBlock(_block);
        }
    }
}
