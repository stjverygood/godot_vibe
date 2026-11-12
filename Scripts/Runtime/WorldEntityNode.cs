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
    private bool isBuilding;
    private float nightLightUpdateTimer;
    private OmniLight3D porchLight;
    private StandardMaterial3D windowMaterial;

    public Node3D DamageableNode => this;
    public ResourceKind ResourceKind => resourceKind;
    public bool CanBeUsedBy(ItemKind item) => item switch
    {
        ItemKind.WoodenAxe => resourceKind == ResourceKind.Wood,
        ItemKind.StonePickaxe => resourceKind == ResourceKind.Stone,
        ItemKind.WoodenSword or ItemKind.Fireball or ItemKind.IceSpell => !isBuilding,
        _ => false
    };

    public void HitWithItem(ItemKind item, float damage)
    {
        float multiplier = item == ItemKind.WoodenAxe && resourceKind == ResourceKind.Wood ? 2.5f
            : item == ItemKind.StonePickaxe && resourceKind == ResourceKind.Stone ? 2.5f : 1f;
        TakeDamage(damage * multiplier);
    }

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
                isBuilding = true;
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
        if (isBuilding) return;
        if (destroyed) return;
        health -= damage;
        GameLogger.Info($"场景实体受击：-{damage:0}，剩余生命 {Mathf.Max(0, health):0}");
        if (health > 0f) return;

        destroyed = true;
        owner.GainExperience(resourceKind == ResourceKind.Brick ? 20f : resourceKind == ResourceKind.Stone ? 10f : 5f);
        GameLogger.Info($"场景实体被摧毁，掉落 {resourceKind} x{dropAmount}");
        var drop = new WorldDropNode(owner, resourceKind, dropAmount)
        {
            GlobalPosition = GlobalPosition + new Vector3(0, 0.5f, 0)
        };
        owner.AddChild(drop);
        QueueFree();
    }

    public override void _ExitTree() => owner?.UnregisterDamageable(this);

    public override void _Process(double delta)
    {
        if (!isBuilding) return;
        nightLightUpdateTimer += (float)delta;
        if (nightLightUpdateTimer < 0.1f) return;
        nightLightUpdateTimer = 0f;

        float night = DayNightCycle.NightFactor;
        porchLight.LightEnergy = 1.1f * night;
        windowMaterial.EmissionEnergyMultiplier = 1.4f * night;
    }

    private void BuildTree()
    {
        var wood = Material("79502d");
        var leaves = Material("43824a");
        AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = 0.38f, Height = 3.2f }, Position = new Vector3(0, 1.6f, 0) });
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.32f, BottomRadius = 0.48f, Height = 3.2f, RadialSegments = 5 }, MaterialOverride = wood, Position = new Vector3(0, 1.6f, 0) });
        AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 1.35f }, Position = new Vector3(0, 3.4f, 0) });
        AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 1.45f, Height = 2.8f, RadialSegments = 5, Rings = 3 }, MaterialOverride = leaves, Position = new Vector3(0, 3.4f, 0) });
    }

    private void BuildRock()
    {
        AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 1.15f }, Position = new Vector3(0, 0.95f, 0) });
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 1f, Height = 1.8f, RadialSegments = 5, Rings = 2 },
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
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.15f, BottomRadius = 2.8f, Height = 0.4f, RadialSegments = 4 }, MaterialOverride = Material("a64e3f"), Position = new Vector3(0, 3.1f, 0) });
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.1f, BottomRadius = 1.9f, Height = 0.4f, RadialSegments = 4 }, MaterialOverride = Material("c07848"), Position = new Vector3(0, 3.65f, 0) });
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.08f, BottomRadius = 0.9f, Height = 0.4f, RadialSegments = 4 }, MaterialOverride = Material("d5aa61"), Position = new Vector3(0, 4.2f, 0) });
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

        windowMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("ffbd68"),
            EmissionEnabled = true,
            Emission = new Color("ff9b42"),
            EmissionEnergyMultiplier = 0f,
            Roughness = 0.75f
        };
        foreach (float x in new[] { -1.15f, 1.15f })
        {
            AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.65f, 0.72f, 0.1f) },
                MaterialOverride = windowMaterial,
                Position = new Vector3(x, 1.85f, -2.06f)
            });
        }

        porchLight = new OmniLight3D
        {
            Name = "PorchLight",
            Position = new Vector3(0f, 2.3f, -2.45f),
            LightColor = new Color("ffbd78"),
            LightEnergy = 0f,
            OmniRange = 8f,
            OmniAttenuation = 1.2f,
            ShadowEnabled = false
        };
        AddChild(porchLight);
    }

    private static StandardMaterial3D Material(string color) => new() { AlbedoColor = new Color(color), Roughness = 0.9f };
}
