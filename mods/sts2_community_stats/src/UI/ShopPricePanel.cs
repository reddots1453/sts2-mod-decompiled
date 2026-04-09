using CommunityStats.Config;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// Hover panel showing shop price ranges. PRD 3.16.
/// Values come from decompiled source:
/// - Cards: 50/75/150 (±5% shop roll), ×1.15 for colorless pool
/// - Relics: 200/250/300 (±15% shop roll); Shop pool 225
/// - Potions: 50/75/100 (±5% shop roll)
/// - Removal: 75 + 25 × CardShopRemovalsUsed
/// Discount: all prices halved.
/// </summary>
public static class ShopPricePanel
{
    private static readonly Color GoldColor = new("#EFC851");
    private static readonly Color AquaColor = new(0.16f, 0.92f, 0.75f);
    private static readonly Color CreamColor = new("#FFF6E2");

    public static InfoModPanel Create(int cardRemovalsUsed = 0)
    {
        var panel = InfoModPanel.Create(L.Get("shop.title"), L.Get("shop.subtitle"));
        panel.AddSeparator();

        int removalCost = 75 + 25 * cardRemovalsUsed;
        panel.AddRow(L.Get("shop.removal").Replace("{0}", removalCost.ToString()), "", CreamColor, GoldColor);

        panel.AddSeparator();

        // Relics: 200/250/300, ±15%
        AddPriceRow(panel, L.Get("shop.relics") + " " + L.Get("shop.col_common"), 200, 0.15f, GoldColor);
        AddPriceRow(panel, L.Get("shop.relics") + " " + L.Get("shop.col_uncommon"), 250, 0.15f, GoldColor);
        AddPriceRow(panel, L.Get("shop.relics") + " " + L.Get("shop.col_rare"), 300, 0.15f, GoldColor);

        // Cards: 50/75/150, ±5%
        AddPriceRow(panel, L.Get("shop.cards") + " " + L.Get("shop.col_common"), 50, 0.05f, AquaColor);
        AddPriceRow(panel, L.Get("shop.cards") + " " + L.Get("shop.col_uncommon"), 75, 0.05f, AquaColor);
        AddPriceRow(panel, L.Get("shop.cards") + " " + L.Get("shop.col_rare"), 150, 0.05f, AquaColor);

        // Potions: 50/75/100, ±5%
        AddPriceRow(panel, L.Get("shop.potions") + " " + L.Get("shop.col_common"), 50, 0.05f, CreamColor);
        AddPriceRow(panel, L.Get("shop.potions") + " " + L.Get("shop.col_uncommon"), 75, 0.05f, CreamColor);
        AddPriceRow(panel, L.Get("shop.potions") + " " + L.Get("shop.col_rare"), 100, 0.05f, CreamColor);

        return panel;
    }

    private static void AddPriceRow(InfoModPanel panel, string label, int basePrice, float jitter, Color labelColor)
    {
        int low = Mathf.RoundToInt(basePrice * (1f - jitter));
        int high = Mathf.RoundToInt(basePrice * (1f + jitter));
        panel.AddRow(label, $"{low}-{high}", labelColor, CreamColor);
    }
}
