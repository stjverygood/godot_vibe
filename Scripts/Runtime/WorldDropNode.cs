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
        visual = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.28f, Height = 0.56f },
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
        ResourceKind.Wood => "木材",
        ResourceKind.Stone => "石料",
        _ => "砖块"
    };
}
