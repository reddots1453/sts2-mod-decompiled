using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;

namespace CommunityStats.Patches;

/// <summary>
/// Postfix on NGeneralStatsGrid.LoadStats to append the mod's career stats
/// section under the existing per-character stats container.
/// PRD §3.11. Phase 6 task 4.
/// </summary>
[HarmonyPatch]
public static class CareerStatsPatch
{
    private const string SectionMeta = "sts_career_stats_section";

    [HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
    [HarmonyPostfix]
    public static void AfterLoadStats(NGeneralStatsGrid __instance)
    {
        if (!ModConfig.Toggles.CareerStats) return;
        Safe.Run(() => Inject(__instance));
    }

    private static void Inject(NGeneralStatsGrid grid)
    {
        // PRD §3.11 round 7: walk the parent chain from `_characterStatContainer`
        // looking for the first auto-layout ancestor (VBox / GridContainer /
        // ScrollContainer). Add the career section as a child of THAT
        // container so Godot lays it out alongside the native overall + character
        // sections, instead of dropping it at (0,0) and overlapping the cards.
        Control? characterContainer = null;
        try
        {
            characterContainer = Traverse.Create(grid)
                .Field("_characterStatContainer")
                .GetValue<Control>();
        }
        catch (System.Exception ex)
        {
            Safe.Warn($"CareerStatsPatch: failed to read _characterStatContainer: {ex.Message}");
            return;
        }
        if (characterContainer == null) return;

        // Diagnostic: log the ancestor chain so we know where the section is going.
        Safe.Info("[CareerStatsPatch] character container ancestry:\n" +
                  CommunityStats.Util.LayoutHelper.DescribeAncestry(characterContainer));

        // Idempotent: drop any previously injected section from BOTH the
        // character container and any layout ancestor we might have used
        // before, so re-opening the screen replaces it cleanly.
        RemoveExistingSection(characterContainer);
        var ancestor = CommunityStats.Util.LayoutHelper.FindLayoutAncestor(characterContainer);
        if (ancestor != null) RemoveExistingSection(ancestor);
        if (grid != null) RemoveExistingSection(grid);

        var section = CareerStatsSection.Create(characterFilter: null);
        section.SetMeta(SectionMeta, true);

        // Try the layout ancestor first (insert RIGHT BEFORE the character
        // container's path so the visual order is overall → career → characters).
        var added = CommunityStats.Util.LayoutHelper.AppendToLayoutAncestor(
            characterContainer, section, moveBeforeAnchor: true);
        if (added != null)
        {
            Safe.Info($"[CareerStatsPatch] injected into {added.GetType().Name} \"{added.Name}\"");
            return;
        }

        // Fallback A: add as the first child of the character container itself.
        // If the container is a VBox, the section appears above the cards.
        characterContainer.AddChild(section);
        try { characterContainer.MoveChild(section, 0); } catch { }
        Safe.Info("[CareerStatsPatch] fallback: added as first child of character container");
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
