using CommunityStats.Collection;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace CommunityStats.Patches;

/// <summary>
/// Patches card reward screen: auto-closes contribution panel when the
/// reward overlay opens, and records card picks. Community stat labels
/// removed (local edition).
/// </summary>
[HarmonyPatch]
public static class CardRewardScreenPatch
{
    /// <summary>Auto-close contribution panel when the reward overlay opens.
    /// This covers both card-pick and skip paths.</summary>
    [HarmonyPatch(typeof(NCardRewardSelectionScreen), "AfterOverlayOpened")]
    [HarmonyPostfix]
    public static void AfterOverlayOpened()
    {
        Safe.Run(() => UI.ContributionPanel.HideIfVisible());
    }

    /// <summary>Record card picks for local stats.</summary>
    [HarmonyPatch(typeof(NCardRewardSelectionScreen), "SelectCard")]
    [HarmonyPostfix]
    public static void AfterSelectCard(NCardRewardSelectionScreen __instance,
        NCardHolder cardHolder)
    {
        Safe.Run(() =>
        {
            var options = Traverse.Create(__instance).Field("_options").GetValue<IReadOnlyList<CardCreationResult>>();
            if (options == null) return;

            var pickedCard = cardHolder?.CardModel;
            var pickedId = pickedCard?.Id.Entry;

            var offeredCards = options.Select(o => (
                cardId: o.Card?.Id.Entry ?? "unknown",
                upgradeLevel: o.Card?.CurrentUpgradeLevel ?? 0
            )).ToList();

            RunDataCollector.RecordCardReward(offeredCards, pickedId, RunDataCollector.CurrentFloor);
        });
    }
}
