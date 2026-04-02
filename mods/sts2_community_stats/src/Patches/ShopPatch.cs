using CommunityStats.Collection;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace CommunityStats.Patches;

/// <summary>
/// Patches shop purchase flow to record purchases for community stats.
/// Uses OnSuccessfulPurchase (sync) instead of OnTryPurchase (async).
/// Prefix is used so _cardNode is still available (it's nulled in the method body).
/// </summary>
[HarmonyPatch]
public static class ShopPatch
{
    [HarmonyPatch(typeof(NMerchantCard), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforeCardPurchaseVisual(NMerchantCard __instance)
    {
        Safe.Run(() =>
        {
            // _cardNode is private NCard? — still non-null in Prefix
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
                Safe.Info($"Shop card purchase recorded: {entry} for {cost}g");
            }
        });
    }
}
