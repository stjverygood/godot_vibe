using Godot;

public interface IDamageable
{
    Node3D DamageableNode { get; }
    void TakeDamage(float damage);
}
