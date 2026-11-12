using Godot;
using System.Collections.Generic;

public partial class GameBootstrap : Node3D
{
    internal const float DayNightCycleDurationSeconds = 10f;

    private CharacterBody3D player;
    internal CharacterBody3D Player => player;
    internal bool CanInteract => gameStarted && !playerDead && !GetTree().Paused;
    private Camera3D camera;
    private float cameraYaw;
    private float cameraPitch = 0.38f;
    private float cameraDistance = 6.5f;
    private readonly Dictionary<Vector2I, ChunkNode> chunks = new();
    private Vector2I lastChunk = new(int.MinValue, int.MinValue);
    private Label status;
    private Label vitalsLabel;
    private ProgressBar healthBar;
    private ProgressBar experienceBar;
    private Label attackLabel;
    private float attackFeedback;
    private float playerHealth = 100f;
    private float playerQi = 50f;
    private float attackCooldown;
    private float attackBufferTimer;
    private float dashTimer;
    private float jumpBufferTimer;
    private MeshInstance3D attackRangeVisual;
    private MeshInstance3D handItem;
    private MeshInstance3D slashVisual;
    private float slashTimer;
    private Label controlsLabel;
    private Control hotbarPanel;
    private Button[] hudQuickbarSlots;
    private Control inventoryOverlay;
    private InventorySlotButton[] quickbarInventorySlots;
    private InventorySlotButton[] backpackSlots;
    private readonly InventorySystem inventory = new();
    private readonly PlayerProgression progression = new();
    private SaveSlotData activeSave;
    private bool playerDead;
    private float attackActionTimer;
    private float attackActionDuration;
    private bool attackHitDone;
    private Vector3 attackDirection;
    private ItemKind actionItemKind;
    private Control pauseOverlay;
    private Control helpOverlay;
    private Control statsOverlay;
    private Control deathOverlay;
    private Label statsText;
    private bool gameStarted;
    private readonly List<IDamageable> damageables = new();
    internal Vector3 AimDirection => new(-Mathf.Sin(cameraYaw), 0f, -Mathf.Cos(cameraYaw));

    public override void _Ready()
    {
        GameLogger.Initialize();
        GameLogger.Info("游戏初始化开始");
        ProcessMode = ProcessModeEnum.Always;
        BuildLighting(); BuildPlayer(); BuildUi();
        Vector2I screen = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(20, Mathf.Max(20, screen.Y - 560)));
        UpdateUiLayout();
        GameLogger.Info("游戏场景初始化完成");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMSizeChanged) UpdateUiLayout();
        if (what == NotificationWMCloseRequest)
        {
            SaveCurrentGame();
            GameLogger.Info("窗口关闭请求");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            cameraYaw -= motion.Relative.X * 0.0035f;
            cameraPitch = Mathf.Clamp(cameraPitch - motion.Relative.Y * 0.0025f, 0.12f, 0.95f);
        }
        if (CanInteract && @event is InputEventMouseButton wheel && wheel.Pressed)
        {
            if (wheel.ButtonIndex == MouseButton.WheelUp) cameraDistance = Mathf.Max(3.6f, cameraDistance - 0.55f);
            if (wheel.ButtonIndex == MouseButton.WheelDown) cameraDistance = Mathf.Min(9f, cameraDistance + 0.55f);
        }
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape && gameStarted)
        {
            if (inventoryOverlay.Visible)
            {
                inventoryOverlay.Visible = false;
                SetMouseCaptured(true);
                GameLogger.Info("关闭背包界面");
            }
            else
            {
                GetTree().Paused = !GetTree().Paused;
                pauseOverlay.Visible = GetTree().Paused;
                SetMouseCaptured(!GetTree().Paused);
                GameLogger.Info(GetTree().Paused ? "游戏暂停" : "游戏继续");
            }
        }
        if (@event is InputEventKey statsKey && statsKey.Pressed && statsKey.Keycode == Key.C && gameStarted && !playerDead && (!GetTree().Paused || statsOverlay.Visible))
        {
            ToggleStats();
            return;
        }
        if (CanInteract && @event is InputEventKey jumpKey && jumpKey.Pressed && !jumpKey.Echo && jumpKey.Keycode == Key.Space)
            jumpBufferTimer = PlayerController.JumpBufferDuration;
        if (CanInteract && @event is InputEventKey attackKey && attackKey.Pressed && !attackKey.Echo && attackKey.Keycode == Key.F)
            attackBufferTimer = 0.18f;
        if (CanInteract && @event is InputEventMouseButton attackMouse && attackMouse.Pressed && attackMouse.ButtonIndex == MouseButton.Left && !inventoryOverlay.Visible)
            attackBufferTimer = 0.18f;
        if (@event is InputEventKey itemKey && itemKey.Pressed && gameStarted && !GetTree().Paused)
        {
            if (playerDead) return;
            if (itemKey.Keycode == Key.Q) SelectItem(inventory.SelectedIndex - 1);
            if (itemKey.Keycode == Key.E) SelectItem(inventory.SelectedIndex + 1);
            if (itemKey.Keycode == Key.B)
            {
                inventoryOverlay.Visible = !inventoryOverlay.Visible;
                SetMouseCaptured(!inventoryOverlay.Visible);
                GameLogger.Info(inventoryOverlay.Visible ? "打开背包界面" : "关闭背包界面");
            }
        }
    }

    private void BuildLighting()
    {
        AddChild(new DayNightCycle(this));
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateUiLayout();
        if (!gameStarted || playerDead || statsOverlay.Visible || GetTree().Paused) return;
        Vector2 input = Input.GetVector("left", "right", "forward", "back");
        Vector3 forward = AimDirection;
        Vector3 right = new(Mathf.Cos(cameraYaw), 0f, -Mathf.Sin(cameraYaw));
        Vector3 direction = right * input.X + forward * -input.Y;
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - (float)delta);
        bool sprintHeld = Input.IsActionPressed("slow_walk");
        if (Input.IsActionJustPressed("slow_walk")) dashTimer = PlayerController.DashDuration;
        float moveSpeed = (dashTimer > 0f ? PlayerController.DashSpeed : sprintHeld ? PlayerController.RunSpeed : PlayerController.WalkSpeed) * progression.SpeedMultiplier;
        if (dashTimer > 0f) dashTimer -= (float)delta;
        float groundHeight = GetTerrainHeight(player.Position.X, player.Position.Z);
        bool grounded = player.Position.Y <= groundHeight + 0.08f;
        Vector3 velocity = direction.Normalized() * moveSpeed;
        if (grounded)
        {
            if (jumpBufferTimer > 0f)
            {
                velocity.Y = PlayerController.JumpVelocity;
                jumpBufferTimer = 0f;
                GameLogger.Info("玩家起跳");
            }
            else velocity.Y = 0f;
        }
        else velocity.Y = player.Velocity.Y - PlayerController.Gravity * (float)delta;
        player.Velocity = velocity;
        player.MoveAndSlide();
        if (direction.LengthSquared() > 0.01f) player.Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z), 0);
        UpdateThirdPersonCamera((float)delta);
        float newGroundHeight = GetTerrainHeight(player.Position.X, player.Position.Z);
        if (player.Position.Y <= newGroundHeight)
        {
            player.Position = new Vector3(player.Position.X, newGroundHeight, player.Position.Z);
            if (player.Velocity.Y < 0f) player.Velocity = new Vector3(player.Velocity.X, 0f, player.Velocity.Z);
        }
        playerQi = Mathf.Min(progression.MaxQi, playerQi + (float)delta * 1.5f);
        attackCooldown = Mathf.Max(0f, attackCooldown - (float)delta);
        attackBufferTimer = Mathf.Max(0f, attackBufferTimer - (float)delta);
        attackFeedback = Mathf.Max(0f, attackFeedback - (float)delta);
        slashTimer = Mathf.Max(0f, slashTimer - (float)delta);
        if (slashVisual != null) slashVisual.Visible = slashTimer > 0f;
        if (attackActionTimer > 0f)
        {
            attackActionTimer -= (float)delta;
            float hitMoment = actionItemKind switch
            {
                ItemKind.Fireball or ItemKind.IceSpell => 0.1f,
                ItemKind.WoodenSword => 0.14f,
                _ => 0.18f
            };
            handItem.Rotation = new Vector3(Mathf.Sin((1f - attackActionTimer / attackActionDuration) * Mathf.Pi) * 0.8f, 0, 0);
            if (!attackHitDone && attackActionTimer <= hitMoment)
            {
                attackHitDone = true;
                float damage = GetPlayerAttackDamage(actionItemKind == ItemKind.Fireball);
                if (actionItemKind == ItemKind.Fireball || actionItemKind == ItemKind.IceSpell)
                {
                    SpawnDirectionalProjectile(player.GlobalPosition + new Vector3(0, 1.1f, 0), attackDirection, damage,
                        actionItemKind == ItemKind.Fireball ? new Color("ff7a32") : new Color("79c9ed"),
                        actionItemKind == ItemKind.IceSpell, actionItemKind == ItemKind.Fireball ? 14f : 12f);
                    ShowUseFeedback($"{inventory.SelectedItem?.Name}！");
                }
                else ResolveMeleeAttack(actionItemKind, attackDirection, damage);
            }
            if (attackActionTimer <= 0f) handItem.Rotation = Vector3.Zero;
        }
        else if (attackBufferTimer > 0f && attackCooldown <= 0f)
        {
            attackBufferTimer = 0f;
            StartAttack();
        }
        Vector2I current = new(Mathf.FloorToInt(player.Position.X / WorldManager.ChunkSize), Mathf.FloorToInt(player.Position.Z / WorldManager.ChunkSize));
        if (current != lastChunk) UpdateChunks(false);
        status.Text = $"{activeSave?.Name ?? "洞天"}  ·  {progression.Realm}";
        vitalsLabel.Text = $"血 {playerHealth:0}/{progression.MaxHealth:0}   气 {playerQi:0}/{progression.MaxQi:0}";
        healthBar.Value = progression.MaxHealth <= 0 ? 0 : playerHealth / progression.MaxHealth * 100f;
        experienceBar.Value = progression.ExperienceToNextLevel <= 0 ? 0 : progression.Experience / progression.ExperienceToNextLevel * 100f;
        attackLabel.Visible = attackFeedback > 0f;
        attackRangeVisual.Visible = attackFeedback > 0f;
    }

    private void UpdateThirdPersonCamera(float delta)
    {
        Vector3 focus = player.GlobalPosition + new Vector3(0f, 1.25f, 0f);
        float horizontal = Mathf.Cos(cameraPitch) * cameraDistance;
        Vector3 offset = new(Mathf.Sin(cameraYaw) * horizontal, Mathf.Sin(cameraPitch) * cameraDistance, Mathf.Cos(cameraYaw) * horizontal);
        Vector3 desired = focus + offset;
        var query = PhysicsRayQueryParameters3D.Create(focus, desired);
        query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count > 0) desired = (Vector3)hit["position"] + (focus - desired).Normalized() * 0.25f;
        camera.GlobalPosition = camera.GlobalPosition.Lerp(desired, Mathf.Clamp(delta * 12f, 0f, 1f));
        camera.LookAt(focus, Vector3.Up);
    }

    private void SetMouseCaptured(bool captured)
    {
        Input.MouseMode = captured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }
}
