using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace CommunityStats.Patches;

/// <summary>
/// Compendium card library: when the player opens the card inspect screen,
/// inject a small stats panel (pick / win / buy / upgrade rates) to the
/// right of the existing hover-tip anchor. PRD 3.2.
///
/// Patches the private NInspectCardScreen.UpdateCardDisplay method so the
/// panel refreshes when the player pages between cards.
/// </summary>
[HarmonyPatch]
public static class CardLibraryPatch
{
    private const string PanelName = "StatsTheSpireCardStats";

    [HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
    [HarmonyPostfix]
    public static void AfterUpdateCardDisplay(NInspectCardScreen __instance)
    {
        if (!ModConfig.Toggles.CardLibraryStats) return;
        Safe.Run(() => InjectOrUpdate(__instance));
    }

    private static void InjectOrUpdate(NInspectCardScreen screen)
    {
        // Extract current card model via private fields
        var cards = Traverse.Create(screen).Field("_cards").GetValue<System.Collections.IList>();
        var index = Traverse.Create(screen).Field("_index").GetValue<int>();
        if (cards == null || index < 0 || index >= cards.Count) return;

        var card = cards[index] as CardModel;
        if (card == null) return;

        string? cardId = null;
        try { cardId = card.Id.Entry; } catch { }
        if (string.IsNullOrEmpty(cardId)) return;

        var hoverTipRect = Traverse.Create(screen).Field("_hoverTipRect").GetValue<Control>();
        if (hoverTipRect == null) return;

        // Find or create the stats panel as a child of the screen
        var panel = screen.GetNodeOrNull<VBoxContainer>(PanelName);
        if (panel == null)
        {
            panel = new VBoxContainer { Name = PanelName };
            panel.AddThemeConstantOverride("separation", 2);
            panel.ZIndex = 100;
            screen.AddChild(panel);
        }

        // Position to the right of the hover-tip anchor
        panel.Position = hoverTipRect.Position + new Vector2(hoverTipRect.Size.X + 20f, 0f);

        // Clear previous children
        foreach (var child in panel.GetChildren())
            child.QueueFree();

        // Populate
        if (!StatsProvider.Instance.HasBundle)
        {
            panel.AddChild(StatsLabel.ForLoading());
            return;
        }

        var stats = StatsProvider.Instance.GetCardStats(cardId);
        if (stats == null)
        {
            panel.AddChild(StatsLabel.ForUnavailable());
            return;
        }

        panel.AddChild(StatsLabel.ForCardStats(stats));
        panel.AddChild(StatsLabel.ForUpgradeRate(stats));
        panel.AddChild(StatsLabel.ForRemovalRate(stats));
        panel.AddChild(StatsLabel.ForShopBuyRate(stats.ShopBuyRate, stats.WinRate));
    }
}
