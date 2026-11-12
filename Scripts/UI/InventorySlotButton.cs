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
        AddThemeColorOverride("font_color", item?.Color ?? UIManager.TextSecondary);
        TooltipText = item == null ? "空槽位" : $"[{item.CategoryName}] {item.Name}\n{item.Description}";
        ApplySimpleBorder(this, isQuickbar && inventory.SelectedIndex == slotIndex);
    }

    public static void ApplySimpleBorder(Button button, bool selected = false)
    {
        var normal = new StyleBoxFlat
        {
            BgColor = selected ? new Color("303b41") : new Color("1a242b"),
            BorderColor = selected ? UIManager.Accent : UIManager.Border,
            BorderWidthLeft = selected ? 2 : 1,
            BorderWidthTop = selected ? 2 : 1,
            BorderWidthRight = selected ? 2 : 1,
            BorderWidthBottom = selected ? 2 : 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 7,
            ContentMarginRight = 7,
            ContentMarginTop = 5,
            ContentMarginBottom = 5
        };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color("2a3a43"),
            BorderColor = new Color("9db1bc"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2
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
