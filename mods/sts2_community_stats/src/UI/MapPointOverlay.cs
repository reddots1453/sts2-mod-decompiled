using CommunityStats.Api;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// Overlays death rate / average damage on map encounter nodes.
/// Attaches a small label below the map point icon.
/// </summary>
public static class MapPointOverlay
{
    private const string OverlayMeta = "cs_overlay";

    /// <summary>
    /// Attach encounter stats overlay to a map point node.
    /// Skips if already attached or no data available.
    /// </summary>
    public static void AttachTo(Control mapPointNode, EncounterStats? stats)
    {
        if (mapPointNode.HasMeta(OverlayMeta)) return;
        if (stats == null) return;

        var label = StatsLabel.ForEncounter(stats);
        label.Position = new Vector2(-30, mapPointNode.Size.Y + 2);
        // Round 9 round 49: previously ZIndex = 10 — that punches above any
        // later sibling Control in the same CanvasLayer, so opening the
        // 百科大全 (compendium) screen on top of the map showed our overlay
        // bleeding through. Default ZIndex (0) lets normal sibling tree-order
        // hide it correctly.
        label.ZIndex = 0;

        mapPointNode.AddChild(label);
        mapPointNode.SetMeta(OverlayMeta, true);
    }

    /// <summary>
    /// Remove overlay from a map point (e.g., when filter changes).
    /// </summary>
    public static void DetachFrom(Control mapPointNode)
    {
        if (!mapPointNode.HasMeta(OverlayMeta)) return;

        foreach (var child in mapPointNode.GetChildren())
        {
            if (child is StatsLabel)
            {
                child.QueueFree();
                break;
            }
        }
        mapPointNode.RemoveMeta(OverlayMeta);
    }
}
