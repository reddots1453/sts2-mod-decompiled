using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CommunityStats.Patches;

/// <summary>
/// Hooks NIntent.OnHovered / OnUnhovered (private) to display the
/// IntentStateMachinePanel for the owning monster. PRD 3.10.
/// </summary>
[HarmonyPatch]
public static class IntentHoverPatch
{
    private const string PanelMeta = "sts_intent_state_panel";

    [HarmonyPatch(typeof(NIntent), "OnHovered")]
    [HarmonyPostfix]
    public static void AfterOnHovered(NIntent __instance)
    {
        if (!ModConfig.Toggles.IntentStateMachine) return;
        Safe.Run(() => ShowPanel(__instance));
    }

    [HarmonyPatch(typeof(NIntent), "OnUnhovered")]
    [HarmonyPostfix]
    public static void AfterOnUnhovered(NIntent __instance)
    {
        Safe.Run(() => HidePanel(__instance));
    }

    private static void ShowPanel(NIntent intentNode)
    {
        if (intentNode.HasMeta(PanelMeta)) return;

        Creature? owner = null;
        try
        {
            owner = Traverse.Create(intentNode).Field("_owner").GetValue<Creature>();
        }
        catch (System.Exception ex)
        {
            Safe.Warn($"IntentHoverPatch: failed to read _owner: {ex.Message}");
            return;
        }
        if (owner == null) return;

        var panel = IntentStateMachinePanel.Create(owner);
        if (panel == null) return;

        intentNode.AddChild(panel);
        panel.ZIndex = 500;
        panel.GlobalPosition = intentNode.GlobalPosition + new Vector2(0f, -160f);
        intentNode.SetMeta(PanelMeta, true);
    }

    private static void HidePanel(NIntent intentNode)
    {
        if (!intentNode.HasMeta(PanelMeta)) return;

        foreach (var child in intentNode.GetChildren())
        {
            if (child is InfoModPanel)
            {
                child.QueueFree();
                break;
            }
        }
        intentNode.RemoveMeta(PanelMeta);
    }
}
