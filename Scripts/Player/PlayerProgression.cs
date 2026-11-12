public sealed class PlayerProgression
{
    public int Level { get; set; } = 1;
    public float Experience { get; set; }
    public int AttributePoints { get; set; }
    public int Blood { get; set; }
    public int Qi { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }

    public float MaxHealth => 100f + Blood * 15f;
    public float MaxQi => 50f + Qi * 12f;
    public float AttackMultiplier => 1f + Attack * 0.08f;
    public float SpeedMultiplier => 1f;
    public float DamageReduction => System.Math.Min(0.7f, Defense * 0.04f);
    public float ExperienceToNextLevel => 100f + (Level - 1) * 50f;
    public string Realm => Level switch
    {
        < 5 => $"炼气 {Level} 层", < 10 => $"筑基 {Level - 4} 层",
        < 15 => $"金丹 {Level - 9} 层", _ => $"元婴 {Level - 14} 层"
    };
}
