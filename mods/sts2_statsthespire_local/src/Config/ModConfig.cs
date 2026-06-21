using System.Text.Json;

namespace CommunityStats.Config;

/// <summary>
/// Central configuration for the Community Stats mod.
/// </summary>
public static class ModConfig
{
    public const string ModVersion = "1.3";

    // User preferences
    public static string Language { get; set; } = "CN"; // "CN" | "EN"

    // Feature toggles
    public static FeatureToggles Toggles { get; set; } = new();

    // Panel position (null = default right side)
    public static float? PanelPositionX { get; set; }
    public static float? PanelPositionY { get; set; }

    // "My Data" filter
    public static bool UseMyDataOnly { get; set; }

    // Cache
    public static int MemoryCacheTtlSeconds { get; set; } = 900;   // 15 min
    public static int DiskCacheTtlHours { get; set; } = 24;

    // Paths
    public static string DataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "sts2_community_stats");
    public static string CacheDir => Path.Combine(DataDir, "cache");
    public static string ContributionsDir => Path.Combine(DataDir, "contributions");
    public static string SettingsPath => Path.Combine(DataDir, "settings.json");

    // Active filter (mutable at runtime)
    public static FilterSettings CurrentFilter { get; set; } = new();

    // Config override file path (next to the mod DLL).
    // Named .cfg so the game engine doesn't scan it as a mod manifest.
    public static string ConfigPath
    {
        get
        {
            var asmLocation = typeof(ModConfig).Assembly.Location;
            if (!string.IsNullOrEmpty(asmLocation))
                return Path.Combine(Path.GetDirectoryName(asmLocation)!, "settings.cfg");
            return Path.Combine(DataDir, "settings.cfg");
        }
    }

    /// <summary>User-modifiable preferences saved to AppData (always writable).</summary>
    public static string PrefsPath => Path.Combine(DataDir, "mod_prefs.json");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(CacheDir);
        Directory.CreateDirectory(ContributionsDir);
    }

    /// <summary>
    /// Load config overrides from disk. Reads shipped config.json first, then
    /// user's mod_prefs.json from AppData (overrides take priority — survives
    /// mod updates and directory permission issues).
    /// </summary>
    public static void LoadOverrides()
    {
        // Phase 1: shipped defaults (config.json next to the DLL)
        ApplyConfigFile(ConfigPath);
        // Phase 2: user prefs (AppData, always writable — overrides shipped defaults)
        ApplyConfigFile(PrefsPath);
    }

    private static void ApplyConfigFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("language", out var lang))
                Language = lang.GetString() ?? Language;

            if (root.TryGetProperty("feature_toggles", out var toggles))
            {
                try
                {
                    Toggles = JsonSerializer.Deserialize<FeatureToggles>(toggles.GetRawText()) ?? new();
                }
                catch { Toggles = new(); }
            }

            if (root.TryGetProperty("panel_position", out var pos))
            {
                if (pos.TryGetProperty("x", out var px)) PanelPositionX = px.GetSingle();
                if (pos.TryGetProperty("y", out var py)) PanelPositionY = py.GetSingle();
            }

            if (root.TryGetProperty("use_my_data_only", out var myData))
                UseMyDataOnly = myData.GetBoolean();

        }
        catch { /* ignore malformed config */ }
    }

    /// <summary>
    /// Save current settings (feature toggles, language, preferences) to AppData.
    /// Uses PrefsPath so settings survive mod updates and directory permissions.
    /// </summary>
    public static void SaveSettings()
    {
        try
        {
            var data = new Dictionary<string, object?>
            {
                ["language"] = Language,
                ["feature_toggles"] = Toggles,
                ["use_my_data_only"] = UseMyDataOnly,
            };

            if (PanelPositionX.HasValue && PanelPositionY.HasValue)
            {
                data["panel_position"] = new { x = PanelPositionX.Value, y = PanelPositionY.Value };
            }

            EnsureDirectories();
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PrefsPath, json);
        }
        catch { /* ignore write failures */ }
    }
}
