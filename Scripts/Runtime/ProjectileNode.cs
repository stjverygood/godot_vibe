using Godot;

public partial class ProjectileNode : Node3D
{
    private readonly GameBootstrap owner;
    private readonly Node3D target;
    private readonly float damage;
    private readonly bool homing;
    private Vector3 travelDirection;
    private float lifetime = 3f;
    private readonly float speed = CombatSystem.ProjectileSpeed;

    public ProjectileNode(GameBootstrap owner, Node3D target, float damage, Color color, bool homing)
    {
        this.owner = owner;
        this.target = target;
        this.damage = damage;
        this.homing = homing;
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.18f, Height = 0.36f },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = color,
                EmissionEnabled = true,
                Emission = color,
                EmissionEnergyMultiplier = 2f
            }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        lifetime -= (float)delta;
        if (lifetime <= 0f || !IsInstanceValid(target))
        {
            QueueFree();
            return;
        }

        Vector3 offset = target.GlobalPosition + new Vector3(0, 0.8f, 0) - GlobalPosition;
        if (!homing && travelDirection == Vector3.Zero) travelDirection = offset.Normalized();
        float distance = offset.Length();
        if (distance < 0.75f)
        {
            if (target == owner.Player) owner.ApplyPlayerDamage(damage);
            else if (target is EnemyNode enemy) enemy.TakeDamage(damage);
            QueueFree();
            return;
        }

        GlobalPosition += (homing ? offset.Normalized() : travelDirection) * speed * (float)delta;
    }
}
