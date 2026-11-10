using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    private const int ChunkSize = 32, LoadRadius = 2, Seed = 24681357;
    private CharacterBody3D player;
    private readonly Dictionary<Vector2I, ChunkNode> chunks = new();
    private Vector2I lastChunk = new(int.MinValue, int.MinValue);
    private Label status;

    public override void _Ready() { BuildLighting(); BuildPlayer(); BuildUi(); UpdateChunks(true); }

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
        player.Velocity = direction.Normalized() * (Input.IsActionPressed("slow_walk") ? 3f : 6f);
        player.MoveAndSlide();
        Vector2I current = new(Mathf.FloorToInt(player.Position.X / ChunkSize), Mathf.FloorToInt(player.Position.Z / ChunkSize));
        if (current != lastChunk) UpdateChunks(false);
        status.Text = $"无限世界 3D\n区块 {current.X}, {current.Y}\n位置 {player.Position.X:0}, {player.Position.Z:0}";
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
            var ground = new StaticBody3D();
            ground.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(ChunkSize, 0.4f, ChunkSize) }, Position = new Vector3(ChunkSize / 2f, -0.2f, ChunkSize / 2f) }); AddChild(ground);
            AddCube(new Vector3(ChunkSize / 2f, -0.2f, ChunkSize / 2f), new Vector3(ChunkSize, 0.4f, ChunkSize), new Color("5e8b59"));
            for (int z = 1; z < ChunkSize - 1; z += 3) for (int x = 1; x < ChunkSize - 1; x += 3)
            {
                int worldX = coord.X * ChunkSize + x;
                int worldZ = coord.Y * ChunkSize + z;
                float value = noise.GetNoise2D(worldX, worldZ);
                float detailValue = detail.GetNoise2D(worldX, worldZ);
                float height = Mathf.Clamp((value + 0.35f) * 4.5f + detailValue * 1.2f, 0f, 5.5f);
                int variant = Mathf.Abs((x * 17 + z * 31 + coord.X * 7 + coord.Y * 13) % 11);
                if (value < -0.32f) AddCube(new Vector3(x, 0.15f, z), new Vector3(3, 0.3f, 3), new Color("3f7478"));
                else if (height > 1.0f)
                {
                    Color terrainColor = height > 4.0f ? new Color("8b7650") : height > 2.4f ? new Color("6b8f55") : new Color("5e8b59");
                    AddCube(new Vector3(x, 0.2f + height / 2f, z), new Vector3(3, height, 3), terrainColor);
                }
                if (variant == 0 && value > -0.05f) AddCube(new Vector3(x, height + 1.0f, z), new Vector3(1.8f, 2.0f, 1.8f), new Color("31533b"));
                else if (variant == 1 && height > 0.8f) AddCube(new Vector3(x, height + 0.55f, z), new Vector3(1.3f, 1.1f, 1.3f), new Color("7b7772"));
            }
        }

        private void AddCube(Vector3 position, Vector3 size, Color color)
        {
            AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.9f }, Position = position });
        }
    }
}

