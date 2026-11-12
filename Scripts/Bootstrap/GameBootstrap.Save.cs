using Godot;

public partial class GameBootstrap : Node3D
{
    internal void BeginSave(SaveSlotData save)
    {
        ClearLoadedChunks();
        activeSave = save;
        WorldManager.SetSeed(save.Seed);
        TerrainGenerator.SetSeed(save.Seed);

        if (save.HasProgress)
        {
            progression.Level = Mathf.Max(1, save.Level);
            progression.Experience = Mathf.Max(0f, save.Experience);
            progression.AttributePoints = Mathf.Max(0, save.AttributePoints);
            progression.Blood = Mathf.Max(0, save.AttributeSchema > 0 ? save.Blood : save.Vitality);
            progression.Qi = Mathf.Max(0, save.AttributeSchema > 0 ? save.QiAttribute : save.Agility);
            progression.Attack = Mathf.Max(0, save.AttributeSchema > 0 ? save.Attack : save.Strength);
            progression.Defense = Mathf.Max(0, save.Defense);
            inventory.Load(save);
            playerHealth = Mathf.Clamp(save.Health, 1f, progression.MaxHealth);
            playerQi = Mathf.Clamp(save.CurrentQi, 0f, progression.MaxQi);
            player.Position = new Vector3(save.PlayerX, save.PlayerY, save.PlayerZ);
        }
        else
        {
            inventory.Reset();
            progression.Level = 1;
            progression.Experience = 0f;
            progression.AttributePoints = 0;
            progression.Blood = progression.Qi = progression.Attack = progression.Defense = 0;
            playerHealth = progression.MaxHealth;
            playerQi = progression.MaxQi;
            player.Position = Vector3.Zero;
        }

        player.Position = new Vector3(player.Position.X, TerrainGenerator.GetHeight(player.Position.X, player.Position.Z), player.Position.Z);
        playerDead = false;
        gameStarted = true;
        GetTree().Paused = false;
        SetMouseCaptured(true);
        deathOverlay.Visible = false;
        statsOverlay.Visible = false;
        lastChunk = new Vector2I(int.MinValue, int.MinValue);
        UpdateChunks(true);
        UpdateThirdPersonCamera(1f);
        UpdateHeldItemVisual();
        RefreshHotbar();
        RefreshStats();
        GameLogger.Info($"进入存档 {save.Name}，种子 {save.Seed}");
    }

    private void SaveAndReturnToMenu()
    {
        SaveCurrentGame();
        GetTree().Paused = false;
        gameStarted = false;
        playerDead = false;
        activeSave = null;
        statsOverlay.Visible = false;
        SetMouseCaptured(false);
        if (GetParent() is MainMenu main) main.ReturnToMenu(this);
    }

    private void ClearLoadedChunks()
    {
        foreach (ChunkNode chunk in chunks.Values) chunk.QueueFree();
        chunks.Clear();
        damageables.Clear();
        lastChunk = new Vector2I(int.MinValue, int.MinValue);
    }

    private void SaveCurrentGame()
    {
        if (activeSave == null || !gameStarted) return;
        activeSave.PlayerX = player.Position.X;
        activeSave.PlayerY = player.Position.Y;
        activeSave.PlayerZ = player.Position.Z;
        activeSave.Health = Mathf.Max(1f, playerHealth);
        activeSave.CurrentQi = playerQi;
        activeSave.Level = progression.Level;
        activeSave.Experience = progression.Experience;
        activeSave.AttributePoints = progression.AttributePoints;
        activeSave.AttributeSchema = 1;
        activeSave.Blood = progression.Blood;
        activeSave.QiAttribute = progression.Qi;
        activeSave.Attack = progression.Attack;
        activeSave.Defense = progression.Defense;
        activeSave.Strength = progression.Attack;
        activeSave.Vitality = progression.Blood;
        activeSave.Agility = progression.Qi;
        activeSave.HasProgress = true;
        inventory.SaveTo(activeSave);
        SaveSystem.Write(activeSave);
        GameLogger.Info($"存档已保存：{activeSave.Name}");
    }

    private void AllocateAttack()
    {
        if (progression.AttributePoints <= 0) return;
        progression.AttributePoints--;
        progression.Attack++;
        RefreshStats();
    }

    private void AllocateBlood()
    {
        if (progression.AttributePoints <= 0) return;
        float oldMax = progression.MaxHealth;
        progression.AttributePoints--;
        progression.Blood++;
        playerHealth += progression.MaxHealth - oldMax;
        RefreshStats();
    }

    private void AllocateQi()
    {
        if (progression.AttributePoints <= 0) return;
        progression.AttributePoints--;
        progression.Qi++;
        RefreshStats();
    }

    private void AllocateDefense()
    {
        if (progression.AttributePoints <= 0) return;
        progression.AttributePoints--;
        progression.Defense++;
        RefreshStats();
    }

    private void RespawnPlayer()
    {
        playerDead = false;
        GetTree().Paused = false;
        SetMouseCaptured(true);
        playerHealth = Mathf.Max(1f, progression.MaxHealth * 0.5f);
        player.Position = new Vector3(activeSave.PlayerX, TerrainGenerator.GetHeight(activeSave.PlayerX, activeSave.PlayerZ) + 1f, activeSave.PlayerZ);
        deathOverlay.Visible = false;
        UpdateChunks(true);
        GameLogger.Info("玩家复活");
    }
}
