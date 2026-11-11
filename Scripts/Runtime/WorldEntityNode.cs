using Godot;

public enum WorldEntityKind
{
    Tree,
    Rock,
    Building
}

public partial class WorldEntityNode : StaticBody3D, IDamageable
{
    private readonly GameBootstrap owner;
    private readonly ResourceKind resourceKind;
    private readonly int dropAmount;
    private float health;
    private bool destroyed;

    public Node3D DamageableNode => this;

    public WorldEntityNode(GameBootstrap owner, WorldEntityKind kind, Vector3 position)
    {
        this.owner = owner;
        Position = position;

        switch (kind)
        {
            case WorldEntityKind.Tree:
                health = 60f;
                resourceKind = ResourceKind.Wood;
                dropAmount = 3;
                BuildTree();
                break;
            case WorldEntityKind.Rock:
                health = 90f;
                resourceKind = ResourceKind.Stone;
                dropAmount = 2;
                BuildRock();
                break;
            default:
                health = 120f;
                resourceKind = ResourceKind.Brick;
                dropAmount = 4;
                BuildBuilding();
                break;
        }

        owner.RegisterDamageable(this);
    }

    public void TakeDamage(float damage)
    {
        if (destroyed) return;
        health -= damage;
        GameLogger.Info($"场景实体受击：-{damage:0}，剩余生命 {Mathf.Max(0, health):0}");
        if (health > 0f) return;

        destroyed = true;
        GameLogger.Info($"场景实体被摧毁，掉落 {resourceKind} x{dropAmount}");
        var drop = new WorldDropNode(owner, resourceKind, dropAmount)
        {
            GlobalPosition = GlobalPosition + new Vector3(0, 0.5f, 0)
        };
        owner.AddChild(drop);
        QueueFree();
    }

    public override void _ExitTree() => owner?.UnregisterDamageable(this);

    private void BuildTree()
    {
        var wood = Material("79502d");
        var leaves = Material("43824a");
        AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = 0.38f, Height = 3.2f }, Position = new Vector3(0, 1.6f, 0) });
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.32f, BottomRadius = 0.48f, Height = 3.2f }, MaterialOverride = wood, Position = new Vector3(0, 1.6f, 0) });
        AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 1.35f }, Position = new Vector3(0, 3.4f, 0) });
        AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 1.45f, Height = 2.8f }, MaterialOverride = leaves, Position = new Vector3(0, 3.4f, 0) });
    }

    private void BuildRock()
    {
        AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 1.15f }, Position = new Vector3(0, 0.95f, 0) });
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 1f, Height = 1.8f },
            MaterialOverride = Material("777d82"),
            Scale = new Vector3(1.25f, 0.95f, 1.1f),
            Position = new Vector3(0, 0.95f, 0)
        });
    }

    private void BuildBuilding()
    {
        var wall = Material("b59b72");
        var roof = Material("784b42");
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(4f, 3f, 4f) }, Position = new Vector3(0, 1.5f, 0) });
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(4f, 3f, 4f) }, MaterialOverride = wall, Position = new Vector3(0, 1.5f, 0) });
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(2.7f, 0.25f, 4.5f) },
            MaterialOverride = roof,
            Position = new Vector3(-0.75f, 3.15f, 0),
            Rotation = new Vector3(0, 0, 0.55f)
        });
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(2.7f, 0.25f, 4.5f) },
            MaterialOverride = roof,
            Position = new Vector3(0.75f, 3.15f, 0),
            Rotation = new Vector3(0, 0, -0.55f)
        });
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1f, 2.1f, 0.12f) }, MaterialOverride = Material("5b382b"), Position = new Vector3(0, 1.05f, -2.06f) });
    }

    private static StandardMaterial3D Material(string color) => new() { AlbedoColor = new Color(color), Roughness = 0.9f };
}
