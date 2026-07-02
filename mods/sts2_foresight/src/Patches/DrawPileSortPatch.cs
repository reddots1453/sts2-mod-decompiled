using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;
using Foresight.Util;

namespace Foresight.Patches;

/// <summary>
/// Frozen Eye effect: when the player has Foresight Eye, the draw pile viewer
/// shows cards in actual draw order instead of sorting by rarity→ID.
/// </summary>
[HarmonyPatch(typeof(NCardPileScreen), "OnPileContentsChanged")]
public static class DrawPileSortPatch
{
    private const string RelicId = "STS2_FORESIGHT_RELIC_FORESIGHT_EYE";
    private static bool _shouldReorder;

    [HarmonyPrefix]
    public static void Prefix(NCardPileScreen __instance)
    {
        _shouldReorder = false;
        Safe.Run(() =>
        {
            if (__instance.Pile?.Type == PileType.Draw && HasForesightEye())
                _shouldReorder = true;
        });
    }

    [HarmonyPostfix]
    public static void Postfix(NCardPileScreen __instance)
    {
        if (!_shouldReorder) return;
        Safe.Run(() =>
        {
            var grid = Traverse.Create(__instance).Field("_grid").GetValue<NCardGrid>();
            if (grid == null) return;

            var drawOrder = __instance.Pile?.Cards?.ToList();
            if (drawOrder == null || drawOrder.Count == 0) return;

            grid.SetCards(drawOrder, PileType.Draw,
                new List<SortingOrders> { SortingOrders.Ascending }, null);
        });
    }

    private static bool HasForesightEye()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            return state?.Players.Any(p =>
                p?.Relics?.Any(r =>
                    string.Equals(r.Id.Entry, RelicId, System.StringComparison.OrdinalIgnoreCase)) == true) == true;
        }
        catch { return false; }
    }
}
