using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Ground and air horizontal acceleration (0.06 s up to speed, 0.04 s to stop).</summary>
    public static class HorizontalMotion
    {
        /// <param name="speedScale">Run speed multiplier (Fire aura); scales the rates so timing stays constant.</param>
        public static float Step(PlayerMovementConfig config, float current, float target, float deltaTime, float speedScale)
        {
            float full = config.RunSpeed * speedScale;
            float rate = Mathf.Approximately(target, 0f) ? full / config.DecelerationTime : full / config.AccelerationTime;
            return Mathf.MoveTowards(current, target, rate * deltaTime);
        }
    }
}
