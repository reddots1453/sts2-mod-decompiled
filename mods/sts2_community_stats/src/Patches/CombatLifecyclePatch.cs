using CommunityStats.Collection;
using CommunityStats.UI;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace CommunityStats.Patches;

/// <summary>
/// Patches CombatRoom to track combat start/end for contribution tracking
/// and show the contribution panel after combat.
/// </summary>
[HarmonyPatch]
public static class CombatLifecyclePatch
{
    // NOTE: CombatRoom.Enter is async Task — Harmony Postfix on async methods
    // fires when the Task is *returned*, not when it *completes*.
    // We use Prefix instead, which runs synchronously before the method body.
    // CombatState is set in CombatRoom's constructor, so it's available in Prefix.
    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.Enter))]
    [HarmonyPrefix]
    public static void BeforeCombatEnter(CombatRoom __instance)
    {
        Safe.Run(() =>
        {
            var encounter = __instance.CombatState?.Encounter;
            var encounterId = encounter?.Id.Entry ?? "unknown";
            var encounterType = encounter?.RoomType.ToString().ToLowerInvariant() ?? "normal";
            var floor = RunDataCollector.CurrentFloor;

            CombatTracker.Instance.OnCombatStart(encounterId, encounterType, floor);
            Safe.Info($"Combat started: {encounterId} ({encounterType}) on floor {floor}");
        });
    }

    // OnCombatEnded is a sync void method — Postfix works correctly here.
    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OnCombatEnded))]
    [HarmonyPostfix]
    public static void AfterCombatEnded(CombatRoom __instance)
    {
        Safe.Run(() =>
        {
            CombatTracker.Instance.OnCombatEnd();

            // Show contribution panel after combat
            ContributionPanel.ShowCombatResult(CombatTracker.Instance.LastCombatData);
            Safe.Info("Combat ended, contribution panel shown");
        });
    }
}
