using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// The project's physics layers (defined in TagManager by Aura/Setup Project, collision matrix set there too).
    /// Solid terrain the player walks on: Ground. Triggers the player touches (altars, exits, water, pickups, gates): Interactable.
    /// Combat: hurtboxes sit on the layer of their team, hitboxes on the attack layers (see Combat.HitMasks).
    /// Layer ids are looked up lazily and cached once found, so nothing breaks while layers are still being created.
    /// </summary>
    public static class PhysicsLayers
    {
        public const string Ground = "Ground";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Hazard = "Hazard";
        public const string PlayerAttack = "PlayerAttack";
        public const string EnemyAttack = "EnemyAttack";
        public const string Interactable = "Interactable";

        /// <summary>All project layers in the order they get consecutive ids starting at 8.</summary>
        public static readonly string[] All = { Ground, Player, Enemy, Hazard, PlayerAttack, EnemyAttack, Interactable };

        /// <summary>Layer pairs that collide / produce trigger callbacks (everything else among the project layers is ignored).</summary>
        public static readonly string[][] CollidingPairs =
        {
            new[] { Ground, Player }, new[] { Ground, Enemy },
            new[] { Player, Interactable }, new[] { Player, Hazard }, new[] { Player, EnemyAttack },
            new[] { PlayerAttack, Enemy }, new[] { PlayerAttack, Hazard },
        };

        static readonly int[] Cache = { -1, -1, -1, -1, -1, -1, -1 };

        /// <summary>Layer id, or -1 when the layer is not defined in the project.</summary>
        public static int Id(string layerName)
        {
            int index = System.Array.IndexOf(All, layerName);
            if (index < 0) return LayerMask.NameToLayer(layerName);
            if (Cache[index] < 0) Cache[index] = LayerMask.NameToLayer(layerName);
            return Cache[index];
        }

        /// <summary>Bit mask of the layers; undefined layers contribute nothing.</summary>
        public static int Mask(params string[] layerNames)
        {
            int mask = 0;
            foreach (var layerName in layerNames)
            {
                int id = Id(layerName);
                if (id >= 0) mask |= 1 << id;
            }
            return mask;
        }

        public static int GroundMask => MaskOf(Ground);
        public static int InteractableMask => MaskOf(Interactable);
        /// <summary>Everything a skill probe may touch: triggers (Interactable) and the solid blockers of gates (Ground).</summary>
        public static int ProbeMask => MaskOf(Interactable) | MaskOf(Ground);

        /// <summary>Puts the object on the layer unless that layer is undefined.</summary>
        public static void Apply(GameObject go, string layerName)
        {
            int id = Id(layerName);
            if (id >= 0 && go.layer != id) go.layer = id;
        }

        static int MaskOf(string layerName)
        {
            int id = Id(layerName);
            return id >= 0 ? 1 << id : 0;
        }
    }
}
