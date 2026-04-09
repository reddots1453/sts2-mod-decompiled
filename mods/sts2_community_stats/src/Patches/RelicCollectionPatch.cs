using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace CommunityStats.Patches;

/// <summary>
/// Compendium relic collection: append community win-rate + delta to each
/// relic entry's hover. PRD 3.3/3.4.
///
/// Uses a child StatsLabel attached to the entry (same strategy as
/// RelicHoverPatch) — the label is created on OnFocus and removed on
/// OnUnfocus. We only show stats when the relic is actually visible
/// (not locked / unseen).
/// </summary>
[HarmonyPatch]
public static class RelicCollectionPatch
{
    private const string StatsLabelMeta = "sts_relic_collection_stats";

    [HarmonyPatch(typeof(NRelicCollectionEntry), "OnFocus")]
    [HarmonyPostfix]
    public static void AfterOnFocus(NRelicCollectionEntry __instance)
    {
        if (!ModConfig.Toggles.RelicStats) return;
        Safe.Run(() => ShowStats(__instance));
    }

    [HarmonyPatch(typeof(NRelicCollectionEntry), "OnUnfocus")]
    [HarmonyPostfix]
    public static void AfterOnUnfocus(NRelicCollectionEntry __instance)
    {
        Safe.Run(() => RemoveStats(__instance));
    }

    private static void ShowStats(NRelicCollectionEntry entry)
    {
        if (entry.HasMeta(StatsLabelMeta)) return;
        if (entry.ModelVisibility != ModelVisibility.Visible) return;

        var relic = entry.relic;
        if (relic == null) return;

        string? relicId = null;
        try { relicId = relic.Id.Entry; } catch { }
        if (string.IsNullOrEmpty(relicId)) return;

        StatsLabel label;
        if (!StatsProvider.Instance.HasBundle)
        {
            label = StatsLabel.ForLoading();
        }
        else
        {
            var stats = StatsProvider.Instance.GetRelicStats(relicId);
            if (stats != null)
            {
                var globalAvg = StatsProvider.Instance.GetGlobalAverageRelicWinRate();
                label = StatsLabel.ForRelicStatsWithDelta(stats, globalAvg);
            }
            else
            {
                label = StatsLabel.ForUnavailable();
            }
        }

        label.Position = new Vector2(-30, entry.Size.Y + 2);
        label.ZIndex = 100;
        entry.AddChild(label);
        entry.SetMeta(StatsLabelMeta, true);
    }

    private static void RemoveStats(NRelicCollectionEntry entry)
    {
        if (!entry.HasMeta(StatsLabelMeta)) return;

        foreach (var child in entry.GetChildren())
        {
            if (child is StatsLabel)
            {
                child.QueueFree();
                break;
            }
        }
        entry.RemoveMeta(StatsLabelMeta);
    }
}
