using System.IO;
using System.Net.Http;
using System.Text.Json;
using CommunityStats.Config;
using CommunityStats.Util;
using Godot;

namespace CommunityStats.Api;

/// <summary>
/// Auto-update system. Flow:
///   1. TryApplyPendingUpdate — if .new file exists, replace old DLL
///   2. CheckForUpdateAsync — query server, show dialog, download if user accepts
/// </summary>
public sealed class Updater
{
    public static Updater Instance { get; } = new();

    public string Edition { get; set; } = "community";

    private static string? _pendingUpdateVersion;

    /// <summary>Called from mod init.</summary>
    public static void TryApplyPendingUpdate()
    {
        Safe.Run(() =>
        {
            var dllDir = GetDllDirectory();
            var currentPath = Path.Combine(dllDir, "sts2_community_stats.dll");
            var newPath = currentPath + ".new";

            if (!File.Exists(newPath)) return;

            try
            {
                if (File.Exists(currentPath))
                    File.Delete(currentPath);
                File.Move(newPath, currentPath);
                Safe.Info($"[Updater] Applied pending update: {newPath} → {currentPath}");
            }
            catch (Exception ex)
            {
                Safe.Warn($"[Updater] Could not apply pending update: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Background check. If a newer version is found, shows a dialog on the
    /// main thread asking the user whether to download.
    /// </summary>
    public async System.Threading.Tasks.Task CheckForUpdateAsync()
    {
        if (!ModConfig.AutoUpdate)
        {
            Safe.Info("[Updater] Auto-update disabled via config");
            return;
        }

        UpdateInfo? info;
        try
        {
            info = await FetchUpdateInfoAsync();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Update check failed: {ex.Message}");
            return;
        }

        if (info == null || !info.UpdateAvailable) return;

        Safe.Info($"[Updater] New version available: {info.Latest} (current: {ModConfig.ModVersion})");

        // Marshal to main thread to show dialog.
        _pendingUpdateVersion = info.Latest;
        Callable.From(() => ShowUpdateDialog(info.Latest, info.DownloadUrl)).CallDeferred();
    }

    private static void ShowUpdateDialog(string version, string downloadUrl)
    {
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Title = "Stats the Spire";
            dialog.DialogText = string.Format(L.Get("update.found"), version, ModConfig.ModVersion);
            dialog.OkButtonText = L.Get("update.download_yes");
            dialog.CancelButtonText = L.Get("update.download_no");
            dialog.Exclusive = true;
            dialog.AlwaysOnTop = true;

            dialog.Confirmed += () =>
            {
                dialog.QueueFree();
                StartDownload(version, downloadUrl);
            };
            dialog.Canceled += () => dialog.QueueFree();
            dialog.CloseRequested += () => dialog.QueueFree();

            tree.Root.AddChild(dialog);
            dialog.PopupCentered();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Failed to show dialog: {ex.Message}");
        }
    }

    private static async void StartDownload(string version, string downloadUrl)
    {
        Safe.Info($"[Updater] User accepted update to {version}, downloading...");

        bool ok;
        try
        {
            ok = await DownloadDllAsync(downloadUrl);
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Download failed: {ex.Message}");
            ShowMessageDialog(string.Format(L.Get("update.failed"), version));
            return;
        }

        if (ok)
        {
            var noticePath = GetUpdateNoticePath();
            File.WriteAllText(noticePath, version);
            ShowMessageDialog(string.Format(L.Get("update.ready"), version));
        }
        else
        {
            ShowMessageDialog(string.Format(L.Get("update.failed"), version));
        }
    }

    private static void ShowMessageDialog(string message)
    {
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null) return;

            var dialog = new AcceptDialog();
            dialog.Title = "Stats the Spire";
            dialog.DialogText = message;
            dialog.OkButtonText = "OK";
            dialog.Exclusive = true;
            dialog.AlwaysOnTop = true;
            dialog.Confirmed += () => dialog.QueueFree();
            dialog.CloseRequested += () => dialog.QueueFree();

            tree.Root.AddChild(dialog);
            dialog.PopupCentered();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Failed to show message: {ex.Message}");
        }
    }

    // ── Internal ────────────────────────────────────────────

    private static async Task<UpdateInfo?> FetchUpdateInfoAsync()
    {
        var url = $"meta/update-info?edition={Uri.EscapeDataString(Instance.Edition)}"
                + $"&current={Uri.EscapeDataString(ModConfig.ModVersion)}";

        using var client = new System.Net.Http.HttpClient
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
        using var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new Uri(ModConfig.ApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60),
        };
        client.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);

        var bytes = await client.GetByteArrayAsync(downloadUrl);
        if (bytes.Length < 100_000)
        {
            Safe.Warn($"[Updater] Downloaded DLL too small ({bytes.Length} bytes), ignoring");
            return false;
        }

        var dllDir = GetDllDirectory();
        var newPath = Path.Combine(dllDir, "sts2_community_stats.dll.new");
        await File.WriteAllBytesAsync(newPath, bytes);
        Safe.Info($"[Updater] Downloaded to {newPath} ({bytes.Length} bytes)");
        return true;
    }

    private static string GetDllDirectory() =>
        Path.GetDirectoryName(typeof(Updater).Assembly.Location)!;

    private static string GetUpdateNoticePath() =>
        Path.Combine(ModConfig.DataDir, "update_notice.txt");

    private class UpdateInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("latest")]
        public string Latest { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("update_available")]
        public bool UpdateAvailable { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = "";
    }
}
