using CommunityStats.Api;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace CommunityStats.Patches;

/// <summary>
/// Patches NRelicBasicHolder hover to show relic win/pick rate stats.
/// </summary>
[HarmonyPatch]
public static class RelicHoverPatch
{
    private const string StatsLabelMeta = "cs_relic_stats";

    [HarmonyPatch(typeof(NRelicBasicHolder), "OnFocus")]
    [HarmonyPostfix]
    public static void AfterOnFocus(NRelicBasicHolder __instance)
    {
        Safe.Run(() =>
        {
            if (__instance.HasMeta(StatsLabelMeta)) return;

            var relic = __instance.Relic;
            if (relic?.Model == null) return;

            var relicId = relic.Model.Id.Entry;
            if (string.IsNullOrEmpty(relicId)) return;

            StatsLabel label;
            if (!StatsProvider.Instance.HasBundle)
            {
                label = StatsLabel.ForLoading();
            }
            else
            {
                var stats = StatsProvider.Instance.GetRelicStats(relicId);
                label = stats != null ? StatsLabel.ForRelicStats(stats) : StatsLabel.ForUnavailable();
            }

            // Position below the relic icon
            label.Position = new Vector2(-30, __instance.Size.Y + 2);
            label.ZIndex = 100;
            __instance.AddChild(label);
            __instance.SetMeta(StatsLabelMeta, true);
        });
    }

    [HarmonyPatch(typeof(NRelicBasicHolder), "OnUnfocus")]
    [HarmonyPostfix]
    public static void AfterOnUnfocus(NRelicBasicHolder __instance)
    {
        Safe.Run(() =>
        {
            if (!__instance.HasMeta(StatsLabelMeta)) return;

            foreach (var child in __instance.GetChildren())
            {
                if (child is StatsLabel)
                {
                    child.QueueFree();
                    break;
                }
            }
            __instance.RemoveMeta(StatsLabelMeta);
        });
    }
}
