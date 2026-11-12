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
            progression.Strength = Mathf.Max(0, save.Strength);
            progression.Vitality = Mathf.Max(0, save.Vitality);
            progression.Agility = Mathf.Max(0, save.Agility);
            inventory.Load(save);
            playerHealth = Mathf.Clamp(save.Health, 1f, progression.MaxHealth);
            player.Position = new Vector3(save.PlayerX, save.PlayerY, save.PlayerZ);
        }
        else
        {
            inventory.Reset();
            progression.Level = 1;
            progression.Experience = 0f;
            progression.AttributePoints = 0;
            progression.Strength = 0;
            progression.Vitality = 0;
            progression.Agility = 0;
            playerHealth = progression.MaxHealth;
            player.Position = Vector3.Zero;
        }

        player.Position = new Vector3(player.Position.X, TerrainGenerator.GetHeight(player.Position.X, player.Position.Z) + 1f, player.Position.Z);
        playerDead = false;
        gameStarted = true;
        GetTree().Paused = false;
        deathOverlay.Visible = false;
        statsOverlay.Visible = false;
        lastChunk = new Vector2I(int.MinValue, int.MinValue);
        UpdateChunks(true);
        camera.GlobalPosition = player.GlobalPosition + new Vector3(0, 22, 14);
        camera.LookAt(player.GlobalPosition, Vector3.Up);
        handItem.MaterialOverride = Mat(inventory.SelectedColor);
        handItem.Visible = inventory.SelectedItem != null;
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
        activeSave.Level = progression.Level;
        activeSave.Experience = progression.Experience;
        activeSave.AttributePoints = progression.AttributePoints;
        activeSave.Strength = progression.Strength;
        activeSave.Vitality = progression.Vitality;
        activeSave.Agility = progression.Agility;
        activeSave.HasProgress = true;
        inventory.SaveTo(activeSave);
        SaveSystem.Write(activeSave);
        GameLogger.Info($"存档已保存：{activeSave.Name}");
    }

    private void AllocateStrength()
    {
        if (progression.AttributePoints <= 0) return;
        progression.AttributePoints--;
        progression.Strength++;
        RefreshStats();
    }

    private void AllocateVitality()
    {
        if (progression.AttributePoints <= 0) return;
        float oldMax = progression.MaxHealth;
        progression.AttributePoints--;
        progression.Vitality++;
        playerHealth += progression.MaxHealth - oldMax;
        RefreshStats();
    }

    private void AllocateAgility()
    {
        if (progression.AttributePoints <= 0) return;
        progression.AttributePoints--;
        progression.Agility++;
        RefreshStats();
    }

    private void RespawnPlayer()
    {
        playerDead = false;
        GetTree().Paused = false;
        playerHealth = Mathf.Max(1f, progression.MaxHealth * 0.5f);
        player.Position = new Vector3(activeSave.PlayerX, TerrainGenerator.GetHeight(activeSave.PlayerX, activeSave.PlayerZ) + 1f, activeSave.PlayerZ);
        deathOverlay.Visible = false;
        UpdateChunks(true);
        GameLogger.Info("玩家复活");
    }
}
