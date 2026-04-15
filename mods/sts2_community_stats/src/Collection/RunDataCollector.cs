using System.Linq;
using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.Util;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CommunityStats.Collection;

/// <summary>
/// Hooked into ModManager.OnMetricsUpload. Assembles the complete RunUploadPayload
/// from SerializableRun + locally tracked data (card choices, event choices, shop,
/// combat contributions) and submits via ApiClient.
/// </summary>
public static class RunDataCollector
{
    // ── Tracked data (populated by Harmony patches during the run) ──

    private static readonly List<CardChoiceUpload> _cardChoices = new();
    private static readonly List<EventChoiceUpload> _eventChoices = new();
    private static readonly List<ShopPurchaseUpload> _shopPurchases = new();
    private static readonly List<ShopCardOfferingUpload> _shopCardOfferings = new();
    private static readonly List<CardRemovalUpload> _cardRemovals = new();
    private static readonly List<CardUpgradeUpload> _cardUpgrades = new();

    /// <summary>
    /// Live floor count derived from the run state's MapPointHistory. The
    /// game defines TotalFloor as the sum of visited points across all
    /// acts (RunState.TotalFloor); delegating to it keeps every per-event
    /// Floor field consistent with the engine. Returns 0 when no run is
    /// active.
    /// </summary>
    public static int CurrentFloor
    {
        get
        {
            try { return RunManager.Instance?.DebugOnlyGetState()?.TotalFloor ?? 0; }
            catch { return 0; }
        }
    }

    // ── Recording methods (called by patches) ───────────────

    /// <summary>
    /// Record each offered card individually. pickedCardId is the one actually picked (null if skipped).
    /// </summary>
    public static void RecordCardReward(List<(string cardId, int upgradeLevel)> offeredCards, string? pickedCardId, int floor)
    {
        foreach (var (cardId, upgradeLevel) in offeredCards)
        {
            _cardChoices.Add(new CardChoiceUpload
            {
                CardId = cardId,
                UpgradeLevel = Math.Clamp(upgradeLevel, 0, 10),
                WasPicked = cardId == pickedCardId,
                Floor = floor
            });
        }
    }

    public static void RecordEventChoice(string eventId, int optionIndex, int totalOptions)
    {
        // Server bounds: option_index ∈ [-1, 20], total_options ∈ [0, 20].
        // Clamp at the boundary so an unexpected modded event with many
        // options can't poison the entire run upload.
        _eventChoices.Add(new EventChoiceUpload
        {
            EventId = eventId,
            OptionIndex = Math.Clamp(optionIndex, -1, 20),
            TotalOptions = Math.Clamp(totalOptions, 0, 20)
        });
    }

    public static void RecordShopPurchase(string itemId, string itemType, int cost, int floor)
    {
        _shopPurchases.Add(new ShopPurchaseUpload
        {
            ItemId = itemId,
            ItemType = itemType,
            Cost = Math.Clamp(cost, 0, 9999),
            Floor = floor
        });
    }

    public static void RecordCardRemoval(string cardId, string source, int floor)
    {
        _cardRemovals.Add(new CardRemovalUpload
        {
            CardId = cardId,
            Source = source,
            Floor = floor
        });
    }

    public static void RecordCardUpgrade(string cardId, string source)
    {
        _cardUpgrades.Add(new CardUpgradeUpload
        {
            CardId = cardId,
            Source = source
        });
    }

    public static void RecordShopCardOffering(string cardId, int floor)
    {
        _shopCardOfferings.Add(new ShopCardOfferingUpload
        {
            CardId = cardId,
            Floor = floor
        });
        // Round 14 v5+: shop offerings are not present in the game's
        // MapPointHistory (only purchases are), so persist them to disk so
        // save+quit+resume doesn't drop them.
        ShopOfferingPersistence.Save(_shopCardOfferings);
    }

    // ── Run lifecycle ───────────────────────────────────────

    public static void OnRunStart()
    {
        _cardChoices.Clear();
        _eventChoices.Clear();
        _shopPurchases.Clear();
        _shopCardOfferings.Clear();
        _cardRemovals.Clear();
        _cardUpgrades.Clear();
        RunContributionAggregator.Instance.Reset();

        // Round 14 v5+: reload previously-recorded shop card offerings for
        // this seed (lost from in-memory list by the Clear above).
        var saved = ShopOfferingPersistence.Load();
        if (saved != null) _shopCardOfferings.AddRange(saved);
    }

    /// <summary>
    /// Called from ModManager.OnMetricsUpload hook. Assembles and submits payload.
    /// </summary>
    public static void OnMetricsUpload(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        // Persist run-summary contributions for future Run History replay (PRD §3.12).
        // Done unconditionally — independent of upload toggle.
        Safe.Run(() =>
        {
            var seed = run?.SerializableRng?.Seed;
            if (!string.IsNullOrEmpty(seed))
            {
                // Round 14 v5+ post-test fix: save+quit+resume between combats
                // can clear in-memory run totals (OnRunStart → Reset, live.json
                // hydration is best-effort). Every combat end DOES write a
                // per-combat snapshot to disk, so reassemble the run totals
                // from those authoritative files before serializing.
                ContributionPersistence.AssembleAndHydrateRunTotals(seed!);

                ContributionPersistence.SaveRunSummary(
                    seed!,
                    RunContributionAggregator.Instance.RunTotals);
                // Round 8 §3.6.1: live snapshot is no longer needed after the
                // run finishes — the per-combat and summary files cover replay.
                ContributionPersistence.DeleteLiveState(seed!);
                ShopOfferingPersistence.Delete(seed!);
            }

            // New run finished: invalidate the cached career snapshot so the
            // next Stats screen open re-aggregates the latest data.
            RunHistoryAnalyzer.Instance.InvalidateAll();
        });

        if (!Config.ModConfig.EnableUpload)
        {
            Safe.Info("Data upload disabled by user setting, skipping.");
            return;
        }

        Safe.RunAsync(async () =>
        {
            var payload = BuildPayload(run, isVictory, localPlayerId);
            await ApiClient.Instance.UploadRunAsync(payload);
        });
    }

    private static RunUploadPayload BuildPayload(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        // PRD-04 §4.6 — privacy: localPlayerId is intentionally NOT written into
        // the upload payload. RunUploadPayload has no player_id / steam_id / user_id
        // field. The parameter is received only because the upstream hook passes it.
        // DO NOT add an identifier field to RunUploadPayload.

        var player = run.Players?.FirstOrDefault();
        var characterId = player?.CharacterId?.Entry ?? "unknown";

        var payload = new RunUploadPayload
        {
            ModVersion = ModConfig.ModVersion,
            GameVersion = VersionManager.GameVersion,
            Character = characterId,
            Ascension = run.Ascension,
            Win = isVictory,
            PlayerWinRate = 0f, // Populated server-side from historical data
            NumPlayers = run.Players?.Count ?? 1,
            FloorReached = CurrentFloor,
            ShopCardOfferings = new List<ShopCardOfferingUpload>(_shopCardOfferings),
            Contributions = RunContributionAggregator.Instance.BuildContributionUploads(),
        };

        // Round 14 v5+: walk SerializableRun.MapPointHistory (authoritative,
        // survives save+quit reload) to populate card/event/shop/encounter
        // sections. Previously these lived in in-memory lists populated by
        // Record*() patches — but a mid-run save+quit drops the in-memory
        // state and the game's own MapPointHistory is the only reliable source.
        // The encounter list produced by RunContributionAggregator is
        // redundant with (and lossier than) the MapPointHistory walk, so we
        // overwrite it here.
        int floor = Api.HistoryImporter.PopulateFromMapHistory(
            run.MapPointHistory,
            killedByEncounter: null,
            wasWin: isVictory,
            payload);
        if (floor > 0) payload.FloorReached = floor;

        // Final deck
        if (player?.Deck != null)
        {
            payload.FinalDeck = player.Deck.Select(c => new DeckCardUpload
            {
                CardId = c.Id?.Entry ?? "unknown",
                UpgradeLevel = Math.Clamp(c.CurrentUpgradeLevel, 0, 10)
            }).ToList();
        }

        // Final relics
        if (player?.Relics != null)
        {
            payload.FinalRelics = player.Relics
                .Select(r => r.Id?.Entry ?? "unknown")
                .ToList();
        }

        return payload;
    }
}
