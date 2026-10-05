using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Maps teams to physics layers: hurtboxes live on their team's layer, hitboxes on the attack layers.</summary>
    public static class HitMasks
    {
        public static string HurtboxLayer(Team team)
        {
            switch (team)
            {
                case Team.Player: return PhysicsLayers.Player;
                case Team.Enemy: return PhysicsLayers.Enemy;
                default: return PhysicsLayers.Hazard;
            }
        }

        public static string HitboxLayer(Team team)
        {
            switch (team)
            {
                case Team.Player: return PhysicsLayers.PlayerAttack;
                case Team.Enemy: return PhysicsLayers.EnemyAttack;
                default: return PhysicsLayers.Hazard;
            }
        }

        /// <summary>Layers a hitbox of this team can reach: the player hits enemies and hazards (pogo), everything else hits the player.</summary>
        public static int TargetMask(Team attacker) =>
            attacker == Team.Player
                ? PhysicsLayers.Mask(PhysicsLayers.Enemy, PhysicsLayers.Hazard)
                : PhysicsLayers.Mask(PhysicsLayers.Player);
    }
}
