using CommunityStats.Collection;
using CommunityStats.Config;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;

namespace CommunityStats.Patches;

/// <summary>
/// Triggers ContributionPanel real-time refresh after significant combat events
/// (card play finished, potion used). Fires CombatTracker.CombatDataUpdated which
/// the panel listens to via a 500ms debounce. PRD 3.6.
/// </summary>
[HarmonyPatch]
public static class RealTimeCombatPatch
{
    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
    [HarmonyPostfix]
    public static void AfterCardPlayFinished()
    {
        if (!ModConfig.Toggles.ContributionPanel) return;
        Safe.Run(() => CombatTracker.Instance.NotifyCombatDataUpdated());
    }

    // Note: potion usage already triggers CombatHistory events via card play path in most cases.
    // PotionUsedPatch in CombatHistoryPatch.cs clears _activePotionId; we hook the same event to
    // notify the panel. We patch the method directly so we don't interfere with the existing postfix.
    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PotionUsed))]
    [HarmonyPostfix]
    public static void AfterPotionUsed()
    {
        if (!ModConfig.Toggles.ContributionPanel) return;
        Safe.Run(() => CombatTracker.Instance.NotifyCombatDataUpdated());
    }
}
