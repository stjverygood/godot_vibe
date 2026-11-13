using Godot;
using System.Linq;

public partial class MainMenu : Node
{
    private Control menuPanel;
    private Control savePanel;
    private VBoxContainer saveList;
    private Label selectionLabel;
    private Button enterButton;
    private ConfirmationDialog deleteDialog;
    private SaveSlotData selectedSave;
    private SaveSlotData pendingDelete;
    private GameBootstrap activeGame;

    public override void _Ready()
    {
        BuildMainMenu();
        Vector2I screen = DisplayServer.ScreenGetSize();
        Vector2I window = DisplayServer.WindowGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(20, Mathf.Max(20, screen.Y - window.Y - 90)));
    }

    private void BuildMainMenu()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        menuPanel = CreatePanel(layer);
        AddCard(menuPanel, new Vector2(200, 72), new Vector2(560, 396));
        AddTitle(menuPanel, "TimeToGo", "无限世界 · 3D 漫游");
        Button enter = AddButton(menuPanel, "进入游戏", new Vector2(350, 300), new Vector2(260, 54), ShowSaveManager);
        UIManager.StyleButton(enter, true);
        Button exit = AddButton(menuPanel, "退出游戏", new Vector2(405, 372), new Vector2(150, 44), () => GetTree().Quit());
        UIManager.StyleButton(exit);

        savePanel = CreatePanel(layer);
        savePanel.Visible = false;
        AddCard(savePanel, new Vector2(175, 34), new Vector2(610, 472));
        AddTitle(savePanel, "存档管理", "选择存档进入，或创建一个新世界");
        savePanel.GetChild<Control>(1).Position = new Vector2(320, 35);
        savePanel.GetChild<Control>(2).Position = new Vector2(250, 95);
        selectionLabel = new Label { Position = new Vector2(250, 135), Size = new Vector2(460, 26), HorizontalAlignment = HorizontalAlignment.Center };
        selectionLabel.AddThemeColorOverride("font_color", UIManager.TextSecondary);
        savePanel.AddChild(selectionLabel);

        var scroll = new ScrollContainer { Position = new Vector2(250, 170), Size = new Vector2(460, 240) };
        savePanel.AddChild(scroll);
        saveList = new VBoxContainer { CustomMinimumSize = new Vector2(460, 0) };
        saveList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(saveList);

        AddButton(savePanel, "新建存档", new Vector2(235, 435), new Vector2(130, 42), CreateSave);
        enterButton = AddButton(savePanel, "进入存档", new Vector2(390, 435), new Vector2(130, 42), EnterSelectedSave);
        enterButton.Disabled = true;
        AddButton(savePanel, "返回主菜单", new Vector2(545, 435), new Vector2(145, 42), ReturnToMainMenu);
        UIManager.StyleButton(enterButton, true);
        foreach (Node child in savePanel.GetChildren())
            if (child is Button button && button != enterButton) UIManager.StyleButton(button);

        deleteDialog = new ConfirmationDialog
        {
            Title = "删除存档",
            DialogText = "确定要永久删除这个存档吗？",
            ProcessMode = ProcessModeEnum.Always
        };
        deleteDialog.Confirmed += ConfirmDelete;
        savePanel.AddChild(deleteDialog);
    }

    private Control CreatePanel(CanvasLayer layer)
    {
        var panel = new ColorRect
        {
            Size = new Vector2(UIManager.DefaultWidth, UIManager.DefaultHeight),
            Color = UIManager.Background,
            ProcessMode = ProcessModeEnum.Always
        };
        layer.AddChild(panel);
        return panel;
    }

    private static void AddCard(Control parent, Vector2 position, Vector2 size)
    {
        var card = new Panel
        {
            Position = position,
            Size = size,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        card.AddThemeStyleboxOverride("panel", UIManager.MakePanelStyle(new Color("18232c"), 3));
        parent.AddChild(card);
        parent.MoveChild(card, 0);
    }

    private static void AddTitle(Control panel, string titleText, string subtitleText)
    {
        var title = new Label { Text = titleText, Position = new Vector2(320, 115), Size = new Vector2(320, 55), HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 40);
        title.AddThemeColorOverride("font_color", UIManager.TextPrimary);
        panel.AddChild(title);
        var subtitle = new Label { Text = subtitleText, Position = new Vector2(250, 185), Size = new Vector2(460, 32), HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", UIManager.TextSecondary);
        panel.AddChild(subtitle);
    }

    private static Button AddButton(Control panel, string text, Vector2 position, Vector2 size, System.Action action)
    {
        var button = new Button { Text = text, Position = position, Size = size };
        button.Pressed += action;
        panel.AddChild(button);
        return button;
    }

    private void ShowSaveManager()
    {
        menuPanel.Visible = false;
        savePanel.Visible = true;
        RefreshSaveList();
    }

    private void ReturnToMainMenu()
    {
        selectedSave = null;
        savePanel.Visible = false;
        menuPanel.Visible = true;
    }

    private void RefreshSaveList()
    {
        foreach (Node child in saveList.GetChildren())
        {
            saveList.RemoveChild(child);
            child.QueueFree();
        }

        var saves = SaveSystem.ListSaves();
        if (saves.Count == 0)
        {
            selectedSave = null;
            enterButton.Disabled = true;
            selectionLabel.Text = "当前还没有存档。";
            saveList.AddChild(new Label { Text = "存档列表为空", HorizontalAlignment = HorizontalAlignment.Center });
            return;
        }

        foreach (SaveSlotData save in saves)
        {
            bool selected = selectedSave?.Id == save.Id;
            string progress = save.HasProgress ? $"Lv.{save.Level}" : "新世界";
            var row = new HBoxContainer { CustomMinimumSize = new Vector2(460, 44) };
            var selectButton = new Button
            {
                Text = $"{(selected ? "✓ " : "")} {save.Name}  ·  {progress}  ·  Seed {save.Seed}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            UIManager.StyleButton(selectButton, selected);
            selectButton.Pressed += () => SelectSave(save);
            row.AddChild(selectButton);
            var deleteButton = new Button { Text = "删除", CustomMinimumSize = new Vector2(64, 0) };
            UIManager.StyleButton(deleteButton);
            deleteButton.Pressed += () => RequestDelete(save);
            row.AddChild(deleteButton);
            saveList.AddChild(row);
        }

        enterButton.Disabled = selectedSave == null || saves.All(save => save.Id != selectedSave.Id);
        selectionLabel.Text = selectedSave == null
            ? "请选择一个存档，或创建新存档。"
            : $"已选择 {selectedSave.Name}  ·  Seed {selectedSave.Seed}";
    }

    private void SelectSave(SaveSlotData save)
    {
        selectedSave = save;
        RefreshSaveList();
    }

    private void CreateSave()
    {
        selectedSave = SaveSystem.CreateNew();
        RefreshSaveList();
        GameLogger.Info($"创建存档 {selectedSave.Name}，种子 {selectedSave.Seed}");
    }

    private void EnterSelectedSave()
    {
        if (selectedSave == null) return;
        PackedScene scene = GD.Load<PackedScene>("res://Scenes/world.tscn");
        activeGame = scene.Instantiate<GameBootstrap>();
        AddChild(activeGame);
        activeGame.BeginSave(selectedSave);
        savePanel.Visible = false;
        GameLogger.Info($"载入游戏场景：{selectedSave.Name}");
    }

    private void RequestDelete(SaveSlotData save)
    {
        pendingDelete = save;
        deleteDialog.DialogText = $"确定要永久删除“{save.Name}”吗？此操作无法撤销。";
        deleteDialog.PopupCentered();
    }

    private void ConfirmDelete()
    {
        if (pendingDelete == null) return;
        SaveSystem.Delete(pendingDelete);
        if (selectedSave?.Id == pendingDelete.Id) selectedSave = null;
        GameLogger.Info($"删除存档 {pendingDelete.Name}");
        pendingDelete = null;
        RefreshSaveList();
    }

    internal void ReturnToMenu(GameBootstrap game)
    {
        if (activeGame != game) return;
        activeGame = null;
        game.QueueFree();
        selectedSave = null;
        savePanel.Visible = false;
        menuPanel.Visible = true;
    }
}
