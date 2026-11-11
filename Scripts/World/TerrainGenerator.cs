using Godot;

public static class TerrainGenerator
{
    public const float SeaLevel = 0f;

    private static readonly FastNoiseLite Regions = CreateNoise(WorldManager.Seed, 0.0045f);
    private static readonly FastNoiseLite Plains = CreateNoise(WorldManager.Seed + 11, 0.008f);
    private static readonly FastNoiseLite Hills = CreateNoise(WorldManager.Seed + 23, 0.022f);
    private static readonly FastNoiseLite Ridges = CreateNoise(WorldManager.Seed + 37, 0.011f);
    private static readonly FastNoiseLite Detail = CreateNoise(WorldManager.Seed + 49, 0.055f);
    private static readonly FastNoiseLite Moisture = CreateNoise(WorldManager.Seed + 61, 0.007f);
    private static readonly FastNoiseLite Basins = CreateNoise(WorldManager.Seed + 73, 0.0032f);
    private static readonly FastNoiseLite Depth = CreateNoise(WorldManager.Seed + 89, 0.014f);

    public static float GetHeight(float x, float z)
    {
        float region = (Regions.GetNoise2D(x, z) + 1f) * 0.5f;
        float mountainMask = Mathf.SmoothStep(0.62f, 0.84f, region);
        float hillMask = Mathf.SmoothStep(0.30f, 0.62f, region) * (1f - mountainMask);
        float plainMask = 1f - hillMask - mountainMask;

        float detail = Detail.GetNoise2D(x, z) * 0.18f;
        float plainHeight = 0.65f + Plains.GetNoise2D(x, z) * 0.75f + detail;
        float hillHeight = 1.6f + Mathf.Abs(Hills.GetNoise2D(x, z)) * 5.4f + detail;
        float ridge = 1f - Mathf.Abs(Ridges.GetNoise2D(x, z));
        float mountainHeight = 7f + Mathf.Pow(ridge, 2.2f) * 36f + detail;

        float landHeight = plainHeight * plainMask + hillHeight * hillMask + mountainHeight * mountainMask;
        float oceanMask = GetOceanMask(x, z);
        float seaFloor = -3f - (Depth.GetNoise2D(x, z) + 1f) * 4f;
        return Mathf.Lerp(landHeight, seaFloor, oceanMask);
    }

    public static bool IsLand(float x, float z) => GetHeight(x, z) > SeaLevel + 0.5f;

    public static Color GetSurfaceColor(float x, float z, float height)
    {
        if (height < SeaLevel)
        {
            float depth = 1f - Mathf.SmoothStep(1f, 9f, -height);
            return new Color("bbaa79").Lerp(new Color("345b68"), depth);
        }

        float slopeX = GetHeight(x + 1f, z) - GetHeight(x - 1f, z);
        float slopeZ = GetHeight(x, z + 1f) - GetHeight(x, z - 1f);
        float slope = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ) * 0.5f;
        float rockyAmount = Mathf.Max(Mathf.SmoothStep(0.8f, 2.4f, slope), Mathf.SmoothStep(13f, 23f, height));
        float snowAmount = Mathf.SmoothStep(31f, 39f, height);
        float dryAmount = 1f - Mathf.SmoothStep(-0.45f, 0.15f, Moisture.GetNoise2D(x, z));
        float shoreAmount = 1f - Mathf.SmoothStep(-0.2f, 1.2f, height);

        Color grass = new Color("6c9859").Lerp(new Color("91a75e"), dryAmount);
        Color ground = grass.Lerp(new Color("c3b17d"), shoreAmount * 0.75f);
        ground = ground.Lerp(new Color("80796d"), rockyAmount);
        return ground.Lerp(new Color("e1e5e2"), snowAmount);
    }

    private static float GetOceanMask(float x, float z)
    {
        float basin = Basins.GetNoise2D(x, z);
        float basinMask = 1f - Mathf.SmoothStep(-0.15f, 0.25f, basin);
        float distanceFromStart = Mathf.Sqrt(x * x + z * z);
        float safeStartMask = 1f - Mathf.SmoothStep(45f, 100f, distanceFromStart);
        return basinMask * (1f - safeStartMask);
    }

    private static FastNoiseLite CreateNoise(int seed, float frequency) => new()
    {
        Seed = seed,
        Frequency = frequency,
        NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth
    };
}
