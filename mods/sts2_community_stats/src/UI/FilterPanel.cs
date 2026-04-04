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
    private CheckBox? _uploadCheckbox;
    private OptionButton? _langDropdown;

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

        panel.CustomMinimumSize = new Vector2(340, 420);
        panel.AnchorLeft = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorTop = 0.2f;
        panel.OffsetLeft = -170;
        panel.OffsetRight = 170;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        // Header with close button
        var header = new HBoxContainer();
        vbox.AddChild(header);
        var title = new Label { Text = L.Get("settings.title") };
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(28, 28) };
        closeBtn.Pressed += () => panel.Visible = false;
        header.AddChild(closeBtn);

        // ── Data Upload Toggle ──────────────────────────────
        panel._uploadCheckbox = new CheckBox { Text = L.Get("settings.upload") };
        panel._uploadCheckbox.ButtonPressed = ModConfig.EnableUpload;
        vbox.AddChild(panel._uploadCheckbox);

        // ── Filter Settings ─────────────────────────────────

        // Auto-match ascension
        panel._autoMatchAscCheckbox = new CheckBox { Text = L.Get("settings.auto_asc") };
        panel._autoMatchAscCheckbox.ButtonPressed = ModConfig.CurrentFilter.AutoMatchAscension;
        vbox.AddChild(panel._autoMatchAscCheckbox);

        // Min/Max Ascension
        var minAscRow = CreateSpinRow(L.Get("settings.min_asc"), 0, 20,
            ModConfig.CurrentFilter.MinAscension ?? 0, out panel._minAscSpinBox);
        vbox.AddChild(minAscRow);
        var maxAscRow = CreateSpinRow(L.Get("settings.max_asc"), 0, 20,
            ModConfig.CurrentFilter.MaxAscension ?? 20, out panel._maxAscSpinBox);
        vbox.AddChild(maxAscRow);

        // Min Player Win Rate
        var wrRow = CreateSpinRow(L.Get("settings.min_wr"), 0, 100,
            (int)((ModConfig.CurrentFilter.MinPlayerWinRate ?? 0) * 100), out panel._minWinRateSpinBox);
        panel._minWinRateSpinBox.Suffix = "%";
        vbox.AddChild(wrRow);

        // Game version dropdown
        var verRow = new HBoxContainer();
        verRow.AddChild(new Label { Text = L.Get("settings.version"), CustomMinimumSize = new Vector2(130, 0) });
        panel._versionDropdown = new OptionButton();
        panel._versionDropdown.AddItem(L.Get("settings.ver_current"), 0);
        panel._versionDropdown.AddItem(L.Get("settings.ver_all"), 1);
        panel._versionDropdown.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        verRow.AddChild(panel._versionDropdown);
        vbox.AddChild(verRow);

        // ── Language Switch ─────────────────────────────────
        var langRow = new HBoxContainer();
        langRow.AddChild(new Label { Text = L.Get("settings.language"), CustomMinimumSize = new Vector2(130, 0) });
        panel._langDropdown = new OptionButton();
        panel._langDropdown.AddItem("中文", 0);
        panel._langDropdown.AddItem("English", 1);
        panel._langDropdown.Selected = L.Current == L.Lang.EN ? 1 : 0;
        panel._langDropdown.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        langRow.AddChild(panel._langDropdown);
        vbox.AddChild(langRow);

        // Sample size indicator
        panel._sampleSizeLabel = new Label { Text = "" };
        panel._sampleSizeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.9f));
        panel._sampleSizeLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(panel._sampleSizeLabel);
        panel.UpdateSampleSizeLabel();

        // Apply button
        panel._applyButton = new Button { Text = L.Get("settings.apply") };
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
        var panel = Instance;
        panel.Visible = !panel.Visible;
        if (panel.Visible)
            panel.UpdateSampleSizeLabel();
    }

    public void UpdateSampleSizeLabel()
    {
        if (_sampleSizeLabel == null) return;
        var provider = StatsProvider.Instance;
        if (provider.HasBundle)
        {
            _sampleSizeLabel.Text = string.Format(L.Get("settings.sample"),
                provider.TotalRunCount.ToString("N0"));
        }
        else
        {
            _sampleSizeLabel.Text = L.Get("settings.no_data");
        }
    }

    private void OnApplyPressed()
    {
        Safe.Run(() =>
        {
            // Save upload preference
            ModConfig.EnableUpload = _uploadCheckbox?.ButtonPressed ?? true;

            // Save language preference
            var langIdx = _langDropdown?.Selected ?? 0;
            L.Current = langIdx == 1 ? L.Lang.EN : L.Lang.CN;
            ModConfig.Language = langIdx == 1 ? "EN" : "CN";

            // Save filter settings
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

            // Save config overrides to disk
            SaveConfigOverrides();

            FilterApplied?.Invoke();
            Visible = false;
        });
    }

    private static void SaveConfigOverrides()
    {
        Safe.Run(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                api_base_url = ModConfig.ApiBaseUrl,
                enable_upload = ModConfig.EnableUpload,
                language = ModConfig.Language
            });
            File.WriteAllText(ModConfig.ConfigPath, json);
        });
    }
}
