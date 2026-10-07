namespace AuraKnight.Enemies
{
    /// <summary>Turn decisions for walkers; the physics probes (ledge, wall) are done by the caller and passed in.</summary>
    public static class PatrolRules
    {
        /// <summary>
        /// Next facing (+1 / -1): turns around at a wall, at a ledge, or on reaching the end of the patrol segment
        /// (only when still heading toward that end, so an enemy pushed past a point walks back instead of flickering).
        /// </summary>
        public static int NextFacing(int facing, float x, float minX, float maxX, bool groundAhead, bool wallAhead)
        {
            facing = facing >= 0 ? 1 : -1;
            if (wallAhead || !groundAhead) return -facing;
            if (facing > 0 && x >= maxX) return -1;
            if (facing < 0 && x <= minX) return 1;
            return facing;
        }

        /// <summary>A charging walker holds still at a ledge or wall instead of running off or into it.</summary>
        public static bool ChargeBlocked(bool groundAhead, bool wallAhead) => wallAhead || !groundAhead;
    }
}
