namespace AuraKnight.Audio
{
    /// <summary>Chooses which pooled source plays the next sound: a free one, else the one that has been busy the longest.</summary>
    public static class VoiceSelector
    {
        /// <param name="busyUntil">Per voice: the time its current sound ends (past times mean free).</param>
        /// <param name="startedAt">Per voice: when its current sound started.</param>
        public static int Choose(float[] busyUntil, float[] startedAt, float now)
        {
            int oldest = 0;
            for (int i = 0; i < busyUntil.Length; i++)
            {
                if (busyUntil[i] <= now) return i;
                if (startedAt[i] < startedAt[oldest]) oldest = i;
            }
            return oldest;
        }
    }
}
