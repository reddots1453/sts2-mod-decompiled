using CommunityStats.Api;
using CommunityStats.Util;

namespace CommunityStats.Collection;

/// <summary>
/// Per-combat tracker. Records card plays, damage, block, draws, and attributes
/// indirect contributions (poison, powers) back to their source card/relic.
/// Reset at combat start, flushed to RunContributionAggregator at combat end.
/// </summary>
public sealed class CombatTracker
{
    public static CombatTracker Instance { get; } = new();

    // sourceId → accumulated contributions
    private readonly Dictionary<string, ContributionAccum> _currentCombat = new();

    // The card currently being played (set by CardPlayStarted, cleared by CardPlayFinished)
    private string? _activeCardId;

    // The relic currently executing a hook (set/cleared by relic hook tracking patch)
    private string? _activeRelicId;

    // Current encounter info
    private string _encounterId = "";
    private string _encounterType = "";
    private int _damageTakenByPlayer;
    private int _turnCount;
    private bool _playerDied;
    private int _floor;

    // Snapshot of the last combat (for UI display)
    private Dictionary<string, ContributionAccum>? _lastCombatData;
    private string _lastEncounterId = "";

    public IReadOnlyDictionary<string, ContributionAccum>? LastCombatData => _lastCombatData;
    public string LastEncounterId => _lastEncounterId;

    // ── Lifecycle ────────────────────────────────────────────

    public void OnCombatStart(string encounterId, string encounterType, int floor)
    {
        _currentCombat.Clear();
        _activeCardId = null;
        _activeRelicId = null;
        _encounterId = encounterId;
        _encounterType = encounterType;
        _damageTakenByPlayer = 0;
        _turnCount = 0;
        _playerDied = false;
        _floor = floor;
        ContributionMap.Instance.Clear();
    }

    public void OnCombatEnd()
    {
        // Snapshot for UI
        _lastCombatData = new Dictionary<string, ContributionAccum>(_currentCombat);
        _lastEncounterId = _encounterId;

        // Flush to run-level aggregator
        RunContributionAggregator.Instance.AddCombat(
            _encounterId, _encounterType, _floor,
            _damageTakenByPlayer, _turnCount, _playerDied,
            _currentCombat);

        _currentCombat.Clear();
        ContributionMap.Instance.Clear();
    }

    // ── Card Play Tracking ──────────────────────────────────

    public void OnCardPlayStarted(string cardId)
    {
        _activeCardId = cardId;
        var accum = GetOrCreate(cardId, "card");
        accum.TimesPlayed++;
    }

    public void OnCardPlayFinished()
    {
        _activeCardId = null;
    }

    // ── Relic Context Tracking ──────────────────────────────

    public void SetActiveRelic(string relicId) => _activeRelicId = relicId;
    public void ClearActiveRelic() => _activeRelicId = null;

    // ── Turn Tracking ───────────────────────────────────────

    public void OnTurnStart() => _turnCount++;

    // ── Damage ──────────────────────────────────────────────

    /// <summary>
    /// Called when any creature takes damage.
    /// </summary>
    public void OnDamageDealt(int totalDamage, string? cardSourceId, bool isPlayerReceiver)
    {
        if (isPlayerReceiver)
        {
            _damageTakenByPlayer += totalDamage;
            return;
        }

        // Damage dealt TO enemies — attribute to source
        if (cardSourceId != null)
        {
            // Direct card damage
            GetOrCreate(cardSourceId, "card").DirectDamage += totalDamage;
        }
        else if (_activeCardId != null)
        {
            // Damage during a card play but no explicit cardSource (e.g., orb triggers)
            GetOrCreate(_activeCardId, "card").DirectDamage += totalDamage;
        }
        else if (_activeRelicId != null)
        {
            GetOrCreate(_activeRelicId, "relic").DirectDamage += totalDamage;
        }
        // else: unattributed (environment damage, etc.)
    }

    // ── Block ───────────────────────────────────────────────

    public void OnBlockGained(int amount, string? cardPlayId)
    {
        if (cardPlayId != null)
        {
            GetOrCreate(cardPlayId, "card").BlockGained += amount;
        }
        else if (_activeCardId != null)
        {
            GetOrCreate(_activeCardId, "card").BlockGained += amount;
        }
        else if (_activeRelicId != null)
        {
            GetOrCreate(_activeRelicId, "relic").BlockGained += amount;
        }
    }

    // ── Power Application ───────────────────────────────────

    // Debuff IDs that modify damage and should be attributed
    private static readonly HashSet<string> DamageModifierDebuffs = new()
    {
        "VULNERABLE_POWER", "WEAK_POWER"
    };

    public void OnPowerApplied(string powerId, decimal amount, int creatureHash)
    {
        // Record which card/relic applied this power for future attribution
        if (_activeCardId != null)
        {
            ContributionMap.Instance.RecordPowerSource(powerId, _activeCardId, "card");
            ContributionMap.Instance.RecordDebuffSource(creatureHash, powerId, _activeCardId, "card");
        }
        else if (_activeRelicId != null)
        {
            ContributionMap.Instance.RecordPowerSource(powerId, _activeRelicId, "relic");
            ContributionMap.Instance.RecordDebuffSource(creatureHash, powerId, _activeRelicId, "relic");
        }
    }

    /// <summary>
    /// Attribute bonus damage from debuffs (Vulnerable, etc.) to the card that applied them.
    /// Called after direct damage attribution.
    /// </summary>
    public void AttributeDebuffBonuses(int targetHash, int totalDamage, bool hasVulnerable)
    {
        if (totalDamage <= 0) return;

        // Vulnerable: 1.5x multiplier → bonus = damage / 3
        if (hasVulnerable)
        {
            var vulnSource = ContributionMap.Instance.GetDebuffSource(targetHash, "VULNERABLE_POWER");
            if (vulnSource != null)
            {
                int bonus = totalDamage / 3;
                GetOrCreate(vulnSource.SourceId, vulnSource.SourceType).AttributedDamage += bonus;
            }
        }
    }

    /// <summary>
    /// Called for damage from a power (e.g., Poison tick). Attributes back to origin.
    /// </summary>
    public void OnPowerDamage(string powerId, int damage)
    {
        var source = ContributionMap.Instance.GetPowerSource(powerId);
        if (source != null)
        {
            GetOrCreate(source.SourceId, source.SourceType).AttributedDamage += damage;
        }
    }

    // ── Card Draw ───────────────────────────────────────────

    public void OnCardDrawn(bool fromHandDraw)
    {
        if (fromHandDraw) return; // Normal turn draw, not attributed

        if (_activeCardId != null)
        {
            GetOrCreate(_activeCardId, "card").CardsDrawn++;
        }
        else if (_activeRelicId != null)
        {
            GetOrCreate(_activeRelicId, "relic").CardsDrawn++;
        }
    }

    // ── Player Death ────────────────────────────────────────

    public void OnPlayerDied() => _playerDied = true;

    // ── Helpers ──────────────────────────────────────────────

    private ContributionAccum GetOrCreate(string sourceId, string sourceType)
    {
        if (!_currentCombat.TryGetValue(sourceId, out var accum))
        {
            accum = new ContributionAccum { SourceId = sourceId, SourceType = sourceType };
            _currentCombat[sourceId] = accum;
        }
        return accum;
    }

    /// <summary>
    /// Get a read-only snapshot of current combat contributions (for live UI).
    /// </summary>
    public IReadOnlyDictionary<string, ContributionAccum> GetCurrentCombatData() =>
        _currentCombat;
}
