using Godot;

public enum ItemKind
{
    EmptyHand,
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
        ItemKind.EmptyHand => "空手",
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
        ItemKind.EmptyHand => new Color("ffe08a"),
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

    public InventoryItem[] Quickbar { get; } =
    {
        new(ItemKind.EmptyHand),
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
    public string ResourceSummary => $"木材 {Count(ItemKind.Wood)}   石料 {Count(ItemKind.StoneOre)}   砖块 {Count(ItemKind.Brick)}";

    public void Select(int index) => SelectedIndex = (index % QuickbarSize + QuickbarSize) % QuickbarSize;

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
