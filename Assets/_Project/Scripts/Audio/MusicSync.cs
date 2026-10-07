namespace AuraKnight.Audio
{
    /// <summary>Drift check between two layers of the same loop (both positions in samples, loop wraps at <c>length</c>).</summary>
    public static class MusicSync
    {
        /// <summary>Default tolerance: about 35 ms at 44.1 kHz.</summary>
        public const int DefaultTolerance = 1500;

        public static bool NeedsResync(int samplesA, int samplesB, int length, int tolerance = DefaultTolerance)
        {
            if (length <= 0) return false;
            int diff = samplesA > samplesB ? samplesA - samplesB : samplesB - samplesA;
            diff = System.Math.Min(diff, length - diff); // 10 vs length-5 is only 15 apart across the loop point
            return diff > tolerance;
        }
    }
}
