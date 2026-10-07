using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World.Hazards
{
    /// <summary>
    /// Acid tank (City): the Hazard hitbox hurts Leo unless the current Aura grants acid immunity (Water). Re-evaluated every
    /// physics step so switching Aura while standing in it takes effect at once, like <see cref="HeatVent"/>.
    /// </summary>
    public sealed class AcidPool : MonoBehaviour
    {
        [SerializeField] Hitbox hitbox;

        public bool IsHarmless { get; private set; }

        void OnEnable() => EventBus.Subscribe<AuraChanged>(OnAuraChanged);

        void OnDisable() => EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);

        void Start() => Evaluate();

        void FixedUpdate() => Evaluate();

        void OnAuraChanged(AuraChanged evt) => Evaluate();

        void Evaluate()
        {
            IsHarmless = AuraManager.CurrentPassives.acidImmune;
            if (hitbox == null) return;
            if (!IsHarmless && !hitbox.IsActive) hitbox.Activate(hitbox.Damage);
            else if (IsHarmless && hitbox.IsActive) hitbox.Deactivate();
        }
    }
}
