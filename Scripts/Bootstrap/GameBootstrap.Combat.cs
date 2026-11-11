using Godot;

public partial class GameBootstrap : Node3D
{
    private void StartAttack()
    {
        attackCooldown = 0.5f;
        attackActionTimer = 0.5f;
        attackHitDone = false;
        currentAttackTarget = null;
        if (!inventory.HasAttackEffect)
        {
            attackActionTimer = 0.15f;
            attackFeedback = 0.5f;
            attackLabel.Text = "当前物品没有攻击效果";
            return;
        }
        float closest = CombatSystem.PlayerAttackRange;
        foreach (EnemyNode enemy in enemies)
        {
            if (!IsInstanceValid(enemy)) continue;
            float distance = new Vector2(enemy.GlobalPosition.X - player.GlobalPosition.X, enemy.GlobalPosition.Z - player.GlobalPosition.Z).Length();
            if (distance < closest) { closest = distance; currentAttackTarget = enemy; }
        }
        if (IsInstanceValid(currentAttackTarget)) FaceTarget(currentAttackTarget);
        attackFeedback = 0.5f;
        attackLabel.Text = currentAttackTarget == null ? "攻击落空" : "蓄力攻击";
    }

    private void FaceTarget(EnemyNode target)
    {
        Vector3 toTarget = target.GlobalPosition - player.GlobalPosition;
        toTarget.Y = 0;
        if (toTarget.LengthSquared() > 0.01f) player.Rotation = new Vector3(0, Mathf.Atan2(-toTarget.X, -toTarget.Z), 0);
    }

    internal void SpawnProjectile(Vector3 start, Node3D target, float damage, Color color, bool homing)
    {
        var projectile = new ProjectileNode(this, target, damage, color, homing) { GlobalPosition = start };
        AddChild(projectile);
    }

    internal void RegisterEnemy(EnemyNode enemy) { if (!enemies.Contains(enemy)) enemies.Add(enemy); }
    internal void UnregisterEnemy(EnemyNode enemy) { enemies.Remove(enemy); }
    internal void ApplyPlayerDamage(float damage) => playerHealth = Mathf.Max(0, playerHealth - damage);
}

