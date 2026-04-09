using System.Text.Json;
using CommunityStats.Config;

namespace CommunityStats.Util;

/// <summary>
/// Disk-backed queue for run uploads that failed due to network issues.
/// Payloads are serialized to individual JSON files and retried on next opportunity.
///
/// PRD-04 §4.6: max 10 queued payloads, drop entries older than 7 days.
/// </summary>
public static class OfflineQueue
{
    private const int MaxEntries = 10;
    private const int RetentionDays = 7;

    public static void Enqueue<T>(T payload)
    {
        Safe.Run(() =>
        {
            ModConfig.EnsureDirectories();
            var filename = $"run_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json";
            var path = Path.Combine(ModConfig.PendingDir, filename);
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Safe.Info($"Queued offline upload: {filename}");

            // PRD §4.6: enforce max 10 entries, evicting the oldest first.
            TrimToMaxEntries();
        });
    }

    /// <summary>
    /// Drop payloads older than the retention window. Called by Enqueue/Drain.
    /// </summary>
    private static void PruneExpired()
    {
        if (!Directory.Exists(ModConfig.PendingDir)) return;
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        foreach (var path in Directory.EnumerateFiles(ModConfig.PendingDir, "*.json"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff)
                {
                    File.Delete(path);
                    Safe.Info($"Offline queue: pruned expired entry {Path.GetFileName(path)}");
                }
            }
            catch (Exception ex)
            {
                Safe.Warn($"Offline queue: prune failed for {path}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Enforce the maximum queue depth by deleting the oldest entries.
    /// </summary>
    private static void TrimToMaxEntries()
    {
        if (!Directory.Exists(ModConfig.PendingDir)) return;
        var files = Directory.GetFiles(ModConfig.PendingDir, "*.json")
            .OrderBy(f => File.GetLastWriteTimeUtc(f))
            .ToList();
        int excess = files.Count - MaxEntries;
        for (int i = 0; i < excess; i++)
        {
            try
            {
                File.Delete(files[i]);
                Safe.Info($"Offline queue: evicted {Path.GetFileName(files[i])} (max {MaxEntries})");
            }
            catch (Exception ex)
            {
                Safe.Warn($"Offline queue: evict failed for {files[i]}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Attempts to upload all pending payloads. Called at mod init and after successful uploads.
    /// </summary>
    public static async Task DrainAsync(Func<string, Task<bool>> uploadFunc)
    {
        if (!Directory.Exists(ModConfig.PendingDir)) return;

        // PRD §4.6: drop expired entries before attempting drain.
        PruneExpired();

        var files = Directory.GetFiles(ModConfig.PendingDir, "*.json")
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0) return;
        Safe.Info($"Draining offline queue: {files.Count} pending uploads");

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var success = false;
            var delay = 1000; // exponential backoff: 1s, 2s, 4s

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    success = await uploadFunc(json);
                    if (success) break;
                }
                catch { /* retry */ }

                await Task.Delay(delay);
                delay *= 2;
            }

            if (success)
            {
                File.Delete(file);
                Safe.Info($"Offline upload succeeded: {Path.GetFileName(file)}");
            }
            else
            {
                Safe.Warn($"Offline upload still failing: {Path.GetFileName(file)}, will retry next time");
                break; // stop draining on failure to avoid hammering server
            }
        }
    }
}
