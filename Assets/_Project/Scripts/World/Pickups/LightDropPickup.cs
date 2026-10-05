using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.World.Pickups
{
    /// <summary>
    /// "Giot Sang": heals one heart when the player touches it. Enemies roll <see cref="RollDrop"/> on death
    /// (10%) and spawn this prefab. Stays in the world while the player is at full hearts.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LightDropPickup : MonoBehaviour
    {
        public const float DropChance = 0.10f;
        public const int HealAmount = 1;

        /// <summary>True when a roll in [0,1) is a drop. Exposed so tests and designers can drive it deterministically.</summary>
        public static bool ShouldDrop(float roll) => roll < DropChance;

        /// <summary>Rolls the 10% drop chance with <see cref="Random.value"/>.</summary>
        public static bool RollDrop() => ShouldDrop(Random.value);

        PlayerStats _toucher;

        // The player is resolved once on Enter and kept while inside, so a full-health player standing on the drop costs no lookups per physics step.
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!WorldTags.IsPlayer(other)) return;
            _toucher = other.GetComponentInParent<PlayerStats>();
            TryCollect(_toucher);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (_toucher != null && WorldTags.IsPlayer(other)) TryCollect(_toucher);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) _toucher = null;
        }

        /// <summary>Heals and consumes the pickup; false (pickup kept) for a null, dead or already healthy player.</summary>
        public bool TryCollect(PlayerStats stats)
        {
            if (stats == null || stats.Heal(HealAmount) <= 0) return false;
            if (Application.isPlaying) Destroy(gameObject);
            else gameObject.SetActive(false);
            return true;
        }
    }
}
