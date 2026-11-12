using Godot;

public partial class ProjectileNode : Node3D
{
    private readonly GameBootstrap owner;
    private readonly Node3D target;
    private readonly float damage;
    private readonly bool homing;
    private readonly bool ice;
    private readonly bool directional;
    private float remainingRange;
    private Vector3 travelDirection;
    private float lifetime = 3f;
    private readonly float speed = CombatSystem.ProjectileSpeed;

    public ProjectileNode(GameBootstrap owner, Node3D target, float damage, Color color, bool homing, bool ice = false)
    {
        this.owner = owner;
        this.target = target;
        this.damage = damage;
        this.homing = homing;
        this.ice = ice;
        remainingRange = speed * lifetime;
        if (!homing && IsInstanceValid(target))
            travelDirection = (target.GlobalPosition + Vector3.Up * 0.8f - GlobalPosition).Normalized();
        BuildVisual(color);
    }

    public ProjectileNode(GameBootstrap owner, Vector3 direction, float damage, Color color, bool ice, float range)
    {
        this.owner = owner;
        target = null;
        this.damage = damage;
        this.ice = ice;
        directional = true;
        travelDirection = direction.Normalized();
        remainingRange = range;
        lifetime = range / speed;
        BuildVisual(color);
    }

    private void BuildVisual(Color color)
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = ice ? 0.24f : 0.2f, Height = ice ? 0.48f : 0.4f, RadialSegments = 6, Rings = 3 },
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
        if (lifetime <= 0f || (!directional && !IsInstanceValid(target)))
        {
            QueueFree();
            return;
        }

        if (directional)
        {
            Vector3 step = travelDirection * speed * (float)delta;
            GlobalPosition += step;
            remainingRange -= step.Length();
            IDamageable hit = owner.FindProjectileTarget(GlobalPosition, ice ? 0.85f : 0.7f);
            if (hit != null)
            {
                if (hit.DamageableNode is WorldEntityNode entity) entity.HitWithItem(ice ? ItemKind.IceSpell : ItemKind.Fireball, damage);
                else hit.TakeDamage(damage);
                if (ice && hit is EnemyNode enemy) enemy.ApplySlow(2.4f);
                QueueFree();
            }
            else if (remainingRange <= 0f) QueueFree();
            return;
        }

        Vector3 offset = target.GlobalPosition + new Vector3(0, 0.8f, 0) - GlobalPosition;
        float distance = offset.Length();
        if (distance < 0.75f)
        {
            if (target == owner.Player) owner.ApplyPlayerDamage(damage);
            else if (target is IDamageable damageable) damageable.TakeDamage(damage);
            QueueFree();
            return;
        }
        GlobalPosition += (homing ? offset.Normalized() : travelDirection) * speed * (float)delta;
    }
}
