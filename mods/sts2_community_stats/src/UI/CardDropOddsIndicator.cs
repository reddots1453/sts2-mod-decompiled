using CommunityStats.Config;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// Compact corner indicator showing the current rare-card drop chance.
/// Hovering expands an InfoModPanel with a Regular × Elite rarity table. PRD 3.17.
/// </summary>
public partial class CardDropOddsIndicator : Control
{
    private static readonly Color GoldColor = new("#EFC851");
    private static readonly Color AquaColor = new(0.16f, 0.92f, 0.75f);
    private static readonly Color CreamColor = new("#FFF6E2");

    private Label _rareLabel = null!;
    private InfoModPanel? _hoverPanel;
    private float _currentOffset;

    // STS2 base rates (non-ascension Scarcity values, regular/elite encounters)
    private const float RegularRareBase = 0.03f;
    private const float RegularUncommonBase = 0.37f;
    private const float EliteRareBase = 0.10f;
    private const float EliteUncommonBase = 0.40f;

    public static CardDropOddsIndicator Create()
    {
        var node = new CardDropOddsIndicator
        {
            Name = "StatsTheSpireCardDropOdds",
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(80, 22),
        };
        node.BuildUi();
        return node;
    }

    private void BuildUi()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        AddChild(hbox);

        var icon = new Label { Text = "🃏" };
        icon.AddThemeFontSizeOverride("font_size", 14);
        icon.AddThemeColorOverride("font_color", GoldColor);
        hbox.AddChild(icon);

        _rareLabel = new Label { Text = "—" };
        _rareLabel.AddThemeFontSizeOverride("font_size", 12);
        _rareLabel.AddThemeColorOverride("font_color", CreamColor);
        hbox.AddChild(_rareLabel);

        MouseEntered += ShowHoverPanel;
        MouseExited += HideHoverPanel;
    }

    /// <summary>
    /// Update using the current pity offset (CardRarityOdds.CurrentValue).
    /// </summary>
    public void UpdateOffset(float offset)
    {
        _currentOffset = offset;
        var effectiveRare = Mathf.Clamp(RegularRareBase + offset, 0f, 1f);
        _rareLabel.Text = (effectiveRare * 100f).ToString("F1") + "%";
    }

    private void ShowHoverPanel()
    {
        if (_hoverPanel != null) return;

        _hoverPanel = InfoModPanel.Create(L.Get("carddrop.title"), L.Get("carddrop.subtitle"));
        _hoverPanel.AddSeparator();

        // Header row (rarity | regular | elite)
        var regularRare = Mathf.Clamp(RegularRareBase + _currentOffset, 0f, 1f);
        var regularUncommon = Mathf.Clamp(RegularUncommonBase, 0f, 1f);
        var regularCommon = Mathf.Clamp(1f - regularRare - regularUncommon, 0f, 1f);
        var eliteRare = Mathf.Clamp(EliteRareBase + _currentOffset, 0f, 1f);
        var eliteUncommon = Mathf.Clamp(EliteUncommonBase, 0f, 1f);
        var eliteCommon = Mathf.Clamp(1f - eliteRare - eliteUncommon, 0f, 1f);

        AddTableRow(L.Get("carddrop.rare"), regularRare, eliteRare, GoldColor);
        AddTableRow(L.Get("carddrop.uncommon"), regularUncommon, eliteUncommon, AquaColor);
        AddTableRow(L.Get("carddrop.common"), regularCommon, eliteCommon, CreamColor);

        AddChild(_hoverPanel);
        _hoverPanel.ZIndex = 500;
        _hoverPanel.GlobalPosition = GlobalPosition + new Vector2(0, Size.Y + 4f);
    }

    private void AddTableRow(string rarityLabel, float regular, float elite, Color color)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);

        var name = new Label { Text = rarityLabel };
        name.AddThemeFontSizeOverride("font_size", 12);
        name.AddThemeColorOverride("font_color", color);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(name);

        var reg = new Label { Text = (regular * 100f).ToString("F1") + "%" };
        reg.AddThemeFontSizeOverride("font_size", 12);
        reg.AddThemeColorOverride("font_color", CreamColor);
        reg.CustomMinimumSize = new Vector2(60, 0);
        reg.HorizontalAlignment = HorizontalAlignment.Right;
        hbox.AddChild(reg);

        var eli = new Label { Text = (elite * 100f).ToString("F1") + "%" };
        eli.AddThemeFontSizeOverride("font_size", 12);
        eli.AddThemeColorOverride("font_color", CreamColor);
        eli.CustomMinimumSize = new Vector2(60, 0);
        eli.HorizontalAlignment = HorizontalAlignment.Right;
        hbox.AddChild(eli);

        _hoverPanel!.AddCustom(hbox);
    }

    private void HideHoverPanel()
    {
        if (_hoverPanel == null) return;
        _hoverPanel.QueueFree();
        _hoverPanel = null;
    }
}
