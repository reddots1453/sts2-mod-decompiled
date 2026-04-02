using CommunityStats.Api;
using CommunityStats.Collection;
using CommunityStats.Config;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace CommunityStats.Patches;

/// <summary>
/// Patches RunManager to trigger data preload at run start and upload at run end.
/// Also hooks ModManager.OnMetricsUpload for run data submission.
/// </summary>
[HarmonyPatch]
public static class RunLifecyclePatch
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer))]
    [HarmonyPostfix]
    public static void OnRunStartSP(RunManager __instance, RunState state)
    {
        Safe.Run(() =>
        {
            Safe.Info("[DIAG:RunLifecycle] SetUpNewSinglePlayer Postfix fired");
            RunDataCollector.OnRunStart();

            var players = state.Players;
            Safe.Info($"[DIAG:RunLifecycle] state.Players={players != null}, count={players?.Count}");

            var player = players?.FirstOrDefault();
            Safe.Info($"[DIAG:RunLifecycle] player={player != null}");

            var character = player?.Character?.Id.Entry;
            Safe.Info($"[DIAG:RunLifecycle] character={character ?? "NULL"}");

            if (character != null)
            {
                Safe.Info($"[DIAG:RunLifecycle] Launching PreloadForRunAsync for {character}");
                Safe.RunAsync(() =>
                    StatsProvider.Instance.PreloadForRunAsync(character, ModConfig.CurrentFilter));
            }
            else
            {
                Safe.Warn("[DIAG:RunLifecycle] character is null, skipping preload");
            }
        });
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer))]
    [HarmonyPostfix]
    public static void OnRunStartMP(RunManager __instance, RunState state)
    {
        Safe.Run(() =>
        {
            RunDataCollector.OnRunStart();

            var player = state.Players?.FirstOrDefault();
            var character = player?.Character?.Id.Entry;
            if (character != null)
            {
                Safe.RunAsync(() =>
                    StatsProvider.Instance.PreloadForRunAsync(character, ModConfig.CurrentFilter));
            }
        });
    }

    /// <summary>
    /// Register the OnMetricsUpload hook. Called once at mod init.
    /// </summary>
    public static void RegisterMetricsHook()
    {
        ModManager.OnMetricsUpload += (run, isVictory, localPlayerId) =>
        {
            Safe.Run(() => RunDataCollector.OnMetricsUpload(run, isVictory, localPlayerId));
        };
    }
}
