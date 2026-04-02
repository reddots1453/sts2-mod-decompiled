using CommunityStats.Collection;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace CommunityStats.Patches;

[HarmonyPatch]
public static class EventOptionPatch
{
    private const string StatsLabelMeta = "community_stats_label";

    [HarmonyPatch(typeof(NEventOptionButton), "OnRelease")]
    [HarmonyPostfix]
    public static void AfterEventOptionSelected(NEventOptionButton __instance)
    {
        Safe.Run(() =>
        {
            var eventModel = __instance.Event;
            var option = __instance.Option;
            if (eventModel == null || option == null) return;

            var eventId = eventModel.Id.Entry;
            var index = Traverse.Create(__instance).Property("Index").GetValue<int>();
            var totalOptions = eventModel.CurrentOptions?.Count ?? 0;

            RunDataCollector.RecordEventChoice(eventId, index, totalOptions);
        });
    }

    [HarmonyPatch(typeof(NEventOptionButton), nameof(NEventOptionButton._Ready))]
    [HarmonyPostfix]
    public static void AfterOptionReady(NEventOptionButton __instance)
    {
        Safe.Run(() =>
        {
            var eventModel = __instance.Event;
            if (eventModel == null) return;

            var eventId = eventModel.Id.Entry;
            var index = Traverse.Create(__instance).Property("Index").GetValue<int>();

            // Remove any existing label
            foreach (var child in __instance.GetChildren())
            {
                if (child is Label lbl && lbl.HasMeta(StatsLabelMeta))
                    lbl.QueueFree();
            }

            var eventStats = Api.StatsProvider.Instance.GetEventStats(eventId);
            Label label;
            if (eventStats != null)
            {
                var optionStats = eventStats.Options?.FirstOrDefault(o => o.OptionIndex == index);
                if (optionStats != null)
                    label = UI.StatsLabel.ForEventOption(optionStats);
                else
                    label = UI.StatsLabel.ForUnavailable();
            }
            else if (!Api.StatsProvider.Instance.HasBundle)
            {
                label = UI.StatsLabel.ForLoading();
            }
            else
            {
                label = UI.StatsLabel.ForUnavailable();
            }

            label.SetMeta(StatsLabelMeta, true);
            __instance.AddChild(label);
        });
    }
}
