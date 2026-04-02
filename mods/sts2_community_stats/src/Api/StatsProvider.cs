using CommunityStats.Config;
using CommunityStats.Util;

namespace CommunityStats.Api;

/// <summary>
/// Central data distributor: bulk preload at Run start, cache-first lookups,
/// on-demand HTTP fallback when cache misses.
/// </summary>
public sealed class StatsProvider
{
    public static StatsProvider Instance { get; } = new();

    private BulkStatsBundle? _bundle;
    private FilterSettings? _bundleFilter;
    private volatile bool _isPreloading;

    public bool IsPreloading => _isPreloading;
    public bool HasBundle => _bundle != null;
    public int TotalRunCount => _bundle?.TotalRuns ?? 0;

    // ── Preload (Run start) ─────────────────────────────────

    /// <summary>
    /// Called at Run start. Fetches bulk stats for the character + current filter.
    /// Falls back to disk cache on network failure.
    /// </summary>
    public async Task PreloadForRunAsync(string character, FilterSettings filter)
    {
        _isPreloading = true;
        _bundleFilter = filter;
        var diskKey = $"bulk_{character}_{filter.Hash()}";

        Safe.Info($"[DIAG:Preload] Starting preload for character={character}, diskKey={diskKey}");
        Safe.Info($"[DIAG:Preload] ApiBaseUrl={Config.ModConfig.ApiBaseUrl}");

        try
        {
            var bundle = await ApiClient.Instance.GetBulkStatsAsync(character, filter);
            Safe.Info($"[DIAG:Preload] GetBulkStatsAsync returned: {(bundle != null ? "non-null" : "NULL")}");
            if (bundle != null)
            {
                _bundle = bundle;
                StatsCache.Instance.Set(diskKey, bundle);
                StatsCache.Instance.WriteDisk(diskKey, bundle);
                Safe.Info($"Preloaded bulk stats for {character} ({_bundle.Cards.Count} cards, {_bundle.Relics.Count} relics, {_bundle.Events.Count} events, {_bundle.Encounters.Count} encounters)");
                _isPreloading = false;
                return;
            }
        }
        catch (Exception ex)
        {
            Safe.Warn($"Preload network error: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Safe.Warn($"  InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }

        // Network failed → try disk cache
        var cached = StatsCache.Instance.ReadDisk<BulkStatsBundle>(diskKey);
        if (cached != null)
        {
            _bundle = cached;
            Safe.Info($"Preload fell back to disk cache for {character}");
        }
        else
        {
            Safe.Warn($"No cached data available for {character}");
        }

        _isPreloading = false;
    }

    /// <summary>
    /// Clears bundle and re-preloads when filter changes.
    /// </summary>
    public async Task OnFilterChangedAsync(string? character, FilterSettings newFilter)
    {
        _bundle = null;
        StatsCache.Instance.InvalidateAll();

        if (character != null)
            await PreloadForRunAsync(character, newFilter);
    }

    /// <summary>
    /// Clears all state at Run end.
    /// </summary>
    public void Reset()
    {
        _bundle = null;
        _bundleFilter = null;
        _isPreloading = false;
    }

    // ── Card Stats ──────────────────────────────────────────

    public CardStats? GetCardStats(string cardId)
    {
        if (_bundle?.Cards.TryGetValue(cardId, out var stats) == true)
            return stats;
        return null;
    }

    /// <summary>
    /// Batch lookup: returns stats for each card ID (null if not found).
    /// If bundle covers all, returns immediately. Otherwise falls back to API.
    /// </summary>
    public async Task<Dictionary<string, CardStats?>> GetCardStatsBatchAsync(
        List<string> cardIds, FilterSettings filter)
    {
        var result = new Dictionary<string, CardStats?>();

        // Fast path: all in bundle
        if (_bundle != null && FilterMatches(filter))
        {
            var allHit = true;
            foreach (var id in cardIds)
            {
                if (_bundle.Cards.TryGetValue(id, out var s))
                    result[id] = s;
                else
                    allHit = false;
            }
            if (allHit) return result;
        }

        // Slow path: fetch missing from API
        var missing = cardIds.Where(id => !result.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var fetched = await ApiClient.Instance.GetCardStatsBatchAsync(missing, filter);
            if (fetched != null)
            {
                foreach (var kvp in fetched)
                    result[kvp.Key] = kvp.Value;
            }
        }

        // Fill nulls for anything still missing
        foreach (var id in cardIds)
            result.TryAdd(id, null);

        return result;
    }

    // ── Relic Stats ─────────────────────────────────────────

    public RelicStats? GetRelicStats(string relicId)
    {
        if (_bundle?.Relics.TryGetValue(relicId, out var stats) == true)
            return stats;
        return null;
    }

    public async Task<Dictionary<string, RelicStats?>> GetRelicStatsBatchAsync(
        List<string> relicIds, FilterSettings filter)
    {
        var result = new Dictionary<string, RelicStats?>();

        if (_bundle != null && FilterMatches(filter))
        {
            var allHit = true;
            foreach (var id in relicIds)
            {
                if (_bundle.Relics.TryGetValue(id, out var s))
                    result[id] = s;
                else
                    allHit = false;
            }
            if (allHit) return result;
        }

        var missing = relicIds.Where(id => !result.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var fetched = await ApiClient.Instance.GetRelicStatsBatchAsync(missing, filter);
            if (fetched != null)
            {
                foreach (var kvp in fetched)
                    result[kvp.Key] = kvp.Value;
            }
        }

        foreach (var id in relicIds)
            result.TryAdd(id, null);

        return result;
    }

    // ── Event Stats ─────────────────────────────────────────

    public EventStats? GetEventStats(string eventId)
    {
        if (_bundle?.Events.TryGetValue(eventId, out var stats) == true)
            return stats;
        return null;
    }

    public async Task<EventStats?> GetEventStatsAsync(string eventId, FilterSettings filter)
    {
        if (_bundle != null && FilterMatches(filter) &&
            _bundle.Events.TryGetValue(eventId, out var stats))
            return stats;

        return await ApiClient.Instance.GetEventStatsAsync(eventId, filter);
    }

    // ── Encounter Stats ─────────────────────────────────────

    public EncounterStats? GetEncounterStats(string encounterId)
    {
        if (_bundle?.Encounters.TryGetValue(encounterId, out var stats) == true)
            return stats;
        return null;
    }

    public async Task<Dictionary<string, EncounterStats?>> GetEncounterStatsBatchAsync(
        List<string> encounterIds, FilterSettings filter)
    {
        var result = new Dictionary<string, EncounterStats?>();

        if (_bundle != null && FilterMatches(filter))
        {
            var allHit = true;
            foreach (var id in encounterIds)
            {
                if (_bundle.Encounters.TryGetValue(id, out var s))
                    result[id] = s;
                else
                    allHit = false;
            }
            if (allHit) return result;
        }

        var missing = encounterIds.Where(id => !result.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var fetched = await ApiClient.Instance.GetEncounterStatsBatchAsync(missing, filter);
            if (fetched != null)
            {
                foreach (var kvp in fetched)
                    result[kvp.Key] = kvp.Value;
            }
        }

        foreach (var id in encounterIds)
            result.TryAdd(id, null);

        return result;
    }

    // ── Internal ────────────────────────────────────────────

    private bool FilterMatches(FilterSettings filter) =>
        _bundleFilter != null && filter.Equals(_bundleFilter);
}
