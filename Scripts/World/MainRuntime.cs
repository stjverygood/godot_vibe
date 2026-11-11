using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    private const int ChunkSize = 32, LoadRadius = 2, Seed = 24681357;
    private CharacterBody3D player;
    private Camera3D camera;
    private readonly Dictionary<Vector2I, ChunkNode> chunks = new();
    private Vector2I lastChunk = new(int.MinValue, int.MinValue);
    private Label status;
    private ProgressBar healthBar;
    private Label attackLabel;
    private float attackFeedback;
    private float playerHealth = 100f;
    private float attackCooldown;
    private float dashTimer;
    private MeshInstance3D attackRangeVisual;
    private MeshInstance3D handItem;
    private Label hotbarLabel;
    private Label helpLabel;
    private readonly string[] inventory = { "空手", "木剑", "石块", "药水", "火球" };
    private readonly Color[] itemColors = { new Color("ffe08a"), new Color("d8b06a"), new Color("8b9299"), new Color("4ba5d8"), new Color("e58a3d") };
    private int selectedItem;
    private float attackActionTimer;
    private bool attackHitDone;
    private EnemyNode currentAttackTarget;
    private Control startOverlay;
    private Control pauseOverlay;
    private Control helpOverlay;
    private bool gameStarted;
    private readonly List<EnemyNode> enemies = new();
    private readonly FastNoiseLite terrainNoise = new() { Seed = Seed, Frequency = 0.045f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
    private readonly FastNoiseLite terrainDetail = new() { Seed = Seed + 91, Frequency = 0.12f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        BuildLighting(); BuildPlayer(); BuildUi(); UpdateChunks(true);
        Vector2I screen = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(20, Mathf.Max(20, screen.Y - 560)));
        UpdateUiLayout();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMSizeChanged) UpdateUiLayout();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape && gameStarted)
        {
            GetTree().Paused = !GetTree().Paused;
            pauseOverlay.Visible = GetTree().Paused;
        }
        if (@event is InputEventKey enter && enter.Pressed && enter.Keycode == Key.Enter && !gameStarted) StartGame();
        if (@event is InputEventKey itemKey && itemKey.Pressed && gameStarted && !GetTree().Paused)
        {
            if (itemKey.Keycode == Key.Q) SelectItem(selectedItem - 1);
            if (itemKey.Keycode == Key.E) SelectItem(selectedItem + 1);
        }
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
        UpdateUiLayout();
        if (!gameStarted) return;
        Vector2 input = Input.GetVector("left", "right", "forward", "back");
        Vector3 direction = new(input.X, 0, input.Y);
        bool sprintHeld = Input.IsActionPressed("slow_walk");
        if (Input.IsActionJustPressed("slow_walk")) dashTimer = 0.12f;
        float moveSpeed = dashTimer > 0f ? 30f : sprintHeld ? 12f : 6f;
        if (dashTimer > 0f) dashTimer -= (float)delta;
        player.Velocity = direction.Normalized() * moveSpeed;
        player.MoveAndSlide();
        if (direction.LengthSquared() > 0.01f) player.Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z), 0);
        camera.GlobalPosition = player.GlobalPosition + new Vector3(0, 16, 16);
        camera.LookAt(player.GlobalPosition, Vector3.Up);
        player.Position = new Vector3(player.Position.X, GetTerrainHeight(player.Position.X, player.Position.Z) + 1.0f, player.Position.Z);
        attackCooldown = Mathf.Max(0f, attackCooldown - (float)delta);
        attackFeedback = Mathf.Max(0f, attackFeedback - (float)delta);
        if (attackActionTimer > 0f)
        {
            attackActionTimer -= (float)delta;
            if (IsInstanceValid(currentAttackTarget)) FaceTarget(currentAttackTarget);
            if (!attackHitDone && attackActionTimer <= 0.22f)
            {
                attackHitDone = true;
                if (IsInstanceValid(currentAttackTarget))
                {
                    bool fireball = selectedItem == 4;
                    float damage = fireball ? 50f : 35f;
                    SpawnProjectile(player.GlobalPosition + new Vector3(0, 1.1f, 0), currentAttackTarget, damage, fireball ? new Color("ff7a32") : new Color("dcecff"), fireball);
                    attackLabel.Text = fireball ? "火球！-50" : "剑气！-35";
                }
            }
            if (attackActionTimer <= 0f) currentAttackTarget = null;
        }
        else if ((Input.IsKeyPressed(Key.F) || Input.IsKeyPressed(Key.Space)) && attackCooldown <= 0f) StartAttack();
        Vector2I current = new(Mathf.FloorToInt(player.Position.X / ChunkSize), Mathf.FloorToInt(player.Position.Z / ChunkSize));
        if (current != lastChunk) UpdateChunks(false);
        status.Text = $"无限世界 3D\n生命 {playerHealth:0}\n区块 {current.X}, {current.Y}";
        healthBar.Value = playerHealth;
        attackLabel.Visible = attackFeedback > 0f;
        attackRangeVisual.Visible = attackFeedback > 0f;
    }

    private void StartAttack()
    {
        attackCooldown = 0.5f;
        attackActionTimer = 0.5f;
        attackHitDone = false;
        currentAttackTarget = null;
        if (selectedItem != 1 && selectedItem != 4)
        {
            attackActionTimer = 0.15f;
            attackFeedback = 0.5f;
            attackLabel.Text = "当前物品没有攻击效果";
            return;
        }
        float closest = 7.0f;
        foreach (EnemyNode enemy in enemies)
        {
            if (!IsInstanceValid(enemy)) continue;
            float distance = new Vector2(enemy.GlobalPosition.X - player.GlobalPosition.X, enemy.GlobalPosition.Z - player.GlobalPosition.Z).Length();
            if (distance < closest) { closest = distance; currentAttackTarget = enemy; }
        }
        if (IsInstanceValid(currentAttackTarget)) FaceTarget(currentAttackTarget);
        attackFeedback = 0.5f;
        attackLabel.Text = currentAttackTarget == null ? "攻击落空" : "蓄力攻击";
    }

    private void FaceTarget(EnemyNode target)
    {
        Vector3 toTarget = target.GlobalPosition - player.GlobalPosition;
        toTarget.Y = 0;
        if (toTarget.LengthSquared() > 0.01f) player.Rotation = new Vector3(0, Mathf.Atan2(-toTarget.X, -toTarget.Z), 0);
    }

    private void SpawnProjectile(Vector3 start, Node3D target, float damage, Color color, bool homing)
    {
        var projectile = new ProjectileNode(this, target, damage, color, homing) { GlobalPosition = start };
        AddChild(projectile);
    }

    private void RegisterEnemy(EnemyNode enemy) { if (!enemies.Contains(enemy)) enemies.Add(enemy); }
    private void UnregisterEnemy(EnemyNode enemy) { enemies.Remove(enemy); }

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
            if (!chunks.ContainsKey(coord)) { var chunk = new ChunkNode(coord, this); chunks[coord] = chunk; AddChild(chunk); }
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
        player.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.28f, 0.3f, 0.7f) }, MaterialOverride = Mat(new Color("ffe08a")), Position = new Vector3(0, 1.15f, -0.55f) });
        handItem = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.22f, 0.65f) }, MaterialOverride = Mat(itemColors[0]), Position = new Vector3(0.65f, 1.15f, -0.45f) };
        player.AddChild(handItem);
        attackRangeVisual = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 6.7f, OuterRadius = 7.0f }, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.82f, 0.25f, 0.75f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded }, Visible = false, Position = new Vector3(0, 0.08f, 0) };
        player.AddChild(attackRangeVisual);
        camera = new Camera3D { Position = new Vector3(0, 16, 16), TopLevel = true, Current = true, Fov = 58 };
        player.AddChild(camera);
        camera.GlobalPosition = player.GlobalPosition + new Vector3(0, 16, 16);
        camera.LookAt(player.GlobalPosition, Vector3.Up);
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var panel = new ColorRect { Position = new Vector2(20, 20), Size = new Vector2(300, 105), Color = new Color(0.03f, 0.05f, 0.08f, 0.78f) }; layer.AddChild(panel);
        status = new Label { Position = new Vector2(15, 12), Text = "无限世界 3D" }; status.AddThemeFontSizeOverride("font_size", 18); status.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(status);
        healthBar = new ProgressBar { Position = new Vector2(15, 76), Size = new Vector2(270, 18), MinValue = 0, MaxValue = 100, Value = 100, ShowPercentage = true }; panel.AddChild(healthBar);
        attackLabel = new Label { Position = new Vector2(380, 24), Text = "攻击！", Visible = false }; attackLabel.AddThemeFontSizeOverride("font_size", 24); attackLabel.AddThemeColorOverride("font_color", new Color("ffcf75")); layer.AddChild(attackLabel);
        helpLabel = new Label { Position = new Vector2(20, 660), Text = "WASD 移动   Shift 慢走   窗口化俯视角" }; helpLabel.AddThemeFontSizeOverride("font_size", 16); helpLabel.AddThemeColorOverride("font_color", new Color("f5e6c8")); layer.AddChild(helpLabel);
        hotbarLabel = new Label { Position = new Vector2(350, 485), Size = new Vector2(260, 35), HorizontalAlignment = HorizontalAlignment.Center };
        hotbarLabel.AddThemeFontSizeOverride("font_size", 18); hotbarLabel.AddThemeColorOverride("font_color", new Color("f5e6c8")); layer.AddChild(hotbarLabel);
        RefreshHotbar();
        startOverlay = MakeOverlay(layer, "TimeToGo", "无限世界 · 3D 漫游", "开始游戏  (Enter)");
        pauseOverlay = MakeOverlay(layer, "游戏暂停", "探索暂时停下来了", "继续游戏  (Esc)");
        pauseOverlay.Visible = false;
        startOverlay.GetNode<Button>("ActionButton").Pressed += StartGame;
        pauseOverlay.GetNode<Button>("ActionButton").Pressed += TogglePause;
        startOverlay.GetNode<Button>("HelpButton").Pressed += ShowHelp;
        pauseOverlay.GetNode<Button>("HelpButton").Pressed += ShowHelp;
        startOverlay.GetNode<Button>("QuitButton").Pressed += QuitGame;
        pauseOverlay.GetNode<Button>("QuitButton").Pressed += QuitGame;
        helpOverlay = MakeHelpOverlay(layer);
        helpOverlay.Visible = false;
    }

    private Control MakeOverlay(CanvasLayer layer, string titleText, string subtitleText, string buttonText)
    {
        var panel = new ColorRect { Name = "Overlay", Position = Vector2.Zero, Size = new Vector2(960, 540), Color = new Color(0.02f, 0.03f, 0.05f, 0.92f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = titleText, Position = new Vector2(320, 170), Size = new Vector2(320, 60), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 38); title.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(title);
        var subtitle = new Label { Text = subtitleText, Position = new Vector2(280, 240), Size = new Vector2(400, 40), HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeFontSizeOverride("font_size", 20); subtitle.AddThemeColorOverride("font_color", new Color("b9d6e8")); panel.AddChild(subtitle);
        var button = new Button { Name = "ActionButton", Text = buttonText, Position = new Vector2(360, 315), Size = new Vector2(240, 55) };
        button.AddThemeFontSizeOverride("font_size", 20); panel.AddChild(button);
        panel.AddChild(new Button { Name = "HelpButton", Text = "操作说明", Position = new Vector2(360, 385), Size = new Vector2(115, 42) });
        panel.AddChild(new Button { Name = "QuitButton", Text = "退出游戏", Position = new Vector2(485, 385), Size = new Vector2(115, 42) });
        return panel;
    }

    private void StartGame() { gameStarted = true; startOverlay.Visible = false; }
    private void TogglePause() { GetTree().Paused = false; pauseOverlay.Visible = false; }
    private void ShowHelp() { helpOverlay.Visible = true; }
    private void QuitGame() { GetTree().Quit(); }

    private void SelectItem(int index)
    {
        selectedItem = (index % inventory.Length + inventory.Length) % inventory.Length;
        handItem.MaterialOverride = Mat(itemColors[selectedItem]);
        RefreshHotbar();
    }

    private void RefreshHotbar()
    {
        if (hotbarLabel == null) return;
        string text = "快捷栏  ";
        for (int i = 0; i < inventory.Length; i++) text += i == selectedItem ? $"[ {inventory[i]} ]  " : $"{inventory[i]}  ";
        hotbarLabel.Text = text;
    }

    private void UpdateUiLayout()
    {
        if (startOverlay == null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        hotbarLabel.Position = new Vector2((size.X - 260f) / 2f, size.Y - 55f);
        helpLabel.Position = new Vector2(20, size.Y - 42f);
        LayoutOverlay(startOverlay, size);
        LayoutOverlay(pauseOverlay, size);
        LayoutHelpOverlay(helpOverlay, size);
    }

    private void LayoutOverlay(Control panel, Vector2 size)
    {
        panel.Size = size;
        panel.GetChild<Control>(0).Position = new Vector2((size.X - 320f) / 2f, size.Y * 0.31f);
        panel.GetChild<Control>(1).Position = new Vector2((size.X - 400f) / 2f, size.Y * 0.44f);
        panel.GetNode<Button>("ActionButton").Position = new Vector2((size.X - 240f) / 2f, size.Y * 0.58f);
        panel.GetNode<Button>("HelpButton").Position = new Vector2(size.X / 2f - 120f, size.Y * 0.71f);
        panel.GetNode<Button>("QuitButton").Position = new Vector2(size.X / 2f + 5f, size.Y * 0.71f);
    }

    private void LayoutHelpOverlay(Control panel, Vector2 size)
    {
        if (panel == null) return;
        panel.Size = size;
        panel.GetChild<Control>(0).Position = new Vector2((size.X - 300f) / 2f, size.Y * 0.2f);
        panel.GetChild<Control>(1).Position = new Vector2((size.X - 360f) / 2f, size.Y * 0.35f);
        panel.GetChild<Button>(2).Position = new Vector2((size.X - 180f) / 2f, size.Y * 0.76f);
    }

    private Control MakeHelpOverlay(CanvasLayer layer)
    {
        var panel = new ColorRect { Name = "HelpOverlay", Position = Vector2.Zero, Size = new Vector2(960, 540), Color = new Color(0.02f, 0.03f, 0.05f, 0.96f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = "操作说明", Position = new Vector2(330, 105), Size = new Vector2(300, 55), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 34); title.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(title);
        var content = new Label { Text = "WASD       移动\nShift       突进，继续按住可奔跑\nF / 空格    发射攻击弹体\nEsc         暂停或继续游戏\n\n攻击会自动朝向范围内最近的敌人", Position = new Vector2(300, 190), Size = new Vector2(360, 190), HorizontalAlignment = HorizontalAlignment.Center };
        content.AddThemeFontSizeOverride("font_size", 20); content.AddThemeColorOverride("font_color", new Color("b9d6e8")); panel.AddChild(content);
        var back = new Button { Text = "返回", Position = new Vector2(390, 410), Size = new Vector2(180, 48) };
        back.AddThemeFontSizeOverride("font_size", 18); back.Pressed += () => helpOverlay.Visible = false; panel.AddChild(back);
        return panel;
    }

    private StandardMaterial3D Mat(Color color) => new() { AlbedoColor = color, Roughness = 0.9f };

    private partial class ChunkNode : Node3D
    {
        private readonly Vector2I coord;
        private readonly Main owner;
        public ChunkNode(Vector2I coord, Main owner)
        {
            this.coord = coord; this.owner = owner; Position = new Vector3(coord.X * ChunkSize, 0, coord.Y * ChunkSize);
            var noise = new FastNoiseLite { Seed = Seed, Frequency = 0.045f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
            var detail = new FastNoiseLite { Seed = Seed + 91, Frequency = 0.12f, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
            BuildTerrain(noise, detail);
            AddWaterSurface();
            SpawnEnemies();
        }

        private void SpawnEnemies()
        {
            int count = Mathf.Abs((coord.X * 92821 + coord.Y * 68917 + Seed) % 3);
            for (int i = 0; i < count; i++)
            {
                float x = 5f + Mathf.Abs((coord.X * 31 + i * 13) % 21);
                float z = 5f + Mathf.Abs((coord.Y * 37 + i * 17) % 21);
                AddChild(new EnemyNode(owner, new Vector3(x, owner.GetTerrainHeight(Position.X + x, Position.Z + z) + 0.7f, z), i));
            }
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

    private partial class EnemyNode : CharacterBody3D
    {
        private readonly Main owner;
        private float health = 70f;
        private float attackTimer;
        private Label3D healthBar;
        public EnemyNode(Main owner, Vector3 position, int variant)
        {
            this.owner = owner; Position = position;
            Color color = variant == 0 ? new Color("b84f5c") : variant == 1 ? new Color("9c5bc4") : new Color("d17a42");
            AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1.2f, 1.4f, 1.2f) } });
            AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1.2f, 1.4f, 1.2f) }, MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.85f }, Position = new Vector3(0, 0.7f, 0) });
            healthBar = new Label3D { Text = "██████", FontSize = 32, Modulate = new Color("ff5d68"), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Position = new Vector3(0, 1.8f, 0) };
            AddChild(healthBar);
            owner.RegisterEnemy(this);
        }
        public override void _PhysicsProcess(double delta)
        {
            if (!IsInstanceValid(owner) || owner.player == null) return;
            Vector3 offset = owner.player.GlobalPosition - GlobalPosition; offset.Y = 0;
            float distance = offset.Length(); attackTimer -= (float)delta;
            if (distance < 14f && distance > 1.7f) { Velocity = offset.Normalized() * 2.2f; MoveAndSlide(); Position = new Vector3(Position.X, owner.GetTerrainHeight(GlobalPosition.X, GlobalPosition.Z) + 0.7f, Position.Z); }
            else Velocity = Vector3.Zero;
            if (distance < 10f && attackTimer <= 0f) { owner.SpawnProjectile(GlobalPosition + new Vector3(0, 0.8f, 0), owner.player, 8f, new Color("ff5d68"), false); attackTimer = 1.0f; }
        }
        public void TakeDamage(float damage)
        {
            health -= damage;
            int segments = Mathf.Clamp(Mathf.CeilToInt(health / 12f), 0, 6);
            healthBar.Text = new string('█', segments) + new string('░', 6 - segments);
            if (health <= 0f) QueueFree();
        }
        public override void _ExitTree() { owner?.UnregisterEnemy(this); }
    }

    private partial class ProjectileNode : Node3D
    {
        private readonly Main owner;
        private readonly Node3D target;
        private readonly float damage;
        private readonly bool homing;
        private Vector3 travelDirection;
        private float lifetime = 3f;
        private readonly float speed = 14f;

        public ProjectileNode(Main owner, Node3D target, float damage, Color color, bool homing)
        {
            this.owner = owner; this.target = target; this.damage = damage; this.homing = homing;
            AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.18f, Height = 0.36f }, MaterialOverride = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 2f } });
        }

        public override void _PhysicsProcess(double delta)
        {
            lifetime -= (float)delta;
            if (lifetime <= 0f || !IsInstanceValid(target)) { QueueFree(); return; }
            Vector3 offset = target.GlobalPosition + new Vector3(0, 0.8f, 0) - GlobalPosition;
            if (!homing && travelDirection == Vector3.Zero) travelDirection = offset.Normalized();
            float distance = offset.Length();
            if (distance < 0.75f)
            {
                if (target == owner.player) owner.playerHealth = Mathf.Max(0, owner.playerHealth - damage);
                else if (target is EnemyNode enemy) enemy.TakeDamage(damage);
                QueueFree(); return;
            }
            GlobalPosition += (homing ? offset.Normalized() : travelDirection) * speed * (float)delta;
        }
    }
}
