using CommunityStats.Collection;
using CommunityStats.UI;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace CommunityStats.Patches;

/// <summary>
/// Patches CombatManager to track combat start/end for contribution tracking
/// and show the contribution panel after combat.
/// Uses CombatManager.SetUpCombat (sync, non-override) instead of CombatRoom.Enter
/// (async override, which Harmony struggles to resolve).
/// </summary>
[HarmonyPatch]
public static class CombatLifecyclePatch
{
    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
    [HarmonyPostfix]
    public static void AfterSetUpCombat(CombatManager __instance, CombatState state)
    {
        Safe.Run(() =>
        {
            var encounter = state?.Encounter;
            var encounterId = encounter?.Id.Entry ?? "unknown";
            var encounterType = encounter?.RoomType.ToString().ToLowerInvariant() ?? "normal";
            var floor = RunDataCollector.CurrentFloor;

            CombatTracker.Instance.OnCombatStart(encounterId, encounterType, floor);
            Safe.Info($"Combat started: {encounterId} ({encounterType}) on floor {floor}");

            // Round 9 round 6: retry the intent-metadata eager bake here. At
            // mod-init time ModelDb._contentById is empty, so Initialize() just
            // logs "deferred". By the time combat starts, ModelDb.Init has run
            // and every monster is reachable — idempotent via `_eagerBakeDone`.
            MonsterIntentMetadata.Initialize();
        });
    }

    // OnCombatEnded is a sync void method on CombatRoom — Postfix works correctly.
    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OnCombatEnded))]
    [HarmonyPostfix]
    public static void AfterCombatEnded(CombatRoom __instance)
    {
        Safe.Run(() =>
        {
            CombatTracker.Instance.OnCombatEnd();

            // Persist this combat snapshot for future Run History replay (PRD §3.12).
            ContributionPersistence.SaveCombat(
                RunDataCollector.CurrentFloor,
                CombatTracker.Instance.LastCombatData);

            // Show contribution panel after combat (respect feature toggle)
            if (Config.ModConfig.Toggles.ContributionPanel)
            {
                ContributionPanel.ShowCombatResult(CombatTracker.Instance.LastCombatData);
                Safe.Info("Combat ended, contribution panel shown");
            }
        });
    }

    /// <summary>
    /// Auto-close the contribution panel when the player clicks the Proceed button
    /// after collecting rewards. ProceedFromTerminalRewardsScreen is the async method
    /// called by NRewardsScreen.OnProceedButtonPressed — a Prefix runs synchronously
    /// before the method starts, which is the right time to hide the panel.
    /// </summary>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ProceedFromTerminalRewardsScreen))]
    [HarmonyPrefix]
    public static void BeforeProceed()
    {
        Safe.Run(() => ContributionPanel.Hide());
    }
}
