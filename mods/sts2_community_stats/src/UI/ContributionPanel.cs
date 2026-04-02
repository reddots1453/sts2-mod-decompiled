using CommunityStats.Collection;
using Godot;

namespace CommunityStats.UI;

public partial class ContributionPanel : PanelContainer
{
    private static ContributionPanel? _instance;
    private TabContainer? _tabs;

    private enum Tab { LastCombat = 0, RunSummary = 1 }

    public static ContributionPanel Instance => _instance ??= CreatePanel();

    public new static bool IsVisible => _instance?.Visible ?? false;

    private static ContributionPanel CreatePanel()
    {
        var panel = new ContributionPanel();
        panel.Name = "CommunityStatsContribution";
        panel.Visible = false;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.1f, 0.92f),
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 12, ContentMarginBottom = 12,
            BorderColor = new Color(0.3f, 0.4f, 0.7f, 0.6f),
            BorderWidthBottom = 1, BorderWidthLeft = 1,
            BorderWidthRight = 1, BorderWidthTop = 1
        };
        panel.AddThemeStyleboxOverride("panel", style);

        panel.CustomMinimumSize = new Vector2(500, 400);
        panel.AnchorLeft = 1.0f;
        panel.AnchorRight = 1.0f;
        panel.AnchorTop = 0.1f;
        panel.AnchorBottom = 0.9f;
        panel.OffsetLeft = -520;
        panel.OffsetRight = -10;

        // Main layout
        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.AddChild(vbox);

        // Header row with title + close button
        var header = new HBoxContainer();
        vbox.AddChild(header);
        var title = new Label { Text = "Contribution Stats" };
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);

        var closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(32, 32) };
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.Pressed += () => Hide();
        header.AddChild(closeBtn);

        // Tab container
        panel._tabs = new TabContainer();
        panel._tabs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel._tabs.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(panel._tabs);

        return panel;
    }

    public static void ShowCombatResult(IReadOnlyDictionary<string, ContributionAccum>? combatData)
    {
        var panel = Instance;
        RefreshTabs(combatData);
        panel.Visible = true;
    }

    public static void Toggle()
    {
        var panel = Instance;
        if (panel.Visible)
            panel.Visible = false;
        else
        {
            RefreshTabs(CombatTracker.Instance.LastCombatData);
            panel.Visible = true;
        }
    }

    public new static void Hide()
    {
        if (_instance != null)
            _instance.Visible = false;
    }

    private static void RefreshTabs(IReadOnlyDictionary<string, ContributionAccum>? combatData)
    {
        var panel = Instance;
        var tabs = panel._tabs;
        if (tabs == null) return;

        foreach (var child in tabs.GetChildren())
            child.QueueFree();

        if (combatData != null && combatData.Count > 0)
        {
            var encId = CombatTracker.Instance.LastEncounterId;
            var title = string.IsNullOrEmpty(encId) ? "Last Combat" : $"vs {encId}";
            var scroll = WrapInScroll(ContributionChart.Create(combatData, title));
            scroll.Name = "Last Combat";
            tabs.AddChild(scroll);
        }

        var runData = RunContributionAggregator.Instance.RunTotals;
        if (runData.Count > 0)
        {
            var scroll = WrapInScroll(ContributionChart.Create(runData, "Run Summary"));
            scroll.Name = "Run Summary";
            tabs.AddChild(scroll);
        }
    }

    private static ScrollContainer WrapInScroll(Control content)
    {
        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.AddChild(content);
        return scroll;
    }
}
