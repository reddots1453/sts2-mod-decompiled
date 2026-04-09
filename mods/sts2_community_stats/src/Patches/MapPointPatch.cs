using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

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
        if (!ModConfig.Toggles.MonsterDanger) return;
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

    // ── Hover panels (PRD 3.8 unknown room + 3.16 shop prices) ─

    private const string HoverPanelMeta = "sts_hover_info_panel";

    [HarmonyPatch(typeof(NMapPoint), "OnFocus")]
    [HarmonyPostfix]
    public static void AfterOnFocus(NMapPoint __instance)
    {
        Safe.Run(() => ShowHoverPanel(__instance));
    }

    [HarmonyPatch(typeof(NMapPoint), "OnUnfocus")]
    [HarmonyPostfix]
    public static void AfterOnUnfocus(NMapPoint __instance)
    {
        Safe.Run(() => HideHoverPanel(__instance));
    }

    private static void ShowHoverPanel(NMapPoint mapPoint)
    {
        if (mapPoint.HasMeta(HoverPanelMeta)) return;

        var point = mapPoint.Point;
        if (point == null) return;

        InfoModPanel? panel = null;

        if (point.PointType == MapPointType.Unknown
            && mapPoint.State != MapPointState.Traveled
            && ModConfig.Toggles.UnknownRoomOdds)
        {
            panel = BuildUnknownPanel();
        }
        else if (point.PointType == MapPointType.Shop
            && mapPoint.State != MapPointState.Traveled
            && ModConfig.Toggles.ShopPrices)
        {
            var player = RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault();
            panel = ShopPricePanel.Create(player, GetShopRemovalsUsed());
        }

        if (panel == null) return;

        mapPoint.AddChild(panel);
        panel.ZIndex = 200;
        panel.GlobalPosition = mapPoint.GlobalPosition + new Vector2(40f, 0f);
        mapPoint.SetMeta(HoverPanelMeta, true);
    }

    private static InfoModPanel? BuildUnknownPanel()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        var odds = runState?.Odds?.UnknownMapPoint;
        if (odds == null) return null;
        return UnknownRoomPanel.Create(odds, runState);
    }

    private static int GetShopRemovalsUsed()
    {
        try
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            var me = runState?.Players?.FirstOrDefault();
            if (me == null) return 0;
            var used = Traverse.Create(me).Field("ExtraFields")
                .Field("CardShopRemovalsUsed").GetValue<int>();
            return used;
        }
        catch { return 0; }
    }

    private static void HideHoverPanel(NMapPoint mapPoint)
    {
        if (!mapPoint.HasMeta(HoverPanelMeta)) return;

        foreach (var child in mapPoint.GetChildren())
        {
            if (child is InfoModPanel)
            {
                child.QueueFree();
                break;
            }
        }
        mapPoint.RemoveMeta(HoverPanelMeta);
    }

}
