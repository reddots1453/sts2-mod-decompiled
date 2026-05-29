using System.Linq;
using MoreInfo.Config;
using MoreInfo.UI;
using MoreInfo.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace MoreInfo.Patches;

/// <summary>
/// Hover panels for unvisited map points: Unknown Room encounter odds (PRD §3.8)
/// and Shop price table (PRD §3.16).
/// Extracted from the main mod's MapPointPatch — danger overlay logic stays there.
/// </summary>
[HarmonyPatch]
public static class MoreInfoMapPointPatch
{
    private const string HoverPanelMeta = "sts_moreinfo_hover_panel";

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
            && mapPoint.State != MapPointState.Traveled)
        {
            panel = BuildUnknownPanel();
        }
        else if (point.PointType == MapPointType.Shop
            && mapPoint.State != MapPointState.Traveled)
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
            return me.ExtraFields.CardShopRemovalsUsed;
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
