using Godot;
using System;
using System.Linq;

public partial class GameBootstrap : Node3D
{
    private void BuildUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var hudCard = new Panel { Position = new Vector2(16, 16), Size = new Vector2(250, 91), MouseFilter = Control.MouseFilterEnum.Ignore };
        hudCard.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color(0.08f, 0.11f, 0.13f, 0.78f), 2));
        layer.AddChild(hudCard);
        status = new Label { Position = new Vector2(12, 7), Size = new Vector2(224, 21), Text = "世界  ·  Lv.1" };
        status.AddThemeFontSizeOverride("font_size", 14);
        status.AddThemeColorOverride("font_color", UIManager.TextPrimary);
        hudCard.AddChild(status);
        vitalsLabel = new Label { Position = new Vector2(12, 31), Size = new Vector2(220, 17) };
        vitalsLabel.AddThemeFontSizeOverride("font_size", 11);
        vitalsLabel.AddThemeColorOverride("font_color", UIManager.TextSecondary);
        hudCard.AddChild(vitalsLabel);
        healthBar = MakeHudBar(new Color("d77a65"));
        healthBar.Position = new Vector2(12, 47);
        healthBar.Size = new Vector2(224, 8);
        hudCard.AddChild(healthBar);
        var xpCaption = new Label { Text = "经验", Position = new Vector2(12, 65), Size = new Vector2(34, 17) };
        xpCaption.AddThemeFontSizeOverride("font_size", 11);
        xpCaption.AddThemeColorOverride("font_color", UIManager.TextSecondary);
        hudCard.AddChild(xpCaption);
        experienceBar = MakeHudBar(new Color("d9b66f"));
        experienceBar.Position = new Vector2(48, 69);
        experienceBar.Size = new Vector2(188, 8);
        hudCard.AddChild(experienceBar);
        attackLabel = new Label { Position = new Vector2(0, 24), Size = new Vector2(960, 32), Text = "攻击！", Visible = false, HorizontalAlignment = HorizontalAlignment.Center };
        attackLabel.AddThemeFontSizeOverride("font_size", 20);
        attackLabel.AddThemeColorOverride("font_color", new Color("ffcf75"));
        layer.AddChild(attackLabel);
        var controlsPanel = new Panel { Position = new Vector2(710, 16), Size = new Vector2(234, 34), MouseFilter = Control.MouseFilterEnum.Ignore };
        controlsPanel.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color(0.08f, 0.11f, 0.13f, 0.72f), 2));
        layer.AddChild(controlsPanel);
        controlsLabel = new Label { Position = new Vector2(11, 7), Size = new Vector2(214, 22), Text = "B 背包   C 属性   Q / E 切换" };
        controlsLabel.AddThemeFontSizeOverride("font_size", 14);
        controlsLabel.AddThemeColorOverride("font_color", UIManager.TextPrimary);
        controlsPanel.AddChild(controlsLabel);
        hotbarPanel = new Panel { Size = new Vector2(500, 64), MouseFilter = Control.MouseFilterEnum.Ignore };
        hotbarPanel.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color(0.07f, 0.09f, 0.11f, 0.78f), 2));
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
        pauseOverlay = MakeOverlay(layer, "游戏暂停", "探索暂时停下来了", "继续游戏  (Esc)");
        pauseOverlay.Visible = false;
        pauseOverlay.GetNode<Button>("ActionButton").Pressed += TogglePause;
        pauseOverlay.GetNode<Button>("HelpButton").Pressed += ShowHelp;
        pauseOverlay.GetNode<Button>("QuitButton").Text = "保存并返回主菜单";
        pauseOverlay.GetNode<Button>("QuitButton").Pressed += SaveAndReturnToMenu;
        helpOverlay = MakeHelpOverlay(layer);
        helpOverlay.Visible = false;
        inventoryOverlay = MakeInventoryOverlay(layer);
        inventoryOverlay.Visible = false;
        statsOverlay = MakeStatsOverlay(layer);
        statsOverlay.Visible = false;
        deathOverlay = MakeOverlay(layer, "你已死亡", "本次冒险结束", "复活");
        deathOverlay.Visible = false;
        deathOverlay.GetNode<Button>("ActionButton").Pressed += RespawnPlayer;
        deathOverlay.GetNode<Button>("HelpButton").Visible = false;
        deathOverlay.GetNode<Button>("QuitButton").Text = "保存并返回主菜单";
        deathOverlay.GetNode<Button>("QuitButton").Size = new Vector2(190, 42);
        deathOverlay.GetNode<Button>("QuitButton").Pressed += SaveAndReturnToMenu;
        pauseOverlay.GetNode<Button>("QuitButton").Size = new Vector2(190, 42);
        UIManager.ApplyButtonStyles(layer);
        RefreshHotbar();
    }

    private static ProgressBar MakeHudBar(Color fill)
    {
        var bar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 100, ShowPercentage = false, Size = new Vector2(236, 10), MouseFilter = Control.MouseFilterEnum.Ignore };
        var background = new StyleBoxFlat { BgColor = new Color("303c42"), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        var foreground = new StyleBoxFlat { BgColor = fill, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        bar.AddThemeStyleboxOverride("background", background);
        bar.AddThemeStyleboxOverride("fill", foreground);
        return bar;
    }

    private Control MakeOverlay(CanvasLayer layer, string titleText, string subtitleText, string buttonText)
    {
        var panel = new ColorRect { Name = "Overlay", Position = Vector2.Zero, Size = new Vector2(UIManager.DefaultWidth, UIManager.DefaultHeight), Color = new Color(0.02f, 0.03f, 0.05f, 0.92f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = titleText, Position = new Vector2(320, 170), Size = new Vector2(320, 60), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 38); title.AddThemeColorOverride("font_color", UIManager.TextPrimary); panel.AddChild(title);
        var subtitle = new Label { Text = subtitleText, Position = new Vector2(280, 240), Size = new Vector2(400, 40), HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeFontSizeOverride("font_size", 16); subtitle.AddThemeColorOverride("font_color", UIManager.TextSecondary); panel.AddChild(subtitle);
        var button = new Button { Name = "ActionButton", Text = buttonText, Position = new Vector2(360, 315), Size = new Vector2(240, 55) };
        button.AddThemeFontSizeOverride("font_size", 20); panel.AddChild(button);
        panel.AddChild(new Button { Name = "HelpButton", Text = "操作说明", Position = new Vector2(360, 385), Size = new Vector2(115, 42) });
        panel.AddChild(new Button { Name = "QuitButton", Text = "退出游戏", Position = new Vector2(485, 385), Size = new Vector2(115, 42) });
        AddOverlayCard(panel, new Vector2(240, 105), new Vector2(480, 340));
        UIManager.ApplyButtonStyles(panel);
        return panel;
    }

    private Control MakeStatsOverlay(CanvasLayer layer)
    {
        var panel = new ColorRect { Name = "StatsOverlay", Size = new Vector2(UIManager.DefaultWidth, UIManager.DefaultHeight), Color = new Color(0.02f, 0.03f, 0.05f, 0.96f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = "角色属性", Position = new Vector2(330, 100), Size = new Vector2(300, 50), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", new Color("f5e6c8"));
        panel.AddChild(title);
        statsText = new Label { Position = new Vector2(300, 175), Size = new Vector2(280, 190) };
        statsText.AddThemeFontSizeOverride("font_size", 20);
        statsText.AddThemeColorOverride("font_color", new Color("b9d6e8"));
        panel.AddChild(statsText);
        AddAttributeButton(panel, "力量 +", 190, AllocateStrength);
        AddAttributeButton(panel, "体质 +", 245, AllocateVitality);
        AddAttributeButton(panel, "敏捷 +", 300, AllocateAgility);
        var close = new Button { Text = "关闭 (C)", Position = new Vector2(390, 420), Size = new Vector2(180, 42) };
        close.Pressed += () => statsOverlay.Visible = false;
        panel.AddChild(close);
        AddOverlayCard(panel, new Vector2(220, 70), new Vector2(520, 410));
        UIManager.ApplyButtonStyles(panel);
        return panel;
    }

    private static void AddAttributeButton(Control panel, string text, float y, Action allocate)
    {
        var button = new Button { Text = text, Position = new Vector2(590, y), Size = new Vector2(100, 38) };
        button.Pressed += allocate;
        panel.AddChild(button);
    }

    private void ToggleStats()
    {
        statsOverlay.Visible = !statsOverlay.Visible;
        GetTree().Paused = statsOverlay.Visible;
        RefreshStats();
    }

    private void RefreshStats()
    {
        statsText.Text = $"等级：{progression.Level}    经验：{progression.Experience:0}/{progression.ExperienceToNextLevel:0}\n可用属性点：{progression.AttributePoints}\n力量：{progression.Strength}（攻击 +{progression.Strength * 8}%）\n体质：{progression.Vitality}（生命上限 +{progression.Vitality * 12}）\n敏捷：{progression.Agility}（移动速度 +{progression.Agility * 3.5f:0.#}%）";
    }

    private void TogglePause() { GetTree().Paused = false; pauseOverlay.Visible = false; GameLogger.Info("游戏继续"); }
    private void ShowHelp() { helpOverlay.Visible = true; }
    private void QuitGame() { SaveCurrentGame(); GameLogger.Info("玩家请求退出游戏"); GetTree().Quit(); }

    private void SelectItem(int index)
    {
        inventory.Select(index);
        handItem.MaterialOverride = Mat(inventory.SelectedColor);
        handItem.Visible = inventory.SelectedItem != null;
        GameLogger.Info($"切换快捷栏：{inventory.SelectedItem?.Name ?? "空"}（槽位 {inventory.SelectedIndex + 1}）");
        RefreshHotbar();
    }

    private void RefreshHotbar()
    {
        if (hudQuickbarSlots == null) return;
        for (int i = 0; i < inventory.Quickbar.Length; i++)
        {
            InventoryItem item = inventory.Quickbar[i];
            string name = item == null ? "" : $"{i + 1} · {item.Name}" + (item.Count > 1 ? $"\n×{item.Count}" : "");
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
        if (pauseOverlay == null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        hotbarPanel.Position = new Vector2((size.X - hotbarPanel.Size.X) / 2f, size.Y - hotbarPanel.Size.Y - 16f);
        controlsLabel.GetParent<Control>().Position = new Vector2(size.X - 250f, 16f);
        attackLabel.Position = new Vector2((size.X - 450f) / 2f, 24f);
        attackLabel.Size = new Vector2(450f, 32f);
        if (inventoryOverlay != null)
            inventoryOverlay.Position = new Vector2((size.X - inventoryOverlay.Size.X) / 2f, (size.Y - inventoryOverlay.Size.Y) / 2f);
        LayoutOverlay(pauseOverlay, size);
        LayoutOverlay(deathOverlay, size);
        LayoutStatsOverlay(size);
        LayoutHelpOverlay(helpOverlay, size);
    }

    private void LayoutStatsOverlay(Vector2 size)
    {
        if (statsOverlay == null) return;
        statsOverlay.Size = size;
        statsOverlay.GetChild<Control>(1).Position = new Vector2((size.X - 300f) / 2f, size.Y * 0.18f);
        statsText.Position = new Vector2(size.X / 2f - 180f, size.Y * 0.34f);
        statsOverlay.GetChild<Button>(3).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.37f);
        statsOverlay.GetChild<Button>(4).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.48f);
        statsOverlay.GetChild<Button>(5).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.59f);
        statsOverlay.GetChild<Button>(6).Position = new Vector2((size.X - 180f) / 2f, size.Y * 0.78f);
    }

    private void LayoutOverlay(Control panel, Vector2 size)
    {
        panel.Size = size;
        panel.GetChild<Control>(1).Position = new Vector2((size.X - 320f) / 2f, size.Y * 0.31f);
        panel.GetChild<Control>(2).Position = new Vector2((size.X - 400f) / 2f, size.Y * 0.44f);
        panel.GetNode<Button>("ActionButton").Position = new Vector2((size.X - 240f) / 2f, size.Y * 0.58f);
        panel.GetNode<Button>("HelpButton").Position = new Vector2(size.X / 2f - (panel == pauseOverlay ? 215f : 120f), size.Y * 0.71f);
        panel.GetNode<Button>("QuitButton").Position = new Vector2(size.X / 2f + 5f, size.Y * 0.71f);
        if (panel == deathOverlay)
            panel.GetNode<Button>("QuitButton").Position = new Vector2((size.X - 190f) / 2f, size.Y * 0.71f);
    }

    private void LayoutHelpOverlay(Control panel, Vector2 size)
    {
        if (panel == null) return;
        panel.Size = size;
        panel.GetChild<Control>(1).Position = new Vector2((size.X - 300f) / 2f, size.Y * 0.2f);
        panel.GetChild<Control>(2).Position = new Vector2((size.X - 360f) / 2f, size.Y * 0.35f);
        panel.GetChild<Button>(3).Position = new Vector2((size.X - 180f) / 2f, size.Y * 0.76f);
    }

    private Control MakeHelpOverlay(CanvasLayer layer)
    {
        var panel = new ColorRect { Name = "HelpOverlay", Position = Vector2.Zero, Size = new Vector2(UIManager.DefaultWidth, UIManager.DefaultHeight), Color = new Color(0.02f, 0.03f, 0.05f, 0.96f), ProcessMode = ProcessModeEnum.Always };
        layer.AddChild(panel);
        var title = new Label { Text = "操作说明", Position = new Vector2(330, 105), Size = new Vector2(300, 55), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 34); title.AddThemeColorOverride("font_color", UIManager.TextPrimary); panel.AddChild(title);
        var content = new Label { Text = "WASD       移动\nShift       突进，继续按住可奔跑\nF / 空格    攻击最近的目标\n靠近掉落物    自动拾取材料\nB           打开背包，拖动物品换位\nC           查看/分配属性点\nEsc         关闭界面或暂停游戏\n\n树木、石块和建筑都可以被攻击", Position = new Vector2(300, 190), Size = new Vector2(250, 280), HorizontalAlignment = HorizontalAlignment.Center };
        content.AddThemeFontSizeOverride("font_size", 18); content.AddThemeColorOverride("font_color", UIManager.TextSecondary); panel.AddChild(content);
        var back = new Button { Text = "返回", Position = new Vector2(390, 410), Size = new Vector2(180, 48) };
        back.AddThemeFontSizeOverride("font_size", 16); back.Pressed += () => helpOverlay.Visible = false; panel.AddChild(back);
        AddOverlayCard(panel, new Vector2(230, 75), new Vector2(500, 395));
        UIManager.ApplyButtonStyles(panel);
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
        title.AddThemeColorOverride("font_color", UIManager.TextPrimary);
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

        AddOverlayCard(panel, Vector2.Zero, new Vector2(650, 520));
        UIManager.ApplyButtonStyles(panel);

        return panel;
    }

    private static void AddOverlayCard(Control parent, Vector2 position, Vector2 size)
    {
        var card = new Panel { Position = position, Size = size, MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color("18232c"), 3));
        parent.AddChild(card);
        parent.MoveChild(card, 0);
    }

    private void OnInventoryChanged()
    {
        handItem.MaterialOverride = Mat(inventory.SelectedColor);
        handItem.Visible = inventory.SelectedItem != null;
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

