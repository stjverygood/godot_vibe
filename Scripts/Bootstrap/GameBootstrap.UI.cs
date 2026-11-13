using Godot;
using System;
using System.Linq;

public partial class GameBootstrap : Node3D
{
    private CanvasLayer uiLayer;

    private void BuildUi()
    {
        uiLayer = new CanvasLayer(); AddChild(uiLayer);
        CanvasLayer layer = uiLayer;
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
        aimReticle = new Label
        {
            Text = "+",
            Position = new Vector2(UIManager.DefaultWidth / 2f - 10f, UIManager.DefaultHeight / 2f - 20f),
            Size = new Vector2(20f, 40f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        aimReticle.AddThemeFontSizeOverride("font_size", 28);
        aimReticle.AddThemeColorOverride("font_color", new Color("fff2c8"));
        layer.AddChild(aimReticle);
        var controlsPanel = new Panel { Position = new Vector2(710, 16), Size = new Vector2(234, 34), MouseFilter = Control.MouseFilterEnum.Ignore };
        controlsPanel.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color(0.08f, 0.11f, 0.13f, 0.72f), 2));
        layer.AddChild(controlsPanel);
        controlsLabel = new Label { Position = new Vector2(11, 7), Size = new Vector2(214, 22), Text = "右键瞄准  左键使用" };
        controlsLabel.AddThemeFontSizeOverride("font_size", 14);
        controlsLabel.AddThemeColorOverride("font_color", UIManager.TextPrimary);
        controlsPanel.AddChild(controlsLabel);
        hotbarPanel = new Panel { Size = new Vector2(348, 76), MouseFilter = Control.MouseFilterEnum.Ignore };
        hotbarPanel.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color(0.07f, 0.09f, 0.11f, 0.78f), 2));
        layer.AddChild(hotbarPanel);
        hudQuickbarSlots = new Button[InventorySystem.QuickbarSize];
        for (int i = 0; i < hudQuickbarSlots.Length; i++)
        {
            int index = i;
            var slot = new Button
            {
                Position = new Vector2(8 + i * 68, 8),
                Size = new Vector2(60, 60),
                Text = "",
                FocusMode = Control.FocusModeEnum.None,
                ToggleMode = true
            };
            slot.AddThemeFontSizeOverride("font_size", 11);
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
        AddAttributeButton(panel, "血 +", 175, AllocateBlood);
        AddAttributeButton(panel, "气 +", 230, AllocateQi);
        AddAttributeButton(panel, "攻 +", 285, AllocateAttack);
        AddAttributeButton(panel, "防 +", 340, AllocateDefense);
        var close = new Button { Text = "关闭 (C)", Position = new Vector2(390, 420), Size = new Vector2(180, 42) };
        close.Pressed += () => { statsOverlay.Visible = false; GetTree().Paused = false; SetMouseCaptured(true); };
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
        aiming = false;
        aimReticle.Visible = false;
        GetTree().Paused = statsOverlay.Visible;
        SetMouseCaptured(!statsOverlay.Visible);
        RefreshStats();
    }

    private void RefreshStats()
    {
        statsText.Text = $"境界：{progression.Realm}    修为：{progression.Experience:0}/{progression.ExperienceToNextLevel:0}\n血：{progression.Blood}（生命上限 +{progression.Blood * 15}）\n气：{progression.Qi}（灵气上限 +{progression.Qi * 12}）\n攻：{progression.Attack}（伤害 +{progression.Attack * 8}%）\n防：{progression.Defense}（减伤上限 70%）\n悟道点：{progression.AttributePoints}";
    }

    private void TogglePause() { GetTree().Paused = false; pauseOverlay.Visible = false; SetMouseCaptured(true); GameLogger.Info("游戏继续"); }
    private void ShowHelp() { aiming = false; aimReticle.Visible = false; helpOverlay.Visible = true; SetMouseCaptured(false); }
    private void QuitGame() { SaveCurrentGame(); GameLogger.Info("玩家请求退出游戏"); GetTree().Quit(); }

    private void SelectItem(int index)
    {
        inventory.Select(index);
        UpdateHeldItemVisual();
        GameLogger.Info($"切换快捷栏：{inventory.SelectedItem?.Name ?? "空"}（槽位 {inventory.SelectedIndex + 1}）");
        RefreshHotbar();
    }

    private void RefreshHotbar()
    {
        if (hudQuickbarSlots == null) return;
        for (int i = 0; i < inventory.Quickbar.Length; i++)
        {
            InventoryItem item = inventory.Quickbar[i];
            string name = item == null ? "" : $"{i + 1}\n{item.Name}" + (item.Count > 1 ? $"\n×{item.Count}" : "");
            Button slot = hudQuickbarSlots[i];
            slot.Text = name;
            slot.AddThemeColorOverride("font_color", item?.Color ?? new Color("dce4eb"));
            slot.TooltipText = item == null ? "空槽位" : $"[{item.CategoryName}] {item.Name}\n{item.Description}";
            slot.ButtonPressed = i == inventory.SelectedIndex;
            InventorySlotButton.ApplySimpleBorder(slot, i == inventory.SelectedIndex);
        }
        RefreshInventorySlots();
    }

    private void UpdateUiLayout()
    {
        if (pauseOverlay == null) return;
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        float uiScale = Mathf.Max(0.5f, Mathf.Min(viewportSize.X / UIManager.DefaultWidth, viewportSize.Y / UIManager.DefaultHeight));
        Vector2 size = viewportSize / uiScale;
        uiLayer.Transform = new Transform2D(0f, Vector2.One * uiScale, 0f, Vector2.Zero);
        hotbarPanel.Position = new Vector2((size.X - hotbarPanel.Size.X) / 2f, size.Y - hotbarPanel.Size.Y - 16f);
        controlsLabel.GetParent<Control>().Position = new Vector2(size.X - 250f, 16f);
        attackLabel.Position = new Vector2((size.X - 450f) / 2f, 24f);
        attackLabel.Size = new Vector2(450f, 32f);
        aimReticle.Position = new Vector2((size.X - aimReticle.Size.X) / 2f, (size.Y - aimReticle.Size.Y) / 2f);
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
        statsOverlay.GetChild<Control>(0).Position = new Vector2((size.X - 520f) / 2f, (size.Y - 410f) / 2f);
        statsOverlay.GetChild<Control>(1).Position = new Vector2((size.X - 300f) / 2f, size.Y * 0.18f);
        statsText.Position = new Vector2(size.X / 2f - 180f, size.Y * 0.34f);
        statsOverlay.GetChild<Button>(3).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.33f);
        statsOverlay.GetChild<Button>(4).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.43f);
        statsOverlay.GetChild<Button>(5).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.53f);
        statsOverlay.GetChild<Button>(6).Position = new Vector2(size.X / 2f + 110f, size.Y * 0.63f);
        statsOverlay.GetChild<Button>(7).Position = new Vector2((size.X - 180f) / 2f, size.Y * 0.78f);
    }

    private void LayoutOverlay(Control panel, Vector2 size)
    {
        panel.Size = size;
        panel.GetChild<Control>(0).Position = new Vector2((size.X - 480f) / 2f, (size.Y - 340f) / 2f);
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
        panel.GetChild<Control>(0).Position = new Vector2((size.X - 500f) / 2f, (size.Y - 395f) / 2f);
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
        var content = new Label { Text = "WASD       相对镜头移动\n鼠标       环绕角色环视\n按住右键    瞄准，角色转向准星\n左键       使用当前物品\n滚轮       切换快捷栏物品\n空格       跳跃\nShift       身法突进 / 奔跑\nF          使用当前物品\nQ / E      切换快捷栏\n靠近灵材    自动收入储物袋\nB           打开储物袋\nC           查看/分配血气攻防\nEsc         闭关或暂停\n\n瞄准时法术朝准星方向施放\n建筑暂为场景装饰", Position = new Vector2(300, 190), Size = new Vector2(250, 280), HorizontalAlignment = HorizontalAlignment.Center };
        content.AddThemeFontSizeOverride("font_size", 18); content.AddThemeColorOverride("font_color", UIManager.TextSecondary); panel.AddChild(content);
        var back = new Button { Text = "返回", Position = new Vector2(390, 410), Size = new Vector2(180, 48) };
        back.AddThemeFontSizeOverride("font_size", 16); back.Pressed += () => { helpOverlay.Visible = false; if (!GetTree().Paused) SetMouseCaptured(true); }; panel.AddChild(back);
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
        close.Pressed += () => { inventoryOverlay.Visible = false; aiming = false; aimReticle.Visible = false; SetMouseCaptured(true); };
        panel.AddChild(close);

        var quickbarTitle = new Label { Text = "快捷栏", Position = new Vector2(30, 75), Size = new Vector2(140, 28) };
        quickbarTitle.AddThemeColorOverride("font_color", new Color("b9d6e8"));
        panel.AddChild(quickbarTitle);

        quickbarInventorySlots = new InventorySlotButton[InventorySystem.QuickbarSize];
        for (int i = 0; i < quickbarInventorySlots.Length; i++)
        {
            var slot = new InventorySlotButton(inventory, true, i, OnInventoryChanged)
            {
                Position = new Vector2(30 + i * 82, 105),
                Size = new Vector2(68, 68)
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
                Position = new Vector2(30 + column * 82, 218 + row * 80),
                Size = new Vector2(68, 68)
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
        UpdateHeldItemVisual();
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

