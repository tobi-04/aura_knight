namespace AuraKnight.Audio
{
    /// <summary>Picks a clip index from a random roll, never repeating the previous one when there is a choice.</summary>
    public static class ClipPicker
    {
        /// <param name="count">Number of clips.</param>
        /// <param name="lastIndex">Index played last time, or -1.</param>
        /// <param name="roll01">Random value in [0,1].</param>
        public static int Pick(int count, int lastIndex, float roll01)
        {
            if (count <= 1) return 0;
            float roll = roll01 < 0f ? 0f : roll01;
            if (lastIndex < 0 || lastIndex >= count)
                return System.Math.Min((int)(roll * count), count - 1);
            // choose among the other count-1 clips, then skip over lastIndex
            int pick = System.Math.Min((int)(roll * (count - 1)), count - 2);
            return pick >= lastIndex ? pick + 1 : pick;
        }
    }
}
