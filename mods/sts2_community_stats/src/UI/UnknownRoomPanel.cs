using CommunityStats.Config;
using Godot;
using MegaCrit.Sts2.Core.Odds;

namespace CommunityStats.UI;

/// <summary>
/// Hover panel for unvisited `?` map points. Shows the current odds
/// distribution (Monster / Elite / Treasure / Shop / Event). PRD 3.8.
///
/// Built on top of InfoModPanel for consistent styling.
/// </summary>
public static class UnknownRoomPanel
{
    // Category colors (loosely following game room-type accents)
    private static readonly Color MonsterColor = new(0.9f, 0.4f, 0.4f);   // red
    private static readonly Color EliteColor = new(0.95f, 0.6f, 0.25f);   // orange
    private static readonly Color TreasureColor = new(0.95f, 0.85f, 0.4f); // gold-ish
    private static readonly Color ShopColor = new(0.95f, 0.8f, 0.3f);     // gold
    private static readonly Color EventColor = new(0.5f, 0.95f, 0.6f);    // green
    private static readonly Color CreamColor = new("#FFF6E2");

    public static InfoModPanel Create(UnknownMapPointOdds odds)
    {
        var panel = InfoModPanel.Create(
            L.Get("unknown.title"),
            L.Get("unknown.subtitle"));

        panel.AddSeparator();

        // Event is always > 0 (remainder). Monster/Treasure/Shop usually > 0. Elite may be -1.
        AddRow(panel, L.Get("room.event"), odds.EventOdds, EventColor);
        AddRow(panel, L.Get("room.monster"), odds.MonsterOdds, MonsterColor);
        if (odds.EliteOdds > 0f)
            AddRow(panel, L.Get("room.elite"), odds.EliteOdds, EliteColor);
        AddRow(panel, L.Get("room.treasure"), odds.TreasureOdds, TreasureColor);
        AddRow(panel, L.Get("room.shop"), odds.ShopOdds, ShopColor);

        return panel;
    }

    private static void AddRow(InfoModPanel panel, string label, float value, Color labelColor)
    {
        if (value <= 0f) return;
        var pct = (value * 100f).ToString("F1") + "%";
        panel.AddRow(label, pct, labelColor, CreamColor);
    }
}
