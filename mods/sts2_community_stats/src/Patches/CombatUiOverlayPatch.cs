using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CommunityStats.Patches;

/// <summary>
/// Injects the potion-drop and card-drop odds indicators into the top-right of
/// the combat UI. PRD 3.9 + 3.17.
/// </summary>
[HarmonyPatch]
public static class CombatUiOverlayPatch
{
    private const string PotionMeta = "sts_potion_indicator";
    private const string CardDropMeta = "sts_card_drop_indicator";

    [HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
    [HarmonyPostfix]
    public static void AfterActivate(NCombatUi __instance, CombatState state)
    {
        Safe.Run(() => InjectIndicators(__instance, state));
    }

    private static void InjectIndicators(NCombatUi ui, CombatState state)
    {
        Player? me = null;
        try { me = LocalContext.GetMe(state); } catch { }
        if (me == null) return;

        // Potion odds
        if (ModConfig.Toggles.PotionDropOdds && !ui.HasMeta(PotionMeta))
        {
            var potion = PotionOddsIndicator.Create();
            potion.AnchorLeft = 1.0f;
            potion.AnchorRight = 1.0f;
            potion.AnchorTop = 0.0f;
            potion.AnchorBottom = 0.0f;
            potion.OffsetLeft = -200;
            potion.OffsetRight = -110;
            potion.OffsetTop = 10;
            potion.OffsetBottom = 32;
            ui.AddChild(potion);
            ui.SetMeta(PotionMeta, true);

            try
            {
                var oddsValue = me.PlayerOdds?.PotionReward?.CurrentValue ?? 0.4f;
                potion.UpdateOdds(oddsValue);
            }
            catch (System.Exception ex)
            {
                Safe.Warn($"PotionOdds update failed: {ex.Message}");
            }
        }

        // Card drop odds
        if (ModConfig.Toggles.CardDropOdds && !ui.HasMeta(CardDropMeta))
        {
            var drops = CardDropOddsIndicator.Create();
            drops.AnchorLeft = 1.0f;
            drops.AnchorRight = 1.0f;
            drops.AnchorTop = 0.0f;
            drops.AnchorBottom = 0.0f;
            drops.OffsetLeft = -100;
            drops.OffsetRight = -10;
            drops.OffsetTop = 10;
            drops.OffsetBottom = 32;
            ui.AddChild(drops);
            ui.SetMeta(CardDropMeta, true);

            try
            {
                var offset = me.PlayerOdds?.CardRarity?.CurrentValue ?? -0.05f;
                drops.UpdateOffset(offset);
            }
            catch (System.Exception ex)
            {
                Safe.Warn($"CardDropOdds update failed: {ex.Message}");
            }
        }
    }
}
