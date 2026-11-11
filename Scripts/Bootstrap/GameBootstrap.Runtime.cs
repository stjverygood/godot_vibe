using Godot;
using System.Collections.Generic;

public partial class GameBootstrap : Node3D
{
    private CharacterBody3D player;
    internal CharacterBody3D Player => player;
    internal bool CanInteract => gameStarted && !GetTree().Paused;
    private Camera3D camera;
    private readonly Dictionary<Vector2I, ChunkNode> chunks = new();
    private Vector2I lastChunk = new(int.MinValue, int.MinValue);
    private Label status;
    private Label attackLabel;
    private float attackFeedback;
    private float playerHealth = 100f;
    private float attackCooldown;
    private float dashTimer;
    private MeshInstance3D attackRangeVisual;
    private MeshInstance3D handItem;
    private Label resourceLabel;
    private Label controlsLabel;
    private ColorRect hotbarPanel;
    private Button[] hudQuickbarSlots;
    private Control inventoryOverlay;
    private InventorySlotButton[] quickbarInventorySlots;
    private InventorySlotButton[] backpackSlots;
    private readonly InventorySystem inventory = new();
    private float attackActionTimer;
    private bool attackHitDone;
    private Node3D currentAttackTarget;
    private Control startOverlay;
    private Control pauseOverlay;
    private Control helpOverlay;
    private bool gameStarted;
    private readonly List<IDamageable> damageables = new();

    public override void _Ready()
    {
        GameLogger.Initialize();
        GameLogger.Info("游戏初始化开始");
        ProcessMode = ProcessModeEnum.Always;
        BuildLighting(); BuildPlayer(); BuildUi(); UpdateChunks(true);
        Vector2I screen = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(20, Mathf.Max(20, screen.Y - 560)));
        UpdateUiLayout();
        GameLogger.Info("游戏场景初始化完成");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMSizeChanged) UpdateUiLayout();
        if (what == NotificationWMCloseRequest) GameLogger.Info("窗口关闭请求");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape && gameStarted)
        {
            if (inventoryOverlay.Visible)
            {
                inventoryOverlay.Visible = false;
                GameLogger.Info("关闭背包界面");
            }
            else
            {
                GetTree().Paused = !GetTree().Paused;
                pauseOverlay.Visible = GetTree().Paused;
                GameLogger.Info(GetTree().Paused ? "游戏暂停" : "游戏继续");
            }
        }
        if (@event is InputEventKey enter && enter.Pressed && enter.Keycode == Key.Enter && !gameStarted) StartGame();
        if (@event is InputEventKey itemKey && itemKey.Pressed && gameStarted && !GetTree().Paused)
        {
            if (itemKey.Keycode == Key.Q) SelectItem(inventory.SelectedIndex - 1);
            if (itemKey.Keycode == Key.E) SelectItem(inventory.SelectedIndex + 1);
            if (itemKey.Keycode == Key.B)
            {
                inventoryOverlay.Visible = !inventoryOverlay.Visible;
                GameLogger.Info(inventoryOverlay.Visible ? "打开背包界面" : "关闭背包界面");
            }
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
        if (Input.IsActionJustPressed("slow_walk")) dashTimer = PlayerController.DashDuration;
        float moveSpeed = dashTimer > 0f ? PlayerController.DashSpeed : sprintHeld ? PlayerController.RunSpeed : PlayerController.WalkSpeed;
        if (dashTimer > 0f) dashTimer -= (float)delta;
        player.Velocity = direction.Normalized() * moveSpeed;
        player.MoveAndSlide();
        if (direction.LengthSquared() > 0.01f) player.Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z), 0);
        camera.GlobalPosition = player.GlobalPosition + new Vector3(0, 22, 14);
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
                    bool fireball = inventory.IsFireball;
                    float damage = fireball ? CombatSystem.FireballDamage : CombatSystem.SwordDamage;
                    SpawnProjectile(player.GlobalPosition + new Vector3(0, 1.1f, 0), currentAttackTarget, damage, fireball ? new Color("ff7a32") : new Color("dcecff"), fireball);
                    attackLabel.Text = fireball ? "火球！-50" : "剑气！-35";
                }
            }
            if (attackActionTimer <= 0f) currentAttackTarget = null;
        }
        else if ((Input.IsKeyPressed(Key.F) || Input.IsKeyPressed(Key.Space)) && attackCooldown <= 0f) StartAttack();
        Vector2I current = new(Mathf.FloorToInt(player.Position.X / WorldManager.ChunkSize), Mathf.FloorToInt(player.Position.Z / WorldManager.ChunkSize));
        if (current != lastChunk) UpdateChunks(false);
        status.Text = $"无限世界 3D\n生命 {playerHealth:0}\n区块 {current.X}, {current.Y}";
        resourceLabel.Text = inventory.ResourceSummary;
        attackLabel.Visible = attackFeedback > 0f;
        attackRangeVisual.Visible = attackFeedback > 0f;
    }
}
