using System.Linq;
using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.Util;
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
    private static readonly List<CardRemovalUpload> _cardRemovals = new();
    private static readonly List<CardUpgradeUpload> _cardUpgrades = new();

    public static int CurrentFloor { get; set; }

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
                UpgradeLevel = upgradeLevel,
                WasPicked = cardId == pickedCardId,
                Floor = floor
            });
        }
    }

    public static void RecordEventChoice(string eventId, int optionIndex, int totalOptions)
    {
        _eventChoices.Add(new EventChoiceUpload
        {
            EventId = eventId,
            OptionIndex = optionIndex,
            TotalOptions = totalOptions
        });
    }

    public static void RecordShopPurchase(string itemId, string itemType, int cost, int floor)
    {
        _shopPurchases.Add(new ShopPurchaseUpload
        {
            ItemId = itemId,
            ItemType = itemType,
            Cost = cost,
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

    // ── Run lifecycle ───────────────────────────────────────

    public static void OnRunStart()
    {
        _cardChoices.Clear();
        _eventChoices.Clear();
        _shopPurchases.Clear();
        _cardRemovals.Clear();
        _cardUpgrades.Clear();
        CurrentFloor = 0;
        RunContributionAggregator.Instance.Reset();
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
                ContributionPersistence.SaveRunSummary(
                    seed!,
                    RunContributionAggregator.Instance.RunTotals);
                // Round 8 §3.6.1: live snapshot is no longer needed after the
                // run finishes — the per-combat and summary files cover replay.
                ContributionPersistence.DeleteLiveState(seed!);
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
            CardChoices = new List<CardChoiceUpload>(_cardChoices),
            EventChoices = new List<EventChoiceUpload>(_eventChoices),
            ShopPurchases = new List<ShopPurchaseUpload>(_shopPurchases),
            CardRemovals = new List<CardRemovalUpload>(_cardRemovals),
            CardUpgrades = new List<CardUpgradeUpload>(_cardUpgrades),
            Encounters = RunContributionAggregator.Instance.BuildEncounterUploads(),
            Contributions = RunContributionAggregator.Instance.BuildContributionUploads(),
        };

        // Final deck
        if (player?.Deck != null)
        {
            payload.FinalDeck = player.Deck.Select(c => new DeckCardUpload
            {
                CardId = c.Id?.Entry ?? "unknown",
                UpgradeLevel = c.CurrentUpgradeLevel
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
