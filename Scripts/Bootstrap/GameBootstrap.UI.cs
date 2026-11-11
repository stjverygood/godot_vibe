using Godot;

public partial class GameBootstrap : Node3D
{
    private void BuildUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        status = new Label { Position = new Vector2(20, 18), Text = "生命 100  ·  区块 0, 0" };
        status.AddThemeFontSizeOverride("font_size", 15);
        status.AddThemeColorOverride("font_color", new Color("f5e6c8"));
        layer.AddChild(status);
        resourceLabel = new Label { Position = new Vector2(20, 41), Text = inventory.ResourceSummary };
        resourceLabel.AddThemeFontSizeOverride("font_size", 13);
        resourceLabel.AddThemeColorOverride("font_color", new Color("b9d6e8"));
        layer.AddChild(resourceLabel);
        attackLabel = new Label { Position = new Vector2(0, 24), Size = new Vector2(960, 32), Text = "攻击！", Visible = false, HorizontalAlignment = HorizontalAlignment.Center };
        attackLabel.AddThemeFontSizeOverride("font_size", 20);
        attackLabel.AddThemeColorOverride("font_color", new Color("ffcf75"));
        layer.AddChild(attackLabel);
        controlsLabel = new Label { Position = new Vector2(760, 24), Text = "B 背包  ·  Q/E 切换" };
        controlsLabel.AddThemeFontSizeOverride("font_size", 14);
        controlsLabel.AddThemeColorOverride("font_color", new Color("e6edf2"));
        layer.AddChild(controlsLabel);
        hotbarPanel = new ColorRect { Size = new Vector2(500, 64), Color = new Color(0.03f, 0.05f, 0.08f, 0.74f) };
        layer.AddChild(hotbarPanel);
        hudQuickbarSlots = new Button[InventorySystem.QuickbarSize];
        for (int i = 0; i < hudQuickbarSlots.Length; i++)
        {
            int index = i;
            var slot = new Button
            {
                Position = new Vector2(8 + i * 97, 7),
                Size = new Vector2(90, 50),
                Text = "",
                FocusMode = Control.FocusModeEnum.None,
                ToggleMode = true
            };
            slot.AddThemeFontSizeOverride("font_size", 14);
            InventorySlotButton.ApplySimpleBorder(slot);
            slot.Pressed += () => SelectItem(index);
            hudQuickbarSlots[i] = slot;
            hotbarPanel.AddChild(slot);
        }
        RefreshHotbar();
        startOverlay = MakeOverlay(layer, "TimeToGo", "无限世界 · 3D 漫游", "开始游戏  (Enter)");
        pauseOverlay = MakeOverlay(layer, "游戏暂停", "探索暂时停下来了", "继续游戏  (Esc)");
        pauseOverlay.Visible = false;
        startOverlay.GetNode<Button>("ActionButton").Pressed += StartGame;
        pauseOverlay.GetNode<Button>("ActionButton").Pressed += TogglePause;
        startOverlay.GetNode<Button>("HelpButton").Pressed += ShowHelp;
        pauseOverlay.GetNode<Button>("HelpButton").Pressed += ShowHelp;
        startOverlay.GetNode<Button>("QuitButton").Pressed += QuitGame;
        pauseOverlay.GetNode<Button>("QuitButton").Pressed += QuitGame;
        helpOverlay = MakeHelpOverlay(layer);
        helpOverlay.Visible = false;
        inventoryOverlay = MakeInventoryOverlay(layer);
        inventoryOverlay.Visible = false;
    }

    private Control MakeOverlay(CanvasLayer layer, string titleText, string subtitleText, string buttonText)
    {
        var panel = new ColorRect { Name = "Overlay", Position = Vector2.Zero, Size = new Vector2(UIManager.DefaultWidth, UIManager.DefaultHeight), Color = new Color(0.02f, 0.03f, 0.05f, 0.92f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = titleText, Position = new Vector2(320, 170), Size = new Vector2(320, 60), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 38); title.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(title);
        var subtitle = new Label { Text = subtitleText, Position = new Vector2(280, 240), Size = new Vector2(400, 40), HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeFontSizeOverride("font_size", 20); subtitle.AddThemeColorOverride("font_color", new Color("b9d6e8")); panel.AddChild(subtitle);
        var button = new Button { Name = "ActionButton", Text = buttonText, Position = new Vector2(360, 315), Size = new Vector2(240, 55) };
        button.AddThemeFontSizeOverride("font_size", 20); panel.AddChild(button);
        panel.AddChild(new Button { Name = "HelpButton", Text = "操作说明", Position = new Vector2(360, 385), Size = new Vector2(115, 42) });
        panel.AddChild(new Button { Name = "QuitButton", Text = "退出游戏", Position = new Vector2(485, 385), Size = new Vector2(115, 42) });
        return panel;
    }

    private void StartGame() { gameStarted = true; startOverlay.Visible = false; }
    private void TogglePause() { GetTree().Paused = false; pauseOverlay.Visible = false; }
    private void ShowHelp() { helpOverlay.Visible = true; }
    private void QuitGame() { GetTree().Quit(); }

    private void SelectItem(int index)
    {
        inventory.Select(index);
        handItem.MaterialOverride = Mat(inventory.SelectedColor);
        handItem.Visible = inventory.SelectedItem != null;
        RefreshHotbar();
    }

    private void RefreshHotbar()
    {
        if (hudQuickbarSlots == null) return;
        for (int i = 0; i < inventory.Quickbar.Length; i++)
        {
            InventoryItem item = inventory.Quickbar[i];
            string name = item == null ? "" : $"{i + 1} · {item.Name}";
            Button slot = hudQuickbarSlots[i];
            slot.Text = name;
            slot.AddThemeColorOverride("font_color", item?.Color ?? new Color("dce4eb"));
            slot.ButtonPressed = i == inventory.SelectedIndex;
            InventorySlotButton.ApplySimpleBorder(slot, i == inventory.SelectedIndex);
        }
        RefreshInventorySlots();
    }

    private void UpdateUiLayout()
    {
        if (startOverlay == null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        hotbarPanel.Position = new Vector2((size.X - hotbarPanel.Size.X) / 2f, size.Y - hotbarPanel.Size.Y - 16f);
        controlsLabel.Position = new Vector2(size.X - 205f, 24f);
        attackLabel.Position = new Vector2((size.X - 450f) / 2f, 24f);
        attackLabel.Size = new Vector2(450f, 32f);
        if (inventoryOverlay != null)
            inventoryOverlay.Position = new Vector2((size.X - inventoryOverlay.Size.X) / 2f, (size.Y - inventoryOverlay.Size.Y) / 2f);
        LayoutOverlay(startOverlay, size);
        LayoutOverlay(pauseOverlay, size);
        LayoutHelpOverlay(helpOverlay, size);
    }

    private void LayoutOverlay(Control panel, Vector2 size)
    {
        panel.Size = size;
        panel.GetChild<Control>(0).Position = new Vector2((size.X - 320f) / 2f, size.Y * 0.31f);
        panel.GetChild<Control>(1).Position = new Vector2((size.X - 400f) / 2f, size.Y * 0.44f);
        panel.GetNode<Button>("ActionButton").Position = new Vector2((size.X - 240f) / 2f, size.Y * 0.58f);
        panel.GetNode<Button>("HelpButton").Position = new Vector2(size.X / 2f - 120f, size.Y * 0.71f);
        panel.GetNode<Button>("QuitButton").Position = new Vector2(size.X / 2f + 5f, size.Y * 0.71f);
    }

    private void LayoutHelpOverlay(Control panel, Vector2 size)
    {
        if (panel == null) return;
        panel.Size = size;
        panel.GetChild<Control>(0).Position = new Vector2((size.X - 300f) / 2f, size.Y * 0.2f);
        panel.GetChild<Control>(1).Position = new Vector2((size.X - 360f) / 2f, size.Y * 0.35f);
        panel.GetChild<Button>(2).Position = new Vector2((size.X - 180f) / 2f, size.Y * 0.76f);
    }

    private Control MakeHelpOverlay(CanvasLayer layer)
    {
        var panel = new ColorRect { Name = "HelpOverlay", Position = Vector2.Zero, Size = new Vector2(UIManager.DefaultWidth, UIManager.DefaultHeight), Color = new Color(0.02f, 0.03f, 0.05f, 0.96f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = "操作说明", Position = new Vector2(330, 105), Size = new Vector2(300, 55), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 34); title.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(title);
        var content = new Label { Text = "WASD       移动\nShift       突进，继续按住可奔跑\nF / 空格    攻击最近的目标\n靠近掉落物    自动拾取材料\nB           打开背包，拖动物品换位\nEsc         关闭背包或暂停游戏\n\n树木、石块和建筑都可以被攻击", Position = new Vector2(300, 190), Size = new Vector2(250, 280), HorizontalAlignment = HorizontalAlignment.Center };
        content.AddThemeFontSizeOverride("font_size", 20); content.AddThemeColorOverride("font_color", new Color("b9d6e8")); panel.AddChild(content);
        var back = new Button { Text = "返回", Position = new Vector2(390, 410), Size = new Vector2(180, 48) };
        back.AddThemeFontSizeOverride("font_size", 18); back.Pressed += () => helpOverlay.Visible = false; panel.AddChild(back);
        return panel;
    }

    private Control MakeInventoryOverlay(CanvasLayer layer)
    {
        var panel = new ColorRect
        {
            Name = "InventoryOverlay",
            Size = new Vector2(650, 520),
            Color = new Color(0.03f, 0.04f, 0.07f, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        layer.AddChild(panel);

        var title = new Label { Text = "背包  ·  拖动物品可与快捷栏交换", Position = new Vector2(28, 20), Size = new Vector2(500, 36) };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color("f5e6c8"));
        panel.AddChild(title);

        var close = new Button { Text = "关闭 (B)", Position = new Vector2(525, 20), Size = new Vector2(100, 36) };
        close.Pressed += () => inventoryOverlay.Visible = false;
        panel.AddChild(close);

        var quickbarTitle = new Label { Text = "快捷栏", Position = new Vector2(30, 75), Size = new Vector2(140, 28) };
        quickbarTitle.AddThemeColorOverride("font_color", new Color("b9d6e8"));
        panel.AddChild(quickbarTitle);

        quickbarInventorySlots = new InventorySlotButton[InventorySystem.QuickbarSize];
        for (int i = 0; i < quickbarInventorySlots.Length; i++)
        {
            var slot = new InventorySlotButton(inventory, true, i, OnInventoryChanged)
            {
                Position = new Vector2(30 + i * 120, 105)
            };
            quickbarInventorySlots[i] = slot;
            panel.AddChild(slot);
        }

        var backpackTitle = new Label { Text = "背包", Position = new Vector2(30, 185), Size = new Vector2(140, 28) };
        backpackTitle.AddThemeColorOverride("font_color", new Color("b9d6e8"));
        panel.AddChild(backpackTitle);

        backpackSlots = new InventorySlotButton[InventorySystem.BackpackSize];
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            int column = i % 5;
            int row = i / 5;
            var slot = new InventorySlotButton(inventory, false, i, OnInventoryChanged)
            {
                Position = new Vector2(30 + column * 120, 218 + row * 68)
            };
            backpackSlots[i] = slot;
            panel.AddChild(slot);
        }

        return panel;
    }

    private void OnInventoryChanged()
    {
        handItem.MaterialOverride = Mat(inventory.SelectedColor);
        handItem.Visible = inventory.SelectedItem != null;
        resourceLabel.Text = inventory.ResourceSummary;
        RefreshHotbar();
    }

    private void RefreshInventorySlots()
    {
        if (quickbarInventorySlots == null) return;
        foreach (InventorySlotButton slot in quickbarInventorySlots) slot.Refresh();
        foreach (InventorySlotButton slot in backpackSlots) slot.Refresh();
    }

    private StandardMaterial3D Mat(Color color) => new() { AlbedoColor = color, Roughness = 0.9f };
}

