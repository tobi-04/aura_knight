namespace AuraKnight.Enemies
{
    /// <summary>Random numbers behind an interface so drop rolls are deterministic in tests.</summary>
    public interface IRandomSource
    {
        /// <summary>Uniform in [0, 1).</summary>
        float Value();

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }
}
