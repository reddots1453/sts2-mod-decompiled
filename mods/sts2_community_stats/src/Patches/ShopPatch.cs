using CommunityStats.Api;
using CommunityStats.Collection;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace CommunityStats.Patches;

/// <summary>
/// Patches shop purchase flow to record purchases and display buy rates.
/// Covers cards, relics, and potions.
/// </summary>
[HarmonyPatch]
public static class ShopPatch
{
    private const string StatsLabelMeta = "cs_shop_stats";

    // ── Card Purchase Recording ─────────────────────────────

    [HarmonyPatch(typeof(NMerchantCard), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforeCardPurchaseVisual(NMerchantCard __instance)
    {
        Safe.Run(() =>
        {
            var cardNode = Traverse.Create(__instance).Field("_cardNode").GetValue<object>();
            if (cardNode == null) return;

            var model = Traverse.Create(cardNode).Property("Model").GetValue<object>();
            if (model == null) return;

            var id = Traverse.Create(model).Property("Id").GetValue<object>();
            var entry = Traverse.Create(id).Property("Entry").GetValue<string>();

            var cardEntry = Traverse.Create(__instance).Field("_cardEntry").GetValue<object>();
            var cost = cardEntry != null
                ? Traverse.Create(cardEntry).Property("Cost").GetValue<int>()
                : 0;

            if (entry != null)
            {
                RunDataCollector.RecordShopPurchase(entry, "card", cost, RunDataCollector.CurrentFloor);
            }
        });
    }

    // ── Relic Purchase Recording ────────────────────────────

    [HarmonyPatch(typeof(NMerchantRelic), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforeRelicPurchaseVisual(NMerchantRelic __instance)
    {
        Safe.Run(() =>
        {
            var relic = Traverse.Create(__instance).Field("_relic").GetValue<object>();
            if (relic == null) return;

            var id = Traverse.Create(relic).Property("Id").GetValue<object>();
            var entry = Traverse.Create(id).Property("Entry").GetValue<string>();

            var relicEntry = Traverse.Create(__instance).Field("_relicEntry").GetValue<object>();
            var cost = relicEntry != null
                ? Traverse.Create(relicEntry).Property("Cost").GetValue<int>()
                : 0;

            if (entry != null)
            {
                RunDataCollector.RecordShopPurchase(entry, "relic", cost, RunDataCollector.CurrentFloor);
            }
        });
    }

    // ── Potion Purchase Recording ───────────────────────────

    [HarmonyPatch(typeof(NMerchantPotion), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforePotionPurchaseVisual(NMerchantPotion __instance)
    {
        Safe.Run(() =>
        {
            var potion = Traverse.Create(__instance).Field("_potion").GetValue<object>();
            if (potion == null) return;

            var id = Traverse.Create(potion).Property("Id").GetValue<object>();
            var entry = Traverse.Create(id).Property("Entry").GetValue<string>();

            var potionEntry = Traverse.Create(__instance).Field("_potionEntry").GetValue<object>();
            var cost = potionEntry != null
                ? Traverse.Create(potionEntry).Property("Cost").GetValue<int>()
                : 0;

            if (entry != null)
            {
                RunDataCollector.RecordShopPurchase(entry, "potion", cost, RunDataCollector.CurrentFloor);
            }
        });
    }

    // ── Shop Card Buy Rate Display ──────────────────────────

    [HarmonyPatch(typeof(NMerchantCard), nameof(NMerchantCard.FillSlot))]
    [HarmonyPostfix]
    public static void AfterCardFillSlot(NMerchantCard __instance)
    {
        Safe.Run(() =>
        {
            RemoveExistingLabel(__instance);

            var cardNode = Traverse.Create(__instance).Field("_cardNode").GetValue<object>();
            if (cardNode == null) return;

            var model = Traverse.Create(cardNode).Property("Model").GetValue<object>();
            if (model == null) return;

            var id = Traverse.Create(model).Property("Id").GetValue<object>();
            var cardId = Traverse.Create(id).Property("Entry").GetValue<string>();
            if (cardId == null) return;

            var stats = StatsProvider.Instance.GetCardStats(cardId);
            Label label;
            if (stats != null)
                label = StatsLabel.ForShopBuyRate(stats.ShopBuyRate, stats.WinRate);
            else if (!StatsProvider.Instance.HasBundle)
                label = StatsLabel.ForLoading();
            else
                label = StatsLabel.ForUnavailable();

            label.Position = new Vector2(0, -18);
            label.SetMeta(StatsLabelMeta, true);
            __instance.AddChild(label);
        });
    }

    // ── Shop Relic Buy Rate Display ─────────────────────────

    [HarmonyPatch(typeof(NMerchantRelic), nameof(NMerchantRelic.FillSlot))]
    [HarmonyPostfix]
    public static void AfterRelicFillSlot(NMerchantRelic __instance)
    {
        Safe.Run(() =>
        {
            RemoveExistingLabel(__instance);

            var relic = Traverse.Create(__instance).Field("_relic").GetValue<object>();
            if (relic == null) return;

            var id = Traverse.Create(relic).Property("Id").GetValue<object>();
            var relicId = Traverse.Create(id).Property("Entry").GetValue<string>();
            if (relicId == null) return;

            var stats = StatsProvider.Instance.GetRelicStats(relicId);
            Label label;
            if (stats != null)
                label = StatsLabel.ForShopBuyRate(stats.ShopBuyRate, stats.WinRate);
            else if (!StatsProvider.Instance.HasBundle)
                label = StatsLabel.ForLoading();
            else
                label = StatsLabel.ForUnavailable();

            label.Position = new Vector2(0, -18);
            label.SetMeta(StatsLabelMeta, true);
            __instance.AddChild(label);
        });
    }

    // ── Helpers ──────────────────────────────────────────────

    private static void RemoveExistingLabel(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is Label lbl && lbl.HasMeta(StatsLabelMeta))
                lbl.QueueFree();
        }
    }
}
