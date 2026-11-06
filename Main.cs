using Godot;
using System;

public partial class Main : Node3D
{
    private CharacterBody3D player;
    private Node3D cameraPivot;
    private Camera3D camera;
    private float yaw;
    private float pitch = -0.22f;
    private bool mouseCaptured = true;

    public override void _Ready()
    {
        BuildEnvironment();
        BuildPlayer();
        BuildUi();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && mouseCaptured)
        {
            yaw -= motion.Relative.X * 0.003f;
            pitch = Mathf.Clamp(pitch - motion.Relative.Y * 0.002f, -0.8f, 0.25f);
        }
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            mouseCaptured = !mouseCaptured;
            Input.MouseMode = mouseCaptured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null) return;
        float dt = (float)delta;
        Vector2 input = Input.GetVector("left", "right", "forward", "back");
        Vector3 direction = new Basis(Vector3.Up, yaw) * new Vector3(input.X, 0, -input.Y);
        direction = direction.Normalized();
        float currentSpeed = Input.IsActionPressed("slow_walk") ? 2.5f : 4.5f;
        Vector3 velocity = player.Velocity;
        velocity.X = Mathf.MoveToward(velocity.X, direction.X * currentSpeed, 18f * dt);
        velocity.Z = Mathf.MoveToward(velocity.Z, direction.Z * currentSpeed, 18f * dt);
        velocity.Y = player.IsOnFloor() ? 0f : velocity.Y - 18f * dt;
        player.Velocity = velocity;
        player.MoveAndSlide();
        player.Rotation = new Vector3(0, Mathf.LerpAngle(player.Rotation.Y, yaw, 10f * dt), 0);
        cameraPivot.Rotation = new Vector3(pitch, yaw, 0);
    }

    private StandardMaterial3D Mat(Color color, float roughness = 0.9f, float metallic = 0f)
    {
        return new StandardMaterial3D { AlbedoColor = color, Roughness = roughness, Metallic = metallic };
    }

    private MeshInstance3D MeshNode(Mesh mesh, Material material, Vector3 position, Node parent = null)
    {
        var node = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position };
        (parent ?? this).AddChild(node);
        return node;
    }

    private void BuildEnvironment()
    {
        var world = new WorldEnvironment();
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Sky, AmbientLightSource = Godot.Environment.AmbientSource.Sky, AmbientLightEnergy = 0.8f, TonemapMode = Godot.Environment.ToneMapper.Filmic };
        var sky = new Sky();
        sky.SkyMaterial = new ProceduralSkyMaterial { SkyTopColor = new Color("183e63"), SkyHorizonColor = new Color("f2c58a"), GroundBottomColor = new Color("122b2a"), GroundHorizonColor = new Color("9bc4b0") };
        environment.Sky = sky;
        world.Environment = environment;
        AddChild(world);
        var sun = new DirectionalLight3D { RotationDegrees = new Vector3(-48, -28, 0), LightColor = new Color("ffe0a8"), LightEnergy = 1.4f, ShadowEnabled = true };
        AddChild(sun);
        var ground = new BoxMesh { Size = new Vector3(70, 0.5f, 70) };
        MeshNode(ground, Mat(new Color("476b4c")), new Vector3(0, -0.25f, 0));
        var groundBody = new StaticBody3D { Position = new Vector3(0, -0.25f, 0) };
        groundBody.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(70, 0.5f, 70) } });
        AddChild(groundBody);
        var water = new BoxMesh { Size = new Vector3(24, 0.16f, 18) };
        MeshNode(water, Mat(new Color(0.12f, 0.34f, 0.42f, 0.8f), 0.12f, 0.2f), new Vector3(8, 0.05f, -7));
        foreach (Vector3 p in new Vector3[] { new Vector3(-16, 0, -14), new Vector3(14, 0, -18), new Vector3(24, 0, 5), new Vector3(-24, 0, 14), new Vector3(0, 0, 23) }) BuildHill(p);
        foreach (Vector3 p in new Vector3[] { new Vector3(-10, 0, -3), new Vector3(-15, 0, 6), new Vector3(-5, 0, 13), new Vector3(17, 0, -2), new Vector3(21, 0, 13), new Vector3(7, 0, 17), new Vector3(-21, 0, -5) }) BuildTree(p);
        foreach (Vector3 p in new Vector3[] { new Vector3(-3, 0, -10), new Vector3(2, 0, 4), new Vector3(12, 0, 8), new Vector3(-17, 0, 18), new Vector3(25, 0, -11) }) BuildRock(p);
        MeshNode(new BoxMesh { Size = new Vector3(3.8f, 0.04f, 42) }, Mat(new Color("b28b63")), new Vector3(-1.8f, 0.02f, 4));
    }

    private void BuildHill(Vector3 p)
    {
        var node = MeshNode(new SphereMesh { Radius = 7, Height = 10 }, Mat(new Color("355b46")), p + new Vector3(0, 1.8f, 0));
        node.Scale = new Vector3(1.7f, 0.75f, 1.2f);
    }

    private void BuildTree(Vector3 p)
    {
        MeshNode(new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.35f, Height = 3.5f }, Mat(new Color("5c3e2b")), p + new Vector3(0, 1.75f, 0));
        MeshNode(new CylinderMesh { TopRadius = 0f, BottomRadius = 1.55f, Height = 3.8f }, Mat(new Color("234b3d")), p + new Vector3(0, 4.8f, 0));
        MeshNode(new CylinderMesh { TopRadius = 0f, BottomRadius = 1.15f, Height = 2.8f }, Mat(new Color("2f6a4a")), p + new Vector3(0, 6.1f, 0));
    }

    private void BuildRock(Vector3 p)
    {
        var node = MeshNode(new SphereMesh { Radius = 1.1f, Height = 1.5f }, Mat(new Color("667275")), p + new Vector3(0, 0.65f, 0));
        node.Scale = new Vector3(1.4f, 0.8f, 0.9f);
    }

    private void BuildPlayer()
    {
        player = new CharacterBody3D { Name = "Explorer", Position = new Vector3(0, 1.2f, 10) };
        AddChild(player);
        var capsule = new CapsuleShape3D { Radius = 0.45f, Height = 1.8f };
        player.AddChild(new CollisionShape3D { Shape = capsule, Position = new Vector3(0, 0.9f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new CapsuleMesh { Radius = 0.45f, Height = 1.8f }, MaterialOverride = Mat(new Color("d8845b")), Position = new Vector3(0, 0.9f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.33f, Height = 0.66f }, MaterialOverride = Mat(new Color("f0bd8b")), Position = new Vector3(0, 2.05f, 0) });
        cameraPivot = new Node3D { Position = new Vector3(0, 1.9f, 0) };
        player.AddChild(cameraPivot);
        camera = new Camera3D { Position = new Vector3(0, 1.2f, 6.5f), Current = true, Fov = 68f };
        cameraPivot.AddChild(camera);
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer();
        AddChild(layer);
        var panel = new ColorRect { Position = new Vector2(24, 24), Size = new Vector2(340, 92), Color = new Color(0.02f, 0.05f, 0.07f, 0.72f) };
        layer.AddChild(panel);
        var label = new Label { Position = new Vector2(18, 12), Text = "静谧山谷 · 漫游模式\nWASD 移动   Shift 慢走   鼠标环顾\nEsc 释放/捕获鼠标" };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color("f6e4bd"));
        panel.AddChild(label);
        var title = new Label { Text = "WANDER", Position = new Vector2(26, 650) };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color(1, 0.87f, 0.62f, 0.9f));
        layer.AddChild(title);
    }
}

