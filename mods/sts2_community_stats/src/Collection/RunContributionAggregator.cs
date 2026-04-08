using CommunityStats.Api;

namespace CommunityStats.Collection;

/// <summary>
/// Run-level contribution aggregator. Accumulates data across all combats in a run.
/// At run end, provides data for upload payload.
/// </summary>
public sealed class RunContributionAggregator
{
    public static RunContributionAggregator Instance { get; } = new();

    // sourceId → run-total contributions
    private readonly Dictionary<string, ContributionAccum> _runTotals = new();

    // Per-encounter records
    private readonly List<EncounterRecord> _encounters = new();

    // Per-encounter contribution snapshots (encounterId → sourceId → ContributionAccum)
    private readonly List<(string EncounterId, Dictionary<string, ContributionAccum> Contributions)> _perEncounter = new();

    public record EncounterRecord(
        string EncounterId,
        string EncounterType,
        int Floor,
        int DamageTaken,
        int TurnsTaken,
        bool PlayerDied);

    public IReadOnlyDictionary<string, ContributionAccum> RunTotals => _runTotals;
    public IReadOnlyList<EncounterRecord> Encounters => _encounters;

    // ── Lifecycle ────────────────────────────────────────────

    public void Reset()
    {
        _runTotals.Clear();
        _encounters.Clear();
        _perEncounter.Clear();
    }

    /// <summary>
    /// Adds healing directly to run totals (for healing that occurs outside combat,
    /// e.g. BurningBlood.AfterCombatVictory, rest site, events).
    /// </summary>
    public void AddHealing(string sourceId, string sourceType, int amount)
    {
        if (!_runTotals.TryGetValue(sourceId, out var accum))
        {
            accum = new ContributionAccum { SourceId = sourceId, SourceType = sourceType };
            _runTotals[sourceId] = accum;
        }
        accum.HpHealed += amount;
    }

    /// <summary>
    /// Called by CombatTracker at combat end. Merges combat data into run totals.
    /// </summary>
    public void AddCombat(
        string encounterId, string encounterType, int floor,
        int damageTaken, int turnsTaken, bool playerDied,
        Dictionary<string, ContributionAccum> combatData)
    {
        _encounters.Add(new EncounterRecord(
            encounterId, encounterType, floor, damageTaken, turnsTaken, playerDied));

        // Deep copy for per-encounter snapshot
        var snapshot = new Dictionary<string, ContributionAccum>();
        foreach (var (key, src) in combatData)
        {
            var copy = new ContributionAccum
            {
                SourceId = src.SourceId,
                SourceType = src.SourceType,
                TimesPlayed = src.TimesPlayed,
                DirectDamage = src.DirectDamage,
                AttributedDamage = src.AttributedDamage,
                ModifierDamage = src.ModifierDamage,
                EffectiveBlock = src.EffectiveBlock,
                ModifierBlock = src.ModifierBlock,
                MitigatedByDebuff = src.MitigatedByDebuff,
                MitigatedByBuff = src.MitigatedByBuff,
                MitigatedByStrReduction = src.MitigatedByStrReduction,
                CardsDrawn = src.CardsDrawn,
                EnergyGained = src.EnergyGained,
                HpHealed = src.HpHealed,
                StarsContribution = src.StarsContribution,
                OriginSourceId = src.OriginSourceId,
                SelfDamage = src.SelfDamage,
                UpgradeDamage = src.UpgradeDamage,
                UpgradeBlock = src.UpgradeBlock
            };
            snapshot[key] = copy;
        }
        _perEncounter.Add((encounterId, snapshot));

        // Merge into run totals
        foreach (var (sourceId, accum) in combatData)
        {
            if (!_runTotals.TryGetValue(sourceId, out var runAccum))
            {
                runAccum = new ContributionAccum
                {
                    SourceId = accum.SourceId,
                    SourceType = accum.SourceType
                };
                _runTotals[sourceId] = runAccum;
            }
            runAccum.MergeFrom(accum);
        }
    }

    // ── Export for Upload ────────────────────────────────────

    public List<EncounterUpload> BuildEncounterUploads()
    {
        return _encounters.Select(e => new EncounterUpload
        {
            EncounterId = e.EncounterId,
            EncounterType = e.EncounterType,
            DamageTaken = e.DamageTaken,
            TurnsTaken = e.TurnsTaken,
            PlayerDied = e.PlayerDied,
            Floor = e.Floor
        }).ToList();
    }

    public List<ContributionUpload> BuildContributionUploads()
    {
        var result = new List<ContributionUpload>();

        // Per-encounter contributions
        foreach (var (encounterId, contributions) in _perEncounter)
        {
            foreach (var (_, accum) in contributions)
            {
                result.Add(ToUpload(accum, encounterId));
            }
        }

        // Run-level totals (encounterId = "" means aggregate)
        foreach (var (_, accum) in _runTotals)
        {
            result.Add(ToUpload(accum, ""));
        }

        return result;
    }

    private static ContributionUpload ToUpload(ContributionAccum accum, string encounterId)
    {
        return new ContributionUpload
        {
            SourceId = accum.SourceId,
            SourceType = accum.SourceType,
            EncounterId = encounterId,
            TimesPlayed = accum.TimesPlayed,
            DirectDamage = accum.DirectDamage,
            AttributedDamage = accum.AttributedDamage,
            EffectiveBlock = accum.EffectiveBlock,
            MitigatedByDebuff = accum.MitigatedByDebuff,
            MitigatedByBuff = accum.MitigatedByBuff,
            CardsDrawn = accum.CardsDrawn,
            EnergyGained = accum.EnergyGained,
            HpHealed = accum.HpHealed,
            StarsContribution = accum.StarsContribution,
            MitigatedByStrReduction = accum.MitigatedByStrReduction,
            ModifierDamage = accum.ModifierDamage,
            ModifierBlock = accum.ModifierBlock,
            SelfDamage = accum.SelfDamage,
            UpgradeDamage = accum.UpgradeDamage,
            UpgradeBlock = accum.UpgradeBlock,
            OriginSourceId = accum.OriginSourceId
        };
    }
}
