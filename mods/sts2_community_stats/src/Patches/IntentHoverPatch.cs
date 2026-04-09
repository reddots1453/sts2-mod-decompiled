using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CommunityStats.Patches;

/// <summary>
/// Round 8: hover trigger moved from NIntent (intent icon) to NCreature
/// (the monster body itself). The user wanted hovering anywhere on the
/// monster to surface the state machine, not just the small intent icon.
///
/// `NCreature.OnFocus` / `OnUnfocus` are private signal handlers that
/// already wire `Hitbox.MouseEntered`/`MouseExited`, so a Harmony postfix
/// gives us a single shared trigger across both mouse and controller focus.
/// </summary>
[HarmonyPatch]
public static class IntentHoverPatch
{
    private const string PanelMeta = "sts_intent_state_panel";

    [HarmonyPatch(typeof(NCreature), "OnFocus")]
    [HarmonyPostfix]
    public static void AfterCreatureFocus(NCreature __instance)
    {
        if (!ModConfig.Toggles.IntentStateMachine) return;
        Safe.Run(() => ShowPanel(__instance));
    }

    [HarmonyPatch(typeof(NCreature), "OnUnfocus")]
    [HarmonyPostfix]
    public static void AfterCreatureUnfocus(NCreature __instance)
    {
        Safe.Run(() => HidePanel(__instance));
    }

    private static void ShowPanel(NCreature creatureNode)
    {
        if (creatureNode == null) return;
        if (creatureNode.HasMeta(PanelMeta)) return;

        var entity = creatureNode.Entity;
        if (entity == null) return;
        // Players don't have monster state machines.
        if (entity.IsPlayer) return;
        if (entity.Monster == null) return;

        var panel = IntentStateMachinePanel.Create(entity);
        if (panel == null)
        {
            Safe.Warn($"[IntentHover] Create() returned null for {entity.Monster.Id.Entry}");
            return;
        }

        creatureNode.AddChild(panel);
        panel.ZIndex = 500;
        creatureNode.SetMeta(PanelMeta, true);

        // Defer position so panel.Size is populated, then clamp to viewport.
        panel.Ready += () => ClampPanelPosition(creatureNode, panel);
        panel.GlobalPosition = creatureNode.GlobalPosition + new Vector2(20f, -200f);
    }

    private static void ClampPanelPosition(NCreature creatureNode, Control panel)
    {
        Safe.Run(() =>
        {
            var viewport = panel.GetViewportRect().Size;
            var size = panel.Size;
            float x = creatureNode.GlobalPosition.X + 20f;
            float y = creatureNode.GlobalPosition.Y - size.Y - 12f;
            if (x + size.X > viewport.X - 8f) x = viewport.X - size.X - 8f;
            if (y + size.Y > viewport.Y - 8f) y = viewport.Y - size.Y - 8f;
            if (x < 8f) x = 8f;
            if (y < 8f) y = 8f;
            panel.GlobalPosition = new Vector2(x, y);
        });
    }

    private static void HidePanel(NCreature creatureNode)
    {
        if (creatureNode == null) return;
        if (!creatureNode.HasMeta(PanelMeta)) return;

        for (int i = creatureNode.GetChildCount() - 1; i >= 0; i--)
        {
            var child = creatureNode.GetChild(i);
            if (child is InfoModPanel)
            {
                creatureNode.RemoveChild(child);
                child.QueueFree();
                break;
            }
        }
        creatureNode.RemoveMeta(PanelMeta);
    }
}
