using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    private const int ChunkSize = 32, LoadRadius = 2, Seed = 24681357;
    private CharacterBody3D player;
    private readonly Dictionary<Vector2I, ChunkNode> chunks = new();
    private Vector2I lastChunk = new(int.MinValue, int.MinValue);
    private Label status;
    private readonly FastNoiseLite terrainNoise = new() { Seed = Seed, Frequency = 0.045f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
    private readonly FastNoiseLite terrainDetail = new() { Seed = Seed + 91, Frequency = 0.12f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };

    public override void _Ready()
    {
        BuildLighting(); BuildPlayer(); BuildUi(); UpdateChunks(true);
        Vector2I screen = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(20, Mathf.Max(20, screen.Y - 560)));
    }

    private void BuildLighting()
    {
        var environment = new WorldEnvironment();
        var settings = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#6fa4c4"), AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("#b9d6e8"), AmbientLightEnergy = 0.8f };
        environment.Environment = settings;
        AddChild(environment);
        var sun = new DirectionalLight3D { RotationDegrees = new Vector3(-55, -35, 0), LightColor = new Color("#ffe2ad"), LightEnergy = 1.4f, ShadowEnabled = true };
        AddChild(sun);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector("left", "right", "forward", "back");
        Vector3 direction = new(input.X, 0, input.Y);
        player.Velocity = direction.Normalized() * (Input.IsActionPressed("slow_walk") ? 60f : 6f);
        player.MoveAndSlide();
        player.Position = new Vector3(player.Position.X, GetTerrainHeight(player.Position.X, player.Position.Z) + 1.0f, player.Position.Z);
        Vector2I current = new(Mathf.FloorToInt(player.Position.X / ChunkSize), Mathf.FloorToInt(player.Position.Z / ChunkSize));
        if (current != lastChunk) UpdateChunks(false);
        status.Text = $"无限世界 3D\n区块 {current.X}, {current.Y}\n位置 {player.Position.X:0}, {player.Position.Z:0}";
    }

    private float GetTerrainHeight(float x, float z)
    {
        float broad = terrainNoise.GetNoise2D(x, z);
        float fine = terrainDetail.GetNoise2D(x, z);
        return Mathf.Clamp((broad + 0.2f) * 5.5f + fine * 0.6f, -1.2f, 7.5f);
    }

    private void UpdateChunks(bool force)
    {
        Vector2I center = new(Mathf.FloorToInt(player.Position.X / ChunkSize), Mathf.FloorToInt(player.Position.Z / ChunkSize));
        if (!force && center == lastChunk) return;
        lastChunk = center;
        var wanted = new HashSet<Vector2I>();
        for (int z = -LoadRadius; z <= LoadRadius; z++) for (int x = -LoadRadius; x <= LoadRadius; x++)
        {
            Vector2I coord = center + new Vector2I(x, z); wanted.Add(coord);
            if (!chunks.ContainsKey(coord)) { var chunk = new ChunkNode(coord); chunks[coord] = chunk; AddChild(chunk); }
        }
        var remove = new List<Vector2I>();
        foreach (var pair in chunks) if (!wanted.Contains(pair.Key)) remove.Add(pair.Key);
        foreach (var coord in remove) { chunks[coord].QueueFree(); chunks.Remove(coord); }
    }

    private void BuildPlayer()
    {
        player = new CharacterBody3D { Position = new Vector3(0, 1.2f, 0) }; AddChild(player);
        player.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.45f, Height = 1.8f }, Position = new Vector3(0, 0.9f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new CapsuleMesh { Radius = 0.45f, Height = 1.8f }, MaterialOverride = Mat(new Color("e98b62")), Position = new Vector3(0, 0.9f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.34f, Height = 0.68f }, MaterialOverride = Mat(new Color("f6c48f")), Position = new Vector3(0, 2f, 0) });
        var camera = new Camera3D { Position = new Vector3(0, 16, 16), RotationDegrees = new Vector3(-43, 0, 0), Current = true, Fov = 58 };
        player.AddChild(camera);
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var panel = new ColorRect { Position = new Vector2(20, 20), Size = new Vector2(300, 105), Color = new Color(0.03f, 0.05f, 0.08f, 0.78f) }; layer.AddChild(panel);
        status = new Label { Position = new Vector2(15, 12), Text = "无限世界 3D" }; status.AddThemeFontSizeOverride("font_size", 18); status.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(status);
        var help = new Label { Position = new Vector2(20, 660), Text = "WASD 移动   Shift 慢走   窗口化俯视角" }; help.AddThemeFontSizeOverride("font_size", 16); help.AddThemeColorOverride("font_color", new Color("f5e6c8")); layer.AddChild(help);
    }

    private StandardMaterial3D Mat(Color color) => new() { AlbedoColor = color, Roughness = 0.9f };

    private partial class ChunkNode : Node3D
    {
        private readonly Vector2I coord;
        public ChunkNode(Vector2I coord)
        {
            this.coord = coord; Position = new Vector3(coord.X * ChunkSize, 0, coord.Y * ChunkSize);
            var noise = new FastNoiseLite { Seed = Seed, Frequency = 0.045f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
            var detail = new FastNoiseLite { Seed = Seed + 91, Frequency = 0.12f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
            BuildTerrain(noise, detail);
            AddWaterSurface();
        }

        private void AddWaterSurface()
        {
            var waterMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.35f, 0.52f, 0.72f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.15f, Metallic = 0.1f };
            AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(ChunkSize, 0.08f, ChunkSize) }, MaterialOverride = waterMaterial, Position = new Vector3(ChunkSize / 2f, 0, ChunkSize / 2f) });
        }

        private void BuildTerrain(FastNoiseLite noise, FastNoiseLite detail)
        {
            var tool = new SurfaceTool();
            tool.Begin(Mesh.PrimitiveType.Triangles);
            for (int z = 0; z < ChunkSize; z++) for (int x = 0; x < ChunkSize; x++)
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
            int worldX = coord.X * ChunkSize + x;
            int worldZ = coord.Y * ChunkSize + z;
            float broad = noise.GetNoise2D(worldX, worldZ);
            float fine = detail.GetNoise2D(worldX, worldZ);
            return Mathf.Clamp((broad + 0.2f) * 5.5f + fine * 0.6f, -1.2f, 7.5f);
        }

        private Color TerrainColor(float height)
        {
            if (height < -0.35f) return new Color("3f7478");
            if (height > 5.5f) return new Color("8b7650");
            if (height > 3.0f) return new Color("6b8f55");
            return new Color("5e8b59");
        }

    }
}
