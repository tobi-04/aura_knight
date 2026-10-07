using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Tunable numbers of one enemy variant (GDD 7.3). One asset per variant under Data/Enemies; the four archetype
    /// classes read only the fields they need. Created by <c>AuraKnight.Editor.EnemyAssetGenerator</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Aura Knight/Enemy Stats")]
    public sealed class EnemyStats : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "enemy";
        public EnemyArchetype archetype = EnemyArchetype.Walker;

        [Header("Body")]
        [Min(1)] public int maxHp = 2;
        [Min(0)] public int contactDamage = 1;
        [Tooltip("Multiplies the knockback of an incoming hit; 0 = immovable (crawlers on a path).")]
        [Min(0f)] public float knockbackScale = 1f;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 1.5f;
        [Min(0f)] public float chargeSpeed = 3.5f;
        [Min(0f)] public float detectRange = 6f;
        [Tooltip("Walkers only notice Leo within this height difference (0 = ignore height).")]
        [Min(0f)] public float verticalTolerance = 2.5f;
        [Min(0f)] public float attackRange = 1.2f;
        [Min(0.05f)] public float attackSeconds = 0.3f;
        [Min(0f)] public float cooldownSeconds = 0.8f;
        [Tooltip("Walkers: how long Leo must stay on the other side before the enemy turns around (Night Knight: slow to turn, so a slide-through lands a hit).")]
        [Min(0f)] public float turnDelaySeconds;

        [Header("Hopper")]
        [Min(0.1f)] public float hopInterval = 1.5f;
        [Min(0f)] public float hopHeight = 1.6f;
        [Min(0f)] public float hopMaxSpeed = 5f;

        [Header("Flyer")]
        [Min(0f)] public float windupSeconds = 0.5f;
        [Min(0f)] public float diveSpeed = 7f;
        [Min(0.1f)] public float diveMaxSeconds = 1.1f;
        [Min(0.1f)] public float diveMaxDistance = 9f;

        [Header("Static zapper")]
        [Min(0.1f)] public float zapInterval = 3f;
        [Min(0f)] public float zapTelegraphSeconds = 0.6f;
        [Min(0.01f)] public float zapActiveSeconds = 0.2f;
        [Min(0.1f)] public float zapRadius = 2f;

        [Header("Drops")]
        [Min(0)] public int coinsMin = 3;
        [Min(0)] public int coinsMax = 5;

        public DropTable DropTable => new DropTable(coinsMin, coinsMax);

        /// <summary>Seconds a hit stuns the enemy (the same window the player is carried for).</summary>
        public float HurtSeconds => Knockback.Duration;

        void OnValidate()
        {
            if (coinsMax < coinsMin) coinsMax = coinsMin;
            if (chargeSpeed < moveSpeed) chargeSpeed = moveSpeed;
        }
    }
}
