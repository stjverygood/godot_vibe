using Godot;

public partial class EnemyNode : CharacterBody3D, IDamageable
{
    private readonly GameBootstrap owner;
    private float health;
    private float attackTimer;
    private float slowTimer;
    private float attackWindup;
    private float knockbackTimer;
    private float flashTimer;
    private MeshInstance3D bodyVisual;
    private StandardMaterial3D bodyMaterial;
    private readonly EnemySystem.BeastProfile profile;
    private readonly Label3D healthBar;
    public Node3D DamageableNode => this;

    public EnemyNode(GameBootstrap owner, Vector3 position, int variant)
    {
        this.owner = owner;
        Position = position;
        profile = EnemySystem.Profile(variant);
        health = profile.Health;
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1.2f, 1.4f, 1.2f) } });
        bodyVisual = BuildLowPolyModel(variant);
        healthBar = new Label3D
        {
            Text = $"{profile.Name} · {profile.Realm}\n██████",
            FontSize = 32,
            Modulate = new Color("ff5d68"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Position = new Vector3(0, 1.8f, 0)
        };
        AddChild(healthBar);
        owner.RegisterDamageable(this);
    }

    private MeshInstance3D BuildLowPolyModel(int variant)
    {
        bodyMaterial = new StandardMaterial3D { AlbedoColor = profile.Color, Roughness = 1f };
        var material = bodyMaterial;
        MeshInstance3D Part(Mesh mesh, Vector3 position, Vector3 scale)
        {
            var part = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position, Scale = scale };
            AddChild(part);
            return part;
        }
        SphereMesh PolySphere(float radius) => new() { Radius = radius, Height = radius * 2f, RadialSegments = 6, Rings = 3 };

        if (variant % 3 == 2)
        {
            Part(PolySphere(0.72f), new Vector3(0, 0.72f, 0), new Vector3(1.2f, 0.75f, 1.1f));
            Part(new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.68f, Height = 0.25f, RadialSegments = 6 }, new Vector3(0, 1.15f, 0), Vector3.One);
            Part(PolySphere(0.36f), new Vector3(0, 1.05f, -0.52f), Vector3.One);
            return GetChild<MeshInstance3D>(1);
        }

        Part(PolySphere(0.62f), new Vector3(0, 0.58f, 0.05f), new Vector3(0.9f, 0.78f, 1.25f));
        Part(PolySphere(0.37f), new Vector3(0, 0.95f, -0.72f), new Vector3(0.95f, 0.9f, 0.9f));
        for (int side = -1; side <= 1; side += 2)
        {
            Part(new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.16f, Height = variant % 3 == 0 ? 0.52f : 0.34f, RadialSegments = 5 },
                new Vector3(side * 0.23f, 1.27f, -0.69f), new Vector3(1, 1, variant % 3 == 0 ? 1 : 0.65f));
            Part(new PrismMesh { Size = new Vector3(0.22f, 0.42f, 0.22f) }, new Vector3(side * 0.38f, 0.25f, -0.1f), Vector3.One);
        }
        if (variant % 3 == 1)
            Part(new SphereMesh { Radius = 0.34f, Height = 0.8f, RadialSegments = 5, Rings = 3 }, new Vector3(0.12f, 0.54f, 0.95f), new Vector3(0.8f, 0.8f, 1.5f));
        return GetChild<MeshInstance3D>(1);
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
        slowTimer = Mathf.Max(0f, slowTimer - (float)delta);

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= (float)delta;
            MoveAndSlide();
        }
        else if (attackWindup > 0f)
        {
            Velocity = Vector3.Zero;
            attackWindup -= (float)delta;
            if (attackWindup <= 0f && distance < profile.Range + 0.7f)
            {
                if (profile.Range > 3f) owner.SpawnProjectile(GlobalPosition + new Vector3(0, 0.8f, 0), player, profile.Damage, profile.Color, false);
                else owner.ApplyPlayerDamage(profile.Damage);
                attackTimer = profile.Name == "岩甲龟" ? 1.7f : 1.15f;
            }
        }
        else if (distance < EnemySystem.ChaseRange && distance > profile.Range + 0.4f)
        {
            Velocity = offset.Normalized() * profile.Speed * (slowTimer > 0f ? 0.5f : 1f);
            MoveAndSlide();
            Position = new Vector3(Position.X, owner.GetTerrainHeight(GlobalPosition.X, GlobalPosition.Z) + 0.7f, Position.Z);
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        if (attackWindup <= 0f && distance < profile.Range && attackTimer <= 0f)
        {
            attackWindup = profile.Range > 3f ? 0.65f : 0.42f;
            healthBar.Text = $"{profile.Name} · 蓄势！\n" + new string('█', Mathf.Clamp(Mathf.CeilToInt(health / profile.Health * 6f), 0, 6));
        }
    }

    public override void _Process(double delta)
    {
        if (flashTimer > 0f)
        {
            flashTimer -= (float)delta;
            if (flashTimer <= 0f && bodyMaterial != null) bodyMaterial.AlbedoColor = profile.Color;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (bodyMaterial != null) { bodyMaterial.AlbedoColor = new Color("fff0bd"); flashTimer = 0.12f; }
        GameLogger.Info($"敌人受伤：-{damage:0}，剩余生命 {Mathf.Max(0, health):0}");
        int segments = Mathf.Clamp(Mathf.CeilToInt(health / 12f), 0, 6);
        healthBar.Text = $"{profile.Name} · {profile.Realm}\n" + new string('█', segments) + new string('░', 6 - segments);
        if (health <= 0f)
        {
            GameLogger.Info("敌人被击败");
            owner.GainExperience(profile.Reward);
            QueueFree();
        }
    }

    public void ApplySlow(float duration) { slowTimer = Mathf.Max(slowTimer, duration); GameLogger.Info("敌人受到冰霜减速"); }

    public void ApplyKnockback(Vector3 force)
    {
        Velocity = force;
        knockbackTimer = 0.12f;
    }

    public override void _ExitTree() => owner?.UnregisterDamageable(this);
}
