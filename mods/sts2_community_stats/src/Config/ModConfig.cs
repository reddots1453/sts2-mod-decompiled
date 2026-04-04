using System.Text.Json;

namespace CommunityStats.Config;

/// <summary>
/// Central configuration for the Community Stats mod.
/// </summary>
public static class ModConfig
{
    public const string ModVersion = "1.0.0";

    // Server (can be overridden via config.json for local testing)
    public static string ApiBaseUrl { get; set; } = "https://api.sts2stats.example.com/v1";
    public static int QueryTimeoutMs { get; set; } = 5000;
    public static int UploadTimeoutMs { get; set; } = 30000;

    // User preferences
    public static bool EnableUpload { get; set; } = true;
    public static string Language { get; set; } = "CN"; // "CN" | "EN"

    // Cache
    public static int MemoryCacheTtlSeconds { get; set; } = 900;   // 15 min
    public static int DiskCacheTtlHours { get; set; } = 24;

    // Paths
    public static string DataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "sts2_community_stats");
    public static string CacheDir => Path.Combine(DataDir, "cache");
    public static string PendingDir => Path.Combine(DataDir, "pending");
    public static string SettingsPath => Path.Combine(DataDir, "settings.json");

    // Active filter (mutable at runtime)
    public static FilterSettings CurrentFilter { get; set; } = new();

    // Config override file path (next to the mod DLL)
    public static string ConfigPath
    {
        get
        {
            var asmLocation = typeof(ModConfig).Assembly.Location;
            if (!string.IsNullOrEmpty(asmLocation))
                return Path.Combine(Path.GetDirectoryName(asmLocation)!, "config.json");
            return Path.Combine(DataDir, "config.json");
        }
    }

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(CacheDir);
        Directory.CreateDirectory(PendingDir);
    }

    /// <summary>
    /// Load config.json overrides (e.g. api_base_url for local testing).
    /// Call this before any API client initialization.
    /// </summary>
    public static void LoadOverrides()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;
            var json = File.ReadAllText(ConfigPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("api_base_url", out var url))
                ApiBaseUrl = url.GetString() ?? ApiBaseUrl;
            if (root.TryGetProperty("query_timeout_ms", out var qt))
                QueryTimeoutMs = qt.GetInt32();
            if (root.TryGetProperty("upload_timeout_ms", out var ut))
                UploadTimeoutMs = ut.GetInt32();
            if (root.TryGetProperty("enable_upload", out var eu))
                EnableUpload = eu.GetBoolean();
            if (root.TryGetProperty("language", out var lang))
                Language = lang.GetString() ?? Language;
        }
        catch { /* ignore malformed config */ }
    }
}
