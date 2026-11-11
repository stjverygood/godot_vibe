using Godot;

public partial class GameBootstrap : Node3D
{
    private void BuildUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var panel = new ColorRect { Position = new Vector2(20, 20), Size = new Vector2(300, 105), Color = new Color(0.03f, 0.05f, 0.08f, 0.78f) }; layer.AddChild(panel);
        status = new Label { Position = new Vector2(15, 12), Text = "无限世界 3D" }; status.AddThemeFontSizeOverride("font_size", 18); status.AddThemeColorOverride("font_color", new Color("f5e6c8")); panel.AddChild(status);
        healthBar = new ProgressBar { Position = new Vector2(15, 76), Size = new Vector2(270, 18), MinValue = 0, MaxValue = 100, Value = 100, ShowPercentage = true }; panel.AddChild(healthBar);
        attackLabel = new Label { Position = new Vector2(380, 24), Text = "攻击！", Visible = false }; attackLabel.AddThemeFontSizeOverride("font_size", 24); attackLabel.AddThemeColorOverride("font_color", new Color("ffcf75")); layer.AddChild(attackLabel);
        helpLabel = new Label { Position = new Vector2(20, 660), Text = "WASD 移动   Shift 慢走   窗口化俯视角" }; helpLabel.AddThemeFontSizeOverride("font_size", 16); helpLabel.AddThemeColorOverride("font_color", new Color("f5e6c8")); layer.AddChild(helpLabel);
        hotbarLabel = new Label { Position = new Vector2(350, 485), Size = new Vector2(260, 35), HorizontalAlignment = HorizontalAlignment.Center };
        hotbarLabel.AddThemeFontSizeOverride("font_size", 18); hotbarLabel.AddThemeColorOverride("font_color", new Color("f5e6c8")); layer.AddChild(hotbarLabel);
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
        handItem.MaterialOverride = Mat(inventory.Colors[inventory.SelectedIndex]);
        RefreshHotbar();
    }

    private void RefreshHotbar()
    {
        if (hotbarLabel == null) return;
        string text = "快捷栏  ";
        for (int i = 0; i < inventory.Items.Length; i++) text += i == inventory.SelectedIndex ? $"[ {inventory.Items[i]} ]  " : $"{inventory.Items[i]}  ";
        hotbarLabel.Text = text;
    }

    private void UpdateUiLayout()
    {
        if (startOverlay == null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        hotbarLabel.Position = new Vector2((size.X - 260f) / 2f, size.Y - 55f);
        helpLabel.Position = new Vector2(20, size.Y - 42f);
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
        var content = new Label { Text = "WASD       移动\nShift       突进，继续按住可奔跑\nF / 空格    发射攻击弹体\nEsc         暂停或继续游戏\n\n攻击会自动朝向范围内最近的敌人", Position = new Vector2(300, 190), Size = new Vector2(360, 190), HorizontalAlignment = HorizontalAlignment.Center };
        content.AddThemeFontSizeOverride("font_size", 20); content.AddThemeColorOverride("font_color", new Color("b9d6e8")); panel.AddChild(content);
        var back = new Button { Text = "返回", Position = new Vector2(390, 410), Size = new Vector2(180, 48) };
        back.AddThemeFontSizeOverride("font_size", 18); back.Pressed += () => helpOverlay.Visible = false; panel.AddChild(back);
        return panel;
    }

    private StandardMaterial3D Mat(Color color) => new() { AlbedoColor = color, Roughness = 0.9f };
}

