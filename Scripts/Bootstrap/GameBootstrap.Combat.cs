using Godot;

public partial class GameBootstrap : Node3D
{
    private void StartAttack()
    {
        InventoryItem heldItem = inventory.SelectedItem;
        if (heldItem == null) return;
        actionItemKind = heldItem.Kind;
        if (actionItemKind == ItemKind.Potion)
        {
            if (playerHealth >= progression.MaxHealth) { ShowUseFeedback("生命值已满"); return; }
            playerHealth = Mathf.Min(progression.MaxHealth, playerHealth + 35f);
            heldItem.Count--;
            if (heldItem.Count <= 0) inventory.Quickbar[inventory.SelectedIndex] = null;
            attackCooldown = 0.45f;
            ShowUseFeedback("恢复生命 +35");
            OnInventoryChanged();
            GameLogger.Info("使用药水，恢复 35 点生命");
            return;
        }
        attackCooldown = actionItemKind switch
        {
            ItemKind.WoodenSword => 0.34f,
            ItemKind.Fireball => 0.72f,
            ItemKind.IceSpell => 0.62f,
            ItemKind.WoodenAxe or ItemKind.StonePickaxe => 0.52f,
            _ => 0.3f
        };
        attackActionDuration = actionItemKind switch
        {
            ItemKind.WoodenSword => 0.27f,
            ItemKind.Fireball or ItemKind.IceSpell => 0.38f,
            _ => 0.38f
        };
        attackActionTimer = attackActionDuration;
        attackHitDone = false;
        if (!inventory.HasAttackEffect)
        {
            attackActionTimer = 0f;
            attackFeedback = 0.5f;
            attackLabel.Text = "材料无法施展攻击";
            GameLogger.Info($"使用未生效：当前手持材料 {inventory.SelectedItem?.Name ?? "空"}");
            return;
        }
        attackDirection = aiming
            ? GetAimDirection(player.GlobalPosition + new Vector3(0f, 1.1f, 0f))
            : -player.GlobalTransform.Basis.Z;
        if (actionItemKind is not (ItemKind.Fireball or ItemKind.IceSpell))
        {
            attackDirection.Y = 0f;
            attackDirection = attackDirection.Normalized();
        }
        attackFeedback = attackActionDuration;
        if (actionItemKind is ItemKind.WoodenSword or ItemKind.WoodenAxe or ItemKind.StonePickaxe)
        {
            slashTimer = actionItemKind == ItemKind.WoodenSword ? 0.18f : 0.14f;
            slashVisual.Rotation = new Vector3(0, Mathf.Pi * 0.18f, 0);
        }
        attackLabel.Text = $"{inventory.SelectedItem.Name} · 蓄势";
        GameLogger.Info($"开始施展：{inventory.SelectedItem.Name}");
    }

    private void ResolveMeleeAttack(ItemKind item, Vector3 direction, float damage)
    {
        float range = item switch { ItemKind.WoodenSword => 2.8f, ItemKind.WoodenAxe or ItemKind.StonePickaxe => 2.5f, _ => 1.4f };
        float minimumDot = item == ItemKind.WoodenSword ? 0.35f : 0.5f;
        int hits = 0;
        foreach (IDamageable damageable in new System.Collections.Generic.List<IDamageable>(damageables))
        {
            Node3D target = damageable.DamageableNode;
            if (!IsInstanceValid(target)) continue;
            Vector3 offset = target.GlobalPosition - player.GlobalPosition;
            offset.Y = 0;
            float distance = offset.Length();
            if (distance > range || distance < 0.01f || direction.Dot(offset / distance) < minimumDot) continue;
            if (target is WorldEntityNode entity)
            {
                if (!entity.CanBeUsedBy(item)) continue;
                entity.HitWithItem(item, damage);
            }
            else if (target is EnemyNode enemy)
            {
                float weaponMultiplier = item == ItemKind.WoodenSword ? 1f : 0.55f;
                enemy.TakeDamage(damage * weaponMultiplier);
                enemy.ApplyKnockback(direction * (item == ItemKind.WoodenSword ? 4.2f : 2.2f));
            }
            hits++;
        }
        ShowUseFeedback(hits == 0 ? "挥空" : $"命中 {hits} 个目标");
        GameLogger.Info(hits == 0 ? "近战挥空" : $"近战命中 {hits} 个目标");
    }

    private void ShowUseFeedback(string message)
    {
        attackFeedback = 0.8f;
        attackLabel.Text = message;
    }

    internal void SpawnProjectile(Vector3 start, Node3D target, float damage, Color color, bool homing)
    {
        var projectile = new ProjectileNode(this, target, damage, color, homing) { GlobalPosition = start };
        AddChild(projectile);
    }
    internal void SpawnProjectile(Vector3 start, Node3D target, float damage, Color color, bool homing, bool ice)
    {
        var projectile = new ProjectileNode(this, target, damage, color, homing, ice) { GlobalPosition = start };
        AddChild(projectile);
    }
    internal void SpawnDirectionalProjectile(Vector3 start, Vector3 direction, float damage, Color color, bool ice, float range)
    {
        var projectile = new ProjectileNode(this, direction, damage, color, ice, range) { GlobalPosition = start };
        AddChild(projectile);
    }

    internal IDamageable FindProjectileTarget(Vector3 position, float radius)
    {
        IDamageable nearest = null;
        float nearestDistance = radius;
        foreach (IDamageable candidate in damageables)
        {
            Node3D node = candidate.DamageableNode;
            if (!IsInstanceValid(node)) continue;
            float distance = position.DistanceTo(node.GlobalPosition + Vector3.Up * 0.7f);
            if (distance < nearestDistance) { nearestDistance = distance; nearest = candidate; }
        }
        return nearest;
    }

    internal void RegisterDamageable(IDamageable damageable) { if (!damageables.Contains(damageable)) damageables.Add(damageable); }
    internal void UnregisterDamageable(IDamageable damageable) { damageables.Remove(damageable); }
    internal int AddResources(ResourceKind kind, int amount)
    {
        int added = inventory.AddResources(kind, amount);
        GameLogger.Info($"拾取资源：{kind} x{added}");
        if (added < amount) GameLogger.Warning($"背包空间不足，未能收取 {kind} x{amount - added}");
        return added;
    }
    internal void ApplyPlayerDamage(float damage)
    {
        if (playerDead) return;
        float reducedDamage = damage * (1f - progression.DamageReduction);
        playerHealth = Mathf.Max(0f, playerHealth - reducedDamage);
        GameLogger.Info($"玩家受到伤害：-{reducedDamage:0}，生命 {playerHealth:0}/{progression.MaxHealth:0}");
        if (playerHealth <= 0f)
        {
            playerDead = true;
            attackActionTimer = 0f;
            aiming = false;
            aimReticle.Visible = false;
            deathOverlay.Visible = true;
            GetTree().Paused = true;
            SetMouseCaptured(false);
            GameLogger.Info("玩家死亡");
        }
    }

    internal float GetPlayerAttackDamage(bool fireball)
        => (fireball ? CombatSystem.FireballDamage : CombatSystem.SwordDamage) * progression.AttackMultiplier;

    internal void GainExperience(float amount)
    {
        progression.Experience += amount;
        while (progression.Experience >= progression.ExperienceToNextLevel)
        {
            progression.Experience -= progression.ExperienceToNextLevel;
            progression.Level++;
            progression.AttributePoints++;
            playerHealth = progression.MaxHealth;
            GameLogger.Info($"升级到 Lv.{progression.Level}，获得 1 点属性点");
        }
        RefreshStats();
    }
}

