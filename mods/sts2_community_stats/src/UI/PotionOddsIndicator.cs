using CommunityStats.Config;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// Compact corner indicator showing current potion drop odds.
/// Hovering expands an InfoModPanel with cumulative N-combat odds. PRD 3.9.
/// </summary>
public partial class PotionOddsIndicator : Control
{
    private static readonly Color GoldColor = new("#EFC851");
    private static readonly Color CreamColor = new("#FFF6E2");

    private Label _percentLabel = null!;
    private InfoModPanel? _hoverPanel;
    private float _currentOdds;

    public static PotionOddsIndicator Create()
    {
        var node = new PotionOddsIndicator
        {
            Name = "StatsTheSpirePotionOdds",
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

        var icon = new Label { Text = "🧪" }; // placeholder glyph; a sprite could replace this later
        icon.AddThemeFontSizeOverride("font_size", 14);
        icon.AddThemeColorOverride("font_color", GoldColor);
        hbox.AddChild(icon);

        _percentLabel = new Label { Text = "—" };
        _percentLabel.AddThemeFontSizeOverride("font_size", 12);
        _percentLabel.AddThemeColorOverride("font_color", CreamColor);
        hbox.AddChild(_percentLabel);

        MouseEntered += ShowHoverPanel;
        MouseExited += HideHoverPanel;
    }

    public void UpdateOdds(float currentValue)
    {
        _currentOdds = Mathf.Clamp(currentValue, 0f, 1f);
        _percentLabel.Text = (_currentOdds * 100f).ToString("F0") + "%";
    }

    /// <summary>
    /// Cumulative probability of at least one drop over the next N combats,
    /// assuming each subsequent combat the pity adjusts +0.1 (miss path).
    /// </summary>
    public static float CumulativeOdds(float baseOdds, int combats, bool anyElite)
    {
        // Miss-path simulation: assume each combat failed, so pity grows by +0.1.
        // Elite rooms add +0.125 effective (eliteBonus 0.25 * 0.5).
        float missProduct = 1f;
        for (int i = 0; i < combats; i++)
        {
            float p = baseOdds + 0.1f * i + (anyElite ? 0.125f : 0f);
            p = Mathf.Clamp(p, 0f, 1f);
            missProduct *= 1f - p;
        }
        return 1f - missProduct;
    }

    private void ShowHoverPanel()
    {
        if (_hoverPanel != null) return;

        _hoverPanel = InfoModPanel.Create(L.Get("potion.title"), L.Get("potion.subtitle"));
        _hoverPanel.AddSeparator();
        for (int n = 2; n <= 5; n++)
        {
            var p = CumulativeOdds(_currentOdds, n, anyElite: false);
            _hoverPanel.AddRow(string.Format(L.Get("potion.within"), n),
                (p * 100f).ToString("F1") + "%");
        }

        AddChild(_hoverPanel);
        _hoverPanel.ZIndex = 500;
        _hoverPanel.GlobalPosition = GlobalPosition + new Vector2(0, Size.Y + 4f);
    }

    private void HideHoverPanel()
    {
        if (_hoverPanel == null) return;
        _hoverPanel.QueueFree();
        _hoverPanel = null;
    }
}
