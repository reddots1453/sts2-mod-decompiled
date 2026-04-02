using CommunityStats.Collection;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CommunityStats.Patches;

[HarmonyPatch]
public static class CombatHistoryPatch
{
    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
    [HarmonyPostfix]
    public static void AfterCardPlayStarted(CombatState combatState, CardPlay cardPlay)
    {
        Safe.Run(() =>
        {
            var cardId = cardPlay.Card?.Id.Entry;
            if (cardId != null)
                CombatTracker.Instance.OnCardPlayStarted(cardId);
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
    [HarmonyPostfix]
    public static void AfterCardPlayFinished(CombatState combatState, CardPlay cardPlay)
    {
        Safe.Run(() => CombatTracker.Instance.OnCardPlayFinished());
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.DamageReceived))]
    [HarmonyPostfix]
    public static void AfterDamageReceived(CombatState combatState,
        Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
    {
        Safe.Run(() =>
        {
            var isPlayerReceiver = receiver.IsPlayer;
            var cardSourceId = cardSource?.Id.Entry;
            CombatTracker.Instance.OnDamageDealt(result.TotalDamage, cardSourceId, isPlayerReceiver);

            if (isPlayerReceiver && result.WasTargetKilled)
                CombatTracker.Instance.OnPlayerDied();

            // Attribute debuff bonuses (Vulnerable, etc.) to the card that applied them
            if (!isPlayerReceiver && result.TotalDamage > 0)
            {
                bool hasVulnerable = receiver.HasPower<VulnerablePower>();
                CombatTracker.Instance.AttributeDebuffBonuses(
                    receiver.GetHashCode(), result.TotalDamage, hasVulnerable);
            }
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.BlockGained))]
    [HarmonyPostfix]
    public static void AfterBlockGained(CombatState combatState,
        Creature receiver, int amount, ValueProp props, CardPlay? cardPlay)
    {
        Safe.Run(() =>
        {
            var cardPlayId = cardPlay?.Card?.Id.Entry;
            CombatTracker.Instance.OnBlockGained(amount, cardPlayId);
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PowerReceived))]
    [HarmonyPostfix]
    public static void AfterPowerReceived(CombatState combatState,
        PowerModel power, decimal amount, Creature? applier)
    {
        Safe.Run(() =>
        {
            var powerId = power?.Id.Entry;
            if (powerId == null) return;

            // Determine which creature received this power
            int creatureHash = power?.Owner?.GetHashCode() ?? 0;
            CombatTracker.Instance.OnPowerApplied(powerId, amount, creatureHash);
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDrawn))]
    [HarmonyPostfix]
    public static void AfterCardDrawn(CombatState combatState, CardModel card, bool fromHandDraw)
    {
        Safe.Run(() => CombatTracker.Instance.OnCardDrawn(fromHandDraw));
    }
}
