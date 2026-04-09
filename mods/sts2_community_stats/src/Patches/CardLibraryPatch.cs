using System;
using CommunityStats.Api;
using CommunityStats.Collection;
using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace CommunityStats.Patches;

/// <summary>
/// Compendium card library: when the player opens the inspect screen,
/// inject a small stats panel (round 5 PRD §3.2):
///   - 6 metric rows × 3 columns: metric name / mine / community
///   - "Mine" column = LocalCardStats from RunHistoryAnalyzer
///   - "Community" column = StatsProvider bundle (server-side data)
///   - Anchored to the screen's bottom-right so it doesn't conflict with
///     keyword hover-tip popups in the upper-right
///
/// Replaces the previous design that called four ForXxx() factory methods,
/// each of which appended its own win-rate row → win rate appeared 4 times.
/// </summary>
[HarmonyPatch]
public static class CardLibraryPatch
{
    private const string PanelName = "StatsTheSpireCardStats";

    private static readonly Color HeaderColor    = new("#EFC851");
    private static readonly Color CreamColor     = new("#FFF6E2");
    private static readonly Color GrayColor      = new(0.62f, 0.62f, 0.72f);
    private static readonly Color MineColor      = new(0.16f, 0.92f, 0.75f); // aqua
    private static readonly Color CommunityColor = new(0.95f, 0.78f, 0.30f); // gold
    private const int LabelSize  = 12;
    private const int HeaderSize = 11;

    [HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
    [HarmonyPostfix]
    public static void AfterUpdateCardDisplay(NInspectCardScreen __instance)
    {
        if (!ModConfig.Toggles.CardLibraryStats) return;
        Safe.Run(() => InjectOrUpdate(__instance));
    }

    private static void InjectOrUpdate(NInspectCardScreen screen)
    {
        var cards = Traverse.Create(screen).Field("_cards").GetValue<System.Collections.IList>();
        var index = Traverse.Create(screen).Field("_index").GetValue<int>();
        if (cards == null || index < 0 || index >= cards.Count) return;

        var card = cards[index] as CardModel;
        if (card == null) return;

        string? cardId = null;
        try { cardId = card.Id.Entry; } catch { }
        if (string.IsNullOrEmpty(cardId)) return;

        // Find or create the panel anchored to screen bottom-right.
        var panel = screen.GetNodeOrNull<PanelContainer>(PanelName);
        if (panel == null)
        {
            panel = new PanelContainer { Name = PanelName };
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.05f, 0.06f, 0.10f, 0.92f),
                BorderColor = new Color(0.3f, 0.4f, 0.6f, 0.5f),
                BorderWidthBottom = 1, BorderWidthTop = 1,
                BorderWidthLeft = 1, BorderWidthRight = 1,
                CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
                ContentMarginLeft = 12, ContentMarginRight = 12,
                ContentMarginTop = 8, ContentMarginBottom = 8,
            };
            panel.AddThemeStyleboxOverride("panel", style);
            panel.ZIndex = 100;
            panel.AnchorLeft   = 1.0f;
            panel.AnchorRight  = 1.0f;
            panel.AnchorTop    = 1.0f;
            panel.AnchorBottom = 1.0f;
            panel.OffsetLeft   = -300f;
            panel.OffsetRight  = -20f;
            panel.OffsetTop    = -200f;
            panel.OffsetBottom = -20f;
            panel.GrowHorizontal = Control.GrowDirection.Begin;
            panel.GrowVertical   = Control.GrowDirection.Begin;
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            screen.AddChild(panel);
        }

        // Clear any previous content.
        foreach (var c in panel.GetChildren()) c.QueueFree();

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 3);
        grid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddChild(grid);

        // Header row
        AddCell(grid, L.Get("card_lib.metric"),    GrayColor,      HeaderSize, false);
        AddCell(grid, L.Get("card_lib.mine"),      MineColor,      HeaderSize, true);
        AddCell(grid, L.Get("card_lib.community"), CommunityColor, HeaderSize, true);

        // Resolve both data sources.
        var mine     = RunHistoryAnalyzer.Instance.LocalCards;
        var mineRow  = mine.Get(cardId!);
        var community = StatsProvider.Instance.GetCardStats(cardId!);

        // Sample size row
        AddCell(grid, L.Get("card_lib.samples"), CreamColor, LabelSize, false);
        AddCell(grid, $"{mine.TotalRuns}", MineColor, LabelSize, true);
        AddCell(grid, community != null ? $"{community.SampleSize}" : "—",
                community != null ? CommunityColor : GrayColor, LabelSize, true);

        // Pick / Win / Upgrade / Removal / Buy
        AddRatioRow(grid, "card_lib.pick_rate",
            mineRow?.PickRate, community?.PickRate);
        AddRatioRow(grid, "card_lib.win_rate",
            mineRow?.WinRate, community?.WinRate);
        AddRatioRow(grid, "card_lib.upgrade_rate",
            null, community?.UpgradeRate);
        AddRatioRow(grid, "card_lib.removal_rate",
            null, community?.RemovalRate);
        AddRatioRow(grid, "card_lib.buy_rate",
            null, community?.ShopBuyRate);
    }

    private static void AddRatioRow(GridContainer grid, string labelKey, float? mine, float? community)
    {
        AddCell(grid, L.Get(labelKey), CreamColor, LabelSize, false);
        AddCell(grid, mine.HasValue ? $"{mine.Value * 100f:F1}%" : "—",
                mine.HasValue ? MineColor : GrayColor, LabelSize, true);
        AddCell(grid, community.HasValue ? $"{community.Value * 100f:F1}%" : "—",
                community.HasValue ? CommunityColor : GrayColor, LabelSize, true);
    }

    private static void AddCell(GridContainer grid, string text, Color color, int fontSize, bool rightAlign)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        if (rightAlign)
        {
            lbl.HorizontalAlignment = HorizontalAlignment.Right;
            lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }
        grid.AddChild(lbl);
    }
}
