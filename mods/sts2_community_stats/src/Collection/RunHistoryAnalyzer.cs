using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityStats.Config;
using CommunityStats.Util;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CommunityStats.Collection;

/// <summary>
/// Loads RunHistory files in the background and aggregates them into CareerStatsData.
/// Caches per-character results; supports incremental updates when a new run completes.
///
/// Threading model:
///   - LoadAllAsync runs entirely on a worker thread (Task.Run)
///   - Results delivered via the awaited Task; UI subscribes via CareerStatsLoaded event
///   - Reentrancy guarded by SemaphoreSlim
///
/// PRD-04 §3.11. Phase 6 task 1.
/// </summary>
public sealed class RunHistoryAnalyzer
{
    public static RunHistoryAnalyzer Instance { get; } = new();

    // ── Cache ───────────────────────────────────────────────
    // characterFilter ("" = all) → cached snapshot
    private readonly Dictionary<string, CareerStatsData> _cache = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Fired on the worker thread when a new snapshot is ready.</summary>
    public event Action<CareerStatsData>? CareerStatsLoaded;

    // Per-card local aggregations live alongside the career snapshot.
    private LocalCardStatsBundle _cardBundle = LocalCardStatsBundle.Empty;
    public LocalCardStatsBundle LocalCards => _cardBundle;

    // Per-relic local aggregations (PRD §3.3 round 6).
    private LocalRelicStatsBundle _relicBundle = LocalRelicStatsBundle.Empty;
    public LocalRelicStatsBundle LocalRelics => _relicBundle;

    private RunHistoryAnalyzer() { }

    // ── Public API ──────────────────────────────────────────

    /// <summary>
    /// Returns the cached snapshot if present in memory or on disk; otherwise null.
    /// Use to populate UI synchronously without re-loading. Disk hits are
    /// promoted into the in-memory cache so subsequent calls are free.
    /// </summary>
    public CareerStatsData? GetCached(string? characterFilter)
    {
        var key = characterFilter ?? "";
        lock (_cache)
        {
            if (_cache.TryGetValue(key, out var v)) return v;
        }

        // Disk fallback — instant load of last persisted snapshot.
        var fromDisk = CareerStatsCache.Load(characterFilter);
        if (fromDisk != null)
        {
            lock (_cache) { _cache[key] = fromDisk; }
        }
        return fromDisk;
    }

    /// <summary>
    /// Asynchronously load all RunHistory files (filtered by character if non-null)
    /// and aggregate into CareerStatsData. Result is cached and event-fired.
    /// </summary>
    public async Task<CareerStatsData> LoadAllAsync(string? characterFilter, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var key = characterFilter ?? "";
            // Re-check cache after lock acquisition
            lock (_cache)
            {
                if (_cache.TryGetValue(key, out var cached)) return cached;
            }

            var snapshot = await Task.Run(() => BuildSnapshot(characterFilter, ct), ct)
                                     .ConfigureAwait(false);

            lock (_cache) { _cache[key] = snapshot; }

            // Persist the freshly computed snapshot so the next mod load can
            // populate the Stats screen instantly without waiting for the
            // RunHistory aggregation to finish.
            try { CareerStatsCache.Save(snapshot); }
            catch (Exception ex) { Safe.Warn($"CareerStatsCache.Save threw: {ex.Message}"); }

            try { CareerStatsLoaded?.Invoke(snapshot); }
            catch (Exception ex) { Safe.Warn($"CareerStatsLoaded handler threw: {ex.Message}"); }

            return snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Invalidate all cached snapshots. Call when a run finishes so the next read reloads.
    /// Removes both in-memory and on-disk caches.
    /// </summary>
    public void InvalidateAll()
    {
        lock (_cache) _cache.Clear();
        CareerStatsCache.DeleteAll();
    }

    // ── Worker ──────────────────────────────────────────────

    private CareerStatsData BuildSnapshot(string? characterFilter, CancellationToken ct)
    {
        List<string> names;
        try
        {
            names = SaveManager.Instance?.GetAllRunHistoryNames() ?? new List<string>();
        }
        catch (Exception ex)
        {
            Safe.Warn($"RunHistoryAnalyzer: GetAllRunHistoryNames failed: {ex.Message}");
            return CareerStatsData.Empty(characterFilter);
        }

        if (names.Count == 0) return CareerStatsData.Empty(characterFilter);

        // Load all matching histories. Each load is wrapped to survive single-file failure.
        var loaded = new List<RunHistory>(capacity: names.Count);
        foreach (var name in names)
        {
            ct.ThrowIfCancellationRequested();
            RunHistory? history;
            try
            {
                var result = SaveManager.Instance.LoadRunHistory(name);
                if (result == null || !result.Success || result.SaveData == null) continue;
                history = result.SaveData;
            }
            catch (Exception ex)
            {
                Safe.Warn($"RunHistoryAnalyzer: failed to load '{name}': {ex.Message}");
                continue;
            }

            if (characterFilter != null)
            {
                bool matches = history.Players.Any(p => p.Character.Entry == characterFilter);
                if (!matches) continue;
            }
            loaded.Add(history);
        }

        if (loaded.Count == 0) return CareerStatsData.Empty(characterFilter);

        // Sort newest first by StartTime for rolling-window calculation.
        loaded.Sort((a, b) => b.StartTime.CompareTo(a.StartTime));

        // Per-card / per-relic local aggregations run in the same loop.
        // Results are cached on the analyzer instance so the card library
        // and relic library compendia can read them synchronously without
        // re-walking files.
        _cardBundle = ComputeLocalCardBundle(loaded);
        _relicBundle = ComputeLocalRelicBundle(loaded);

        return new CareerStatsData
        {
            CharacterFilter = characterFilter,
            TotalRuns = loaded.Count,
            Wins = loaded.Count(r => r.Win),
            WinRateByWindow = ComputeWinRateWindows(loaded),
            DeathCausesByAct = ComputeDeathCauses(loaded),
            PathStatsByAct = ComputePathStats(loaded),
            AncientPickRates = ComputeAncientPickRates(loaded),
            BossStats = ComputeBossStats(loaded),
            AncientByElder = ComputeAncientByElder(loaded),
        };
    }

    // ── Aggregations ────────────────────────────────────────

    /// <summary>
    /// When KilledBy* fields are both unset, look at the deepest visited floor.
    /// If it was a combat room (Monster/Elite/Boss), return its encounter id so
    /// we attribute the abandon to the fight the player was actually inside.
    /// Returns null when no combat floor was found.
    /// </summary>
    private static string? ResolveAbandonedCause(RunHistory run)
    {
        if (run.MapPointHistory == null) return null;
        for (int actIdx = run.MapPointHistory.Count - 1; actIdx >= 0; actIdx--)
        {
            var floors = run.MapPointHistory[actIdx];
            if (floors == null) continue;
            for (int f = floors.Count - 1; f >= 0; f--)
            {
                var floor = floors[f];
                if (floor == null) continue;
                bool isCombat = floor.MapPointType == MapPointType.Monster
                             || floor.MapPointType == MapPointType.Elite
                             || floor.MapPointType == MapPointType.Boss;
                if (!isCombat) continue;
                if (floor.Rooms == null || floor.Rooms.Count == 0) continue;
                var entry = floor.Rooms[0].ModelId.Entry;
                if (!string.IsNullOrEmpty(entry)) return entry;
            }
        }
        return null;
    }


    private static IReadOnlyDictionary<int, float> ComputeWinRateWindows(List<RunHistory> sortedNewestFirst)
    {
        var result = new Dictionary<int, float>();
        foreach (var window in new[] { 10, 50, int.MaxValue })
        {
            var slice = sortedNewestFirst.Take(window).ToList();
            if (slice.Count == 0) { result[window] = 0f; continue; }
            int wins = slice.Count(r => r.Win);
            result[window] = (float)wins / slice.Count;
        }
        return result;
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<DeathEntry>> ComputeDeathCauses(List<RunHistory> runs)
    {
        // Per-Act bucket: actIndex → encounterId → count
        var bucket = new Dictionary<int, Dictionary<string, int>>();

        foreach (var run in runs)
        {
            if (run.Win) continue;

            int actIndex = Math.Max(1, run.MapPointHistory?.Count ?? 1);
            string cause = "ABANDONED";
            if (run.KilledByEncounter != ModelId.none) cause = run.KilledByEncounter.Entry;
            else if (run.KilledByEvent != ModelId.none) cause = run.KilledByEvent.Entry;
            else
            {
                // Manual feedback Q5: when the player abandoned mid-combat the
                // KilledBy* fields are both none. Walk back to the last visited
                // floor and, if it was a combat room, attribute the death to
                // that encounter so we don't lose information.
                cause = ResolveAbandonedCause(run) ?? "ABANDONED";
            }

            if (!bucket.TryGetValue(actIndex, out var map))
            {
                map = new Dictionary<string, int>();
                bucket[actIndex] = map;
            }
            map[cause] = map.GetValueOrDefault(cause) + 1;
        }

        var result = new Dictionary<int, IReadOnlyList<DeathEntry>>();
        foreach (var (act, map) in bucket)
        {
            int total = map.Values.Sum();
            var rows = map
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .Select(kv => new DeathEntry
                {
                    EncounterId = kv.Key,
                    Count = kv.Value,
                    Share = total > 0 ? (float)kv.Value / total : 0f,
                })
                .ToList();
            result[act] = rows;
        }
        return result;
    }

    private static IReadOnlyDictionary<int, ActPathStats> ComputePathStats(List<RunHistory> runs)
    {
        // Accumulators per Act index (1..N).
        var totals = new Dictionary<int, (long Gained, long Bought, long Removed, long Upgraded,
                                          long Unknown, long Monster, long Elite, long Shop, int Samples)>();

        foreach (var run in runs)
        {
            if (run.MapPointHistory == null) continue;
            for (int i = 0; i < run.MapPointHistory.Count; i++)
            {
                int actIdx = i + 1; // 1-based
                var floors = run.MapPointHistory[i];
                if (floors == null || floors.Count == 0) continue;

                long gained = 0, bought = 0, removed = 0, upgraded = 0;
                long unknown = 0, monster = 0, elite = 0, shop = 0;

                foreach (var floor in floors)
                {
                    switch (floor.MapPointType)
                    {
                        case MapPointType.Unknown:  unknown++;  break;
                        case MapPointType.Monster:  monster++;  break;
                        case MapPointType.Elite:    elite++;    break;
                        case MapPointType.Shop:     shop++;     break;
                    }

                    if (floor.PlayerStats == null) continue;
                    foreach (var ps in floor.PlayerStats)
                    {
                        gained   += ps.CardsGained?.Count ?? 0;
                        removed  += ps.CardsRemoved?.Count ?? 0;
                        upgraded += ps.UpgradedCards?.Count ?? 0;
                        // BoughtRelics list contains relics; bought cards are tracked
                        // separately (via BoughtColorless + shop card purchases routed
                        // through CardsGained on the shop floor). Use BoughtColorless
                        // as a proxy for cards bought from shop until we have a richer
                        // signal.
                        bought   += ps.BoughtColorless?.Count ?? 0;
                    }
                }

                var prev = totals.GetValueOrDefault(actIdx);
                totals[actIdx] = (
                    prev.Gained + gained,
                    prev.Bought + bought,
                    prev.Removed + removed,
                    prev.Upgraded + upgraded,
                    prev.Unknown + unknown,
                    prev.Monster + monster,
                    prev.Elite + elite,
                    prev.Shop + shop,
                    prev.Samples + 1);
            }
        }

        var result = new Dictionary<int, ActPathStats>();
        foreach (var (act, t) in totals)
        {
            float n = Math.Max(1, t.Samples);
            result[act] = new ActPathStats
            {
                CardsGained   = t.Gained / n,
                CardsBought   = t.Bought / n,
                CardsRemoved  = t.Removed / n,
                CardsUpgraded = t.Upgraded / n,
                UnknownRooms  = t.Unknown / n,
                MonsterRooms  = t.Monster / n,
                EliteRooms    = t.Elite / n,
                ShopRooms     = t.Shop / n,
                SampleSize    = t.Samples,
            };
        }
        return result;
    }

    private static IReadOnlyDictionary<string, AncientChoiceStats> ComputeAncientPickRates(List<RunHistory> runs)
    {
        var bucket = new Dictionary<string, (int Opportunities, int Picks)>();

        foreach (var run in runs)
        {
            if (run.MapPointHistory == null) continue;
            foreach (var floors in run.MapPointHistory)
            {
                foreach (var floor in floors)
                {
                    if (floor.PlayerStats == null) continue;
                    foreach (var ps in floor.PlayerStats)
                    {
                        if (ps.AncientChoices == null || ps.AncientChoices.Count == 0) continue;
                        // Each AncientChoices entry is one option offered in this ancient encounter.
                        foreach (var choice in ps.AncientChoices)
                        {
                            string key;
                            try { key = choice.TextKey; }
                            catch { continue; }
                            if (string.IsNullOrEmpty(key)) continue;

                            var prev = bucket.GetValueOrDefault(key);
                            bucket[key] = (prev.Opportunities + 1, prev.Picks + (choice.WasChosen ? 1 : 0));
                        }
                    }
                }
            }
        }

        var result = new Dictionary<string, AncientChoiceStats>();
        foreach (var (key, t) in bucket)
        {
            result[key] = new AncientChoiceStats
            {
                TextKey = key,
                Opportunities = t.Opportunities,
                Picks = t.Picks,
            };
        }
        return result;
    }

    private static IReadOnlyDictionary<string, BossEncounterStats> ComputeBossStats(List<RunHistory> runs)
    {
        // bossId → (totalDmg, encounters, deaths)
        var bucket = new Dictionary<string, (long Damage, int Encounters, int Deaths)>();

        foreach (var run in runs)
        {
            if (run.MapPointHistory == null) continue;
            // Last floor (where the player died if losing) is the deepest entry.
            string? deathRoomEncounterId = null;
            if (!run.Win)
            {
                if (run.KilledByEncounter != ModelId.none) deathRoomEncounterId = run.KilledByEncounter.Entry;
            }

            foreach (var floors in run.MapPointHistory)
            {
                foreach (var floor in floors)
                {
                    if (floor.MapPointType != MapPointType.Boss) continue;
                    if (floor.Rooms == null || floor.Rooms.Count == 0) continue;

                    // Use the first room's model id as boss id.
                    string bossId = floor.Rooms[0].ModelId.Entry;
                    if (string.IsNullOrEmpty(bossId)) bossId = "UNKNOWN_BOSS";

                    int dmg = 0;
                    if (floor.PlayerStats != null)
                    {
                        foreach (var ps in floor.PlayerStats) dmg += ps.DamageTaken;
                    }
                    bool died = bossId == deathRoomEncounterId;

                    var prev = bucket.GetValueOrDefault(bossId);
                    bucket[bossId] = (prev.Damage + dmg, prev.Encounters + 1, prev.Deaths + (died ? 1 : 0));
                }
            }
        }

        var result = new Dictionary<string, BossEncounterStats>();
        foreach (var (id, t) in bucket)
        {
            result[id] = new BossEncounterStats
            {
                EncounterId = id,
                Encounters = t.Encounters,
                Deaths = t.Deaths,
                AverageDamageTaken = t.Encounters > 0 ? (float)t.Damage / t.Encounters : 0f,
            };
        }
        return result;
    }

    /// <summary>
    /// Aggregate per-Elder breakdown for the PRD §3.11 dropdown.
    /// Walks every Ancient floor in the run history and groups offered relics
    /// under their parent elder encounter id.
    /// </summary>
    private static IReadOnlyDictionary<string, ElderEntry> ComputeAncientByElder(List<RunHistory> runs)
    {
        // elderId → optionRelicId → (picks, wins)
        var bucket = new Dictionary<string, Dictionary<string, (int picks, int wins)>>();
        var elderEncounters = new Dictionary<string, int>();

        foreach (var run in runs)
        {
            if (run.MapPointHistory == null) continue;
            foreach (var floors in run.MapPointHistory)
            {
                if (floors == null) continue;
                foreach (var floor in floors)
                {
                    if (floor.MapPointType != MapPointType.Ancient) continue;
                    if (floor.Rooms == null || floor.Rooms.Count == 0) continue;

                    string elderId = floor.Rooms[0].ModelId.Entry;
                    if (string.IsNullOrEmpty(elderId)) continue;

                    elderEncounters[elderId] = elderEncounters.GetValueOrDefault(elderId) + 1;

                    if (floor.PlayerStats == null) continue;
                    foreach (var ps in floor.PlayerStats)
                    {
                        if (ps.AncientChoices == null) continue;
                        foreach (var choice in ps.AncientChoices)
                        {
                            string relicId = ExtractRelicId(choice);
                            if (string.IsNullOrEmpty(relicId)) continue;

                            if (!bucket.TryGetValue(elderId, out var inner))
                            {
                                inner = new Dictionary<string, (int, int)>();
                                bucket[elderId] = inner;
                            }
                            var prev = inner.GetValueOrDefault(relicId);
                            int picks = prev.picks + (choice.WasChosen ? 1 : 0);
                            int wins = prev.wins + (choice.WasChosen && run.Win ? 1 : 0);
                            inner[relicId] = (picks, wins);
                        }
                    }
                }
            }
        }

        var result = new Dictionary<string, ElderEntry>();
        foreach (var (elderId, inner) in bucket)
        {
            var relics = inner
                .Select(kv => new ElderRelicStats
                {
                    RelicId = kv.Key,
                    Picks = kv.Value.picks,
                    Wins = kv.Value.wins,
                })
                .OrderByDescending(r => r.Picks)
                .ToList();

            // Each Pael / Darv / etc. event normally offers ONE option set; we
            // group all observed relics under a single "all" option to keep
            // the data shape compatible with the PRD dropdown.
            var option = new ElderOption
            {
                OptionTextKey = "all",
                Picks = relics.Sum(r => r.Picks),
                Relics = relics,
            };

            result[elderId] = new ElderEntry
            {
                ElderId = elderId,
                Encounters = elderEncounters.GetValueOrDefault(elderId),
                Options = new List<ElderOption> { option },
            };
        }
        return result;
    }

    /// <summary>
    /// Pull a relic id out of an AncientChoiceHistoryEntry. The Title.LocEntryKey
    /// is "RELIC_ID.title" when the option came from EventOption.FromRelic, which
    /// is the path Pael/Darv/Tezcatara/etc. all use.
    /// </summary>
    private static string ExtractRelicId(MegaCrit.Sts2.Core.Runs.History.AncientChoiceHistoryEntry choice)
    {
        try
        {
            var key = choice.Title?.LocEntryKey;
            if (string.IsNullOrEmpty(key)) return "";
            // "RELIC_ID.title" → "RELIC_ID". Anything else: return as-is.
            int dot = key!.LastIndexOf('.');
            return dot > 0 ? key.Substring(0, dot) : key;
        }
        catch { return ""; }
    }

    /// <summary>
    /// Walk every loaded RunHistory and compute per-card pick / win rates.
    /// PRD §3.2 round 5: feeds the "我的数据" column of the card library.
    /// </summary>
    private static LocalCardStatsBundle ComputeLocalCardBundle(List<RunHistory> runs)
    {
        // cardId → (offered, picks, runsWith, winsAfter)
        var bucket = new Dictionary<string, (int offered, int picks, int runsWith, int winsAfter)>();

        foreach (var run in runs)
        {
            if (run.MapPointHistory == null) continue;

            // Per-run set of cards seen so we count each card at most once per run
            // for the win-rate denominator.
            var runCards = new HashSet<string>();

            foreach (var floors in run.MapPointHistory)
            {
                if (floors == null) continue;
                foreach (var floor in floors)
                {
                    if (floor.PlayerStats == null) continue;
                    foreach (var ps in floor.PlayerStats)
                    {
                        // Pick / offer counts come from CardChoices.
                        if (ps.CardChoices != null)
                        {
                            foreach (var choice in ps.CardChoices)
                            {
                                var id = choice.Card.Id?.Entry;
                                if (string.IsNullOrEmpty(id)) continue;
                                var prev = bucket.GetValueOrDefault(id!);
                                bucket[id!] = (prev.offered + 1,
                                               prev.picks + (choice.wasPicked ? 1 : 0),
                                               prev.runsWith,
                                               prev.winsAfter);
                            }
                        }

                        // Cards actually obtained (any source) feed runsWith.
                        if (ps.CardsGained != null)
                        {
                            foreach (var card in ps.CardsGained)
                            {
                                var id = card.Id?.Entry;
                                if (!string.IsNullOrEmpty(id)) runCards.Add(id!);
                            }
                        }
                    }
                }
            }

            foreach (var id in runCards)
            {
                var prev = bucket.GetValueOrDefault(id);
                bucket[id] = (prev.offered, prev.picks, prev.runsWith + 1,
                              prev.winsAfter + (run.Win ? 1 : 0));
            }
        }

        var dict = new Dictionary<string, LocalCardStats>(bucket.Count);
        foreach (var (id, t) in bucket)
        {
            dict[id] = new LocalCardStats
            {
                CardId = id,
                Offered = t.offered,
                Picks = t.picks,
                RunsWith = t.runsWith,
                WinsAfter = t.winsAfter,
            };
        }
        return new LocalCardStatsBundle
        {
            TotalRuns = runs.Count,
            Cards = dict,
        };
    }

    /// <summary>
    /// Walk every loaded RunHistory and compute per-relic win rates.
    /// PRD §3.3 round 6: feeds the "我的数据" column of the relic library.
    ///
    /// "Owned a relic" is detected from `RunHistoryPlayer.Relics` (the final
    /// inventory snapshot). For older saves that don't have that field we
    /// fall back to walking `BoughtRelics` + `RelicChoices.WasChosen`.
    /// </summary>
    private static LocalRelicStatsBundle ComputeLocalRelicBundle(List<RunHistory> runs)
    {
        var bucket = new Dictionary<string, (int runs, int wins)>();

        foreach (var run in runs)
        {
            // Per-run set of relics seen so we count each relic once per run.
            var relics = new HashSet<string>();

            try
            {
                foreach (var player in run.Players)
                {
                    if (player.Relics == null) continue;
                    foreach (var sr in player.Relics)
                    {
                        var entry = sr?.Id?.Entry;
                        if (!string.IsNullOrEmpty(entry)) relics.Add(entry!);
                    }
                }
            }
            catch { /* fall through to MapPointHistory walk */ }

            // Fallback: walk floor history if Players[].Relics is empty.
            if (relics.Count == 0 && run.MapPointHistory != null)
            {
                foreach (var floors in run.MapPointHistory)
                {
                    if (floors == null) continue;
                    foreach (var floor in floors)
                    {
                        if (floor.PlayerStats == null) continue;
                        foreach (var ps in floor.PlayerStats)
                        {
                            if (ps.BoughtRelics != null)
                                foreach (var rid in ps.BoughtRelics)
                                    if (!string.IsNullOrEmpty(rid.Entry)) relics.Add(rid.Entry);
                            if (ps.RelicChoices != null)
                                foreach (var choice in ps.RelicChoices)
                                {
                                    if (!choice.wasPicked) continue;
                                    var entry = choice.choice?.Entry;
                                    if (!string.IsNullOrEmpty(entry)) relics.Add(entry!);
                                }
                        }
                    }
                }
            }

            foreach (var id in relics)
            {
                var prev = bucket.GetValueOrDefault(id);
                bucket[id] = (prev.runs + 1, prev.wins + (run.Win ? 1 : 0));
            }
        }

        var dict = new Dictionary<string, LocalRelicStats>(bucket.Count);
        float sumWinRate = 0f;
        int countWithData = 0;
        foreach (var (id, t) in bucket)
        {
            var entry = new LocalRelicStats
            {
                RelicId = id,
                RunsWith = t.runs,
                WinsWith = t.wins,
            };
            dict[id] = entry;
            if (entry.RunsWith > 0)
            {
                sumWinRate += entry.WinRate;
                countWithData++;
            }
        }
        return new LocalRelicStatsBundle
        {
            TotalRuns = runs.Count,
            Relics = dict,
            AverageWinRate = countWithData > 0 ? sumWinRate / countWithData : 0f,
        };
    }

    // ── Single-run analysis ─────────────────────────────────

    /// <summary>
    /// Build per-Act stats for a SINGLE run (used by Run History detail view, PRD §3.12).
    /// Synchronous — input data is already loaded.
    /// </summary>
    public SingleRunStatsData BuildSingleRunStats(RunHistory run)
    {
        var path = new Dictionary<int, ActPathStats>();
        var ancients = new List<string>();
        var bossDmg = new Dictionary<string, int>();

        if (run.MapPointHistory != null)
        {
            for (int i = 0; i < run.MapPointHistory.Count; i++)
            {
                int actIdx = i + 1;
                var floors = run.MapPointHistory[i];
                if (floors == null) continue;

                int gained = 0, bought = 0, removed = 0, upgraded = 0;
                int unknown = 0, monster = 0, elite = 0, shop = 0;

                foreach (var floor in floors)
                {
                    switch (floor.MapPointType)
                    {
                        case MapPointType.Unknown: unknown++; break;
                        case MapPointType.Monster: monster++; break;
                        case MapPointType.Elite:   elite++;   break;
                        case MapPointType.Shop:    shop++;    break;
                    }

                    if (floor.PlayerStats == null) continue;
                    foreach (var ps in floor.PlayerStats)
                    {
                        gained   += ps.CardsGained?.Count ?? 0;
                        removed  += ps.CardsRemoved?.Count ?? 0;
                        upgraded += ps.UpgradedCards?.Count ?? 0;
                        bought   += ps.BoughtColorless?.Count ?? 0;

                        if (ps.AncientChoices != null)
                        {
                            foreach (var c in ps.AncientChoices)
                            {
                                if (!c.WasChosen) continue;
                                try { ancients.Add(c.TextKey); } catch { }
                            }
                        }
                    }

                    if (floor.MapPointType == MapPointType.Boss && floor.Rooms?.Count > 0)
                    {
                        string bossId = floor.Rooms[0].ModelId.Entry;
                        if (!string.IsNullOrEmpty(bossId) && floor.PlayerStats != null)
                        {
                            int dmg = 0;
                            foreach (var ps in floor.PlayerStats) dmg += ps.DamageTaken;
                            bossDmg[bossId] = dmg;
                        }
                    }
                }

                path[actIdx] = new ActPathStats
                {
                    CardsGained = gained,
                    CardsBought = bought,
                    CardsRemoved = removed,
                    CardsUpgraded = upgraded,
                    UnknownRooms = unknown,
                    MonsterRooms = monster,
                    EliteRooms = elite,
                    ShopRooms = shop,
                    SampleSize = 1,
                };
            }
        }

        return new SingleRunStatsData
        {
            Seed = run.Seed,
            Character = run.Players.FirstOrDefault()?.Character.Entry ?? "",
            Win = run.Win,
            Ascension = run.Ascension,
            FloorReached = run.MapPointHistory?.Sum(act => act?.Count ?? 0) ?? 0,
            PathStatsByAct = path,
            AncientChoicesPicked = ancients,
            BossDamageTaken = bossDmg,
        };
    }
}
