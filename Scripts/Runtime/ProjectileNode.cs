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
    private Node3D visualRoot;
    private MeshInstance3D impactFlash;
    private StandardMaterial3D impactMaterial;
    private Color spellColor;
    private bool impacting;
    private const float ImpactDuration = 0.24f;

    public ProjectileNode(GameBootstrap owner, Node3D target, float damage, Color color, bool homing, bool ice = false)
    {
        this.owner = owner;
        this.target = target;
        this.damage = damage;
        this.homing = homing;
        this.ice = ice;
        remainingRange = speed * lifetime;
        if (IsInstanceValid(target))
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

    public override void _Ready()
    {
        if (travelDirection.LengthSquared() > 0.001f)
            LookAt(GlobalPosition + travelDirection, Vector3.Up);
    }

    private void BuildVisual(Color color)
    {
        spellColor = ice ? new Color("80dfff") : new Color("ff8b32");
        visualRoot = new Node3D();
        AddChild(visualRoot);

        if (ice)
        {
            AddSpellMesh(
                new SphereMesh { Radius = 0.38f, Height = 0.76f, RadialSegments = 6, Rings = 3 },
                SpellMaterial(new Color("c9f7ff"), new Color("72dfff"), 3.6f),
                new Vector3(0.68f, 0.68f, 1.5f));
            AddSpellMesh(
                new SphereMesh { Radius = 0.46f, Height = 0.92f, RadialSegments = 6, Rings = 3 },
                SpellMaterial(new Color(0.22f, 0.78f, 1f, 0.28f), new Color("49caff"), 2.5f, true),
                new Vector3(0.9f, 0.9f, 1.7f));
            AddSpellMesh(
                new CylinderMesh { TopRadius = 0f, BottomRadius = 0.2f, Height = 0.72f, RadialSegments = 5 },
                SpellMaterial(new Color("e7fdff"), new Color("adf4ff"), 3.2f),
                Vector3.One,
                new Vector3(0f, 0f, -0.34f),
                new Vector3(-Mathf.Pi / 2f, 0f, 0f));
            AddSpellMesh(
                new CylinderMesh { TopRadius = 0f, BottomRadius = 0.16f, Height = 0.62f, RadialSegments = 5 },
                SpellMaterial(new Color("67d8ff"), new Color("46bfff"), 2.7f),
                Vector3.One,
                new Vector3(0f, 0f, 0.36f),
                new Vector3(Mathf.Pi / 2f, 0f, 0f));
        }
        else
        {
            AddSpellMesh(
                new SphereMesh { Radius = 0.43f, Height = 0.86f, RadialSegments = 8, Rings = 4 },
                SpellMaterial(new Color("fff2a1"), new Color("ff6b16"), 3.8f));
            AddSpellMesh(
                new SphereMesh { Radius = 0.56f, Height = 1.12f, RadialSegments = 8, Rings = 4 },
                SpellMaterial(new Color(1f, 0.31f, 0.04f, 0.3f), new Color("ff3b08"), 2.7f, true),
                Vector3.One * 1.1f);
            AddSpellMesh(
                new CylinderMesh { TopRadius = 0f, BottomRadius = 0.34f, Height = 1.45f, RadialSegments = 7 },
                SpellMaterial(new Color(1f, 0.3f, 0.03f, 0.65f), new Color("ff4b0a"), 2.4f, true),
                Vector3.One,
                new Vector3(0f, 0f, 0.75f),
                new Vector3(Mathf.Pi / 2f, 0f, 0f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.Tau / 3f;
                AddSpellMesh(
                    new SphereMesh { Radius = 0.13f, Height = 0.26f, RadialSegments = 5, Rings = 2 },
                    SpellMaterial(new Color("ffd867"), new Color("ff6a13"), 3f),
                    Vector3.One,
                    new Vector3(Mathf.Cos(angle) * 0.42f, Mathf.Sin(angle) * 0.42f, 0.16f));
            }
        }

        impactMaterial = SpellMaterial(new Color(spellColor.R, spellColor.G, spellColor.B, 0.82f), spellColor, 2.5f, true);
        impactFlash = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.38f, OuterRadius = 0.68f, Rings = 8, RingSegments = 6 },
            MaterialOverride = impactMaterial,
            Visible = false,
            Scale = Vector3.One * 0.25f
        };
        AddChild(impactFlash);
    }

    private void AddSpellMesh(Mesh mesh, StandardMaterial3D material, Vector3? scale = null, Vector3? position = null, Vector3? rotation = null)
    {
        visualRoot.AddChild(new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = material,
            Scale = scale ?? Vector3.One,
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Vector3.Zero
        });
    }

    private static StandardMaterial3D SpellMaterial(Color albedo, Color emission, float energy, bool transparent = false)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = albedo,
            EmissionEnabled = true,
            Emission = emission,
            EmissionEnergyMultiplier = energy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = transparent ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
            DepthDrawMode = transparent ? BaseMaterial3D.DepthDrawModeEnum.Disabled : BaseMaterial3D.DepthDrawModeEnum.OpaqueOnly
        };
    }

    private void BeginImpact()
    {
        impacting = true;
        lifetime = ImpactDuration;
        visualRoot.Visible = false;
        impactFlash.Visible = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (impacting)
        {
            lifetime -= (float)delta;
            if (lifetime <= 0f) { QueueFree(); return; }
            float progress = 1f - lifetime / ImpactDuration;
            impactFlash.Scale = Vector3.One * (0.35f + progress * 1.5f);
            impactMaterial.AlbedoColor = new Color(spellColor.R, spellColor.G, spellColor.B, 1f - progress);
            return;
        }

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
                BeginImpact();
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
            BeginImpact();
            return;
        }
        Vector3 direction = homing ? offset.Normalized() : travelDirection;
        GlobalPosition += direction * speed * (float)delta;
        if (homing) LookAt(GlobalPosition + direction, Vector3.Up);
    }
}
