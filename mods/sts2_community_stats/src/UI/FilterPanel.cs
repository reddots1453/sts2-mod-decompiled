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
    private CheckBox? _myDataCheckbox;
    private OptionButton? _langDropdown;
    private OptionButton? _characterDropdown;
    private readonly Dictionary<string, CheckBox> _toggleCheckboxes = new();

    // PRD §3.18 — order matches the dropdown items: auto, all, then 5 characters.
    private static readonly string[] _characterModes = new[]
    {
        "auto", "all", "IRONCLAD", "SILENT", "DEFECT", "NECROBINDER", "REGENT",
    };

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

        panel.CustomMinimumSize = new Vector2(360, 600);
        panel.AnchorLeft = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorTop = 0.1f;
        panel.OffsetLeft = -180;
        panel.OffsetRight = 180;

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

        // ── My-Data Filter (PRD §3.13) ──────────────────────
        panel._myDataCheckbox = new CheckBox { Text = L.Get("settings.my_data") };
        panel._myDataCheckbox.ButtonPressed = ModConfig.CurrentFilter.MyDataOnly;
        vbox.AddChild(panel._myDataCheckbox);

        // ── Character Filter (PRD §3.18) ────────────────────
        var charRow = new HBoxContainer();
        charRow.AddChild(new Label
        {
            Text = L.Get("settings.character"),
            CustomMinimumSize = new Vector2(130, 0),
        });
        panel._characterDropdown = new OptionButton();
        panel._characterDropdown.AddItem(L.Get("settings.char_auto"), 0);
        panel._characterDropdown.AddItem(L.Get("settings.char_all"), 1);
        panel._characterDropdown.AddItem(L.Get("char.IRONCLAD"), 2);
        panel._characterDropdown.AddItem(L.Get("char.SILENT"), 3);
        panel._characterDropdown.AddItem(L.Get("char.DEFECT"), 4);
        panel._characterDropdown.AddItem(L.Get("char.NECROBINDER"), 5);
        panel._characterDropdown.AddItem(L.Get("char.REGENT"), 6);
        panel._characterDropdown.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // Restore saved selection from config (default "auto" → index 0)
        var savedMode = ModConfig.CurrentFilter.CharacterFilterMode ?? "auto";
        var savedIdx = Array.IndexOf(_characterModes, savedMode);
        panel._characterDropdown.Selected = savedIdx >= 0 ? savedIdx : 0;
        charRow.AddChild(panel._characterDropdown);
        vbox.AddChild(charRow);

        // ── Filter Settings ─────────────────────────────────

        // Auto-match ascension
        panel._autoMatchAscCheckbox = new CheckBox { Text = L.Get("settings.auto_asc") };
        panel._autoMatchAscCheckbox.ButtonPressed = ModConfig.CurrentFilter.AutoMatchAscension;
        vbox.AddChild(panel._autoMatchAscCheckbox);

        // Min/Max Ascension. Round 9 round 49: capped at 10 (the game ladder
        // top); values >10 caused data-load exceptions in the backend.
        var minAscRow = CreateSpinRow(L.Get("settings.min_asc"), 0, 10,
            Math.Clamp(ModConfig.CurrentFilter.MinAscension ?? 0, 0, 10), out panel._minAscSpinBox);
        vbox.AddChild(minAscRow);
        var maxAscRow = CreateSpinRow(L.Get("settings.max_asc"), 0, 10,
            Math.Clamp(ModConfig.CurrentFilter.MaxAscension ?? 10, 0, 10), out panel._maxAscSpinBox);
        vbox.AddChild(maxAscRow);

        // Round 9 round 49: enforce min ≤ max by clamping the other side
        // whenever either box changes.
        panel._minAscSpinBox.ValueChanged += v =>
        {
            if (panel._maxAscSpinBox != null && v > panel._maxAscSpinBox.Value)
                panel._maxAscSpinBox.Value = v;
        };
        panel._maxAscSpinBox.ValueChanged += v =>
        {
            if (panel._minAscSpinBox != null && v < panel._minAscSpinBox.Value)
                panel._minAscSpinBox.Value = v;
        };

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

        // ── Feature Toggles (PRD §3.14) ─────────────────────
        var togglesHeader = new Label { Text = L.Get("settings.toggles_title") };
        togglesHeader.AddThemeFontSizeOverride("font_size", 14);
        togglesHeader.AddThemeColorOverride("font_color", new Color("#EFC851"));
        vbox.AddChild(togglesHeader);

        var togglesScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 180),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        vbox.AddChild(togglesScroll);

        var togglesVbox = new VBoxContainer();
        togglesVbox.AddThemeConstantOverride("separation", 2);
        togglesVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        togglesScroll.AddChild(togglesVbox);

        foreach (var (key, labelKey) in FeatureToggles.ToggleDefinitions)
        {
            var cb = new CheckBox
            {
                Text = L.Get(labelKey),
                ButtonPressed = ModConfig.Toggles.GetByName(key),
            };
            cb.AddThemeFontSizeOverride("font_size", 12);
            togglesVbox.AddChild(cb);
            panel._toggleCheckboxes[key] = cb;
        }

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
            filter.MyDataOnly = _myDataCheckbox?.ButtonPressed ?? false;

            // PRD §3.18 — persist the user's character preference (mode), not
            // the resolved character ID. The actual character used for queries
            // is computed at request time via FilterSettings.ResolveCharacter().
            var charIdx = _characterDropdown?.Selected ?? 0;
            if (charIdx < 0 || charIdx >= _characterModes.Length) charIdx = 0;
            filter.CharacterFilterMode = _characterModes[charIdx];

            filter.Save();

            // Persist feature toggle values
            foreach (var (key, cb) in _toggleCheckboxes)
            {
                ModConfig.Toggles.SetByName(key, cb.ButtonPressed);
            }

            // Save config overrides to disk
            SaveConfigOverrides();

            FilterApplied?.Invoke();
            Visible = false;
        });
    }

    private static void SaveConfigOverrides()
    {
        // ModConfig.SaveSettings() writes the canonical config including
        // feature_toggles, language, upload prefs, panel position, etc.
        // It must be called whenever any of those mutate from this panel.
        Safe.Run(() => ModConfig.SaveSettings());
    }
}
