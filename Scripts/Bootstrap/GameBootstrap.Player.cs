using Godot;

public partial class GameBootstrap : Node3D
{
    private void BuildPlayer()
    {
        player = new CharacterBody3D { Position = new Vector3(0, 1.2f, 0) }; AddChild(player);
        player.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.45f, Height = 1.8f }, Position = new Vector3(0, 0.9f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new CapsuleMesh { Radius = 0.45f, Height = 1.8f }, MaterialOverride = Mat(new Color("e98b62")), Position = new Vector3(0, 0.9f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.34f, Height = 0.68f }, MaterialOverride = Mat(new Color("f6c48f")), Position = new Vector3(0, 2f, 0) });
        player.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.28f, 0.3f, 0.7f) }, MaterialOverride = Mat(new Color("ffe08a")), Position = new Vector3(0, 1.15f, -0.55f) });
        handItem = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.22f, 0.65f) }, MaterialOverride = Mat(inventory.SelectedColor), Position = new Vector3(0.65f, 1.15f, -0.45f) };
        handItem.Visible = inventory.SelectedItem != null;
        player.AddChild(handItem);
        attackRangeVisual = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 6.7f, OuterRadius = 7.0f }, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.82f, 0.25f, 0.75f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded }, Visible = false, Position = new Vector3(0, 0.08f, 0) };
        player.AddChild(attackRangeVisual);
        camera = new Camera3D { Position = new Vector3(0, 22, 14), TopLevel = true, Current = true, Fov = 58 };
        player.AddChild(camera);
        camera.GlobalPosition = player.GlobalPosition + new Vector3(0, 22, 14);
        camera.LookAt(player.GlobalPosition, Vector3.Up);
    }
}

