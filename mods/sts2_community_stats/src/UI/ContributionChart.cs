using CommunityStats.Collection;
using CommunityStats.Config;
using CommunityStats.Util;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace CommunityStats.UI;

/// <summary>
/// Horizontal bar chart showing per-source (card/relic/potion) contributions.
/// Sections: Damage, Defense, Card Draw, Energy Gained, Healing summary.
/// Supports sub-bars for generated/transformed cards displayed under their origin.
/// </summary>
public partial class ContributionChart : VBoxContainer
{
    private static readonly Color CardBarColor = new(0.3f, 0.5f, 0.9f);     // blue
    private static readonly Color RelicBarColor = new(0.9f, 0.75f, 0.2f);   // gold
    private static readonly Color PotionBarColor = new(0.2f, 0.85f, 0.6f);  // teal
    private static readonly Color AttrBarColor = new(0.5f, 0.7f, 1.0f);     // light blue (attributed)
    private static readonly Color ModifierBarColor = new(0.7f, 0.5f, 0.9f); // purple (modifier bonus)
    private static readonly Color MitigateBarColor = new(0.4f, 0.8f, 0.5f); // green (debuff/buff mitigation)
    private static readonly Color StrReduceBarColor = new(0.9f, 0.5f, 0.2f);// orange (enemy str reduction)
    private static readonly Color StarBarColor = new(1.0f, 0.85f, 0.3f);    // gold/yellow for stars
    private static readonly Color SelfDmgBarColor = new(0.9f, 0.25f, 0.2f);  // red for self-damage
    private static readonly Color HealBarColor = new(0.2f, 0.9f, 0.3f);     // bright green for healing
    private static readonly Color OstyBarColor = new(0.6f, 0.9f, 0.5f);     // green for Osty
    private static readonly Color SubBarColor = new(0.4f, 0.6f, 0.8f, 0.7f);// dimmed blue for sub-bars
    private static readonly Color HeaderColor = new(1f, 1f, 1f);
    private static readonly Color PlayCountColor = new(0.6f, 0.6f, 0.7f);
    private const int MaxBarsPerSection = 10;
    private const float BarHeight = 20f;
    private const float MaxBarWidth = 260f;

    public enum Category { Damage, Defense, Draw, Energy, Stars, Healing }

    public static ContributionChart Create(
        IReadOnlyDictionary<string, ContributionAccum> data,
        string title,
        bool isRunLevel = false)
    {
        var chart = new ContributionChart();
        chart.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        chart.AddThemeConstantOverride("separation", 4);

        // Title
        var titleLabel = new Label { Text = title };
        titleLabel.AddThemeColorOverride("font_color", HeaderColor);
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        chart.AddChild(titleLabel);

        chart.AddSeparator();
        chart.BuildSection(L.Get("chart.damage"), data, Category.Damage);
        chart.AddSeparator();
        chart.BuildSection(L.Get("chart.defense"), data, Category.Defense);
        chart.AddSeparator();
        chart.BuildSection(L.Get("chart.draw"), data, Category.Draw);
        chart.AddSeparator();
        chart.BuildSection(L.Get("chart.energy"), data, Category.Energy);
        chart.AddSeparator();
        chart.BuildSection(L.Get("chart.stars"), data, Category.Stars);

        // Healing section: only show in run-level summary
        if (isRunLevel)
        {
            chart.AddSeparator();
            chart.BuildSection(L.Get("chart.healing"), data, Category.Healing);
        }

        return chart;
    }

    private void BuildSection(string header, IReadOnlyDictionary<string, ContributionAccum> data, Category category)
    {
        var headerLabel = new Label { Text = header };
        headerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        headerLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(headerLabel);

        // Separate top-level entries and sub-entries (cards with OriginSourceId)
        var topLevel = new List<(ContributionAccum accum, int value)>();
        var subEntries = new Dictionary<string, List<(ContributionAccum accum, int value)>>();

        foreach (var accum in data.Values)
        {
            int value = category switch
            {
                Category.Damage => accum.TotalDamage,
                Category.Defense => accum.TotalDefense,
                Category.Draw => accum.CardsDrawn,
                Category.Energy => accum.EnergyGained,
                Category.Stars => accum.StarsContribution,
                Category.Healing => accum.HpHealed,
                _ => 0
            };

            if (accum.OriginSourceId != null)
            {
                if (value <= 0 && !(category == Category.Defense && accum.SelfDamage > 0)) continue;
                if (!subEntries.TryGetValue(accum.OriginSourceId, out var list))
                {
                    list = new List<(ContributionAccum, int)>();
                    subEntries[accum.OriginSourceId] = list;
                }
                list.Add((accum, value));
            }
            else
            {
                // Keep entries with value > 0, or entries that might be parents (value == 0 checked later)
                topLevel.Add((accum, value));
            }
        }

        // Include parent entries that have sub-entries even if own value is 0
        // Remove top-level entries with 0 value AND no sub-entries
        // For Defense: keep entries with SelfDamage (they have negative value)
        topLevel.RemoveAll(x => x.value <= 0
            && !subEntries.ContainsKey(x.accum.SourceId)
            && !(category == Category.Defense && x.accum.SelfDamage > 0));

        // Also add sub-entry values to their parent's total for sorting (use abs for negative defense)
        var sorted = topLevel
            .OrderByDescending(x => Math.Abs(x.value) + (subEntries.TryGetValue(x.accum.SourceId, out var subs)
                ? subs.Sum(s => Math.Abs(s.value)) : 0))
            .Take(MaxBarsPerSection)
            .ToList();

        // Also check if there are orphaned sub-entries whose parent is not in topLevel
        // (shouldn't happen normally, but handle gracefully)
        var topLevelIds = new HashSet<string>(sorted.Select(x => x.accum.SourceId));
        foreach (var (parentId, subs) in subEntries)
        {
            if (!topLevelIds.Contains(parentId) && data.TryGetValue(parentId, out var parentAccum))
            {
                sorted.Add((parentAccum, 0));
                topLevelIds.Add(parentId);
            }
        }

        if (sorted.Count == 0)
        {
            var none = new Label { Text = L.Get("chart.none") };
            none.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            none.AddThemeFontSizeOverride("font_size", 12);
            AddChild(none);
            return;
        }

        var maxVal = sorted.Max(x => Math.Abs(x.value) + (subEntries.TryGetValue(x.accum.SourceId, out var s)
            ? s.Sum(sub => Math.Abs(sub.value)) : 0));
        var totalVal = data.Values.Sum(a => category switch
        {
            Category.Damage => a.TotalDamage,
            Category.Defense => a.TotalDefense,
            Category.Draw => a.CardsDrawn,
            Category.Energy => a.EnergyGained,
            Category.Stars => a.StarsContribution,
            _ => 0
        });

        foreach (var (accum, value) in sorted)
        {
            BuildEntryRow(accum, value, maxVal, totalVal, category, false);

            // Render sub-bars if this entry has children
            if (subEntries.TryGetValue(accum.SourceId, out var subs))
            {
                foreach (var (subAccum, subValue) in subs.OrderByDescending(x => x.value))
                {
                    BuildEntryRow(subAccum, subValue, maxVal, totalVal, category, true);
                }
            }
        }
    }

    private void BuildEntryRow(ContributionAccum accum, int value, int maxVal, int totalVal,
        Category category, bool isSub)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);

        // Source name
        var prefix = isSub ? "  \u2514 " : accum.SourceType switch
        {
            "relic" => "[R] ",
            "potion" => "[P] ",
            "osty" => "[O] ",
            "rest" or "merchant" or "floor_regen" => "",
            "event" => L.Get("source.event_prefix"),
            _ => ""
        };
        var displayName = GetLocalizedName(accum.SourceId, accum.SourceType);
        var nameLabel = new Label
        {
            Text = $"{prefix}{displayName}",
            CustomMinimumSize = new Vector2(isSub ? 100 : 110, BarHeight)
        };
        nameLabel.AddThemeColorOverride("font_color",
            isSub ? new Color(0.7f, 0.7f, 0.8f) : new Color(0.9f, 0.9f, 0.9f));
        nameLabel.AddThemeFontSizeOverride("font_size", isSub ? 11 : 12);
        nameLabel.ClipText = true;
        row.AddChild(nameLabel);

        // Times played (for cards)
        if (accum.SourceType == "card" && accum.TimesPlayed > 0)
        {
            var playLabel = new Label
            {
                Text = $"x{accum.TimesPlayed}",
                CustomMinimumSize = new Vector2(30, BarHeight)
            };
            playLabel.AddThemeColorOverride("font_color", PlayCountColor);
            playLabel.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(playLabel);
        }
        else
        {
            row.AddChild(new Control { CustomMinimumSize = new Vector2(30, BarHeight) });
        }

        // Bar container
        var barContainer = new Control { CustomMinimumSize = new Vector2(MaxBarWidth, BarHeight) };

        if (category == Category.Damage)
            BuildDamageBar(barContainer, accum, maxVal, isSub);
        else if (category == Category.Defense)
            BuildDefenseBar(barContainer, accum, maxVal, isSub);
        else if (category == Category.Stars)
            BuildStarBar(barContainer, accum, value, maxVal, isSub);
        else if (category == Category.Healing)
            BuildHealBar(barContainer, accum, value, maxVal, isSub);
        else
            BuildSimpleBar(barContainer, accum, value, maxVal, isSub);

        row.AddChild(barContainer);

        // Value + percentage
        var pct = totalVal > 0 ? (float)value / totalVal * 100 : 0;
        var valText = value < 0 ? $"{value}" : $"{value}  ({pct:F0}%)";
        var valLabel = new Label { Text = valText };
        var valColor = value < 0
            ? SelfDmgBarColor  // red for negative
            : isSub ? new Color(0.5f, 0.5f, 0.6f) : new Color(0.7f, 0.7f, 0.7f);
        valLabel.AddThemeColorOverride("font_color", valColor);
        valLabel.AddThemeFontSizeOverride("font_size", isSub ? 11 : 12);
        row.AddChild(valLabel);

        AddChild(row);
    }

    private static Color GetSourceColor(string sourceType) => sourceType switch
    {
        "relic" => RelicBarColor,
        "potion" => PotionBarColor,
        "osty" => OstyBarColor,
        _ => CardBarColor
    };

    private static void BuildDamageBar(Control container, ContributionAccum accum, int maxVal, bool isSub)
    {
        var baseColor = isSub ? SubBarColor : GetSourceColor(accum.SourceType);
        float offset = 0;

        // Direct damage
        if (accum.DirectDamage > 0)
        {
            var w = maxVal > 0 ? (float)accum.DirectDamage / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, baseColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        // Attributed damage (poison, vuln bonus, etc.)
        if (accum.AttributedDamage > 0)
        {
            var w = maxVal > 0 ? (float)accum.AttributedDamage / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, AttrBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        // Modifier damage (Strength, etc.)
        if (accum.ModifierDamage > 0)
        {
            var w = maxVal > 0 ? (float)accum.ModifierDamage / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, ModifierBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        // Upgrade damage bonus
        if (accum.UpgradeDamage > 0)
        {
            var w = maxVal > 0 ? (float)accum.UpgradeDamage / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, new Color(0.9f, 0.6f, 0.2f)); // orange for upgrade
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
        }

        if (accum.TotalDamage <= 0)
            container.AddChild(CreateBar(0, BarHeight, baseColor));
    }

    private static void BuildDefenseBar(Control container, ContributionAccum accum, int maxVal, bool isSub)
    {
        var baseColor = isSub ? SubBarColor : GetSourceColor(accum.SourceType);
        float offset = 0;

        if (accum.EffectiveBlock > 0)
        {
            var w = maxVal > 0 ? (float)accum.EffectiveBlock / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, baseColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        if (accum.ModifierBlock > 0)
        {
            var w = maxVal > 0 ? (float)accum.ModifierBlock / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, ModifierBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        if (accum.MitigatedByDebuff > 0)
        {
            var w = maxVal > 0 ? (float)accum.MitigatedByDebuff / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, MitigateBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        if (accum.MitigatedByBuff > 0)
        {
            var w = maxVal > 0 ? (float)accum.MitigatedByBuff / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, AttrBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        if (accum.MitigatedByStrReduction > 0)
        {
            var w = maxVal > 0 ? (float)accum.MitigatedByStrReduction / maxVal * MaxBarWidth : 0;
            var bar = CreateBar(w, BarHeight, StrReduceBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
            offset += w;
        }

        // Self-damage: red bar extending to the left (visually, we just show a red bar)
        if (accum.SelfDamage > 0)
        {
            var w = maxVal > 0 ? (float)accum.SelfDamage / maxVal * MaxBarWidth : MaxBarWidth * 0.2f;
            var bar = CreateBar(w, BarHeight, SelfDmgBarColor);
            bar.Position = new Vector2(offset, 0);
            container.AddChild(bar);
        }

        if (accum.TotalDefense <= 0 && accum.SelfDamage <= 0)
            container.AddChild(CreateBar(0, BarHeight, baseColor));
    }

    private static void BuildStarBar(Control container, ContributionAccum accum, int value, int maxVal, bool isSub)
    {
        var barWidth = maxVal > 0 ? (float)value / maxVal * MaxBarWidth : 0;
        var color = isSub ? SubBarColor : StarBarColor;
        container.AddChild(CreateBar(barWidth, BarHeight, color));
    }

    private static void BuildHealBar(Control container, ContributionAccum accum, int value, int maxVal, bool isSub)
    {
        var barWidth = maxVal > 0 ? (float)value / maxVal * MaxBarWidth : 0;
        var color = isSub ? SubBarColor : HealBarColor;
        container.AddChild(CreateBar(barWidth, BarHeight, color));
    }

    private static void BuildSimpleBar(Control container, ContributionAccum accum, int value, int maxVal, bool isSub)
    {
        var barWidth = maxVal > 0 ? (float)value / maxVal * MaxBarWidth : 0;
        var color = isSub ? SubBarColor : GetSourceColor(accum.SourceType);
        container.AddChild(CreateBar(barWidth, BarHeight, color));
    }

    private static ColorRect CreateBar(float width, float height, Color color)
    {
        return new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(width, height),
            Size = new Vector2(width, height)
        };
    }

    private void AddSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 2);
        AddChild(sep);
    }

    /// <summary>
    /// Resolves an internal ID (e.g. STRIKE_IRONCLAD) to its localized display name
    /// using the game's LocString system.
    /// </summary>
    private static string GetLocalizedName(string id, string sourceType)
    {
        // Rest site
        if (sourceType == "rest")
            return L.Get("source.rest_site");

        // Merchant
        if (sourceType == "merchant")
            return L.Get("source.merchant");

        // Per-floor recovery
        if (sourceType == "floor_regen")
        {
            // Extract floor number from "FLOOR_X_REGEN"
            var parts = id.Split('_');
            var floor = parts.Length >= 2 ? parts[1] : "?";
            return string.Format(L.Get("source.floor_regen"), floor);
        }

        // Event: resolve event ID to localized name
        if (sourceType == "event")
        {
            try
            {
                var locStr = new LocString("events", id + ".title");
                var result = locStr.GetFormattedText();
                if (!string.IsNullOrEmpty(result) && result != id + ".title")
                    return result;
            }
            catch { /* fall through */ }
            return id;
        }

        try
        {
            var category = sourceType switch
            {
                "relic" => "relics",
                "potion" => "potions",
                _ => "cards"
            };
            var locStr = new LocString(category, id + ".title");
            var result = locStr.GetFormattedText();
            // LocString returns the key itself if not found; fall back to raw ID
            if (!string.IsNullOrEmpty(result) && result != id + ".title")
                return result;
        }
        catch { /* fall through to raw ID */ }
        return id;
    }
}
