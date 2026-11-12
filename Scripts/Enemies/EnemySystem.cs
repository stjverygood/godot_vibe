public sealed class EnemySystem
{
    public enum BeastKind { GrayWolf, FlameFox, StoneTurtle }
    public sealed record BeastProfile(string Name, string Realm, float Health, float Damage, float Speed, float Range, float Reward, Godot.Color Color);

    public const float ChaseRange = 18f;
    public const float ProjectileRange = 11f;
    public const float EnemyDamage = 9f;
    public const float EnemyHealth = 65f;

    public static BeastProfile Profile(int variant) => (variant % 3) switch
    {
        0 => new("青风狼", "炼气二层", 55f, 7f, 3.2f, 1.8f, 22f, new Godot.Color("6a9b81")),
        1 => new("赤焰狐", "炼气三层", 48f, 10f, 2.8f, 10f, 30f, new Godot.Color("d77b50")),
        _ => new("岩甲龟", "炼气四层", 115f, 12f, 1.35f, 2f, 38f, new Godot.Color("8d8270"))
    };
}
