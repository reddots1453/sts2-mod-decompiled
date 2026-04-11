using System;
using System.Linq;
using CommunityStats.Config;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CommunityStats.UI;

/// <summary>
/// Hover panel showing shop price ranges (PRD §3.16, manual feedback round 4).
///
/// Layout matches the sts1 InfoMod reference: a 4-column table where the
/// header row carries rarity columns (Common / Uncommon / Rare) and each
/// content row is a category (Relics / Cards / Potions). When the player
/// owns MembershipCard or TheCourier the displayed prices are *already*
/// discounted (per Q3c — show only the post-discount value, not both).
///
/// Base prices (decompiled CardModel/RelicModel/PotionModel shop pools):
///   Cards: 50 / 75 / 150 (±5%), ×1.15 for colorless cards
///   Relics: 200 / 250 / 300 (±15%); shop-pool relics use 225
///   Potions: 50 / 75 / 100 (±5%)
///   Removal: 75 + 25 × CardShopRemovalsUsed
/// </summary>
public static class ShopPricePanel
{
    private static readonly Color GoldColor   = new("#EFC851");
    private static readonly Color AquaColor   = new(0.16f, 0.92f, 0.75f);
    private static readonly Color CreamColor  = new("#FFF6E2");
    private static readonly Color GrayColor   = new(0.62f, 0.62f, 0.72f);
    private static readonly Color RedColor    = new(0.94f, 0.42f, 0.42f);
    private static readonly Color BlueColor   = new(0.36f, 0.66f, 0.98f);
    private static readonly Color GreenColor  = new(0.45f, 0.88f, 0.50f);
    private static readonly Color DimColor    = new(0.45f, 0.45f, 0.50f);

    private const int LabelSize  = 13;
    private const int HeaderSize = 12;
    private const int ValueSize  = 12;

    public static InfoModPanel Create(Player? player = null, int cardRemovalsUsed = 0)
    {
        var panel = InfoModPanel.Create(L.Get("shop.title"), L.Get("shop.subtitle"));
        panel.AddSeparator();

        // Compute the multiplier from the player's active discount relics.
        float multiplier = ComputeDiscountMultiplier(player);
        AppendDiscountSummary(panel, player, multiplier);

        int removalCost = (int)Math.Round((75 + 25 * cardRemovalsUsed) * multiplier);
        int playerGold = TryGetGold(player);
        var removalColor = (playerGold >= 0 && playerGold < removalCost) ? DimColor : RedColor;
        panel.AddRow(string.Format(L.Get("shop.removal"), removalCost), "", removalColor, removalColor);

        panel.AddSeparator();

        // Build the table grid: header row + 3 category rows × 3 rarity columns.
        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 4);
        panel.AddCustom(grid);

        // Header row
        AddCell(grid, "", GrayColor, HeaderSize);
        AddCell(grid, L.Get("shop.col_common"), GrayColor, HeaderSize);
        AddCell(grid, L.Get("shop.col_uncommon"), GrayColor, HeaderSize);
        AddCell(grid, L.Get("shop.col_rare"), GrayColor, HeaderSize);

        // Relics: 200 / 250 / 300, ±15%
        AddCategoryRow(grid, L.Get("shop.relics"), GoldColor, multiplier,
            new[] { 200, 250, 300 }, 0.15f, playerGold);

        // Cards: 50 / 75 / 150, ±5%
        AddCategoryRow(grid, L.Get("shop.cards"), BlueColor, multiplier,
            new[] { 50, 75, 150 }, 0.05f, playerGold);

        // Potions: 50 / 75 / 100, ±5%
        AddCategoryRow(grid, L.Get("shop.potions"), GreenColor, multiplier,
            new[] { 50, 75, 100 }, 0.05f, playerGold);

        panel.AddSeparator();
        panel.AddLabel(L.Get("shop.colorless_note"), GrayColor);

        return panel;
    }

    /// <summary>
    /// Walk the player's relics and combine all merchant-price discounts.
    /// MembershipCard yields ×0.5, TheCourier yields ×0.8 — both stack.
    /// Returns 1.0 when the player has neither (or when player is null).
    /// </summary>
    private static float ComputeDiscountMultiplier(Player? player)
    {
        if (player == null) return 1f;
        try
        {
            float m = 1f;
            foreach (var relic in player.Relics)
            {
                if (relic is MembershipCard) m *= 0.5f;
                else if (relic is TheCourier) m *= 0.8f;
            }
            return m;
        }
        catch { return 1f; }
    }

    private static void AppendDiscountSummary(InfoModPanel panel, Player? player, float multiplier)
    {
        if (player == null || multiplier >= 0.999f) return;
        try
        {
            var names = player.Relics
                .Where(r => r is MembershipCard || r is TheCourier)
                .Select(r => r.Title?.GetFormattedText() ?? r.Id.Entry)
                .ToList();
            if (names.Count == 0) return;
            panel.AddLabel(L.Get("shop.discount_active") + " " + string.Join(", ", names), GoldColor);
        }
        catch { /* swallow */ }
    }

    private static void AddCategoryRow(GridContainer grid, string label, Color labelColor,
        float multiplier, int[] basePrices, float jitter, int playerGold)
    {
        AddCell(grid, label, labelColor, LabelSize);
        for (int i = 0; i < basePrices.Length; i++)
        {
            int low  = (int)Math.Round(basePrices[i] * (1f - jitter) * multiplier);
            int high = (int)Math.Round(basePrices[i] * (1f + jitter) * multiplier);
            // Gray out when the player can't even afford the cheapest variant.
            var cellColor = (playerGold >= 0 && playerGold < low) ? DimColor : labelColor;
            AddCell(grid, low == high ? low.ToString() : $"{low}-{high}", cellColor, ValueSize);
        }
    }

    private static int TryGetGold(Player? player)
    {
        if (player == null) return -1;
        try { return player.Gold; }
        catch { return -1; }
    }

    private static void AddCell(GridContainer grid, string text, Color color, int fontSize)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.HorizontalAlignment = HorizontalAlignment.Right;
        grid.AddChild(lbl);
    }
}
