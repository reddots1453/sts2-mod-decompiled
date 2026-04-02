using System.Text.Json;
using CommunityStats.Config;

namespace CommunityStats.Util;

/// <summary>
/// Disk-backed queue for run uploads that failed due to network issues.
/// Payloads are serialized to individual JSON files and retried on next opportunity.
/// </summary>
public static class OfflineQueue
{
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
        });
    }

    /// <summary>
    /// Attempts to upload all pending payloads. Called at mod init and after successful uploads.
    /// </summary>
    public static async Task DrainAsync(Func<string, Task<bool>> uploadFunc)
    {
        if (!Directory.Exists(ModConfig.PendingDir)) return;

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
