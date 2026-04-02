using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityStats.Config;
using CommunityStats.Util;

namespace CommunityStats.Api;

/// <summary>
/// HttpClient wrapper for all server communication.
/// Handles upload, bulk/on-demand queries, retries, and offline fallback.
/// </summary>
public sealed class ApiClient : IDisposable
{
    public static ApiClient Instance { get; } = new();

    private readonly HttpClient _queryClient;
    private readonly HttpClient _uploadClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private ApiClient()
    {
        // BaseAddress MUST end with '/' for relative URL resolution to work correctly.
        // Without trailing slash, HttpClient treats paths starting without '/' as
        // relative to the parent, and paths with '/' as absolute from host root.
        var baseUrl = ModConfig.ApiBaseUrl.TrimEnd('/') + "/";

        _queryClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMilliseconds(ModConfig.QueryTimeoutMs)
        };
        _queryClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _queryClient.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);

        _uploadClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMilliseconds(ModConfig.UploadTimeoutMs)
        };
        _uploadClient.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);
    }

    // ── Upload ──────────────────────────────────────────────

    /// <summary>
    /// Uploads a completed run. On failure, queues for offline retry.
    /// </summary>
    public async Task UploadRunAsync(RunUploadPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var success = await PostJsonAsync("runs", json);

        if (success)
        {
            Safe.Info("Run uploaded successfully");
            // Drain any pending offline uploads while we have connectivity
            Safe.RunAsync(() => OfflineQueue.DrainAsync(rawJson => PostJsonAsync("runs", rawJson)));
        }
        else
        {
            OfflineQueue.Enqueue(payload);
        }
    }

    /// <summary>
    /// Posts raw JSON to an endpoint. Returns true on 2xx.
    /// </summary>
    public async Task<bool> PostJsonAsync(string endpoint, string json)
    {
        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _uploadClient.PostAsync(endpoint, content);
            if (response.IsSuccessStatusCode) return true;

            Safe.Warn($"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            return false;
        }
        catch (Exception ex)
        {
            Safe.Warn($"Upload error: {ex.Message}");
            return false;
        }
    }

    // ── Bulk Query ──────────────────────────────────────────

    /// <summary>
    /// Fetches the pre-computed bulk stats bundle for a character + filter.
    /// </summary>
    public async Task<BulkStatsBundle?> GetBulkStatsAsync(string character, FilterSettings filter)
    {
        var version = VersionManager.GetEffectiveVersion(filter);
        var qs = filter.ToQueryString();
        var sep = qs.Length > 0 ? "&" : "?";
        var url = $"stats/bulk?char={Uri.EscapeDataString(character)}&ver={Uri.EscapeDataString(version)}{(qs.Length > 0 ? "&" + qs[1..] : "")}";

        return await GetAsync<BulkStatsBundle>(url);
    }

    // ── On-Demand Fallback Queries ──────────────────────────

    public async Task<Dictionary<string, CardStats>?> GetCardStatsBatchAsync(
        IEnumerable<string> cardIds, FilterSettings filter)
    {
        var ids = string.Join(",", cardIds.Select(Uri.EscapeDataString));
        var qs = filter.ToQueryString();
        var url = $"stats/cards?cards={ids}{(qs.Length > 0 ? "&" + qs[1..] : "")}";
        return await GetAsync<Dictionary<string, CardStats>>(url);
    }

    public async Task<Dictionary<string, RelicStats>?> GetRelicStatsBatchAsync(
        IEnumerable<string> relicIds, FilterSettings filter)
    {
        var ids = string.Join(",", relicIds.Select(Uri.EscapeDataString));
        var qs = filter.ToQueryString();
        var url = $"stats/relics?relics={ids}{(qs.Length > 0 ? "&" + qs[1..] : "")}";
        return await GetAsync<Dictionary<string, RelicStats>>(url);
    }

    public async Task<EventStats?> GetEventStatsAsync(string eventId, FilterSettings filter)
    {
        var qs = filter.ToQueryString();
        var url = $"stats/events/{Uri.EscapeDataString(eventId)}{qs}";
        return await GetAsync<EventStats>(url);
    }

    public async Task<Dictionary<string, EncounterStats>?> GetEncounterStatsBatchAsync(
        IEnumerable<string> encounterIds, FilterSettings filter)
    {
        var ids = string.Join(",", encounterIds.Select(Uri.EscapeDataString));
        var qs = filter.ToQueryString();
        var url = $"stats/encounters?ids={ids}{(qs.Length > 0 ? "&" + qs[1..] : "")}";
        return await GetAsync<Dictionary<string, EncounterStats>>(url);
    }

    public async Task<List<string>?> GetAvailableVersionsAsync()
    {
        return await GetAsync<List<string>>("meta/versions");
    }

    // ── Internal ────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string url) where T : class
    {
        try
        {
            using var response = await _queryClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Safe.Warn($"Query failed: GET {url} → {(int)response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (TaskCanceledException)
        {
            Safe.Warn($"Query timeout: GET {url}");
            return null;
        }
        catch (Exception ex)
        {
            Safe.Warn($"Query error: GET {url} → {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _queryClient.Dispose();
        _uploadClient.Dispose();
    }
}
