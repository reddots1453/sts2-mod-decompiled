using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityStats.Config;

public class FilterSettings
{
    [JsonPropertyName("character")]
    public string? Character { get; set; }

    [JsonPropertyName("min_ascension")]
    public int? MinAscension { get; set; }

    [JsonPropertyName("max_ascension")]
    public int? MaxAscension { get; set; }

    [JsonPropertyName("min_player_win_rate")]
    public float? MinPlayerWinRate { get; set; }

    [JsonPropertyName("num_players")]
    public int? NumPlayers { get; set; }

    [JsonPropertyName("game_version")]
    public string? GameVersion { get; set; }          // null = current, "all" = all versions

    [JsonPropertyName("auto_match_ascension")]
    public bool AutoMatchAscension { get; set; } = false;

    /// <summary>
    /// Builds query string parameters for API requests.
    /// </summary>
    public string ToQueryString()
    {
        var parts = new List<string>();
        if (Character != null) parts.Add($"char={Uri.EscapeDataString(Character)}");
        if (MinAscension.HasValue) parts.Add($"min_asc={MinAscension.Value}");
        if (MaxAscension.HasValue) parts.Add($"max_asc={MaxAscension.Value}");
        if (MinPlayerWinRate.HasValue) parts.Add($"min_wr={MinPlayerWinRate.Value:F2}");
        if (NumPlayers.HasValue) parts.Add($"num_players={NumPlayers.Value}");
        if (GameVersion != null) parts.Add($"ver={Uri.EscapeDataString(GameVersion)}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
    }

    /// <summary>
    /// Deterministic hash for cache keying.
    /// </summary>
    public string Hash()
    {
        var json = JsonSerializer.Serialize(this);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FilterSettings other) return false;
        return Character == other.Character
            && MinAscension == other.MinAscension
            && MaxAscension == other.MaxAscension
            && MinPlayerWinRate == other.MinPlayerWinRate
            && NumPlayers == other.NumPlayers
            && GameVersion == other.GameVersion;
    }

    public override int GetHashCode() =>
        System.HashCode.Combine(Character, MinAscension, MaxAscension, MinPlayerWinRate, NumPlayers, GameVersion);

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ModConfig.SettingsPath, json);
    }

    public static FilterSettings Load()
    {
        if (!File.Exists(ModConfig.SettingsPath)) return new FilterSettings();
        try
        {
            var json = File.ReadAllText(ModConfig.SettingsPath);
            return JsonSerializer.Deserialize<FilterSettings>(json) ?? new FilterSettings();
        }
        catch { return new FilterSettings(); }
    }
}
