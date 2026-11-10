using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
    private const int ChunkSize = 32, TileSize = 32, LoadRadius = 3, WorldSeed = 24681357;
    private CharacterBody2D player;
    private readonly Dictionary<Vector2I, ChunkNode> chunks = new();
    private Vector2I lastChunk = new(int.MinValue, int.MinValue);
    private Label locationLabel;

    public override void _Ready() { BuildPlayer(); BuildUi(); UpdateChunks(true); }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector("left", "right", "forward", "back");
        player.Velocity = input.Normalized() * (Input.IsActionPressed("slow_walk") ? 110f : 220f);
        player.MoveAndSlide();
        Vector2I current = WorldToChunk(player.Position);
        if (current != lastChunk) UpdateChunks(false);
        locationLabel.Text = $"无限漫游\n区块 {current.X}, {current.Y}\n坐标 {player.Position.X:0}, {player.Position.Y:0}";
    }

    private Vector2I WorldToChunk(Vector2 position)
    {
        float size = ChunkSize * TileSize;
        return new Vector2I(Mathf.FloorToInt(position.X / size), Mathf.FloorToInt(position.Y / size));
    }

    private void UpdateChunks(bool force)
    {
        Vector2I center = WorldToChunk(player.Position);
        if (!force && center == lastChunk) return;
        lastChunk = center;
        var wanted = new HashSet<Vector2I>();
        for (int y = -LoadRadius; y <= LoadRadius; y++)
        for (int x = -LoadRadius; x <= LoadRadius; x++)
        {
            Vector2I coord = center + new Vector2I(x, y); wanted.Add(coord);
            if (!chunks.ContainsKey(coord)) { var chunk = new ChunkNode(coord, WorldSeed); chunks[coord] = chunk; AddChild(chunk); }
        }
        var remove = new List<Vector2I>();
        foreach (var pair in chunks) if (!wanted.Contains(pair.Key)) remove.Add(pair.Key);
        foreach (Vector2I coord in remove) { chunks[coord].QueueFree(); chunks.Remove(coord); }
    }

    private void BuildPlayer()
    {
        player = new CharacterBody2D { Position = Vector2.Zero, ZIndex = 10 }; AddChild(player);
        player.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 12f } });
        player.AddChild(new PlayerVisual());
        player.AddChild(new Camera2D { Enabled = true, Zoom = new Vector2(1.2f, 1.2f) });
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var panel = new ColorRect { Position = new Vector2(20, 20), Size = new Vector2(300, 122), Color = new Color(0.03f, 0.05f, 0.08f, 0.8f) }; layer.AddChild(panel);
        locationLabel = new Label { Position = new Vector2(16, 12), Text = "无限漫游" };
        locationLabel.AddThemeFontSizeOverride("font_size", 18); locationLabel.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(locationLabel);
        var help = new Label { Position = new Vector2(20, 660), Text = "WASD 移动   Shift 慢走   Esc 释放鼠标" };
        help.AddThemeFontSizeOverride("font_size", 16); help.AddThemeColorOverride("font_color", new Color("f5e6c8")); layer.AddChild(help);
    }

    private partial class PlayerVisual : Node2D
    {
        public override void _Draw() { DrawCircle(Vector2.Zero, 13, new Color("e98b62")); DrawCircle(new Vector2(0, -3), 8, new Color("f6c48f")); DrawLine(new Vector2(0, -12), new Vector2(0, -20), new Color("f6e2b8"), 3); }
    }

    private partial class ChunkNode : Node2D
    {
        private readonly Vector2I coord;
        public ChunkNode(Vector2I coord, int seed)
        {
            this.coord = coord; Position = new Vector2(coord.X * ChunkSize * TileSize, coord.Y * ChunkSize * TileSize); ZIndex = -5; QueueRedraw();
        }
        public override void _Draw()
        {
            var noise = new FastNoiseLite { Seed = WorldSeed, Frequency = 0.025f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
            for (int y = 0; y < ChunkSize; y++) for (int x = 0; x < ChunkSize; x++)
            {
                float value = noise.GetNoise2D(coord.X * ChunkSize + x, coord.Y * ChunkSize + y);
                Color color = value < -0.25f ? new Color("427b82") : value > 0.48f ? new Color("9a865c") : new Color("5f8c58");
                DrawRect(new Rect2(x * TileSize, y * TileSize, TileSize + 1, TileSize + 1), color);
                if (value > 0.2f && ((x * 17 + y * 31 + coord.X * 7 + coord.Y * 13) % 47 == 0)) DrawRect(new Rect2(x * TileSize + 10, y * TileSize + 8, 12, 16), new Color("31533b"));
                if (value < -0.25f && ((x + y * 3) % 19 == 0)) DrawCircle(new Vector2(x * TileSize + 16, y * TileSize + 16), 5, new Color("8bb4b2"));
            }
        }
    }
}

