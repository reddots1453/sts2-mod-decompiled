using Godot;

namespace IntentGraph.UI;

/// <summary>
/// Base class for InfoMod-style panels. Dark background, thin border,
/// title/subtitle/separator/content rows.
/// </summary>
public class InfoModPanel : PanelContainer
{
    private static readonly Color BgColor = new(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color BorderColor = new(0.3f, 0.4f, 0.6f, 0.4f);
    private const int CornerRadius = 6;
    private const int BorderWidth = 1;

    private static readonly Color White = new(1f, 1f, 1f);
    private static readonly Color Gray = new(0.5f, 0.5f, 0.5f);
    private static readonly Color Cream = new("#FFF6E2");
    private static readonly Color Gold = new("#EFC851");

    private const int TitleSize = 14;
    private const int SubtitleSize = 11;
    private const int ContentSize = 12;

    protected VBoxContainer Content { get; private set; } = null!;

    private InfoModPanel() { }

    public static InfoModPanel Create(string title, string? subtitle = null)
    {
        var panel = new InfoModPanel();
        panel.Name = "InfoModPanel";
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat();
        style.BgColor = BgColor;
        style.BorderColor = BorderColor;
        style.BorderWidthTop = BorderWidth;
        style.BorderWidthBottom = BorderWidth;
        style.BorderWidthLeft = BorderWidth;
        style.BorderWidthRight = BorderWidth;
        style.CornerRadiusTopLeft = CornerRadius;
        style.CornerRadiusTopRight = CornerRadius;
        style.CornerRadiusBottomLeft = CornerRadius;
        style.CornerRadiusBottomRight = CornerRadius;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.Name = "Content";
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);
        panel.Content = vbox;

        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.AddThemeFontSizeOverride("font_size", TitleSize);
        titleLabel.AddThemeColorOverride("font_color", White);
        vbox.AddChild(titleLabel);

        if (subtitle != null)
        {
            var subLabel = new Label();
            subLabel.Text = subtitle;
            subLabel.AddThemeFontSizeOverride("font_size", SubtitleSize);
            subLabel.AddThemeColorOverride("font_color", Gray);
            vbox.AddChild(subLabel);
        }

        return panel;
    }

    public void AddSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 4);
        Content.AddChild(sep);
    }

    public void AddRow(string label, string value, Color? labelColor = null, Color? valueColor = null)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);

        var lbl = new Label();
        lbl.Text = label;
        lbl.AddThemeFontSizeOverride("font_size", ContentSize);
        lbl.AddThemeColorOverride("font_color", labelColor ?? Cream);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(lbl);

        var val = new Label();
        val.Text = value;
        val.AddThemeFontSizeOverride("font_size", ContentSize);
        val.AddThemeColorOverride("font_color", valueColor ?? Cream);
        val.HorizontalAlignment = HorizontalAlignment.Right;
        hbox.AddChild(val);

        Content.AddChild(hbox);
    }

    public void AddLabel(string text, Color? color = null, int fontSize = ContentSize)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", color ?? Cream);
        Content.AddChild(lbl);
    }

    public void AddCustom(Control node)
    {
        Content.AddChild(node);
    }

    public void ClearContent()
    {
        for (int i = Content.GetChildCount() - 1; i >= 0; i--)
        {
            var child = Content.GetChild(i);
            if (child.HasMeta("is_header")) continue;
            child.QueueFree();
        }
    }

    public void PositionNear(Control target)
    {
        var targetRect = target.GetGlobalRect();
        var viewportSize = GetViewportRect().Size;
        var panelSize = Size;

        float x = targetRect.End.X + 8;
        float y = targetRect.Position.Y;

        if (x + panelSize.X > viewportSize.X)
            x = targetRect.Position.X - panelSize.X - 8;

        if (y + panelSize.Y > viewportSize.Y)
            y = viewportSize.Y - panelSize.Y - 8;
        if (y < 8)
            y = 8;

        GlobalPosition = new Vector2(x, y);
    }
}
