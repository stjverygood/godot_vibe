using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;

public sealed class SaveSlotData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新存档";
    public int Seed { get; set; }
    public long UpdatedUtcTicks { get; set; }
    public bool HasProgress { get; set; }
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float PlayerZ { get; set; }
    public float Health { get; set; } = 100f;
    public int Level { get; set; } = 1;
    public float Experience { get; set; }
    public int AttributePoints { get; set; }
    public int Strength { get; set; }
    public int Vitality { get; set; }
    public int Agility { get; set; }
    public int SelectedQuickbarIndex { get; set; }
    public List<InventorySlotData> Quickbar { get; set; } = new();
    public List<InventorySlotData> Backpack { get; set; } = new();
}

public sealed class InventorySlotData
{
    public int Kind { get; set; }
    public int Count { get; set; }
}

public static class SaveSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string SaveDirectory => ProjectSettings.GlobalizePath("user://saves");

    public static List<SaveSlotData> ListSaves()
    {
        Directory.CreateDirectory(SaveDirectory);
        var saves = new List<SaveSlotData>();
        foreach (string path in Directory.EnumerateFiles(SaveDirectory, "*.json"))
        {
            try
            {
                SaveSlotData data = JsonSerializer.Deserialize<SaveSlotData>(File.ReadAllText(path));
                if (data != null) saves.Add(data);
            }
            catch (Exception exception)
            {
                GameLogger.Warning($"忽略无法读取的存档 {Path.GetFileName(path)}：{exception.Message}");
            }
        }
        return saves.OrderByDescending(save => save.UpdatedUtcTicks).ToList();
    }

    public static SaveSlotData CreateNew()
    {
        List<SaveSlotData> existing = ListSaves();
        int seed;
        do seed = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, int.MaxValue);
        while (existing.Any(save => save.Seed == seed));
        var save = new SaveSlotData
        {
            Name = $"存档 {existing.Count + 1}",
            Seed = seed,
            UpdatedUtcTicks = DateTime.UtcNow.Ticks
        };
        Write(save);
        return save;
    }

    public static void Write(SaveSlotData save)
    {
        Directory.CreateDirectory(SaveDirectory);
        save.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
        string path = Path.Combine(SaveDirectory, $"{save.Id}.json");
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(save, JsonOptions));
        File.Move(tempPath, path, true);
    }

    public static void Delete(SaveSlotData save)
    {
        string path = Path.Combine(SaveDirectory, $"{save.Id}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
