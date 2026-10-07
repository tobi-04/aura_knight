namespace AuraKnight.Enemies
{
    /// <summary>Production <see cref="IRandomSource"/> backed by UnityEngine.Random.</summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public static readonly UnityRandomSource Instance = new UnityRandomSource();

        public float Value() => UnityEngine.Random.value;

        public int Range(int minInclusive, int maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive);
    }
}
