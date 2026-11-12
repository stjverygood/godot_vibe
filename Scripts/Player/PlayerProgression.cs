public sealed class PlayerProgression
{
    public int Level { get; set; } = 1;
    public float Experience { get; set; }
    public int AttributePoints { get; set; }
    public int Strength { get; set; }
    public int Vitality { get; set; }
    public int Agility { get; set; }

    public float MaxHealth => 100f + Vitality * 12f;
    public float AttackMultiplier => 1f + Strength * 0.08f;
    public float SpeedMultiplier => 1f + Agility * 0.035f;
    public float DamageReduction => System.Math.Min(0.5f, Vitality * 0.025f);
    public float ExperienceToNextLevel => 100f + (Level - 1) * 50f;
}
