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
    Brick,
    WoodenAxe,
    StonePickaxe,
    IceSpell
}

public enum ItemCategory { Material, Weapon, Spell, Tool }

public static class ItemCatalog
{
    public static ItemCategory Category(ItemKind kind) => kind switch
    {
        ItemKind.Wood or ItemKind.StoneOre or ItemKind.Brick or ItemKind.Stone => ItemCategory.Material,
        ItemKind.WoodenSword => ItemCategory.Weapon,
        ItemKind.Fireball or ItemKind.IceSpell or ItemKind.Potion => ItemCategory.Spell,
        _ => ItemCategory.Tool
    };
    public static string StableId(ItemKind kind) => kind switch
    {
        ItemKind.WoodenSword => "weapon_wooden_sword", ItemKind.Stone => "material_stone",
        ItemKind.Potion => "spell_potion", ItemKind.Fireball => "spell_fireball",
        ItemKind.Wood => "material_wood", ItemKind.StoneOre => "material_stone_ore",
        ItemKind.Brick => "material_brick", ItemKind.WoodenAxe => "tool_wooden_axe",
        ItemKind.StonePickaxe => "tool_stone_pickaxe", ItemKind.IceSpell => "spell_ice", _ => ""
    };
    public static bool TryParseId(string id, out ItemKind kind)
    {
        foreach (ItemKind candidate in Enum.GetValues(typeof(ItemKind)))
            if (StableId(candidate) == id) { kind = candidate; return true; }
        kind = default; return false;
    }
}

public sealed class InventoryItem
{
    public ItemKind Kind { get; }
    public int Count { get; set; }
    public string Name => Kind switch
    {
        ItemKind.WoodenSword => "青锋剑",
        ItemKind.Stone => "灵石碎片",
        ItemKind.Potion => "回春丹",
        ItemKind.Fireball => "赤焰诀",
        ItemKind.WoodenAxe => "灵木斧",
        ItemKind.StonePickaxe => "灵矿镐",
        ItemKind.IceSpell => "寒霜诀",
        ItemKind.Wood => "灵木",
        ItemKind.StoneOre => "灵矿",
        _ => "阵材"
    };

    public Color Color => Kind switch
    {
        ItemKind.WoodenSword => new Color("d8b06a"),
        ItemKind.Stone => new Color("8b9299"),
        ItemKind.Potion => new Color("4ba5d8"),
        ItemKind.Fireball => new Color("e58a3d"),
        ItemKind.WoodenAxe => new Color("b77b3e"),
        ItemKind.StonePickaxe => new Color("aeb8c0"),
        ItemKind.IceSpell => new Color("79c9ed"),
        ItemKind.Wood => new Color("b77b3e"),
        ItemKind.StoneOre => new Color("aeb8c0"),
        _ => new Color("c76a4e")
    };
    public string CategoryName => ItemCatalog.Category(Kind) switch
    {
        ItemCategory.Material => "材料", ItemCategory.Weapon => "武器", ItemCategory.Spell => "法术", _ => "工具"
    };
    public string Description => Kind switch
    {
        ItemKind.WoodenSword => "下品法器。挥出剑气，对妖兽造成伤害。",
        ItemKind.Fireball => "火系术法。凝聚火球追击目标。",
        ItemKind.IceSpell => "冰系术法。造成伤害并减缓妖兽。",
        ItemKind.Potion => "一阶回春丹。恢复 35 点气血，服用后消耗。",
        ItemKind.WoodenAxe => "采伐灵木的法器，对灵木造成额外伤害。",
        ItemKind.StonePickaxe => "开采灵矿的法器，对灵矿造成额外伤害。",
        _ => "天地灵材，可用于炼器、布阵与修建洞府。"
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
    public bool HasAttackEffect => SelectedItem != null && ItemCatalog.Category(SelectedItem.Kind) != ItemCategory.Material;
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
        Backpack[0] = new InventoryItem(ItemKind.WoodenAxe);
        Backpack[1] = new InventoryItem(ItemKind.StonePickaxe);
        Backpack[2] = new InventoryItem(ItemKind.IceSpell);
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
            if (ItemCatalog.TryParseId(slot.ItemId, out ItemKind stableKind) && slot.Count > 0)
                target[i] = new InventoryItem(stableKind, Math.Min(slot.Count, StackLimit));
            else if (Enum.IsDefined(typeof(ItemKind), slot.Kind) && slot.Count > 0)
                target[i] = new InventoryItem((ItemKind)slot.Kind, Math.Min(slot.Count, StackLimit));
        }
    }

    private static System.Collections.Generic.List<InventorySlotData> SaveSlots(InventoryItem[] items)
    {
        var data = new System.Collections.Generic.List<InventorySlotData>(items.Length);
        foreach (InventoryItem item in items)
            data.Add(item == null ? new InventorySlotData() { Kind = -1, Count = 0 } : new InventorySlotData { Kind = (int)item.Kind, ItemId = ItemCatalog.StableId(item.Kind), Count = item.Count });
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
