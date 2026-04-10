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

    // Round 9: percentage label removed per user feedback (was always
    // showing "0%" / not meaningful at a glance). The hover panel now
    // carries all the rate detail.
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
            CustomMinimumSize = new Vector2(48, 48),
        };
        node.BuildUi();
        return node;
    }

    private void BuildUi()
    {
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 0);
        vbox.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(vbox);

        // Native reward-screen "add a card" icon — round 7 PRD §3.17.
        // Round 9: enlarged 10% (40 → 44) per user feedback ("原图标较小").
        Control iconNode;
        var icon = LoadNativeTexture(
            "ui/reward_screen/reward_icon_card.png",
            "ui/reward_screen/reward_icon_card.tres");
        if (icon != null)
        {
            iconNode = new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(44, 44),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }
        else
        {
            var lbl = new Label { Text = "🃏" };
            lbl.AddThemeFontSizeOverride("font_size", 28);
            lbl.AddThemeColorOverride("font_color", GoldColor);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            iconNode = lbl;
        }
        vbox.AddChild(iconNode);

        // Round 9: removed _rareLabel (showed redundant "0%" below icon).

        MouseEntered += ShowHoverPanel;
        MouseExited += HideHoverPanel;
    }

    private static Texture2D? LoadNativeTexture(params string[] candidates)
    {
        foreach (var path in candidates)
        {
            try
            {
                var resolved = MegaCrit.Sts2.Core.Helpers.ImageHelper.GetImagePath(path);
                if (Godot.ResourceLoader.Exists(resolved))
                    return Godot.ResourceLoader.Load<Texture2D>(resolved, null, ResourceLoader.CacheMode.Reuse);
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Update using the current pity offset (CardRarityOdds.CurrentValue).
    /// </summary>
    public void UpdateOffset(float offset)
    {
        // Round 9: percentage label removed; offset is still cached so the
        // hover panel can compute up-to-date Regular/Elite rates.
        _currentOffset = offset;
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
