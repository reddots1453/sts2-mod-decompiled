using CommunityStats.Api;
using CommunityStats.Collection;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace CommunityStats.Patches;

[HarmonyPatch]
public static class CardRewardScreenPatch
{
    [HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.RefreshOptions))]
    [HarmonyPostfix]
    public static void AfterRefreshOptions(NCardRewardSelectionScreen __instance,
        IReadOnlyList<CardCreationResult> options)
    {
        Safe.Run(() =>
        {
            var cardRow = Traverse.Create(__instance).Field("_cardRow").GetValue<Control>();
            if (cardRow == null) return;

            int index = 0;
            foreach (var child in cardRow.GetChildren())
            {
                if (child is not NGridCardHolder holder) continue;
                if (index >= options.Count) break;

                var cardId = options[index].Card?.Id.Entry;
                index++;
                if (cardId == null) continue;

                DeckViewPatch.RemoveExistingLabel(holder);

                var stats = StatsProvider.Instance.GetCardStats(cardId);
                Label label;
                if (stats != null)
                {
                    label = StatsLabel.ForCardStats(stats);
                }
                else if (!StatsProvider.Instance.HasBundle)
                {
                    label = StatsLabel.ForLoading();
                }
                else
                {
                    label = StatsLabel.ForUnavailable();
                }
                label.Position = new Vector2(0, 200);
                label.Size = new Vector2(350, 30);
                label.SetMeta(DeckViewPatch.StatsLabelMeta, true);
                holder.AddChild(label);
            }
        });
    }

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
