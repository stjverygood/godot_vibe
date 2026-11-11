using Godot;

public sealed class InventorySystem
{
    public readonly string[] Items = { "空手", "木剑", "石块", "药水", "火球" };
    public readonly Color[] Colors = { new("ffe08a"), new("d8b06a"), new("8b9299"), new("4ba5d8"), new("e58a3d") };
    public int SelectedIndex { get; private set; }
    public string SelectedItem => Items[SelectedIndex];
    public bool HasAttackEffect => SelectedIndex == 1 || SelectedIndex == 4;
    public bool IsFireball => SelectedIndex == 4;
    public void Select(int index) => SelectedIndex = (index % Items.Length + Items.Length) % Items.Length;
}
