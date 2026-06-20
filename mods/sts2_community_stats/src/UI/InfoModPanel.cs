﻿﻿﻿using CommunityStats.Config;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// Base class for all InfoMod-style hover panels.
/// Dark background, thin border, title/subtitle/separator/content rows.
/// Used by: UnknownRoomPanel, PotionOddsPanel, CardDropPanel, ShopPricePanel.
///
/// Style (from UI_STYLE_GUIDE.md §3.3):
///   bg: rgba(0.08, 0.08, 0.12, 0.95)
///   border: 1px rgba(0.3, 0.4, 0.6, 0.4)
///   corner radius: 6px
///   title: white 14px, subtitle: gray 11px
/// </summary>
public class InfoModPanel : PanelContainer
{
    private static readonly Color BgColor = new(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color BorderColor = new(0.3f, 0.4f, 0.6f, 0.4f);
    private const int CornerRadius = 6;
    private const int BorderWidth = 1;

    // StsColors references
    private static readonly Color White = new(1f, 1f, 1f);
    private static readonly Color Gray = new(0.5f, 0.5f, 0.5f);
    private static readonly Color Cream = new("#FFF6E2");
    private static readonly Color Gold = new("#EFC851");

    // Font sizes
    private int _titleSize = 14;
    private int _subtitleSize = 11;
    private int _contentSize = 12;

    protected VBoxContainer Content { get; private set; } = null!;

    private InfoModPanel() { }

    /// <summary>
    /// Create a new InfoMod-style panel with title and optional subtitle.
    /// </summary>
    public static InfoModPanel Create(string title, string? subtitle = null, int titleSize = 14, int subtitleSize = 11, int contentSize = 12)
    {
        var panel = new InfoModPanel();
        panel.Name = "InfoModPanel";
        panel._titleSize = titleSize;
        panel._subtitleSize = subtitleSize;
        panel._contentSize = contentSize;
        panel.MouseFilter = MouseFilterEnum.Ignore;

        // StyleBoxFlat for the panel background
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

        // VBox for content
        var vbox = new VBoxContainer();
        vbox.Name = "Content";
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);
        panel.Content = vbox;

        // Title label
        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.AddThemeFontSizeOverride("font_size", _titleSize);
        titleLabel.AddThemeColorOverride("font_color", White);
        vbox.AddChild(titleLabel);

        // Optional subtitle
        if (subtitle != null)
        {
            var subLabel = new Label();
            subLabel.Text = subtitle;
            subLabel.AddThemeFontSizeOverride("font_size", _subtitleSize);
            subLabel.AddThemeColorOverride("font_color", Gray);
            vbox.AddChild(subLabel);
        }

        return panel;
    }

    /// <summary>
    /// Add a horizontal separator line.
    /// </summary>
    public void AddSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 4);
        Content.AddChild(sep);
    }

    /// <summary>
    /// Add a label + value row.
    /// </summary>
    public void AddRow(string label, string value, Color? labelColor = null, Color? valueColor = null)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);

        var lbl = new Label();
        lbl.Text = label;
        lbl.AddThemeFontSizeOverride("font_size", _contentSize);
        lbl.AddThemeColorOverride("font_color", labelColor ?? Cream);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(lbl);

        var val = new Label();
        val.Text = value;
        val.AddThemeFontSizeOverride("font_size", _contentSize);
        val.AddThemeColorOverride("font_color", valueColor ?? Cream);
        val.HorizontalAlignment = HorizontalAlignment.Right;
        hbox.AddChild(val);

        Content.AddChild(hbox);
    }

    /// <summary>
    /// Add a single label row (no value column).
    /// </summary>
    public void AddLabel(string text, Color? color = null, int? fontSize = null)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", fontSize ?? _contentSize);
        lbl.AddThemeColorOverride("font_color", color ?? Cream);
        Content.AddChild(lbl);
    }

    /// <summary>
    /// Add a raw Control as a content row.
    /// </summary>
    public void AddCustom(Control node)
    {
        Content.AddChild(node);
    }

    /// <summary>
    /// Clear all content rows (keeps title and subtitle).
    /// Then rebuild from scratch — simpler than partial updates.
    /// </summary>
    public void ClearContent()
    {
        // Title is child 0, subtitle (if any) is child 1
        // Remove from the end to avoid index shifting
        for (int i = Content.GetChildCount() - 1; i >= 0; i--)
        {
            var child = Content.GetChild(i);
            if (child.HasMeta("is_header")) continue;
            child.QueueFree();
        }
    }

    /// <summary>
    /// Position this panel near a target control, staying within the viewport.
    /// </summary>
    public void PositionNear(Control target)
    {
        // Get target's global rect
        var targetRect = target.GetGlobalRect();
        var viewportSize = GetViewportRect().Size;
        var panelSize = Size;

        // Try right of target first
        float x = targetRect.End.X + 8;
        float y = targetRect.Position.Y;

        // If overflows right, try left
        if (x + panelSize.X > viewportSize.X)
            x = targetRect.Position.X - panelSize.X - 8;

        // Clamp vertical
        if (y + panelSize.Y > viewportSize.Y)
            y = viewportSize.Y - panelSize.Y - 8;
        if (y < 8)
            y = 8;

        GlobalPosition = new Vector2(x, y);
    }
}
