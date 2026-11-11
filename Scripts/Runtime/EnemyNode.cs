using Godot;

public partial class EnemyNode : CharacterBody3D, IDamageable
{
    private readonly GameBootstrap owner;
    private float health = EnemySystem.EnemyHealth;
    private float attackTimer;
    private readonly Label3D healthBar;
    public Node3D DamageableNode => this;

    public EnemyNode(GameBootstrap owner, Vector3 position, int variant)
    {
        this.owner = owner;
        Position = position;

        Color color = variant == 0 ? new Color("b84f5c") : variant == 1 ? new Color("9c5bc4") : new Color("d17a42");
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1.2f, 1.4f, 1.2f) } });
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1.2f, 1.4f, 1.2f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.85f },
            Position = new Vector3(0, 0.7f, 0)
        });
        healthBar = new Label3D
        {
            Text = "██████",
            FontSize = 32,
            Modulate = new Color("ff5d68"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Position = new Vector3(0, 1.8f, 0)
        };
        AddChild(healthBar);
        owner.RegisterDamageable(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(owner)) return;
        CharacterBody3D player = owner.Player;
        if (player == null) return;

        Vector3 offset = player.GlobalPosition - GlobalPosition;
        offset.Y = 0;
        float distance = offset.Length();
        attackTimer -= (float)delta;

        if (distance < EnemySystem.ChaseRange && distance > 1.7f)
        {
            Velocity = offset.Normalized() * 2.2f;
            MoveAndSlide();
            Position = new Vector3(Position.X, owner.GetTerrainHeight(GlobalPosition.X, GlobalPosition.Z) + 0.7f, Position.Z);
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        if (distance < EnemySystem.ProjectileRange && attackTimer <= 0f)
        {
            owner.SpawnProjectile(GlobalPosition + new Vector3(0, 0.8f, 0), player, EnemySystem.EnemyDamage, new Color("ff5d68"), false);
            attackTimer = 1.0f;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        GameLogger.Info($"敌人受伤：-{damage:0}，剩余生命 {Mathf.Max(0, health):0}");
        int segments = Mathf.Clamp(Mathf.CeilToInt(health / 12f), 0, 6);
        healthBar.Text = new string('█', segments) + new string('░', 6 - segments);
        if (health <= 0f)
        {
            GameLogger.Info("敌人被击败");
            QueueFree();
        }
    }

    public override void _ExitTree() => owner?.UnregisterDamageable(this);
}
