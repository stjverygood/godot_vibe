public sealed class WorldManager
{
    public const int ChunkSize = 32;
    public const int LoadRadius = 2;
    public static int Seed { get; private set; } = 24681357;

    public static void SetSeed(int seed) => Seed = seed;
}
