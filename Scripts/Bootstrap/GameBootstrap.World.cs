using Godot;
using System.Collections.Generic;

public partial class GameBootstrap : Node3D
{
    internal float GetTerrainHeight(float x, float z)
    {
        float broad = terrainNoise.GetNoise2D(x, z);
        float fine = terrainDetail.GetNoise2D(x, z);
        return Mathf.Clamp((broad + 0.2f) * 5.5f + fine * 0.6f, -1.2f, 7.5f);
    }

    private void UpdateChunks(bool force)
    {
        Vector2I center = new(Mathf.FloorToInt(player.Position.X / WorldManager.ChunkSize), Mathf.FloorToInt(player.Position.Z / WorldManager.ChunkSize));
        if (!force && center == lastChunk) return;
        lastChunk = center;
        var wanted = new HashSet<Vector2I>();
        for (int z = -WorldManager.LoadRadius; z <= WorldManager.LoadRadius; z++) for (int x = -WorldManager.LoadRadius; x <= WorldManager.LoadRadius; x++)
        {
            Vector2I coord = center + new Vector2I(x, z); wanted.Add(coord);
            if (!chunks.ContainsKey(coord)) { var chunk = new ChunkNode(coord, this); chunks[coord] = chunk; AddChild(chunk); }
        }
        var remove = new List<Vector2I>();
        foreach (var pair in chunks) if (!wanted.Contains(pair.Key)) remove.Add(pair.Key);
        foreach (var coord in remove) { chunks[coord].QueueFree(); chunks.Remove(coord); }
    }

}


