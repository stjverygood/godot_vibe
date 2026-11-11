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
        ApplySimpleBorder(this);
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
        Text = item == null ? "" : item.Count > 1 ? $"{item.Name}\n×{item.Count}" : item.Name;
        AddThemeColorOverride("font_color", item?.Color ?? new Color("dce4eb"));
        ApplySimpleBorder(this, isQuickbar && inventory.SelectedIndex == slotIndex);
    }

    public static void ApplySimpleBorder(Button button, bool selected = false)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = selected ? new Color("25313c") : new Color("151c23"),
            BorderColor = selected ? new Color("e8c879") : new Color("56616b"),
            BorderWidthLeft = selected ? 2 : 1,
            BorderWidthTop = selected ? 2 : 1,
            BorderWidthRight = selected ? 2 : 1,
            BorderWidthBottom = selected ? 2 : 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 5,
            ContentMarginRight = 5,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color("26323d"),
            BorderColor = new Color("dce4eb"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
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
