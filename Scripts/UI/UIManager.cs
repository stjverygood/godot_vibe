using Godot;

/// <summary>Shared visual language and small UI helpers.</summary>
public static class UIManager
{
    public const int DefaultWidth = 960;
    public const int DefaultHeight = 540;

    public static readonly Color Background = new("111820");
    public static readonly Color Surface = new("1c2731");
    public static readonly Color SurfaceRaised = new("263541");
    public static readonly Color Border = new("465865");
    public static readonly Color TextPrimary = new("f2f0e8");
    public static readonly Color TextSecondary = new("aebdc5");
    public static readonly Color Accent = new("e6bd72");
    public static readonly Color AccentMuted = new("806842");

    public static StyleBoxFlat MakePanelStyle(Color? color = null, int radius = 3)
    {
        return new StyleBoxFlat
        {
            BgColor = color ?? Surface,
            BorderColor = Border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
    }

    public static void StyleButton(Button button, bool accent = false)
    {
        Color normalColor = accent ? new Color("3b352a") : new Color("242e34");
        Color hoverColor = accent ? new Color("494132") : new Color("303b42");
        Color pressedColor = accent ? new Color("302b23") : new Color("20282d");
        Color borderColor = accent ? new Color("9a8354") : new Color("48565e");
        button.AddThemeColorOverride("font_color", TextPrimary);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", TextPrimary);
        button.AddThemeColorOverride("font_disabled_color", new Color("76838a"));
        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeStyleboxOverride("normal", MakeButtonBox(normalColor, borderColor));
        button.AddThemeStyleboxOverride("hover", MakeButtonBox(hoverColor, accent ? new Color("f0d397") : new Color("91a5b0")));
        button.AddThemeStyleboxOverride("pressed", MakeButtonBox(pressedColor, borderColor));
        button.AddThemeStyleboxOverride("disabled", MakeButtonBox(new Color("20272d"), new Color("36424a")));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
    }

    public static void StyleLabel(Label label, bool secondary = false)
    {
        label.AddThemeColorOverride("font_color", secondary ? TextSecondary : TextPrimary);
    }

    public static void ApplyButtonStyles(Node root)
    {
        foreach (Node node in root.FindChildren("*", "Button", true, false))
        {
            if (node is Button button) StyleButton(button);
        }
    }

    private static StyleBoxFlat MakeButtonBox(Color fill, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
    }
}
