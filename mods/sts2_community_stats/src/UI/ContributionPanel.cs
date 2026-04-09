using CommunityStats.Collection;
using CommunityStats.Config;
using CommunityStats.Util;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace CommunityStats.UI;

/// <summary>
/// Combat contribution panel. Shows per-combat and per-run breakdown of damage,
/// defense, draws, energy, stars, and healing per source (card/relic/potion/power).
///
/// v2 features (PRD 3.6, 3.7, 4.5):
/// - Default anchor = RIGHT side (avoids reward selection overlay)
/// - Draggable via title bar; position persisted to config
/// - DPS row (damage / turn count)
/// - Help (?) button with tooltip
/// - Real-time refresh via CombatTracker.CombatDataUpdated event (500ms debounce)
/// </summary>
public partial class ContributionPanel : PanelContainer
{
    private static ContributionPanel? _instance;
    private TabContainer? _tabs;
    private Label? _dpsLabel;
    private HBoxContainer? _header;

    // Real-time refresh debounce (PRD 3.6)
    private static readonly Debounce _refreshDebounce = new(500);
    private static bool _refreshPending;

    public static ContributionPanel Instance => _instance ??= CreatePanel();

    public new static bool IsVisible => _instance?.Visible ?? false;

    private static ContributionPanel CreatePanel()
    {
        var panel = new ContributionPanel();
        panel.Name = "StatsTheSpireContribution";
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

        // Narrower than before (PRD 4.5) — 460 instead of 500
        panel.CustomMinimumSize = new Vector2(460, 400);

        // Default: right side, avoiding left reward selection area (PRD 4.5)
        panel.AnchorLeft = 1.0f;
        panel.AnchorRight = 1.0f;
        panel.AnchorTop = 0.1f;
        panel.AnchorBottom = 0.9f;
        panel.OffsetLeft = -470;
        panel.OffsetRight = -10;

        // Main layout
        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.AddChild(vbox);

        // Header row with title + help + close button (draggable handle)
        var header = new HBoxContainer();
        header.MouseFilter = MouseFilterEnum.Stop; // ensure it receives drag input
        vbox.AddChild(header);
        panel._header = header;

        var title = new Label { Text = L.Get("contrib.title") };
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.MouseFilter = MouseFilterEnum.Ignore; // let header receive clicks
        header.AddChild(title);

        // Help (?) button (PRD 4.5)
        var helpBtn = new Button
        {
            Text = "?",
            CustomMinimumSize = new Vector2(32, 32),
            TooltipText = L.Get("contrib.help_body")
        };
        helpBtn.AddThemeFontSizeOverride("font_size", 14);
        header.AddChild(helpBtn);

        var closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(32, 32) };
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        closeBtn.Pressed += () => Hide();
        header.AddChild(closeBtn);

        // DPS row (PRD 3.7) — below header
        var dpsRow = new HBoxContainer();
        vbox.AddChild(dpsRow);
        panel._dpsLabel = new Label { Text = $"{L.Get("contrib.dps")} 0.0" };
        panel._dpsLabel.AddThemeColorOverride("font_color", new Color("#FFF6E2"));
        panel._dpsLabel.AddThemeFontSizeOverride("font_size", 12);
        dpsRow.AddChild(panel._dpsLabel);

        // Tab container
        panel._tabs = new TabContainer();
        panel._tabs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel._tabs.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(panel._tabs);

        // Enable drag via title bar (PRD 4.5)
        DraggablePanel.Attach(panel, header);

        // Restore saved position if present
        panel.Ready += () => DraggablePanel.RestorePosition(panel, panel.GlobalPosition);

        // Subscribe to real-time updates (PRD 3.6)
        CombatTracker.Instance.CombatDataUpdated += panel.OnCombatDataUpdated;

        return panel;
    }

    /// <summary>
    /// Handler for CombatTracker.CombatDataUpdated. Uses a 500ms debounce and
    /// dispatches the UI refresh to the main thread via CallDeferred.
    /// </summary>
    private void OnCombatDataUpdated()
    {
        if (!Visible) return;
        if (!_refreshDebounce.CanFire())
        {
            // Debounce suppressed; schedule a trailing refresh on next fire
            if (!_refreshPending)
            {
                _refreshPending = true;
                CallDeferred(nameof(DeferredTrailingRefresh));
            }
            return;
        }
        CallDeferred(nameof(DeferredRefresh));
    }

    private void DeferredRefresh()
    {
        Safe.Run(() =>
        {
            RefreshLive();
        });
    }

    private void DeferredTrailingRefresh()
    {
        // Small delay to coalesce bursts (runs on main thread)
        _refreshPending = false;
        Safe.Run(() => RefreshLive());
    }

    private void RefreshLive()
    {
        var data = CombatTracker.Instance.GetCurrentCombatData();
        RefreshTabs(data);
        UpdateDps();
    }

    private void UpdateDps()
    {
        if (_dpsLabel == null) return;
        var tracker = CombatTracker.Instance;
        var turns = Math.Max(1, tracker.TurnCount);
        var dps = (float)tracker.TotalDamageDealt / turns;
        _dpsLabel.Text = $"{L.Get("contrib.dps")} {dps:F1}";
    }

    public static void ShowCombatResult(IReadOnlyDictionary<string, ContributionAccum>? combatData)
    {
        if (!ModConfig.Toggles.ContributionPanel) return;

        var panel = Instance;
        RefreshTabs(combatData);
        panel.UpdateDps();
        panel.Visible = true;
        panel.ApplyAvoidanceOffset();
    }

    public static void Toggle()
    {
        if (!ModConfig.Toggles.ContributionPanel) return;

        var panel = Instance;
        if (panel.Visible)
            panel.Visible = false;
        else
        {
            RefreshTabs(CombatTracker.Instance.LastCombatData);
            panel.UpdateDps();
            panel.Visible = true;
        }
    }

    public new static void Hide()
    {
        if (_instance != null)
            _instance.Visible = false;
    }

    /// <summary>
    /// When showing after combat end, if the rewards screen is already visible,
    /// shift the panel slightly to minimize overlap. Users can still drag freely.
    /// </summary>
    private void ApplyAvoidanceOffset()
    {
        Safe.Run(() =>
        {
            // Only apply if the user hasn't manually placed the panel yet
            if (ModConfig.PanelPositionX.HasValue) return;

            var root = GetTree()?.Root;
            if (root == null) return;

            // Best-effort search for a rewards screen node on the tree
            var rewardsVisible = FindRewardsScreen(root);
            if (rewardsVisible)
            {
                // Nudge panel upward slightly
                AnchorTop = 0.02f;
                AnchorBottom = 0.82f;
            }
        });
    }

    private static bool FindRewardsScreen(Node node)
    {
        if (node is Control ctrl && ctrl.Visible &&
            (node.Name.ToString().Contains("Reward") || node.Name.ToString().Contains("reward")))
            return true;
        foreach (var c in node.GetChildren())
            if (FindRewardsScreen(c)) return true;
        return false;
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
            // Fall back to current combat's encounter via tracker if "last" is empty
            if (string.IsNullOrEmpty(encId))
                encId = "";

            var encName = encId;
            if (!string.IsNullOrEmpty(encId))
            {
                try
                {
                    var loc = new LocString("encounters", encId + ".title");
                    var localized = loc.GetFormattedText();
                    if (!string.IsNullOrEmpty(localized) && localized != encId + ".title")
                        encName = localized;
                }
                catch { /* keep raw ID */ }
            }
            var title = string.IsNullOrEmpty(encId)
                ? L.Get("contrib.last_combat")
                : $"{L.Get("contrib.vs")} {encName}";
            var scroll = WrapInScroll(ContributionChart.Create(combatData, title));
            scroll.Name = L.Get("contrib.last_combat");
            tabs.AddChild(scroll);
        }

        var runData = RunContributionAggregator.Instance.RunTotals;
        if (runData.Count > 0)
        {
            var scroll = WrapInScroll(ContributionChart.Create(runData, L.Get("contrib.run_summary"), isRunLevel: true));
            scroll.Name = L.Get("contrib.run_summary");
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
