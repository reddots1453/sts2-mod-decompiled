using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;
using Foresight.Engine;
using Foresight.Util;

namespace Foresight.Patches;

[HarmonyPatch]
public static class RelicHoverPatch
{
    private const string MetaKey = "foresight_hover";
    private const string RelicId = "STS2_FORESIGHT_RELIC_FORESIGHT_EYE";

    // ── debug toggle: set false before release ──
    private static readonly bool Debug = true;

    private static void Log(string msg)
    {
        if (Debug) ForesightMod.Logger.Info($"[HoverPatch] {msg}");
    }

    [HarmonyPatch(typeof(NRelicInventoryHolder), "OnFocus")]
    [HarmonyPostfix]
    public static void AfterFocus(NRelicInventoryHolder __instance)
    {
        Log("=== OnFocus fired ===");

        if (__instance.HasMeta(MetaKey))
        {
            Log("Already has meta, skip");
            return;
        }

        var id = GetRelicId(__instance);
        Log($"Detected relic ID: {id ?? "NULL"}");

        if (id == null)
        {
            Log("GetRelicId returned null, skip");
            return;
        }

        bool idMatch = string.Equals(id, RelicId, System.StringComparison.OrdinalIgnoreCase);
        Log($"ID match ({RelicId}): {idMatch}");

        if (!idMatch) return;

        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null)
        {
            Log("RunState is null, skip");
            return;
        }
        Log($"RunState obtained: players={state.Players?.Count}, actIdx={state.CurrentActIndex}");

        var text = BuildTooltip(state);
        if (string.IsNullOrEmpty(text))
        {
            Log("BuildTooltip returned null/empty");
            return;
        }
        Log($"Tooltip built: {text.Length} chars, {text.Split('\n').Length} lines");

        var label = new Godot.Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", new Godot.Color(0.87f, 0.82f, 0.68f));
        label.AddThemeFontSizeOverride("font_size", 13);
        label.Position = new Godot.Vector2(-30, __instance.Size.Y + 10);
        label.ZIndex = 101;
        __instance.AddChild(label);
        __instance.SetMeta(MetaKey, true);
        Log("Label attached successfully");
    }

    [HarmonyPatch(typeof(NRelicInventoryHolder), "OnUnfocus")]
    [HarmonyPostfix]
    public static void AfterUnfocus(NRelicInventoryHolder __instance)
    {
        Log("=== OnUnfocus fired ===");
        int removed = 0;
        foreach (var child in __instance.GetChildren())
        {
            if (child is Godot.Label l && l.HasMeta(MetaKey))
            {
                l.QueueFree();
                removed++;
            }
        }
        __instance.RemoveMeta(MetaKey);
        Log($"Removed {removed} label(s)");
    }

    private static string? GetRelicId(NRelicInventoryHolder holder)
    {
        try
        {
            var relic = holder.Relic;
            Log($"holder.Relic is null: {relic == null}");
            var model = relic?.Model;
            Log($"relic.Model is null: {model == null}");
            var id = model?.Id.Entry;
            Log($"holder.Relic.Model.Id.Entry: {id ?? "NULL"}");
            if (!string.IsNullOrEmpty(id)) return id;
        }
        catch (Exception ex)
        {
            Log($"holder.Relic path exception: {ex.GetType().Name}: {ex.Message}");
        }
        try
        {
            var m = Traverse.Create(holder).Field("_model")
                .GetValue<MegaCrit.Sts2.Core.Models.RelicModel>();
            Log($"_model field: {m?.Id.Entry ?? "NULL"}");
            return m?.Id.Entry;
        }
        catch (Exception ex)
        {
            Log($"_model fallback exception: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static string? BuildTooltip(RunState state)
    {
        var sb = new StringBuilder();
        try
        {
            var player = state.Players[0];
            if (player == null)
            {
                Log("player[0] is null");
                return null;
            }
            Log($"Player obtained: char={player.Character?.Id.Entry}, relics={player.Relics?.Count}");

            var snapshot = RngSnapshot.FromPlayer(player);
            Log($"Snapshot: seed={snapshot.RewardsSeed}, counter={snapshot.RewardsCounter}, cardPity={snapshot.CardRarityPity:F4}, potPity={snapshot.PotionDropPity:F4}");

            // ── Card Rewards ──
            Log("--- Predicting cards ---");
            List<CardRewardPredictor.FightReward> cards;
            try
            {
                cards = CardRewardPredictor.PredictNext(player, snapshot, 3);
                Log($"Card prediction: {cards.Count} fight entries");
            }
            catch (Exception ex)
            {
                Log($"Card prediction FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                cards = new();
            }

            sb.AppendLine("── 卡牌奖励 ──");
            foreach (var f in cards)
            {
                var cardNames = f.Cards
                    .Select(c => c.CardId != null ? NameLookup.Card(c.CardId) : "?")
                    .ToList();
                sb.AppendLine($"{f.FightType}: {string.Join(" / ", cardNames)}");
            }

            // ── Relic Sequence ──
            Log("--- Predicting relics ---");
            List<RelicSequenceReader.RelicEntry> relics;
            try
            {
                relics = RelicSequenceReader.ReadNext(player, snapshot, 8);
                Log($"Relic prediction: {relics.Count} entries");
            }
            catch (Exception ex)
            {
                Log($"Relic prediction FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                relics = new();
            }

            sb.AppendLine("── 遗物序列 ──");
            foreach (var g in relics
                .GroupBy(r => r.Rarity)
                .Select(g => $"{g.Key}: {string.Join(" / ", g.Select(r => r.RelicId != null ? NameLookup.Relic(r.RelicId) : "?"))}"))
                sb.AppendLine(g);

            // ── Encounters ──
            Log("--- Reading encounters ---");
            try
            {
                var normals = EncounterReader.GetNextNormals(state, 3);
                var elites = EncounterReader.GetNextElites(state, 3);
                Log($"Encounters: {normals.Count} normals, {elites.Count} elites");

                sb.AppendLine("── 遭遇战 ──");
                sb.Append("普通: ");
                sb.AppendLine(string.Join(" / ", normals.Select(e => NameLookup.Encounter(e.Id))));
                sb.Append("精英: ");
                sb.AppendLine(string.Join(" / ", elites.Select(e => NameLookup.Encounter(e.Id))));
            }
            catch (Exception ex)
            {
                Log($"Encounter read FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                sb.AppendLine("── 遭遇战 ──").AppendLine("(读取失败)");
            }

            // ── Events ──
            Log("--- Reading events ---");
            try
            {
                var events = EventReader.GetNextEvents(state, 4);
                Log($"Events: {events.Count} entries");
                sb.AppendLine("── 事件 ──");
                sb.AppendLine(string.Join(" / ", events.Select(e => NameLookup.Event(e.Id))));
            }
            catch (Exception ex)
            {
                Log($"Event read FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                sb.AppendLine("── 事件 ──").AppendLine("(读取失败)");
            }

            // ── Bosses ──
            Log("--- Reading bosses ---");
            try
            {
                var bosses = BossReader.GetAllBosses(state);
                Log($"Bosses: {bosses.Count} entries");
                sb.AppendLine("── Boss ──");
                foreach (var b in bosses)
                    sb.AppendLine($"{b.ActId}: {NameLookup.Encounter(b.BossName)}");
            }
            catch (Exception ex)
            {
                Log($"Boss read FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                sb.AppendLine("── Boss ──").AppendLine("(读取失败)");
            }

            // ── Potions ──
            Log("--- Predicting potions ---");
            try
            {
                var potions = PotionPredictor.PredictNext(player, snapshot, 5);
                Log($"Potion prediction: {potions.Count} entries");
                sb.AppendLine("── 药水 ──");
                foreach (var p in potions)
                    sb.AppendLine(p.WillDrop
                        ? $"掉率{p.DropChance:P0} → {NameLookup.Potion(p.PotionId ?? "?")}"
                        : $"不掉 (下次掉率 {p.DropChance:P0})");
            }
            catch (Exception ex)
            {
                Log($"Potion prediction FAILED: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                sb.AppendLine("── 药水 ──").AppendLine("(预测失败)");
            }

            Log($"Tooltip complete: {sb.Length} chars");
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            Log($"BuildTooltip OUTER exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }
}
