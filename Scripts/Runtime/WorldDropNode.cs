using Godot;

public partial class WorldDropNode : Node3D
{
    private readonly GameBootstrap owner;
    private readonly ResourceKind kind;
    private int amount;
    private float bobTime;
    private readonly MeshInstance3D visual;
    private readonly Label3D label;

    public WorldDropNode(GameBootstrap owner, ResourceKind kind, int amount)
    {
        this.owner = owner;
        this.kind = kind;
        this.amount = amount;

        Color color = kind switch
        {
            ResourceKind.Wood => new Color("b77b3e"),
            ResourceKind.Stone => new Color("aeb8c0"),
            _ => new Color("c76a4e")
        };
        Mesh dropMesh = kind switch
        {
            ResourceKind.Wood => new PrismMesh { Size = new Vector3(0.55f, 0.25f, 0.22f) },
            ResourceKind.Stone => new PrismMesh { Size = new Vector3(0.45f, 0.65f, 0.38f) },
            _ => new BoxMesh { Size = new Vector3(0.48f, 0.42f, 0.48f) }
        };
        visual = new MeshInstance3D
        {
            Mesh = dropMesh,
            Rotation = new Vector3(0.25f, 0.6f, 0.15f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                EmissionEnabled = true,
                Emission = color,
                EmissionEnergyMultiplier = 0.35f
            }
        };
        AddChild(visual);

        label = new Label3D
        {
            Text = $"{ResourceName(kind)} x{amount}",
            FontSize = 24,
            Modulate = new Color("fff0c4"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Position = new Vector3(0, 0.55f, 0)
        };
        AddChild(label);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!owner.CanInteract) return;

        bobTime += (float)delta;
        visual.Position = new Vector3(0, 0.12f + Mathf.Sin(bobTime * 3f) * 0.08f, 0);
        if (owner.Player.GlobalPosition.DistanceTo(GlobalPosition) <= 1.8f)
        {
            int collected = owner.AddResources(kind, amount);
            amount -= collected;
            if (amount <= 0)
            {
                QueueFree();
                return;
            }
            label.Text = $"{ResourceName(kind)} x{amount}";
        }
    }

    private static string ResourceName(ResourceKind resource) => resource switch
    {
        ResourceKind.Wood => "灵木",
        ResourceKind.Stone => "灵矿",
        _ => "阵材"
    };
}
