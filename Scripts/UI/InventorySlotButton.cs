using Godot;
using System;

public partial class InventorySlotButton : Button
{
    private readonly InventorySystem inventory;
    private readonly bool isQuickbar;
    private readonly int slotIndex;
    private readonly Action onChanged;

    public InventorySlotButton(InventorySystem inventory, bool isQuickbar, int slotIndex, Action onChanged)
    {
        this.inventory = inventory;
        this.isQuickbar = isQuickbar;
        this.slotIndex = slotIndex;
        this.onChanged = onChanged;
        CustomMinimumSize = new Vector2(100, 58);
        if (isQuickbar)
        {
            Pressed += () =>
            {
                inventory.Select(slotIndex);
                onChanged.Invoke();
            };
        }
        Refresh();
    }

    public void Refresh()
    {
        InventoryItem item = inventory.GetItem(isQuickbar, slotIndex);
        string slotName = isQuickbar ? $"快捷栏 {slotIndex + 1}" : $"背包 {slotIndex + 1}";
        string selected = isQuickbar && inventory.SelectedIndex == slotIndex ? "★ " : "";
        Text = item == null ? $"{slotName}\n空" : $"{selected}{slotName}\n{item.Name} x{item.Count}";
        Modulate = item?.Color ?? Colors.White;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        InventoryItem item = inventory.GetItem(isQuickbar, slotIndex);
        if (item == null) return default;

        var preview = new Label { Text = item.Name, Modulate = item.Color };
        SetDragPreview(preview);
        return new Vector2I(isQuickbar ? 1 : 0, slotIndex);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Vector2I) return false;
        Vector2I source = data.AsVector2I();
        bool sourceQuickbar = source.X == 1;
        int sourceSize = sourceQuickbar ? InventorySystem.QuickbarSize : InventorySystem.BackpackSize;
        int targetSize = isQuickbar ? InventorySystem.QuickbarSize : InventorySystem.BackpackSize;
        return source.X is 0 or 1 && source.Y >= 0 && source.Y < sourceSize
            && slotIndex >= 0 && slotIndex < targetSize
            && (sourceQuickbar != isQuickbar || source.Y != slotIndex);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        Vector2I source = data.AsVector2I();
        inventory.Swap(source.X == 1, source.Y, isQuickbar, slotIndex);
        onChanged.Invoke();
    }
}
