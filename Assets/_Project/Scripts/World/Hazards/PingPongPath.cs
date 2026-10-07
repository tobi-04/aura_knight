using System;

namespace AuraKnight.World.Hazards
{
    /// <summary>Pure back-and-forth motion along a line: <see cref="Offset"/> is the signed distance from the centre at a given time.</summary>
    public static class PingPongPath
    {
        /// <summary>Triangle wave between -travel/2 and +travel/2 at <paramref name="speed"/> units per second, starting at -travel/2.</summary>
        public static float Offset(float travel, float speed, float time)
        {
            if (travel <= 0f || speed <= 0f) return 0f;
            float lap = travel / speed;
            float t = time % (2f * lap);
            if (t < 0f) t += 2f * lap;
            float along = t <= lap ? t * speed : (2f * lap - t) * speed;
            return Math.Min(travel, Math.Max(0f, along)) - travel * 0.5f;
        }
    }
}
