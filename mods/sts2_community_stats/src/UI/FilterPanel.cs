using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.Util;
using Godot;

namespace CommunityStats.UI;

public partial class FilterPanel : PanelContainer
{
    private static FilterPanel? _instance;

    private SpinBox? _minAscSpinBox;
    private SpinBox? _maxAscSpinBox;
    private CheckBox? _autoMatchAscCheckbox;
    private OptionButton? _versionDropdown;
    private SpinBox? _minWinRateSpinBox;
    private Label? _sampleSizeLabel;
    private Button _applyButton = null!;

    public static event Action? FilterApplied;

    public static FilterPanel Instance => _instance ??= CreatePanel();

    private static FilterPanel CreatePanel()
    {
        var panel = new FilterPanel();
        panel.Name = "CommunityStatsFilter";
        panel.Visible = false;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f),
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 10, ContentMarginBottom = 10,
            BorderColor = new Color(0.4f, 0.4f, 0.6f, 0.5f),
            BorderWidthBottom = 1, BorderWidthLeft = 1,
            BorderWidthRight = 1, BorderWidthTop = 1
        };
        panel.AddThemeStyleboxOverride("panel", style);

        panel.CustomMinimumSize = new Vector2(340, 340);
        panel.AnchorLeft = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorTop = 0.25f;
        panel.OffsetLeft = -170;
        panel.OffsetRight = 170;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        // Header with close button
        var header = new HBoxContainer();
        vbox.AddChild(header);
        var title = new Label { Text = "Community Stats — Filter" };
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(28, 28) };
        closeBtn.Pressed += () => panel.Visible = false;
        header.AddChild(closeBtn);

        // Auto-match ascension
        panel._autoMatchAscCheckbox = new CheckBox { Text = "Auto-match my Ascension" };
        panel._autoMatchAscCheckbox.ButtonPressed = ModConfig.CurrentFilter.AutoMatchAscension;
        vbox.AddChild(panel._autoMatchAscCheckbox);

        // Min/Max Ascension
        var minAscRow = CreateSpinRow("Min Ascension:", 0, 20,
            ModConfig.CurrentFilter.MinAscension ?? 0, out panel._minAscSpinBox);
        vbox.AddChild(minAscRow);
        var maxAscRow = CreateSpinRow("Max Ascension:", 0, 20,
            ModConfig.CurrentFilter.MaxAscension ?? 20, out panel._maxAscSpinBox);
        vbox.AddChild(maxAscRow);

        // Min Player Win Rate
        var wrRow = CreateSpinRow("Min Player Win%:", 0, 100,
            (int)((ModConfig.CurrentFilter.MinPlayerWinRate ?? 0) * 100), out panel._minWinRateSpinBox);
        panel._minWinRateSpinBox.Suffix = "%";
        vbox.AddChild(wrRow);

        // Game version dropdown
        var verRow = new HBoxContainer();
        verRow.AddChild(new Label { Text = "Version: ", CustomMinimumSize = new Vector2(130, 0) });
        panel._versionDropdown = new OptionButton();
        panel._versionDropdown.AddItem("Current", 0);
        panel._versionDropdown.AddItem("All Versions", 1);
        panel._versionDropdown.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        verRow.AddChild(panel._versionDropdown);
        vbox.AddChild(verRow);

        // Sample size indicator
        panel._sampleSizeLabel = new Label { Text = "" };
        panel._sampleSizeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.9f));
        panel._sampleSizeLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(panel._sampleSizeLabel);
        panel.UpdateSampleSizeLabel();

        // Apply button
        panel._applyButton = new Button { Text = "Apply" };
        panel._applyButton.Pressed += panel.OnApplyPressed;
        vbox.AddChild(panel._applyButton);

        return panel;
    }

    private static HBoxContainer CreateSpinRow(string label, int min, int max, int value, out SpinBox spinBox)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(130, 0) });
        spinBox = new SpinBox
        {
            MinValue = min, MaxValue = max, Value = value, Step = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddChild(spinBox);
        return row;
    }

    public static void Toggle()
    {
        Instance.Visible = !Instance.Visible;
    }

    public void UpdateSampleSizeLabel()
    {
        if (_sampleSizeLabel == null) return;
        var provider = StatsProvider.Instance;
        if (provider.HasBundle)
        {
            int totalRuns = provider.TotalRunCount;
            _sampleSizeLabel.Text = $"Filtered data: {totalRuns:N0} runs";
        }
        else
        {
            _sampleSizeLabel.Text = "No data loaded";
        }
    }

    private void OnApplyPressed()
    {
        Safe.Run(() =>
        {
            var filter = ModConfig.CurrentFilter;
            filter.AutoMatchAscension = _autoMatchAscCheckbox?.ButtonPressed ?? false;
            if (!filter.AutoMatchAscension)
            {
                filter.MinAscension = (int?)_minAscSpinBox?.Value;
                filter.MaxAscension = (int?)_maxAscSpinBox?.Value;
            }
            var wrPercent = (int?)_minWinRateSpinBox?.Value ?? 0;
            filter.MinPlayerWinRate = wrPercent > 0 ? wrPercent / 100f : null;
            var verIdx = _versionDropdown?.Selected ?? 0;
            filter.GameVersion = verIdx == 1 ? "all" : null;
            filter.Save();
            FilterApplied?.Invoke();
            Visible = false;
        });
    }
}
