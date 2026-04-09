using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;

namespace CommunityStats.Patches;

/// <summary>
/// Postfix on NRunHistory.SelectPlayer to inject the per-run statistics
/// section underneath the existing run details (PRD §3.12).
/// Phase 6 task 6.
/// </summary>
[HarmonyPatch]
public static class RunHistoryPatch
{
    private const string SectionMeta = "sts_run_hist_stats";

    [HarmonyPatch(typeof(NRunHistory), "SelectPlayer")]
    [HarmonyPostfix]
    public static void AfterSelectPlayer(NRunHistory __instance)
    {
        if (!ModConfig.Toggles.CareerStats) return;
        Safe.Run(() => Inject(__instance));
    }

    private static void Inject(NRunHistory screen)
    {
        // Read private _history + _screenContents + _deckHistory fields.
        RunHistory? history;
        Control? screenContents;
        Control? deckHistory;
        Control? relicHistory;
        try
        {
            history = Traverse.Create(screen).Field("_history").GetValue<RunHistory>();
            screenContents = Traverse.Create(screen).Field("_screenContents").GetValue<Control>();
            deckHistory = Traverse.Create(screen).Field("_deckHistory").GetValue<Control>();
            relicHistory = Traverse.Create(screen).Field("_relicHistory").GetValue<Control>();
        }
        catch (System.Exception ex)
        {
            Safe.Warn($"RunHistoryPatch: failed to read NRunHistory fields: {ex.Message}");
            return;
        }
        if (history == null || screenContents == null) return;

        // Diagnostic ancestry dump (helps debug layout issues round-by-round).
        if (deckHistory != null)
            Safe.Info("[RunHistoryPatch] deckHistory ancestry:\n" +
                      CommunityStats.Util.LayoutHelper.DescribeAncestry(deckHistory));

        // Idempotent: drop any previously injected wrapper from screenContents
        // AND from any layout ancestor we may have inserted into.
        RemoveExistingSection(screenContents);
        var ancestor = CommunityStats.Util.LayoutHelper.FindLayoutAncestor(deckHistory ?? relicHistory);
        if (ancestor != null) RemoveExistingSection(ancestor);

        var section = RunHistoryStatsSection.Create(history);
        section.SetMeta(SectionMeta, true);
        section.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        // Round 7: try the layout-ancestor approach. If we land in a real
        // VBox/HBox, the section sits naturally next to deck/relic history.
        var added = CommunityStats.Util.LayoutHelper.AppendToLayoutAncestor(
            deckHistory ?? relicHistory ?? screenContents, section, moveBeforeAnchor: false);
        if (added != null)
        {
            Safe.Info($"[RunHistoryPatch] injected into {added.GetType().Name} \"{added.Name}\"");
            return;
        }

        // Fallback: legacy right-side floating ScrollContainer wrapper.
        var wrapper = new ScrollContainer
        {
            Name = "ModRunHistoryStatsWrapper",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        wrapper.SetMeta(SectionMeta, true);
        wrapper.AnchorLeft   = 1.0f;
        wrapper.AnchorRight  = 1.0f;
        wrapper.AnchorTop    = 0.0f;
        wrapper.AnchorBottom = 1.0f;
        wrapper.OffsetLeft   = -360f;
        wrapper.OffsetRight  = -20f;
        wrapper.OffsetTop    = 160f;
        wrapper.OffsetBottom = -50f;
        wrapper.GrowHorizontal = Control.GrowDirection.Begin;
        wrapper.AddChild(section);
        screenContents.AddChild(wrapper);
        Safe.Info("[RunHistoryPatch] fallback: floating right-column ScrollContainer");
    }

    private static void RemoveExistingSection(Node container)
    {
        for (int i = container.GetChildCount() - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child.HasMeta(SectionMeta))
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
