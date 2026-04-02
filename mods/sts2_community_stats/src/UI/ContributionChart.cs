using CommunityStats.Collection;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// Horizontal bar chart showing per-source (card/relic) contributions.
/// Builds Godot UI nodes programmatically.
/// </summary>
public partial class ContributionChart : VBoxContainer
{
    private static readonly Color CardBarColor = new(0.3f, 0.5f, 0.9f);     // blue
    private static readonly Color RelicBarColor = new(0.9f, 0.75f, 0.2f);   // gold
    private static readonly Color AttrBarColor = new(0.5f, 0.7f, 1.0f);     // light blue (attributed)
    private static readonly Color HeaderColor = new(1f, 1f, 1f);
    private const int MaxBarsPerSection = 8;
    private const float BarHeight = 20f;
    private const float MaxBarWidth = 300f;

    public enum Category { Damage, Block, Misc }

    public static ContributionChart Create(
        IReadOnlyDictionary<string, ContributionAccum> data,
        string title)
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

        // Damage section
        chart.BuildSection("Damage", data, Category.Damage);

        chart.AddSeparator();

        // Block section
        chart.BuildSection("Block", data, Category.Block);

        chart.AddSeparator();

        // Misc summary (cards drawn, energy, healing)
        chart.BuildMiscLine(data);

        return chart;
    }

    private void BuildSection(string header, IReadOnlyDictionary<string, ContributionAccum> data, Category category)
    {
        var headerLabel = new Label { Text = header };
        headerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        headerLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(headerLabel);

        // Sort by the relevant metric
        var sorted = data.Values
            .Select(a => (a, Value: category == Category.Damage ? a.TotalDamage : a.BlockGained))
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .Take(MaxBarsPerSection)
            .ToList();

        if (sorted.Count == 0)
        {
            var none = new Label { Text = "  (none)" };
            none.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            none.AddThemeFontSizeOverride("font_size", 12);
            AddChild(none);
            return;
        }

        var maxVal = sorted.Max(x => x.Value);
        var totalVal = sorted.Sum(x => x.Value);

        foreach (var (accum, value) in sorted)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            // Source name with [R] prefix for relics
            var prefix = accum.SourceType == "relic" ? "[R] " : "";
            var nameLabel = new Label
            {
                Text = $"{prefix}{accum.SourceId}",
                CustomMinimumSize = new Vector2(120, BarHeight)
            };
            nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            nameLabel.ClipText = true;
            row.AddChild(nameLabel);

            // Bar container
            var barContainer = new Control { CustomMinimumSize = new Vector2(MaxBarWidth, BarHeight) };

            if (category == Category.Damage && accum.AttributedDamage > 0)
            {
                // Split bar: direct + attributed
                var directWidth = maxVal > 0 ? (float)accum.DirectDamage / maxVal * MaxBarWidth : 0;
                var attrWidth = maxVal > 0 ? (float)accum.AttributedDamage / maxVal * MaxBarWidth : 0;

                var directBar = CreateBar(directWidth, BarHeight,
                    accum.SourceType == "relic" ? RelicBarColor : CardBarColor);
                barContainer.AddChild(directBar);

                var attrBar = CreateBar(attrWidth, BarHeight, AttrBarColor);
                attrBar.Position = new Vector2(directWidth, 0);
                barContainer.AddChild(attrBar);
            }
            else
            {
                var barWidth = maxVal > 0 ? (float)value / maxVal * MaxBarWidth : 0;
                var barColor = accum.SourceType == "relic" ? RelicBarColor : CardBarColor;
                barContainer.AddChild(CreateBar(barWidth, BarHeight, barColor));
            }

            row.AddChild(barContainer);

            // Value + percentage
            var pct = totalVal > 0 ? (float)value / totalVal * 100 : 0;
            var valLabel = new Label { Text = $"{value}  ({pct:F0}%)" };
            valLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            valLabel.AddThemeFontSizeOverride("font_size", 12);
            row.AddChild(valLabel);

            AddChild(row);
        }
    }

    private void BuildMiscLine(IReadOnlyDictionary<string, ContributionAccum> data)
    {
        int totalDrawn = data.Values.Sum(a => a.CardsDrawn);
        int totalEnergy = data.Values.Sum(a => a.EnergyGained);
        int totalHealed = data.Values.Sum(a => a.HpHealed);

        var text = $"Cards Drawn: {totalDrawn}  |  Energy: {totalEnergy}  |  Healed: {totalHealed}";
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        label.AddThemeFontSizeOverride("font_size", 12);
        AddChild(label);
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
}
