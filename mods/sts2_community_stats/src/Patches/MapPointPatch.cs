using CommunityStats.Api;
using CommunityStats.UI;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace CommunityStats.Patches;

/// <summary>
/// Patches NMapPoint.RefreshVisualsInstantly to overlay encounter danger stats
/// (death rate, avg damage) on combat map nodes.
/// </summary>
[HarmonyPatch]
public static class MapPointPatch
{
    private static readonly HashSet<MapPointType> CombatPointTypes = new()
    {
        MapPointType.Monster,
        MapPointType.Elite,
        MapPointType.Boss
    };

    [HarmonyPatch(typeof(NMapPoint), nameof(NMapPoint.RefreshVisualsInstantly))]
    [HarmonyPostfix]
    public static void AfterRefreshVisuals(NMapPoint __instance)
    {
        Safe.Run(() =>
        {
            var point = __instance.Point;
            if (point == null) return;
            if (!CombatPointTypes.Contains(point.PointType)) return;

            // Only show stats on nodes the player has already visited
            if (__instance.State != MapPointState.Traveled)
            {
                // Remove overlay if node reverts to non-traveled state (e.g., filter change)
                MapPointOverlay.DetachFrom(__instance);
                return;
            }

            // Use the point type as a general encounter category for stats lookup
            var encounterType = point.PointType.ToString().ToLowerInvariant();

            // Try to get encounter stats from the bulk bundle
            var stats = StatsProvider.Instance.GetEncounterStats(encounterType);
            MapPointOverlay.AttachTo(__instance, stats);
        });
    }
}
