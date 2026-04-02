using CommunityStats.Api;
using CommunityStats.Util;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// A small label overlay showing pick rate / win rate (or relic win rate, event %, etc).
/// Attach as a child of the target UI element.
/// </summary>
public partial class StatsLabel : Label
{
    private static readonly Color HighWinColor = new(0.3f, 0.9f, 0.3f);   // green
    private static readonly Color MidWinColor = new(1f, 0.85f, 0.2f);     // yellow
    private static readonly Color LowWinColor = new(0.9f, 0.3f, 0.3f);    // red
    private static readonly Color NeutralColor = new(0.85f, 0.85f, 0.85f); // light gray

    public static StatsLabel Create(string text, Color? color = null)
    {
        var label = new StatsLabel();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color ?? NeutralColor);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        return label;
    }

    // ── Factory methods for common stat displays ────────────

    public static StatsLabel ForCardStats(CardStats stats)
    {
        var pickPct = (stats.PickRate * 100).ToString("F0");
        var winPct = (stats.WinRate * 100).ToString("F0");
        var text = $"Pick {pickPct}% | Win {winPct}%";
        var color = WinRateColor(stats.WinRate);
        return Create(text, color);
    }

    public static StatsLabel ForRelicStats(RelicStats stats)
    {
        var winPct = (stats.WinRate * 100).ToString("F0");
        var pickPct = (stats.PickRate * 100).ToString("F0");
        var text = $"Pick {pickPct}% | Win {winPct}%";
        var color = WinRateColor(stats.WinRate);
        return Create(text, color);
    }

    public static StatsLabel ForEventOption(EventOptionStats stats)
    {
        var selPct = (stats.SelectionRate * 100).ToString("F0");
        var winPct = (stats.WinRate * 100).ToString("F0");
        var text = $"Chosen {selPct}% | Win {winPct}%";
        var color = WinRateColor(stats.WinRate);
        return Create(text, color);
    }

    public static StatsLabel ForEncounter(EncounterStats stats)
    {
        var deathPct = (stats.DeathRate * 100).ToString("F1");
        var avgDmg = stats.AvgDamageTaken.ToString("F0");
        var text = $"Death {deathPct}% | Avg DMG {avgDmg}";
        var color = DeathRateColor(stats.DeathRate);
        return Create(text, color);
    }

    public static StatsLabel ForLoading()
    {
        return Create("Loading...", NeutralColor);
    }

    public static StatsLabel ForUnavailable()
    {
        return Create("No data", new Color(0.5f, 0.5f, 0.5f));
    }

    // ── Color helpers ───────────────────────────────────────

    public static Color WinRateColor(float winRate) => winRate switch
    {
        >= 0.6f => HighWinColor,
        >= 0.4f => MidWinColor,
        _ => LowWinColor
    };

    public static Color DeathRateColor(float deathRate) => deathRate switch
    {
        >= 0.15f => LowWinColor,   // high death = red
        >= 0.05f => MidWinColor,
        _ => HighWinColor
    };
}
