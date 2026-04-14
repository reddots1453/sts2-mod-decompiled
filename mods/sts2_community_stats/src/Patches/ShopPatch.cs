using CommunityStats.Api;
using CommunityStats.Collection;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;

namespace CommunityStats.Patches;

/// <summary>
/// Patches shop purchase flow to record purchases and display buy rates.
/// Covers cards, relics, and potions.
/// </summary>
[HarmonyPatch]
public static class ShopPatch
{
    private const string StatsLabelMeta = "cs_shop_stats";

    // PRD §3.13: track live slot nodes so we can refresh their labels
    // when StatsProvider.DataRefreshed fires (e.g. after F9 apply).
    private static readonly List<WeakReference<NMerchantCard>> _liveCards = new();
    private static readonly List<WeakReference<NMerchantRelic>> _liveRelics = new();

    public static void SubscribeRefresh()
    {
        StatsProvider.DataRefreshed += OnDataRefreshed;
    }

    private static void OnDataRefreshed()
    {
        Safe.Run(() =>
        {
            for (int i = _liveCards.Count - 1; i >= 0; i--)
            {
                if (_liveCards[i].TryGetTarget(out var card) &&
                    GodotObject.IsInstanceValid(card) && card.IsInsideTree())
                    AfterCardFillSlot(card);
                else
                    _liveCards.RemoveAt(i);
            }
            for (int i = _liveRelics.Count - 1; i >= 0; i--)
            {
                if (_liveRelics[i].TryGetTarget(out var relic) &&
                    GodotObject.IsInstanceValid(relic) && relic.IsInsideTree())
                    AfterRelicFillSlot(relic);
                else
                    _liveRelics.RemoveAt(i);
            }
        });
    }

    private static void TrackCard(NMerchantCard card)
    {
        for (int i = _liveCards.Count - 1; i >= 0; i--)
        {
            if (!_liveCards[i].TryGetTarget(out var t) || !GodotObject.IsInstanceValid(t))
                _liveCards.RemoveAt(i);
            else if (t == card) return;
        }
        _liveCards.Add(new WeakReference<NMerchantCard>(card));
    }

    private static void TrackRelic(NMerchantRelic relic)
    {
        for (int i = _liveRelics.Count - 1; i >= 0; i--)
        {
            if (!_liveRelics[i].TryGetTarget(out var t) || !GodotObject.IsInstanceValid(t))
                _liveRelics.RemoveAt(i);
            else if (t == relic) return;
        }
        _liveRelics.Add(new WeakReference<NMerchantRelic>(relic));
    }

    // ── Shop Card Offerings Recording (on shop enter) ───────

    [HarmonyPatch(typeof(MerchantRoom), nameof(MerchantRoom.Enter))]
    [HarmonyPostfix]
    public static void AfterMerchantRoomEnter(MerchantRoom __instance)
    {
        Safe.Run(() =>
        {
            var inventory = __instance.Inventory;
            if (inventory == null) return;

            var floor = RunDataCollector.CurrentFloor;

            foreach (var entry in inventory.CharacterCardEntries)
            {
                if (!entry.IsStocked || entry.CreationResult == null) continue;
                var card = entry.CreationResult.Card;
                var cardId = card?.Id?.Entry;
                if (cardId != null)
                    RunDataCollector.RecordShopCardOffering(cardId, floor);
            }

            foreach (var entry in inventory.ColorlessCardEntries)
            {
                if (!entry.IsStocked || entry.CreationResult == null) continue;
                var card = entry.CreationResult.Card;
                var cardId = card?.Id?.Entry;
                if (cardId != null)
                    RunDataCollector.RecordShopCardOffering(cardId, floor);
            }
        });
    }

    // ── Card Purchase Recording ─────────────────────────────

    [HarmonyPatch(typeof(NMerchantCard), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforeCardPurchaseVisual(NMerchantCard __instance)
    {
        Safe.Run(() =>
        {
            var cardNode = Traverse.Create(__instance).Field("_cardNode").GetValue<object>();
            if (cardNode == null) return;

            var model = Traverse.Create(cardNode).Property("Model").GetValue<object>();
            if (model == null) return;

            var id = Traverse.Create(model).Property("Id").GetValue<object>();
            var entry = Traverse.Create(id).Property("Entry").GetValue<string>();

            var cardEntry = Traverse.Create(__instance).Field("_cardEntry").GetValue<object>();
            var cost = cardEntry != null
                ? Traverse.Create(cardEntry).Property("Cost").GetValue<int>()
                : 0;

            if (entry != null)
            {
                RunDataCollector.RecordShopPurchase(entry, "card", cost, RunDataCollector.CurrentFloor);
            }
        });
    }

    // ── Relic Purchase Recording ────────────────────────────

    [HarmonyPatch(typeof(NMerchantRelic), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforeRelicPurchaseVisual(NMerchantRelic __instance)
    {
        Safe.Run(() =>
        {
            var relic = Traverse.Create(__instance).Field("_relic").GetValue<object>();
            if (relic == null) return;

            var id = Traverse.Create(relic).Property("Id").GetValue<object>();
            var entry = Traverse.Create(id).Property("Entry").GetValue<string>();

            var relicEntry = Traverse.Create(__instance).Field("_relicEntry").GetValue<object>();
            var cost = relicEntry != null
                ? Traverse.Create(relicEntry).Property("Cost").GetValue<int>()
                : 0;

            if (entry != null)
            {
                RunDataCollector.RecordShopPurchase(entry, "relic", cost, RunDataCollector.CurrentFloor);
            }
        });
    }

    // ── Potion Purchase Recording ───────────────────────────

    [HarmonyPatch(typeof(NMerchantPotion), "OnSuccessfulPurchase")]
    [HarmonyPrefix]
    public static void BeforePotionPurchaseVisual(NMerchantPotion __instance)
    {
        Safe.Run(() =>
        {
            var potion = Traverse.Create(__instance).Field("_potion").GetValue<object>();
            if (potion == null) return;

            var id = Traverse.Create(potion).Property("Id").GetValue<object>();
            var entry = Traverse.Create(id).Property("Entry").GetValue<string>();

            var potionEntry = Traverse.Create(__instance).Field("_potionEntry").GetValue<object>();
            var cost = potionEntry != null
                ? Traverse.Create(potionEntry).Property("Cost").GetValue<int>()
                : 0;

            if (entry != null)
            {
                RunDataCollector.RecordShopPurchase(entry, "potion", cost, RunDataCollector.CurrentFloor);
            }
        });
    }

    // ── Shop Card Buy Rate Display ──────────────────────────

    [HarmonyPatch(typeof(NMerchantCard), nameof(NMerchantCard.FillSlot))]
    [HarmonyPostfix]
    public static void AfterCardFillSlot(NMerchantCard __instance)
    {
        Safe.Run(() =>
        {
            // Use the slot's Hitbox as anchor parent — it matches the actual
            // card click bounds, unlike _cardHolder which is often a zero-sized
            // pivot and pushes anchored children to the scene origin.
            var hitbox = __instance.Hitbox;
            if (hitbox == null || !GodotObject.IsInstanceValid(hitbox)) return;

            RemoveExistingLabel(hitbox);
            TrackCard(__instance);

            var cardNode = Traverse.Create(__instance).Field("_cardNode").GetValue<object>();
            if (cardNode == null) return;

            var model = Traverse.Create(cardNode).Property("Model").GetValue<object>();
            if (model == null) return;

            var id = Traverse.Create(model).Property("Id").GetValue<object>();
            var cardId = Traverse.Create(id).Property("Entry").GetValue<string>();
            if (cardId == null) return;

            var stats = StatsProvider.Instance.GetCardStats(cardId);
            Label label;
            if (stats != null)
                label = StatsLabel.ForShopBuyRate(stats.ShopBuyRate);
            else if (!StatsProvider.Instance.HasBundle)
                label = StatsLabel.ForLoading();
            else
                label = StatsLabel.ForUnavailable();

            label.SetMeta(StatsLabelMeta, true);
            hitbox.AddChild(label);
            // Anchor to bottom-right of the click hitbox (matches visual bounds).
            label.AnchorLeft = 1f;
            label.AnchorTop = 1f;
            label.AnchorRight = 1f;
            label.AnchorBottom = 1f;
            label.OffsetLeft = -110;
            label.OffsetTop = -26;
            label.OffsetRight = -4;
            label.OffsetBottom = -2;
        });
    }

    // ── Shop Relic Buy Rate Display ─────────────────────────

    [HarmonyPatch(typeof(NMerchantRelic), nameof(NMerchantRelic.FillSlot))]
    [HarmonyPostfix]
    public static void AfterRelicFillSlot(NMerchantRelic __instance)
    {
        Safe.Run(() =>
        {
            var hitbox = __instance.Hitbox;
            if (hitbox == null || !GodotObject.IsInstanceValid(hitbox)) return;

            RemoveExistingLabel(hitbox);
            TrackRelic(__instance);

            var relic = Traverse.Create(__instance).Field("_relic").GetValue<object>();
            if (relic == null) return;

            var id = Traverse.Create(relic).Property("Id").GetValue<object>();
            var relicId = Traverse.Create(id).Property("Entry").GetValue<string>();
            if (relicId == null) return;

            var stats = StatsProvider.Instance.GetRelicStats(relicId);
            Label label;
            if (stats != null)
                label = StatsLabel.ForShopBuyRate(stats.ShopBuyRate, fontSize: 14);
            else if (!StatsProvider.Instance.HasBundle)
                label = StatsLabel.ForLoading();
            else
                label = StatsLabel.ForUnavailable();

            label.SetMeta(StatsLabelMeta, true);
            hitbox.AddChild(label);
            label.AnchorLeft = 1f;
            label.AnchorTop = 1f;
            label.AnchorRight = 1f;
            label.AnchorBottom = 1f;
            label.OffsetLeft = -90;
            label.OffsetTop = -22;
            label.OffsetRight = -4;
            label.OffsetBottom = -2;
        });
    }

    // ── Helpers ──────────────────────────────────────────────

    private static void RemoveExistingLabel(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is Label lbl && lbl.HasMeta(StatsLabelMeta))
                lbl.QueueFree();
        }
    }
}
