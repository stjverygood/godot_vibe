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
            if (!TryFindLand(ref x, ref z)) continue;
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
        if (!TryFindLand(ref x, ref z)) return;
        float worldX = Position.X + x;
        float worldZ = Position.Z + z;
        float height = owner.GetTerrainHeight(worldX, worldZ);
        AddChild(new WorldEntityNode(owner, kind, new Vector3(x, height, z)));
    }

    private bool TryFindLand(ref float x, ref float z)
    {
        if (TerrainGenerator.IsLand(Position.X + x, Position.Z + z)) return true;

        for (float radius = 2f; radius <= 12f; radius += 2f)
        for (int step = 0; step < 8; step++)
        {
            float angle = step * Mathf.Pi / 4f;
            float candidateX = Mathf.Clamp(x + Mathf.Cos(angle) * radius, 1f, WorldManager.ChunkSize - 1f);
            float candidateZ = Mathf.Clamp(z + Mathf.Sin(angle) * radius, 1f, WorldManager.ChunkSize - 1f);
            if (!TerrainGenerator.IsLand(Position.X + candidateX, Position.Z + candidateZ)) continue;
            x = candidateX;
            z = candidateZ;
            return true;
        }

        return false;
    }

    private void AddWaterSurface()
    {
        var waterMesh = new SurfaceTool();
        waterMesh.Begin(Mesh.PrimitiveType.Triangles);
        int waterCellCount = 0;
        for (int z = 0; z < WorldManager.ChunkSize; z++)
        for (int x = 0; x < WorldManager.ChunkSize; x++)
        {
            float worldX = coord.X * WorldManager.ChunkSize + x + 0.5f;
            float worldZ = coord.Y * WorldManager.ChunkSize + z + 0.5f;
            if (TerrainGenerator.GetHeight(worldX, worldZ) >= TerrainGenerator.SeaLevel) continue;

            waterCellCount++;
            AddWaterVertex(waterMesh, x, z);
            AddWaterVertex(waterMesh, x + 1, z);
            AddWaterVertex(waterMesh, x, z + 1);
            AddWaterVertex(waterMesh, x + 1, z);
            AddWaterVertex(waterMesh, x + 1, z + 1);
            AddWaterVertex(waterMesh, x, z + 1);
        }

        if (waterCellCount == 0) return;

        AddChild(new MeshInstance3D { Mesh = waterMesh.Commit(), MaterialOverride = DayNightCycle.WaterMaterial });
    }

    private static void AddWaterVertex(SurfaceTool tool, int x, int z) =>
        tool.AddVertex(new Vector3(x, TerrainGenerator.SeaLevel + 0.02f, z));

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

        tool.GenerateNormals();
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
