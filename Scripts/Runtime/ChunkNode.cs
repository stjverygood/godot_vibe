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

        BuildTerrain();
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

    private void BuildTerrain()
    {
        var tool = new SurfaceTool();
        tool.Begin(Mesh.PrimitiveType.Triangles);
        for (int z = 0; z < WorldManager.ChunkSize; z++)
        for (int x = 0; x < WorldManager.ChunkSize; x++)
        {
            AddTriangle(tool, x, z, x + 1, z, x, z + 1);
            AddTriangle(tool, x + 1, z, x + 1, z + 1, x, z + 1);
        }

        var mesh = tool.Commit();
        var material = new StandardMaterial3D { AlbedoColor = Colors.White, Roughness = 0.95f, VertexColorUseAsAlbedo = true };
        AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });

        var terrainBody = new StaticBody3D();
        terrainBody.AddChild(new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = mesh.GetFaces() } });
        AddChild(terrainBody);
    }

    private void AddTriangle(SurfaceTool tool, int ax, int az, int bx, int bz, int cx, int cz)
    {
        AddTerrainVertex(tool, ax, az);
        AddTerrainVertex(tool, bx, bz);
        AddTerrainVertex(tool, cx, cz);
    }

    private void AddTerrainVertex(SurfaceTool tool, int x, int z)
    {
        int worldX = coord.X * WorldManager.ChunkSize + x;
        int worldZ = coord.Y * WorldManager.ChunkSize + z;
        float height = TerrainGenerator.GetHeight(worldX, worldZ);
        tool.SetColor(TerrainGenerator.GetSurfaceColor(worldX, worldZ, height));
        tool.AddVertex(new Vector3(x, height, z));
    }
}
