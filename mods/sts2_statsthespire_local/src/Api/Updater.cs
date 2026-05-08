using System.IO;
using System.Net.Http;
using System.Text.Json;
using CommunityStats.Config;
using CommunityStats.Util;

namespace CommunityStats.Api;

/// <summary>
/// Auto-update system for mod DLLs. Works identically for community
/// ("community") and local ("local") editions.
///
/// Flow:
///   1. TryApplyPendingUpdate — if .new file exists, replace old DLL
///   2. CheckForUpdateAsync — query server, download if newer
/// </summary>
public sealed class Updater
{
    public static Updater Instance { get; } = new();

    /// <summary>Edition string sent to the API.</summary>
    public string Edition { get; set; } = "community";

    /// <summary>Called from mod init before the first API call.</summary>
    public static void TryApplyPendingUpdate()
    {
        Safe.Run(() =>
        {
            var dllDir = GetDllDirectory();
            var currentPath = Path.Combine(dllDir, "sts2_community_stats.dll");
            var newPath = currentPath + ".new";

            if (!File.Exists(newPath)) return;

            // Delete old DLL and rename .new to .dll. If the DLL is locked
            // (game still running), the rename will fail and we try again
            // next launch.
            try
            {
                if (File.Exists(currentPath))
                    File.Delete(currentPath);
                File.Move(newPath, currentPath);
                Safe.Info($"[Updater] Applied pending update: {Path.GetFileName(newPath)} → {Path.GetFileName(currentPath)}");
            }
            catch (Exception ex)
            {
                Safe.Warn($"[Updater] Could not apply pending update: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Fire-and-forget: check the server for a newer version. If found,
    /// download the DLL to a .new file for next launch.
    /// </summary>
    public async System.Threading.Tasks.Task CheckForUpdateAsync()
    {
        if (!ModConfig.AutoUpdate)
        {
            Safe.Info("[Updater] Auto-update disabled via config");
            return;
        }

        await System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var info = await FetchUpdateInfoAsync();
                if (info == null || !info.UpdateAvailable) return;

                Safe.Info($"[Updater] New version available: {info.Latest} (current: {ModConfig.ModVersion})");

                var downloaded = await DownloadDllAsync(info.DownloadUrl);
                if (!downloaded) return;

                // Persist so we show the notice next startup too.
                var noticePath = GetUpdateNoticePath();
                File.WriteAllText(noticePath, info.Latest);
            }
            catch (Exception ex)
            {
                Safe.Warn($"[Updater] Update check failed: {ex.Message}");
            }
        });
    }

    private static async Task<UpdateInfo?> FetchUpdateInfoAsync()
    {
        var url = $"v1/meta/update-info?edition={Uri.EscapeDataString(Instance.Edition)}"
                + $"&current={Uri.EscapeDataString(ModConfig.ModVersion)}";

        using var client = new HttpClient
        {
            BaseAddress = new Uri(ModConfig.ApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(10),
        };
        client.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);

        var json = await client.GetStringAsync(url);
        return JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    private static async Task<bool> DownloadDllAsync(string downloadUrl)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ModConfig.ApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60),
        };
        client.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);

        var bytes = await client.GetByteArrayAsync(downloadUrl);
        if (bytes.Length < 100_000) // sanity — DLL should be 600+ KB
        {
            Safe.Warn($"[Updater] Downloaded DLL too small ({bytes.Length} bytes), ignoring");
            return false;
        }

        var dllDir = GetDllDirectory();
        var newPath = Path.Combine(dllDir, "sts2_community_stats.dll.new");
        await File.WriteAllBytesAsync(newPath, bytes);
        Safe.Info($"[Updater] Downloaded update to {newPath} ({bytes.Length} bytes) — will apply on next launch");
        return true;
    }

    /// <summary>
    /// Returns the notice text if a pending update was downloaded, or null.
    /// Caller should show a toast/notice to the user.
    /// </summary>
    public static string? GetPendingUpdateVersion()
    {
        try
        {
            var noticePath = GetUpdateNoticePath();
            if (File.Exists(noticePath))
            {
                var ver = File.ReadAllText(noticePath).Trim();
                if (!string.IsNullOrEmpty(ver)) return ver;
            }

            // Also check if a .new file exists — this means we downloaded
            // an update but haven't shown the notice yet.
            var dllDir = GetDllDirectory();
            var newPath = Path.Combine(dllDir, "sts2_community_stats.dll.new");
            if (File.Exists(newPath))
                return "new";
        }
        catch { }
        return null;
    }

    /// <summary>Delete the update notice file (called after showing toast).</summary>
    public static void ClearUpdateNotice()
    {
        try
        {
            var path = GetUpdateNoticePath();
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    private static string GetDllDirectory()
    {
        var asmLocation = typeof(Updater).Assembly.Location;
        return Path.GetDirectoryName(asmLocation)!;
    }

    private static string GetUpdateNoticePath() =>
        Path.Combine(ModConfig.DataDir, "update_notice.txt");

    private class UpdateInfo
    {
        public string Latest { get; set; } = "";
        public bool UpdateAvailable { get; set; }
        public string DownloadUrl { get; set; } = "";
    }
}
