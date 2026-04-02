namespace CommunityStats.Collection;

/// <summary>
/// Accumulates combat contribution for a single source (card or relic).
/// </summary>
public class ContributionAccum
{
    public string SourceId { get; set; } = "";
    public string SourceType { get; set; } = "card"; // "card" | "relic"
    public int TimesPlayed { get; set; }
    public int DirectDamage { get; set; }
    public int AttributedDamage { get; set; }
    public int BlockGained { get; set; }
    public int CardsDrawn { get; set; }
    public int EnergyGained { get; set; }
    public int HpHealed { get; set; }

    public void MergeFrom(ContributionAccum other)
    {
        TimesPlayed += other.TimesPlayed;
        DirectDamage += other.DirectDamage;
        AttributedDamage += other.AttributedDamage;
        BlockGained += other.BlockGained;
        CardsDrawn += other.CardsDrawn;
        EnergyGained += other.EnergyGained;
        HpHealed += other.HpHealed;
    }

    public int TotalDamage => DirectDamage + AttributedDamage;
}

/// <summary>
/// Tracks which card or relic applied each power, enabling indirect damage attribution.
/// e.g., if "Noxious Fumes" applied Poison, damage from Poison ticks is attributed back to that card.
/// </summary>
public class ContributionMap
{
    public static ContributionMap Instance { get; } = new();

    // PowerModel.Id.Entry → (sourceId, sourceType)
    private readonly Dictionary<string, PowerSource> _powerSources = new();

    public record PowerSource(string SourceId, string SourceType);

    // Per-creature debuff tracking: (creatureHash, powerId) → source
    private readonly Dictionary<(int, string), PowerSource> _creatureDebuffSources = new();

    public void RecordPowerSource(string powerId, string sourceId, string sourceType)
    {
        _powerSources[powerId] = new PowerSource(sourceId, sourceType);
    }

    /// <summary>
    /// Record that a specific debuff was applied to a creature by a card/relic.
    /// Uses creature hash code for per-creature tracking.
    /// </summary>
    public void RecordDebuffSource(int creatureHash, string powerId, string sourceId, string sourceType)
    {
        _creatureDebuffSources[(creatureHash, powerId)] = new PowerSource(sourceId, sourceType);
    }

    public PowerSource? GetPowerSource(string powerId)
    {
        return _powerSources.GetValueOrDefault(powerId);
    }

    public PowerSource? GetDebuffSource(int creatureHash, string powerId)
    {
        return _creatureDebuffSources.GetValueOrDefault((creatureHash, powerId));
    }

    public void Clear()
    {
        _powerSources.Clear();
        _creatureDebuffSources.Clear();
    }
}
