using AuraKnight.Combat;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>
    /// Base of the three Aura skills. One instance per Aura lives under the player; <see cref="AuraManager"/> pays the
    /// energy and cooldown first, then calls <see cref="Cast"/>. Damage always goes through <see cref="Hitbox"/>.
    /// </summary>
    public abstract class AuraSkillBase : MonoBehaviour
    {
        protected PlayerController Controller { get; private set; }
        protected Health OwnerHealth { get; private set; }

        /// <summary>The Aura whose skill this is; interactions are offered with this id.</summary>
        public abstract AuraId Aura { get; }

        /// <summary>False while the skill cannot start (for example a shield that is already up). Checked before energy is spent.</summary>
        public virtual bool CanCast => true;

        public void Bind(PlayerController controller, Health ownerHealth)
        {
            Controller = controller;
            OwnerHealth = ownerHealth;
            OnBound();
        }

        protected virtual void OnBound() { }

        /// <summary>Fires the skill from the player's current position and facing.</summary>
        public abstract void Cast();

        protected Vector2 Origin => Controller != null ? (Vector2)Controller.transform.position : (Vector2)transform.position;
        protected int Facing => Controller != null ? Controller.Facing : 1;
    }
}
