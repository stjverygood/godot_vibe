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
            GameLogger.Info($"攻击未生效：当前快捷栏物品为 {inventory.SelectedItem?.Name ?? "空"}");
            return;
        }
        float closest = CombatSystem.PlayerAttackRange;
        foreach (IDamageable damageable in damageables)
        {
            Node3D target = damageable.DamageableNode;
            if (!IsInstanceValid(target)) continue;
            float distance = new Vector2(target.GlobalPosition.X - player.GlobalPosition.X, target.GlobalPosition.Z - player.GlobalPosition.Z).Length();
            if (distance < closest) { closest = distance; currentAttackTarget = target; }
        }
        if (IsInstanceValid(currentAttackTarget)) FaceTarget(currentAttackTarget);
        attackFeedback = 0.5f;
        attackLabel.Text = currentAttackTarget == null ? "攻击落空" : "蓄力攻击";
        GameLogger.Info(currentAttackTarget == null
            ? $"攻击落空，物品：{inventory.SelectedItem.Name}"
            : $"开始攻击，物品：{inventory.SelectedItem.Name}，目标：{currentAttackTarget.GetType().Name}");
    }

    private void FaceTarget(Node3D target)
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

    internal void RegisterDamageable(IDamageable damageable) { if (!damageables.Contains(damageable)) damageables.Add(damageable); }
    internal void UnregisterDamageable(IDamageable damageable) { damageables.Remove(damageable); }
    internal int AddResources(ResourceKind kind, int amount)
    {
        int added = inventory.AddResources(kind, amount);
        resourceLabel.Text = inventory.ResourceSummary;
        GameLogger.Info($"拾取资源：{kind} x{added}");
        if (added < amount) GameLogger.Warning($"背包空间不足，未能收取 {kind} x{amount - added}");
        return added;
    }
    internal void ApplyPlayerDamage(float damage) => playerHealth = Mathf.Max(0, playerHealth - damage);
}

