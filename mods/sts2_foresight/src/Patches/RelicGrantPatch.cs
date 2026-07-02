using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Foresight.Util;

namespace Foresight.Patches;

[HarmonyPatch]
public static class RelicGrantPatch
{
    private const string RelicId = "STS2_FORESIGHT_RELIC_FORESIGHT_EYE";
    private static readonly bool Debug = true;
    private static void Log(string msg) { if (Debug) ForesightMod.Logger.Info($"[GrantPatch] {msg}"); }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer))]
    [HarmonyPostfix]
    public static void OnNewSP(RunManager __instance, RunState state)
    {
        if (state == null) { Log("OnNewSP: state is null"); return; }
        Log($"SetUpNewSingleplayer fired: players={state.Players?.Count}");
        Safe.Run(() => GrantRelic(state));
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiplayer))]
    [HarmonyPostfix]
    public static void OnNewMP(RunManager __instance, RunState state)
    {
        if (state == null) { Log("OnNewMP: state is null"); return; }
        Log($"SetUpNewMultiplayer fired: players={state.Players?.Count}");
        Safe.Run(() => GrantRelic(state));
    }

    private static void GrantRelic(RunState state)
    {
        if (state?.Players == null || state.Players.Count == 0)
        {
            Log("No players, skip");
            return;
        }

        Log($"Looking up relic model: {typeof(Relic.ForesightEye).FullName}");
        var relicModel = ModelDb.Relic<Relic.ForesightEye>();
        if (relicModel == null)
        {
            Log("ModelDb.Relic<ForesightEye>() returned NULL — class not registered?");
            return;
        }
        Log($"Relic model found: Id={relicModel.Id.Entry}, Rarity={relicModel.Rarity}");

        foreach (var player in state.Players)
        {
            if (player == null) continue;
            if (HasRelic(player))
            {
                Log($"Player already has relic, skip");
                continue;
            }

            var relic = relicModel.ToMutable();
            Log($"Calling RelicCmd.Obtain for player {player.NetId}...");
            Safe.RunAsync(async () =>
            {
                try
                {
                    await RelicCmd.Obtain(relic, player);
                    Log($"RelicCmd.Obtain SUCCESS for player {player.NetId}");
                }
                catch (Exception ex)
                {
                    ForesightMod.Logger.Warn($"[GrantPatch] RelicCmd.Obtain failed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }
    }

    private static bool HasRelic(Player player)
    {
        try
        {
            return player.Relics?.Any(r =>
                string.Equals(r.Id.Entry, RelicId, System.StringComparison.OrdinalIgnoreCase)) == true;
        }
        catch { return false; }
    }
}
