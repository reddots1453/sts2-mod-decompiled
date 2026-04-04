namespace CommunityStats.Collection;

/// <summary>
/// Accumulates combat contribution for a single source (card or relic).
/// </summary>
public class ContributionAccum
{
    public string SourceId { get; set; } = "";
    public string SourceType { get; set; } = "card"; // "card" | "relic" | "potion"
    public int TimesPlayed { get; set; }
    public int DirectDamage { get; set; }
    public int AttributedDamage { get; set; }   // indirect (poison, vuln bonus, etc.)
    public int ModifierDamage { get; set; }      // from additive/multiplicative modifiers (Strength, etc.)
    public int EffectiveBlock { get; set; }      // actual damage mitigated by block
    public int ModifierBlock { get; set; }       // from additive/multiplicative block modifiers (Dexterity, etc.)
    public int MitigatedByDebuff { get; set; }   // damage prevented by Weak/etc on enemies
    public int MitigatedByBuff { get; set; }     // damage prevented by Buffer/Intangible
    public int CardsDrawn { get; set; }
    public int EnergyGained { get; set; }
    public int HpHealed { get; set; }
    public int StarsContribution { get; set; }       // stars gained or saved (VoidForm cost reduction)
    public int MitigatedByStrReduction { get; set; } // damage prevented by reducing enemy strength
    public int SelfDamage { get; set; }              // HP lost from self-damage cards (negative defense)

    /// <summary>Parent card that generated/transformed this card (for sub-bar display).</summary>
    public string? OriginSourceId { get; set; }

    /// <summary>Damage contributed by upgrade delta (attributed to the upgrade source).</summary>
    public int UpgradeDamage { get; set; }
    public int UpgradeBlock { get; set; }

    public int TotalDamage => DirectDamage + AttributedDamage + ModifierDamage + UpgradeDamage;
    public int TotalDefense => EffectiveBlock + ModifierBlock + MitigatedByDebuff + MitigatedByBuff + MitigatedByStrReduction + UpgradeBlock - SelfDamage;

    public void MergeFrom(ContributionAccum other)
    {
        TimesPlayed += other.TimesPlayed;
        DirectDamage += other.DirectDamage;
        AttributedDamage += other.AttributedDamage;
        ModifierDamage += other.ModifierDamage;
        EffectiveBlock += other.EffectiveBlock;
        ModifierBlock += other.ModifierBlock;
        MitigatedByDebuff += other.MitigatedByDebuff;
        MitigatedByBuff += other.MitigatedByBuff;
        CardsDrawn += other.CardsDrawn;
        EnergyGained += other.EnergyGained;
        HpHealed += other.HpHealed;
        StarsContribution += other.StarsContribution;
        MitigatedByStrReduction += other.MitigatedByStrReduction;
        SelfDamage += other.SelfDamage;
        UpgradeDamage += other.UpgradeDamage;
        UpgradeBlock += other.UpgradeBlock;
    }
}

/// <summary>
/// Tracks which card or relic applied each power, enabling indirect damage attribution.
/// e.g., if "Noxious Fumes" applied Poison, damage from Poison ticks is attributed back to that card.
/// Also tracks debuffs on specific creatures and block sources for FIFO attribution.
/// </summary>
public class ContributionMap
{
    public static ContributionMap Instance { get; } = new();

    // PowerModel.Id.Entry → (sourceId, sourceType)
    private readonly Dictionary<string, PowerSource> _powerSources = new();

    public record PowerSource(string SourceId, string SourceType);

    // Per-creature debuff tracking: (creatureHash, powerId) → source
    private readonly Dictionary<(int, string), PowerSource> _creatureDebuffSources = new();

    // Per-creature buff tracking (on player): (powerId) → source
    private readonly Dictionary<string, PowerSource> _playerBuffSources = new();

    // Block pool: FIFO queue of (sourceId, sourceType, remainingAmount)
    private readonly List<BlockEntry> _blockPool = new();

    public class BlockEntry
    {
        public string SourceId = "";
        public string SourceType = "";
        public int Remaining;
    }

    public void RecordPowerSource(string powerId, string sourceId, string sourceType)
    {
        _powerSources[powerId] = new PowerSource(sourceId, sourceType);
    }

    public void RecordDebuffSource(int creatureHash, string powerId, string sourceId, string sourceType)
    {
        _creatureDebuffSources[(creatureHash, powerId)] = new PowerSource(sourceId, sourceType);
    }

    public void RecordPlayerBuffSource(string powerId, string sourceId, string sourceType)
    {
        _playerBuffSources[powerId] = new PowerSource(sourceId, sourceType);
    }

    public void AddBlock(string sourceId, string sourceType, int amount)
    {
        if (amount <= 0) return;
        _blockPool.Add(new BlockEntry { SourceId = sourceId, SourceType = sourceType, Remaining = amount });
    }

    /// <summary>
    /// Consume block from the pool (FIFO) and return per-source consumption.
    /// </summary>
    public List<(string SourceId, string SourceType, int Amount)> ConsumeBlock(int blockedDamage)
    {
        var result = new List<(string, string, int)>();
        var remaining = blockedDamage;

        for (int i = 0; i < _blockPool.Count && remaining > 0; i++)
        {
            var entry = _blockPool[i];
            if (entry.Remaining <= 0) continue;

            var consumed = Math.Min(entry.Remaining, remaining);
            result.Add((entry.SourceId, entry.SourceType, consumed));
            entry.Remaining -= consumed;
            remaining -= consumed;
        }

        // Cleanup empty entries
        _blockPool.RemoveAll(e => e.Remaining <= 0);
        return result;
    }

    /// <summary>
    /// Clear block pool (called at turn start when block resets).
    /// </summary>
    public void ClearBlockPool()
    {
        _blockPool.Clear();
    }

    public PowerSource? GetPowerSource(string powerId)
    {
        return _powerSources.GetValueOrDefault(powerId);
    }

    public PowerSource? GetDebuffSource(int creatureHash, string powerId)
    {
        return _creatureDebuffSources.GetValueOrDefault((creatureHash, powerId));
    }

    public PowerSource? GetPlayerBuffSource(string powerId)
    {
        return _playerBuffSources.GetValueOrDefault(powerId);
    }

    // ── Damage Modifier Context ──────────────────────────────
    // Populated by Hook.ModifyDamageInternal patch, consumed by DamageReceived patch.

    /// <summary>Per-modifier damage contributions from the most recent ModifyDamage call.</summary>
    public List<ModifierContribution> LastDamageModifiers { get; } = new();

    /// <summary>Per-modifier block contributions from the most recent ModifyBlock call.</summary>
    public List<ModifierContribution> LastBlockModifiers { get; } = new();

    public record ModifierContribution(string SourceId, string SourceType, int Amount);

    // ── Card Origin Tracking ───────────────────────────────
    // Tracks which card generated/transformed another card (for sub-bar display).

    private readonly Dictionary<int, (string originId, string originType)> _cardOriginMap = new();

    public void RecordCardOrigin(int cardHash, string originId, string originType)
    {
        _cardOriginMap[cardHash] = (originId, originType);
    }

    public (string originId, string originType)? GetCardOrigin(int cardHash)
    {
        return _cardOriginMap.TryGetValue(cardHash, out var origin) ? origin : null;
    }

    // ── Enemy Strength Reduction Tracking ──────────────────
    // Tracks player-caused strength reductions on enemies for defense attribution.

    private readonly Dictionary<int, List<StrReductionEntry>> _enemyStrReductions = new();

    public record StrReductionEntry(string SourceId, string SourceType, int Amount, bool IsTemporary);

    public void RecordStrengthReduction(int enemyHash, string sourceId, string sourceType, int amount, bool isTemporary)
    {
        if (amount <= 0) return;
        if (!_enemyStrReductions.TryGetValue(enemyHash, out var list))
        {
            list = new List<StrReductionEntry>();
            _enemyStrReductions[enemyHash] = list;
        }
        var existing = list.FindIndex(e => e.SourceId == sourceId);
        if (existing >= 0)
            list[existing] = list[existing] with { Amount = list[existing].Amount + amount };
        else
            list.Add(new StrReductionEntry(sourceId, sourceType, amount, isTemporary));
    }

    public void RevertTemporaryStrReduction(int enemyHash)
    {
        if (_enemyStrReductions.TryGetValue(enemyHash, out var list))
            list.RemoveAll(e => e.IsTemporary);
    }

    public IReadOnlyList<StrReductionEntry>? GetStrReductions(int enemyHash)
        => _enemyStrReductions.GetValueOrDefault(enemyHash);

    // ── SeekingEdge Context ─────────────────────────────────
    // When SovereignBlade attacks all enemies via SeekingEdge, damage to non-primary
    // targets is attributed to SeekingEdge source instead of SovereignBlade.

    private int _seekingEdgePrimaryTargetHash;
    private bool _seekingEdgeActive;

    public bool IsSeekingEdgeActive => _seekingEdgeActive;
    public int SeekingEdgePrimaryTarget => _seekingEdgePrimaryTargetHash;

    public void SetSeekingEdgeContext(int primaryTargetHash)
    {
        _seekingEdgeActive = true;
        _seekingEdgePrimaryTargetHash = primaryTargetHash;
    }

    public void ClearSeekingEdgeContext()
    {
        _seekingEdgeActive = false;
        _seekingEdgePrimaryTargetHash = 0;
    }

    // ── Pending Cost Savings ────────────────────────────────
    // Recorded when VoidFormPower reduces card cost, consumed at card play.

    private (string sourceId, string sourceType, int energy, int stars) _pendingCostSaving;
    private bool _hasPendingCostSaving;

    public void RecordPendingCostSavings(string sourceId, string sourceType, int energySaved, int starsSaved)
    {
        if (_hasPendingCostSaving)
        {
            _pendingCostSaving = (_pendingCostSaving.sourceId, _pendingCostSaving.sourceType,
                _pendingCostSaving.energy + energySaved, _pendingCostSaving.stars + starsSaved);
        }
        else
        {
            _pendingCostSaving = (sourceId, sourceType, energySaved, starsSaved);
            _hasPendingCostSaving = true;
        }
    }

    public (string sourceId, string sourceType, int energy, int stars)? ConsumePendingCostSavings()
    {
        if (!_hasPendingCostSaving) return null;
        var result = _pendingCostSaving;
        _hasPendingCostSaving = false;
        _pendingCostSaving = default;
        return result;
    }

    // ── Pending Hand Draw Bonus ─────────────────────────────
    // Recorded by ModifyHandDraw patches, consumed after hand draw completes.

    private readonly List<(string sourceId, string sourceType, int count)> _pendingHandDrawBonus = new();

    public void RecordHandDrawBonus(string sourceId, string sourceType, int extraDraw)
    {
        if (extraDraw > 0)
            _pendingHandDrawBonus.Add((sourceId, sourceType, extraDraw));
    }

    public List<(string sourceId, string sourceType, int count)> ConsumePendingHandDrawBonus()
    {
        var result = new List<(string, string, int)>(_pendingHandDrawBonus);
        _pendingHandDrawBonus.Clear();
        return result;
    }

    // ── Osty HP Source LIFO Stack ─────────────────────────────
    // Tracks which card/relic summoned Osty HP, consumed LIFO when Osty takes damage.

    private readonly List<OstyHpEntry> _ostyHpStack = new();

    public class OstyHpEntry
    {
        public string SourceId = "";
        public string SourceType = "";
        public int Remaining;
    }

    public void PushOstyHp(string sourceId, string sourceType, int amount)
    {
        if (amount <= 0) return;
        _ostyHpStack.Add(new OstyHpEntry { SourceId = sourceId, SourceType = sourceType, Remaining = amount });
    }

    /// <summary>
    /// Consume Osty HP from the stack (LIFO) and return per-source consumption.
    /// </summary>
    public List<(string SourceId, string SourceType, int Amount)> ConsumeOstyHp(int damage)
    {
        var result = new List<(string, string, int)>();
        var remaining = damage;

        // LIFO: consume from end of list
        for (int i = _ostyHpStack.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var entry = _ostyHpStack[i];
            if (entry.Remaining <= 0) continue;

            var consumed = Math.Min(entry.Remaining, remaining);
            result.Add((entry.SourceId, entry.SourceType, consumed));
            entry.Remaining -= consumed;
            remaining -= consumed;
        }

        // Cleanup empty entries
        _ostyHpStack.RemoveAll(e => e.Remaining <= 0);
        return result;
    }

    /// <summary>
    /// Returns total remaining HP in the Osty LIFO stack.
    /// </summary>
    public int GetRemainingOstyHpTotal()
    {
        int total = 0;
        foreach (var entry in _ostyHpStack) total += entry.Remaining;
        return total;
    }

    public void ClearOstyHpStack()
    {
        _ostyHpStack.Clear();
    }

    // ── Doom HP Capture ─────────────────────────────────────
    // Captures enemy HP before Doom kills them, so we can attribute damage.

    private readonly List<(int CreatureHash, int Hp)> _pendingDoomHp = new();

    public void RecordDoomTargetHp(int creatureHash, int hp)
    {
        _pendingDoomHp.Add((creatureHash, hp));
    }

    public List<(int CreatureHash, int Hp)> ConsumeDoomTargetHp()
    {
        var result = new List<(int, int)>(_pendingDoomHp);
        _pendingDoomHp.Clear();
        return result;
    }

    // ── Orb Source Tracking ────────────────────────────────
    // Tracks which card/relic channeled each orb for indirect contribution attribution.

    private readonly Dictionary<int, OrbSource> _orbSources = new();

    public record OrbSource(string SourceId, string SourceType, string OrbType);

    public void RecordOrbSource(int orbHash, string sourceId, string sourceType, string orbType)
    {
        _orbSources[orbHash] = new OrbSource(sourceId, sourceType, orbType);
    }

    public OrbSource? GetOrbSource(int orbHash)
    {
        return _orbSources.GetValueOrDefault(orbHash);
    }

    // ── Active Orb Context ──────────────────────────────────
    // Set when an orb passive/evoke is executing so damage/block/energy can be attributed.
    // First-trigger logic: during a card play, the first orb trigger goes to channeling source,
    // subsequent triggers go to the evoke/trigger card (_activeCardId).

    private (string sourceId, string sourceType, string orbType)? _activeOrbContext;

    /// <summary>Focus contribution for the current orb trigger (set by patch before orb fires).</summary>
    private (string sourceId, string sourceType, int amount)? _pendingOrbFocusContrib;

    /// <summary>
    /// True after the first orb trigger during a card play has been consumed.
    /// Reset at each card play start. When true, orb context is NOT set so that
    /// _activeCardId (the evoke card) takes priority in ResolveSource.
    /// </summary>
    private bool _orbFirstTriggerUsed;

    public bool HasActiveOrbContext => _activeOrbContext != null;
    public (string sourceId, string sourceType, string orbType)? ActiveOrbContext => _activeOrbContext;
    public (string sourceId, string sourceType, int amount)? PendingOrbFocusContrib => _pendingOrbFocusContrib;
    public bool OrbFirstTriggerUsed => _orbFirstTriggerUsed;

    public void SetActiveOrbContext(string sourceId, string sourceType, string orbType)
    {
        _activeOrbContext = (sourceId, sourceType, orbType);
    }

    public void MarkOrbFirstTriggerUsed()
    {
        _orbFirstTriggerUsed = true;
    }

    public void ResetOrbFirstTrigger()
    {
        _orbFirstTriggerUsed = false;
    }

    public void SetPendingOrbFocusContrib(string sourceId, string sourceType, int amount)
    {
        if (amount > 0)
            _pendingOrbFocusContrib = (sourceId, sourceType, amount);
    }

    public void ClearActiveOrbContext()
    {
        _activeOrbContext = null;
        _pendingOrbFocusContrib = null;
    }

    // ── Upgrade Delta Tracking ─────────────────────────────
    // Tracks damage/block upgrade delta per card instance, attributed to the upgrade source.

    private readonly Dictionary<int, UpgradeDelta> _upgradeDeltaMap = new();

    public record UpgradeDelta(int DamageDelta, int BlockDelta, string SourceId, string SourceType);

    public void RecordUpgradeDelta(int cardHash, int damageDelta, int blockDelta, string sourceId, string sourceType)
    {
        _upgradeDeltaMap[cardHash] = new UpgradeDelta(damageDelta, blockDelta, sourceId, sourceType);
    }

    public UpgradeDelta? GetUpgradeDelta(int cardHash)
    {
        return _upgradeDeltaMap.GetValueOrDefault(cardHash);
    }

    public void Clear()
    {
        _powerSources.Clear();
        _creatureDebuffSources.Clear();
        _playerBuffSources.Clear();
        _blockPool.Clear();
        LastDamageModifiers.Clear();
        LastBlockModifiers.Clear();
        _cardOriginMap.Clear();
        _upgradeDeltaMap.Clear();
        _enemyStrReductions.Clear();
        _seekingEdgeActive = false;
        _seekingEdgePrimaryTargetHash = 0;
        _hasPendingCostSaving = false;
        _pendingCostSaving = default;
        _pendingHandDrawBonus.Clear();
        _ostyHpStack.Clear();
        _pendingDoomHp.Clear();
        _orbSources.Clear();
        _activeOrbContext = null;
        _pendingOrbFocusContrib = null;
        _orbFirstTriggerUsed = false;
    }
}
