using Godot;
using System;

public enum ItemKind
{
    WoodenSword,
    Stone,
    Potion,
    Fireball,
    Wood,
    StoneOre,
    Brick
}

public sealed class InventoryItem
{
    public ItemKind Kind { get; }
    public int Count { get; set; }
    public string Name => Kind switch
    {
        ItemKind.WoodenSword => "木剑",
        ItemKind.Stone => "石块",
        ItemKind.Potion => "药水",
        ItemKind.Fireball => "火球",
        ItemKind.Wood => "木材",
        ItemKind.StoneOre => "石料",
        _ => "砖块"
    };

    public Color Color => Kind switch
    {
        ItemKind.WoodenSword => new Color("d8b06a"),
        ItemKind.Stone => new Color("8b9299"),
        ItemKind.Potion => new Color("4ba5d8"),
        ItemKind.Fireball => new Color("e58a3d"),
        ItemKind.Wood => new Color("b77b3e"),
        ItemKind.StoneOre => new Color("aeb8c0"),
        _ => new Color("c76a4e")
    };

    public InventoryItem(ItemKind kind, int count = 1)
    {
        Kind = kind;
        Count = count;
    }
}

public sealed class InventorySystem
{
    public const int QuickbarSize = 5;
    public const int BackpackSize = 20;
    public const int StackLimit = 99;

    public InventoryItem[] Quickbar { get; } = new InventoryItem[]
    {
        null,
        new(ItemKind.WoodenSword),
        new(ItemKind.Stone),
        new(ItemKind.Potion),
        new(ItemKind.Fireball)
    };
    public InventoryItem[] Backpack { get; } = new InventoryItem[BackpackSize];
    public int SelectedIndex { get; private set; }
    public InventoryItem SelectedItem => Quickbar[SelectedIndex];
    public bool HasAttackEffect => SelectedItem?.Kind is ItemKind.WoodenSword or ItemKind.Fireball;
    public bool IsFireball => SelectedItem?.Kind == ItemKind.Fireball;
    public Color SelectedColor => SelectedItem?.Color ?? new Color("ffe08a");
    public string ResourceSummary => $"木 {Count(ItemKind.Wood)}   石 {Count(ItemKind.StoneOre)}   砖 {Count(ItemKind.Brick)}";

    public void Select(int index) => SelectedIndex = (index % QuickbarSize + QuickbarSize) % QuickbarSize;

    public void Reset()
    {
        Array.Clear(Quickbar);
        Array.Clear(Backpack);
        Quickbar[1] = new InventoryItem(ItemKind.WoodenSword);
        Quickbar[2] = new InventoryItem(ItemKind.Stone);
        Quickbar[3] = new InventoryItem(ItemKind.Potion);
        Quickbar[4] = new InventoryItem(ItemKind.Fireball);
        Select(0);
    }

    public void Load(SaveSlotData save)
    {
        Array.Clear(Quickbar);
        Array.Clear(Backpack);
        LoadSlots(save.Quickbar, Quickbar);
        LoadSlots(save.Backpack, Backpack);
        Select(save.SelectedQuickbarIndex);
    }

    public void SaveTo(SaveSlotData save)
    {
        save.Quickbar = SaveSlots(Quickbar);
        save.Backpack = SaveSlots(Backpack);
        save.SelectedQuickbarIndex = SelectedIndex;
    }

    private static void LoadSlots(System.Collections.Generic.List<InventorySlotData> data, InventoryItem[] target)
    {
        for (int i = 0; i < Math.Min(data.Count, target.Length); i++)
        {
            InventorySlotData slot = data[i];
            if (Enum.IsDefined(typeof(ItemKind), slot.Kind) && slot.Count > 0)
                target[i] = new InventoryItem((ItemKind)slot.Kind, Math.Min(slot.Count, StackLimit));
        }
    }

    private static System.Collections.Generic.List<InventorySlotData> SaveSlots(InventoryItem[] items)
    {
        var data = new System.Collections.Generic.List<InventorySlotData>(items.Length);
        foreach (InventoryItem item in items)
            data.Add(item == null ? new InventorySlotData() { Kind = -1, Count = 0 } : new InventorySlotData { Kind = (int)item.Kind, Count = item.Count });
        return data;
    }

    public InventoryItem GetItem(bool quickbar, int index) => (quickbar ? Quickbar : Backpack)[index];

    public void Swap(bool sourceQuickbar, int sourceIndex, bool targetQuickbar, int targetIndex)
    {
        InventoryItem[] source = sourceQuickbar ? Quickbar : Backpack;
        InventoryItem[] target = targetQuickbar ? Quickbar : Backpack;
        (source[sourceIndex], target[targetIndex]) = (target[targetIndex], source[sourceIndex]);
    }

    public int AddResources(ResourceKind kind, int amount)
    {
        int remaining = amount;
        ItemKind itemKind = kind switch
        {
            ResourceKind.Wood => ItemKind.Wood,
            ResourceKind.Stone => ItemKind.StoneOre,
            _ => ItemKind.Brick
        };

        for (int i = 0; i < Backpack.Length && remaining > 0; i++)
        {
            InventoryItem item = Backpack[i];
            if (item?.Kind != itemKind || item.Count >= StackLimit) continue;
            int moved = Mathf.Min(remaining, StackLimit - item.Count);
            item.Count += moved;
            remaining -= moved;
        }

        for (int i = 0; i < Backpack.Length && remaining > 0; i++)
        {
            if (Backpack[i] != null) continue;
            int moved = Mathf.Min(remaining, StackLimit);
            Backpack[i] = new InventoryItem(itemKind, moved);
            remaining -= moved;
        }

        return amount - remaining;
    }

    private int Count(ItemKind kind)
    {
        int total = 0;
        foreach (InventoryItem item in Backpack)
            if (item?.Kind == kind) total += item.Count;
        foreach (InventoryItem item in Quickbar)
            if (item?.Kind == kind) total += item.Count;
        return total;
    }
}
