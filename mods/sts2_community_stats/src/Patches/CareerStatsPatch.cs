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
        // Round 9 fix: previously the section ended up BELOW the character
        // grid because we relied on `LayoutHelper.AppendToLayoutAncestor`,
        // which lands in the first VBox/Scroll ancestor and just appends
        // there. Per user feedback, the section must sit BETWEEN the
        // overall stats grid and the character grid. We now look up
        // `_characterStatContainer`'s direct parent and insert the section
        // as a sibling immediately before it, so the visual order is
        // overall → CareerStats → 角色数据.
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

        var siblingParent = characterContainer.GetParent() as Control;
        if (siblingParent == null)
        {
            Safe.Warn("CareerStatsPatch: characterContainer has no Control parent.");
            return;
        }

        // Diagnostic: log the ancestor chain so we know where the section is going.
        Safe.Info("[CareerStatsPatch] character container ancestry:\n" +
                  CommunityStats.Util.LayoutHelper.DescribeAncestry(characterContainer));

        // Idempotent cleanup — drop any previously injected section from
        // every container that historic versions of this patch may have used.
        RemoveExistingSection(characterContainer);
        RemoveExistingSection(siblingParent);
        var ancestor = CommunityStats.Util.LayoutHelper.FindLayoutAncestor(characterContainer);
        if (ancestor != null && ancestor != siblingParent) RemoveExistingSection(ancestor);
        if (grid != null) RemoveExistingSection(grid);

        var section = CareerStatsSection.Create(characterFilter: null);
        section.SetMeta(SectionMeta, true);
        section.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        // Insert as a direct sibling immediately before _characterStatContainer.
        siblingParent.AddChild(section);
        try
        {
            int targetIdx = characterContainer.GetIndex();
            siblingParent.MoveChild(section, targetIdx);
            Safe.Info($"[CareerStatsPatch] inserted before character container at index {targetIdx} in {siblingParent.GetType().Name} \"{siblingParent.Name}\"");
        }
        catch (System.Exception ex)
        {
            Safe.Warn($"CareerStatsPatch: MoveChild failed ({ex.Message}); section appended at end.");
        }
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
