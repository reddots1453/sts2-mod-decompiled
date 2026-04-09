using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityStats.Config;

/// <summary>
/// Per-feature toggle settings. Each sub-feature can be independently
/// enabled/disabled via the F9 settings panel.
/// Disabled features skip their Harmony postfix entirely (zero overhead).
/// </summary>
public class FeatureToggles
{
    [JsonPropertyName("contribution_panel")]
    public bool ContributionPanel { get; set; } = true;

    [JsonPropertyName("card_library_stats")]
    public bool CardLibraryStats { get; set; } = true;

    [JsonPropertyName("relic_stats")]
    public bool RelicStats { get; set; } = true;

    [JsonPropertyName("event_pick_rate")]
    public bool EventPickRate { get; set; } = true;

    [JsonPropertyName("monster_danger")]
    public bool MonsterDanger { get; set; } = true;

    [JsonPropertyName("unknown_room_odds")]
    public bool UnknownRoomOdds { get; set; } = true;

    [JsonPropertyName("potion_drop_odds")]
    public bool PotionDropOdds { get; set; } = true;

    [JsonPropertyName("card_drop_odds")]
    public bool CardDropOdds { get; set; } = true;

    [JsonPropertyName("shop_prices")]
    public bool ShopPrices { get; set; } = true;

    [JsonPropertyName("intent_state_machine")]
    public bool IntentStateMachine { get; set; } = true;

    [JsonPropertyName("career_stats")]
    public bool CareerStats { get; set; } = true;

    /// <summary>
    /// Labels for each toggle, used by FilterPanel UI.
    /// Returns (propertyName, localizedLabel) pairs.
    /// </summary>
    public static IReadOnlyList<(string Key, string LabelKey)> ToggleDefinitions { get; } = new List<(string, string)>
    {
        ("ContributionPanel",  "toggle.contribution_panel"),
        ("CardLibraryStats",   "toggle.card_library_stats"),
        ("RelicStats",         "toggle.relic_stats"),
        ("EventPickRate",      "toggle.event_pick_rate"),
        ("MonsterDanger",      "toggle.monster_danger"),
        ("UnknownRoomOdds",    "toggle.unknown_room_odds"),
        ("PotionDropOdds",     "toggle.potion_drop_odds"),
        ("CardDropOdds",       "toggle.card_drop_odds"),
        ("ShopPrices",         "toggle.shop_prices"),
        ("IntentStateMachine", "toggle.intent_state_machine"),
        ("CareerStats",        "toggle.career_stats"),
    };

    /// <summary>
    /// Get toggle value by property name (for UI binding).
    /// </summary>
    public bool GetByName(string name) => name switch
    {
        "ContributionPanel"  => ContributionPanel,
        "CardLibraryStats"   => CardLibraryStats,
        "RelicStats"         => RelicStats,
        "EventPickRate"      => EventPickRate,
        "MonsterDanger"      => MonsterDanger,
        "UnknownRoomOdds"    => UnknownRoomOdds,
        "PotionDropOdds"     => PotionDropOdds,
        "CardDropOdds"       => CardDropOdds,
        "ShopPrices"         => ShopPrices,
        "IntentStateMachine" => IntentStateMachine,
        "CareerStats"        => CareerStats,
        _ => true
    };

    /// <summary>
    /// Set toggle value by property name (for UI binding).
    /// </summary>
    public void SetByName(string name, bool value)
    {
        switch (name)
        {
            case "ContributionPanel":  ContributionPanel = value; break;
            case "CardLibraryStats":   CardLibraryStats = value; break;
            case "RelicStats":         RelicStats = value; break;
            case "EventPickRate":      EventPickRate = value; break;
            case "MonsterDanger":      MonsterDanger = value; break;
            case "UnknownRoomOdds":    UnknownRoomOdds = value; break;
            case "PotionDropOdds":     PotionDropOdds = value; break;
            case "CardDropOdds":       CardDropOdds = value; break;
            case "ShopPrices":         ShopPrices = value; break;
            case "IntentStateMachine": IntentStateMachine = value; break;
            case "CareerStats":        CareerStats = value; break;
        }
    }
}
