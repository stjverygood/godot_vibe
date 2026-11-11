using Godot;

public partial class ChunkNode : Node3D
{
    private readonly Vector2I coord;
    private readonly GameBootstrap owner;

    public ChunkNode(Vector2I coord, GameBootstrap owner)
    {
        this.coord = coord;
        this.owner = owner;
        Position = new Vector3(coord.X * WorldManager.ChunkSize, 0, coord.Y * WorldManager.ChunkSize);

        var noise = new FastNoiseLite { Seed = WorldManager.Seed, Frequency = 0.045f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
        var detail = new FastNoiseLite { Seed = WorldManager.Seed + 91, Frequency = 0.12f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
        BuildTerrain(noise, detail);
        AddWaterSurface();
        SpawnEnemies();
        SpawnHarvestables();
    }

    private void SpawnEnemies()
    {
        int count = Mathf.Abs((coord.X * 92821 + coord.Y * 68917 + WorldManager.Seed) % 3);
        for (int i = 0; i < count; i++)
        {
            float x = 5f + Mathf.Abs((coord.X * 31 + i * 13) % 21);
            float z = 5f + Mathf.Abs((coord.Y * 37 + i * 17) % 21);
            AddChild(new EnemyNode(owner, new Vector3(x, owner.GetTerrainHeight(Position.X + x, Position.Z + z) + 0.7f, z), i));
        }
    }

    private void SpawnHarvestables()
    {
        SpawnEntity(WorldEntityKind.Tree, 4f, 7f);
        SpawnEntity(WorldEntityKind.Rock, 15f, 8f);
        SpawnEntity(WorldEntityKind.Tree, 24f, 10f);
        SpawnEntity(WorldEntityKind.Building, 22f, 24f);
        SpawnEntity(WorldEntityKind.Rock, 8f, 25f);
    }

    private void SpawnEntity(WorldEntityKind kind, float x, float z)
    {
        float worldX = Position.X + x;
        float worldZ = Position.Z + z;
        float height = owner.GetTerrainHeight(worldX, worldZ);
        AddChild(new WorldEntityNode(owner, kind, new Vector3(x, height, z)));
    }

    private void AddWaterSurface()
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.08f, 0.35f, 0.52f, 0.72f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.15f,
            Metallic = 0.1f
        };
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(WorldManager.ChunkSize, 0.08f, WorldManager.ChunkSize) },
            MaterialOverride = material,
            Position = new Vector3(WorldManager.ChunkSize / 2f, 0, WorldManager.ChunkSize / 2f)
        });
    }

    private void BuildTerrain(FastNoiseLite noise, FastNoiseLite detail)
    {
        var tool = new SurfaceTool();
        tool.Begin(Mesh.PrimitiveType.Triangles);
        for (int z = 0; z < WorldManager.ChunkSize; z++)
        for (int x = 0; x < WorldManager.ChunkSize; x++)
        {
            AddTriangle(tool, noise, detail, x, z, x + 1, z, x, z + 1);
            AddTriangle(tool, noise, detail, x + 1, z, x + 1, z + 1, x, z + 1);
        }

        var mesh = tool.Commit();
        var material = new StandardMaterial3D { AlbedoColor = new Color("5e8b59"), Roughness = 0.95f };
        AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });

        var terrainBody = new StaticBody3D();
        terrainBody.AddChild(new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = mesh.GetFaces() } });
        AddChild(terrainBody);
    }

    private void AddTriangle(SurfaceTool tool, FastNoiseLite noise, FastNoiseLite detail, int ax, int az, int bx, int bz, int cx, int cz)
    {
        tool.SetColor(TerrainColor(GetHeight(noise, detail, ax, az)));
        tool.AddVertex(new Vector3(ax, GetHeight(noise, detail, ax, az), az));
        tool.AddVertex(new Vector3(bx, GetHeight(noise, detail, bx, bz), bz));
        tool.AddVertex(new Vector3(cx, GetHeight(noise, detail, cx, cz), cz));
    }

    private float GetHeight(FastNoiseLite noise, FastNoiseLite detail, int x, int z)
    {
        int worldX = coord.X * WorldManager.ChunkSize + x;
        int worldZ = coord.Y * WorldManager.ChunkSize + z;
        float broad = noise.GetNoise2D(worldX, worldZ);
        float fine = detail.GetNoise2D(worldX, worldZ);
        return Mathf.Clamp((broad + 0.2f) * 5.5f + fine * 0.6f, -1.2f, 7.5f);
    }

    private static Color TerrainColor(float height)
    {
        if (height < -0.35f) return new Color("3f7478");
        if (height > 5.5f) return new Color("8b7650");
        if (height > 3.0f) return new Color("6b8f55");
        return new Color("5e8b59");
    }
}
