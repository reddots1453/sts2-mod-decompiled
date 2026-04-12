using CommunityStats.Api;
using CommunityStats.Util;

namespace CommunityStats.Collection;

/// <summary>
/// Per-combat tracker. Records card plays, damage, defense (effective block + mitigation),
/// draws, energy, and attributes indirect contributions back to their source card/relic/potion.
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

    // The potion currently being used (set by PotionContextPatch Prefix, cleared by Postfix)
    private string? _activePotionId;

    // Public accessors for source tagging (NEW-1: local cost modifier patches need active context)
    // ActiveCardId already exposed above
    public string? ActivePotionId => _activePotionId;
    public string? ActiveRelicId => _activeRelicId;

    // The power whose hook is currently executing (for indirect effects like Rage, FlameBarrier)
    private string? _activePowerSourceId;
    private string? _activePowerSourceType;

    // Track DamageResult objects already processed by DamageReceived,
    // so KillingBlowPatcher doesn't double-count them.
    private readonly HashSet<int> _processedDamageResults = new();

    // Pending upgrade delta for the currently playing card (per-hit split, per Review R3)
    private int _pendingUpgradeDamageDelta;
    private int _pendingUpgradeBlockDelta;

    // H3: Deferred Osty death negative defense — only applied when enemy next attacks player
    private (string sourceId, string sourceType, int amount)? _pendingOstyDeathDefense;

    // Forge tracking: each ForgeCmd.Forge call records (sourceId, sourceType, amount).
    // When SovereignBlade plays, these are written as sub-bar entries under SOVEREIGN_BLADE.
    private readonly List<(string sourceId, string sourceType, int amount)> _forgeLog = new();

    // Event fired after card play or potion use completes (for real-time UI refresh)
    public event Action? CombatDataUpdated;

    // Current encounter info
    private string _encounterId = "";
    private string _encounterType = "";
    private int _damageTakenByPlayer;
    private int _turnCount;
    private bool _playerDied;
    private int _floor;
    private int _totalDamageDealt;

    // Snapshot of the last combat (for UI display)
    private Dictionary<string, ContributionAccum>? _lastCombatData;
    private string _lastEncounterId = "";

    public IReadOnlyDictionary<string, ContributionAccum>? LastCombatData => _lastCombatData;
    public string LastEncounterId => _lastEncounterId;

    // Public DPS accessors (PRD 3.7)
    public int TurnCount => _turnCount;
    public int TotalDamageDealt => _totalDamageDealt;

    /// <summary>
    /// Fire CombatDataUpdated event for real-time UI refresh. Called from
    /// CombatHistoryPatch.AfterCardPlayFinished. Round 8: also persist a
    /// live snapshot to disk so save+quit doesn't lose 本场战斗 data.
    /// </summary>
    public void NotifyCombatDataUpdated()
    {
        // Live persistence first so the disk file is always up-to-date for
        // save+quit recovery (PRD §3.6.1).
        try
        {
            Util.ContributionPersistence.SaveLiveState(BuildLiveSnapshot(combatInProgress: true));
        }
        catch (Exception ex)
        {
            Godot.GD.Print($"[StatsTheSpire] SaveLiveState error: {ex.Message}");
        }

        try { CombatDataUpdated?.Invoke(); }
        catch (Exception ex) { Godot.GD.Print($"[StatsTheSpire] CombatDataUpdated error: {ex.Message}"); }
    }

    /// <summary>
    /// Build a snapshot of the current tracker state for disk persistence.
    /// </summary>
    public LiveContributionSnapshot BuildLiveSnapshot(bool combatInProgress)
    {
        return new LiveContributionSnapshot
        {
            EncounterId = _encounterId ?? "",
            EncounterType = _encounterType ?? "",
            Floor = _floor,
            TurnCount = _turnCount,
            TotalDamageDealt = _totalDamageDealt,
            DamageTakenByPlayer = _damageTakenByPlayer,
            CombatInProgress = combatInProgress,
            CurrentCombat = combatInProgress
                ? new Dictionary<string, ContributionAccum>(_currentCombat)
                : null,
            RunTotals = new Dictionary<string, ContributionAccum>(
                RunContributionAggregator.Instance.RunTotals),
        };
    }

    /// <summary>
    /// Restore tracker state from a live snapshot loaded at mod boot.
    /// Called from CommunityStatsMod.Initialize when a `_live.json` file
    /// matches the active run's seed.
    /// </summary>
    public void HydrateFromLiveSnapshot(LiveContributionSnapshot snap)
    {
        if (snap == null) return;
        _encounterId = snap.EncounterId ?? "";
        _encounterType = snap.EncounterType ?? "";
        _floor = snap.Floor;
        _turnCount = snap.TurnCount;
        _totalDamageDealt = snap.TotalDamageDealt;
        _damageTakenByPlayer = snap.DamageTakenByPlayer;

        if (snap.CombatInProgress && snap.CurrentCombat != null)
        {
            _currentCombat.Clear();
            foreach (var (k, v) in snap.CurrentCombat) _currentCombat[k] = v;
        }
        else
        {
            // Combat was not in progress at save time → restore to LastCombatData
            // so the panel still shows the most recent fight.
            _lastCombatData = snap.CurrentCombat ?? new Dictionary<string, ContributionAccum>();
            _lastEncounterId = snap.EncounterId ?? "";
        }

        if (snap.RunTotals != null)
            RunContributionAggregator.Instance.HydrateRunTotals(snap.RunTotals);
    }

    // ── Lifecycle ────────────────────────────────────────────

    public void OnCombatStart(string encounterId, string encounterType, int floor)
    {
        _currentCombat.Clear();
        _activeCardId = null;
        _activeRelicId = null;
        _activePotionId = null;
        _activePowerSourceId = null;
        _activePowerSourceType = null;
        _encounterId = encounterId;
        _encounterType = encounterType;
        _damageTakenByPlayer = 0;
        _turnCount = 0;
        _totalDamageDealt = 0;
        _playerDied = false;
        _floor = floor;
        _pendingOstyDeathDefense = null;
        _pendingUpgradeDamageDelta = 0;
        _pendingUpgradeBlockDelta = 0;
        _processedDamageResults.Clear();
        _forgeLog.Clear();
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

        // Round 8 §3.6.1: persist the post-combat live snapshot so a
        // save+quit between combats keeps the run-totals tab populated.
        try
        {
            Util.ContributionPersistence.SaveLiveState(BuildLiveSnapshot(combatInProgress: false));
        }
        catch { }
    }

    // ── Damage dedup (for KillingBlowPatcher) ────────────────
    public void MarkDamageResultProcessed(int resultHash) => _processedDamageResults.Add(resultHash);
    public bool IsDamageResultProcessed(int resultHash) => _processedDamageResults.Contains(resultHash);

    // ── Exposed Context (for patches that need to read current state) ──

    public string? ActiveCardId => _activeCardId;
    public string? ActivePowerSourceId => _activePowerSourceId;
    public string? ActivePowerSourceType => _activePowerSourceType;

    // ── Card Play Tracking ──────────────────────────────────

    public void OnCardPlayStarted(string cardId, int cardHash)
    {
        _activeCardId = cardId;
        // Safety: clear stale potion context (shouldn't still be set, but guards against
        // edge cases where PotionUsed didn't fire, e.g. combat ended mid-potion)
        _activePotionId = null;
        var accum = GetOrCreate(cardId, "card");
        accum.TimesPlayed++;
        // Tag origin so generated/transformed cards display as sub-bars
        TagCardOrigin(cardId, cardHash);
        // Reset orb first-trigger flag so first orb trigger during this card play
        // goes to channeling source, subsequent triggers go to this card.
        ContributionMap.Instance.ResetOrbFirstTrigger();
        // Load upgrade delta for per-hit split (C2: activated upgrade tracking)
        var upgDelta = ContributionMap.Instance.GetUpgradeDelta(cardHash);
        _pendingUpgradeDamageDelta = upgDelta?.DamageDelta ?? 0;
        _pendingUpgradeBlockDelta = upgDelta?.BlockDelta ?? 0;
    }

    public void OnCardPlayFinished()
    {
        _activeCardId = null;
    }

    // ── Relic Context Tracking ──────────────────────────────

    public void SetActiveRelic(string relicId) => _activeRelicId = relicId;
    public void ClearActiveRelic() => _activeRelicId = null;

    // ── Potion Context Tracking ─────────────────────────────

    public void SetActivePotion(string potionId) => _activePotionId = potionId;
    public void ClearActivePotion() => _activePotionId = null;

    // ── Power Source Context Tracking ───────────────────────
    // Set when a Power's hook method is executing (e.g., RagePower.AfterCardPlayed,
    // FlameBarrierPower.AfterDamageReceived). This lets indirect effects (damage/block
    // caused by the power) be attributed back to the card that originally applied the power.

    public void SetActivePowerSource(string powerId)
    {
        var source = ContributionMap.Instance.GetPowerSource(powerId);
        if (source != null)
        {
            _activePowerSourceId = source.SourceId;
            _activePowerSourceType = source.SourceType;
        }
    }

    public void ClearActivePowerSource()
    {
        _activePowerSourceId = null;
        _activePowerSourceType = null;
    }

    /// <summary>Force-clear ALL context. Called between tests to prevent stale state.</summary>
    public void ForceResetAllContext()
    {
        _activeCardId = null;
        _activePotionId = null;
        _activeRelicId = null;
        _activePowerSourceId = null;
        _activePowerSourceType = null;
        _pendingDrawSourceId = null;
        _pendingDrawSourceType = null;
        ContributionMap.Instance.ClearActiveOrbContext();
    }

    // ── Turn Tracking ───────────────────────────────────────

    public void OnTurnStart()
    {
        _turnCount++;
    }

    // ── Resolve Source ───────────────────────────────────────
    // RESTORED original priority: card first (explicit cardSourceId or _activeCardId),
    // then potion, then relic, then power, then orb.
    // Orb context is checked via separate orb-aware methods, not the main fallback.

    private (string id, string type) ResolveSource(string? cardSourceId)
    {
        if (cardSourceId != null)
            return (cardSourceId, "card");
        // Orb context takes priority over card when set (orb passive/evoke damage)
        var orbCtx = ContributionMap.Instance.ActiveOrbContext;
        if (orbCtx != null)
            return (orbCtx.Value.sourceId, orbCtx.Value.sourceType);
        if (_activeCardId != null)
            return (_activeCardId, "card");
        if (_activePotionId != null)
            return (_activePotionId, "potion");
        if (_activeRelicId != null)
            return (_activeRelicId, "relic");
        if (_activePowerSourceId != null)
            return (_activePowerSourceId, _activePowerSourceType ?? "card");
        // No context found
        Godot.GD.Print($"[CommunityStats] WARN ResolveSource: no active context, using UNTRACKED");
        return ("UNTRACKED", "untracked");
    }

    // ── Damage Dealt to Enemies ─────────────────────────────

    /// <summary>
    /// Called when any creature takes damage.
    /// For damage to enemies: attributes to card/relic source, with modifier bonuses split out.
    /// For damage to player: tracks for encounter stats + block attribution + str reduction mitigation.
    /// </summary>
    public void OnDamageDealt(int totalDamage, int blockedDamage, string? cardSourceId,
        bool isPlayerReceiver, int targetHash, int dealerHash,
        bool isOstyDealer = false, bool isOstyReceiver = false)
    {
        // Osty receiving damage = absorbing for player → defense contribution
        if (isOstyReceiver && !isPlayerReceiver)
        {
            int hpLost = totalDamage; // totalDamage on Osty = HP lost by Osty
            OnOstyAbsorbedDamage(hpLost);
            return;
        }

        // Osty dealing damage to enemies → sub-bar under OSTY parent
        if (isOstyDealer && !isPlayerReceiver && cardSourceId != null)
        {
            OnOstyDamageDealt(totalDamage, cardSourceId);
            return;
        }

        if (isPlayerReceiver)
        {
            // H3: Apply deferred Osty death negative defense on first enemy damage to player
            if (_pendingOstyDeathDefense != null && dealerHash != targetHash)
            {
                var (ostySourceId, ostySourceType, ostyAmount) = _pendingOstyDeathDefense.Value;
                GetOrCreate(ostySourceId, ostySourceType).EffectiveBlock -= ostyAmount;
                _pendingOstyDeathDefense = null;
            }

            _damageTakenByPlayer += totalDamage;

            // Self-damage: player dealing damage to themselves via a card (Bloodletting, Offering, etc.)
            // Record as negative defense contribution
            int unblockedSelf = totalDamage - blockedDamage;
            if (dealerHash != 0 && dealerHash == targetHash && unblockedSelf > 0)
            {
                var (srcId, srcType) = ResolveSource(cardSourceId);
                GetOrCreate(srcId, srcType).SelfDamage += unblockedSelf;
            }

            // Attribute blocked damage to block sources via FIFO pool
            if (blockedDamage > 0)
            {
                var consumed = ContributionMap.Instance.ConsumeBlock(blockedDamage);
                foreach (var (sourceId, sourceType, amount) in consumed)
                {
                    GetOrCreate(sourceId, sourceType).EffectiveBlock += amount;
                }
            }

            // Attribute enemy strength reduction as defense contribution
            if (dealerHash != 0)
            {
                var reductions = ContributionMap.Instance.GetStrReductions(dealerHash);
                if (reductions != null && reductions.Count > 0)
                {
                    int totalReduction = 0;
                    foreach (var r in reductions) totalReduction += r.Amount;
                    if (totalReduction > 0)
                    {
                        // C1 fix: cap effective reduction to enemy base damage per hit
                        int enemyBase = ContributionMap.Instance.PendingEnemyBaseDamage;
                        int effectiveReduction = enemyBase > 0 ? Math.Min(totalReduction, enemyBase) : totalReduction;
                        // Proportional share per source
                        foreach (var entry in reductions)
                        {
                            int share = (int)Math.Round((float)entry.Amount / totalReduction * effectiveReduction);
                            if (share > 0)
                                GetOrCreate(entry.SourceId, entry.SourceType).MitigatedByStrReduction += share;
                        }
                    }
                }
            }
            return;
        }

        // Damage dealt TO enemies — track DPS (PRD 3.7) and attribute to source
        _totalDamageDealt += totalDamage;

        // First, consume damage modifier context from Hook.ModifyDamageInternal patch
        var modifiers = ContributionMap.Instance.LastDamageModifiers;
        int modifierTotal = 0;
        if (modifiers.Count > 0)
        {
            foreach (var mod in modifiers)
            {
                if (mod.Amount > 0)
                {
                    modifierTotal += mod.Amount;
                }
            }
        }

        // PRD-04 §4.1 — multi-multiplicative modifier scaling.
        // When multiple independent multiplicative modifiers stack (e.g. Vulnerable ×1.5
        // and DoubleDamage ×2 on Strike 6 → total 18, raw modifier votes 6 + 9 = 15 > 12),
        // their per-modifier contribution sum can exceed (totalDamage - baseDamage).
        // We don't have baseDamage here, so enforce the weaker invariant
        //   sum(ModifierDamage) <= totalDamage
        // by proportionally scaling down each modifier's vote when overshoot is detected.
        // This preserves the constraint DirectDamage + ModifierDamage = TotalDamage and
        // never reports a negative directDamage.
        if (modifierTotal > totalDamage && modifierTotal > 0 && totalDamage >= 0)
        {
            float scale = (float)totalDamage / modifierTotal;
            int scaledTotal = 0;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                if (m.Amount <= 0) continue;
                int scaledAmount = (int)Math.Round(m.Amount * scale);
                modifiers[i] = new ContributionMap.ModifierContribution(m.SourceId, m.SourceType, scaledAmount);
                scaledTotal += scaledAmount;
            }
            modifierTotal = scaledTotal;
        }

        int directDamage = Math.Max(0, totalDamage - modifierTotal);

        // SeekingEdge split: non-primary targets get damage attributed to SeekingEdge source
        bool seekingRedirect = false;
        if (ContributionMap.Instance.IsSeekingEdgeActive
            && cardSourceId == "SOVEREIGN_BLADE"
            && targetHash != ContributionMap.Instance.SeekingEdgePrimaryTarget)
        {
            var seekingSource = ContributionMap.Instance.GetPowerSource("SEEKING_EDGE_POWER");
            if (seekingSource != null)
            {
                GetOrCreate(seekingSource.SourceId, seekingSource.SourceType).DirectDamage += directDamage;
                // Also redirect modifier damage to SeekingEdge source
                if (modifiers.Count > 0)
                {
                    foreach (var mod in modifiers)
                    {
                        if (mod.Amount > 0)
                            GetOrCreate(seekingSource.SourceId, seekingSource.SourceType).ModifierDamage += mod.Amount;
                    }
                }
                seekingRedirect = true;
            }
        }

        if (!seekingRedirect)
        {
            // Normal attribution
            if (modifiers.Count > 0)
            {
                foreach (var mod in modifiers)
                {
                    if (mod.Amount > 0)
                        GetOrCreate(mod.SourceId, mod.SourceType).ModifierDamage += mod.Amount;
                }
            }

            // Focus contribution split: when orb is the source, split Focus bonus as ModifierDamage
            var focusContrib = ContributionMap.Instance.PendingOrbFocusContrib;
            if (focusContrib != null && focusContrib.Value.amount > 0)
            {
                int focusAmount = Math.Min(focusContrib.Value.amount, directDamage);
                if (focusAmount > 0)
                {
                    GetOrCreate(focusContrib.Value.sourceId, focusContrib.Value.sourceType).ModifierDamage += focusAmount;
                    directDamage -= focusAmount;
                }
            }

            var (sourceId2, sourceType2) = ResolveSource(cardSourceId);

            // Per-hit upgrade delta split (C2): each hit of a multi-hit card gets its upgrade bonus
            if (_pendingUpgradeDamageDelta > 0 && directDamage > 0)
            {
                int upgSplit = Math.Min(_pendingUpgradeDamageDelta, directDamage);
                GetOrCreate(sourceId2, sourceType2).UpgradeDamage += upgSplit;
                directDamage -= upgSplit;
            }

            // Indirect damage (poison, thorns, orbs) → AttributedDamage
            // Direct damage (cards, relics, potions) → DirectDamage
            // Orb context always means indirect (orb passive/evoke damage)
            bool hasOrbContext = ContributionMap.Instance.ActiveOrbContext != null;
            bool isIndirect = hasOrbContext || (cardSourceId == null
                && _activeCardId == null
                && _activePotionId == null
                && _activeRelicId == null
                && _activePowerSourceId != null);

            if (isIndirect)
                GetOrCreate(sourceId2, sourceType2).AttributedDamage += directDamage;
            else
                GetOrCreate(sourceId2, sourceType2).DirectDamage += directDamage;
        }

        modifiers.Clear();
    }

    // ── Defense: Weak on enemies ────────────────────────────

    public void OnWeakMitigation(int actualDamage, int dealerHash, float weakMultiplier = 0.75f)
    {
        if (actualDamage <= 0) return;

        // Fix-2: Use actual multiplier to compute prevented damage
        // prevented = damage_before_weak - damage_after_weak = actualDamage/mult - actualDamage
        int prevented = (int)Math.Round(actualDamage / weakMultiplier - actualDamage);
        if (prevented <= 0) return;

        // H1: Use FIFO fractional attribution for multi-source Weak
        var fractions = ContributionMap.Instance.GetDebuffSourceFractions(dealerHash, "WEAK_POWER");
        if (fractions.Count > 0)
        {
            foreach (var (sourceId, sourceType, frac) in fractions)
            {
                int share = (int)Math.Round(prevented * frac);
                if (share > 0)
                    GetOrCreate(sourceId, sourceType).MitigatedByDebuff += share;
            }
        }
    }

    // ── Defense: Buffer / Intangible ────────────────────────

    public void OnBufferPrevention(int preventedDamage)
    {
        if (preventedDamage <= 0) return;

        var source = ContributionMap.Instance.GetPlayerBuffSource("BUFFER_POWER");
        string sourceId = source?.SourceId ?? "BUFFER_POWER";
        string sourceType = source?.SourceType ?? "power";
        GetOrCreate(sourceId, sourceType).MitigatedByBuff += preventedDamage;
    }

    public void OnIntangibleReduction(int originalDamage, int reducedTo)
    {
        int prevented = originalDamage - reducedTo;
        if (prevented <= 0) return;

        var source = ContributionMap.Instance.GetPlayerBuffSource("INTANGIBLE_POWER");
        string sourceId = source?.SourceId ?? "INTANGIBLE_POWER";
        string sourceType = source?.SourceType ?? "power";
        GetOrCreate(sourceId, sourceType).MitigatedByBuff += prevented;
    }

    /// <summary>
    /// Called when ColossusPower on the player halves damage from a Vulnerable enemy.
    /// Colossus multiplier = 0.5, so prevented = actualDamage (damage would have been 2x).
    /// </summary>
    public void OnColossusMitigation(int actualDamage, int playerHash)
    {
        if (actualDamage <= 0) return;

        // Colossus is 0.5x, so prevented = actualDamage / 0.5 - actualDamage = actualDamage
        int prevented = actualDamage;

        var source = ContributionMap.Instance.GetPlayerBuffSource("COLOSSUS_POWER");
        string sourceId = source?.SourceId ?? "COLOSSUS_POWER";
        string sourceType = source?.SourceType ?? "power";
        GetOrCreate(sourceId, sourceType).MitigatedByBuff += prevented;
    }

    // ── Block Tracking ──────────────────────────────────────

    public void OnBlockGained(int amount, string? cardPlayId)
    {
        var (sourceId, sourceType) = ResolveSource(cardPlayId);

        if (amount > 0)
        {
            ContributionMap.Instance.AddBlock(sourceId, sourceType, amount);

            // Focus contribution split for Frost orb: Focus bonus as ModifierBlock
            var focusContrib = ContributionMap.Instance.PendingOrbFocusContrib;
            if (focusContrib != null && focusContrib.Value.amount > 0)
            {
                int focusAmount = Math.Min(focusContrib.Value.amount, amount);
                if (focusAmount > 0)
                    GetOrCreate(focusContrib.Value.sourceId, focusContrib.Value.sourceType).ModifierBlock += focusAmount;
            }

            // Per-hit upgrade block delta split (C2)
            if (_pendingUpgradeBlockDelta > 0 && amount > 0)
            {
                int upgSplit = Math.Min(_pendingUpgradeBlockDelta, amount);
                GetOrCreate(sourceId, sourceType).UpgradeBlock += upgSplit;
            }

            // Consume block modifier context (Dexterity, etc.)
            var modifiers = ContributionMap.Instance.LastBlockModifiers;
            int modifierTotal = 0;
            if (modifiers.Count > 0)
            {
                foreach (var mod in modifiers)
                {
                    if (mod.Amount > 0)
                    {
                        GetOrCreate(mod.SourceId, mod.SourceType).ModifierBlock += mod.Amount;
                        modifierTotal += mod.Amount;
                    }
                }
                modifiers.Clear();
            }
        }
    }

    // ── Power Application ───────────────────────────────────

    public void OnPowerSourceRecorded(string powerId)
    {
        if (_activeCardId != null)
            ContributionMap.Instance.RecordPowerSource(powerId, _activeCardId, "card");
        else if (_activePotionId != null)
            ContributionMap.Instance.RecordPowerSource(powerId, _activePotionId, "potion");
        else if (_activeRelicId != null)
            ContributionMap.Instance.RecordPowerSource(powerId, _activeRelicId, "relic");
        else if (_activePowerSourceId != null)
            ContributionMap.Instance.RecordPowerSource(powerId, _activePowerSourceId, _activePowerSourceType ?? "card");
    }

    public void OnPowerApplied(string powerId, decimal amount, int creatureHash, bool isPlayerTarget)
    {
        string? sourceId = _activeCardId ?? _activePotionId ?? _activeRelicId ?? _activePowerSourceId;
        string sourceType = _activeCardId != null ? "card" :
                            _activePotionId != null ? "potion" :
                            _activeRelicId != null ? "relic" :
                            _activePowerSourceId != null ? (_activePowerSourceType ?? "card") : "card";

        if (sourceId == null) return;

        // Record power source with amount for proportional multi-source attribution
        int intAmount = (int)amount;
        ContributionMap.Instance.RecordPowerSource(powerId, sourceId, sourceType, intAmount);

        if (isPlayerTarget)
        {
            ContributionMap.Instance.RecordPlayerBuffSource(powerId, sourceId, sourceType, intAmount);
        }
        else
        {
            ContributionMap.Instance.RecordDebuffSource(creatureHash, powerId, sourceId, sourceType);
            // H1: Record FIFO debuff layer for duration-based debuffs (Vulnerable, Weak)
            int duration = (int)amount;
            if (duration > 0)
                ContributionMap.Instance.RecordDebuffLayer(creatureHash, powerId, sourceId, sourceType, duration);
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

    // Pending power/relic draw attribution: set by async power/relic hooks
    // (like DarkEmbrace) whose Harmony postfix fires before the draw completes.
    // The prefix records the intended source here; OnCardDrawn checks it first.
    private string? _pendingDrawSourceId;
    private string? _pendingDrawSourceType;

    public void SetPendingDrawSource(string sourceId, string sourceType)
    {
        _pendingDrawSourceId = sourceId;
        _pendingDrawSourceType = sourceType;
    }

    public void ClearPendingDrawSource()
    {
        _pendingDrawSourceId = null;
        _pendingDrawSourceType = null;
    }

    public void OnCardDrawn(bool fromHandDraw)
    {
        if (fromHandDraw) return; // Normal turn draw, not attributed

        // Check pending power/relic draw source first (for async hooks like DarkEmbrace).
        // Consumes the pending source after use.
        if (_pendingDrawSourceId != null)
        {
            GetOrCreate(_pendingDrawSourceId, _pendingDrawSourceType ?? "card").CardsDrawn++;
            _pendingDrawSourceId = null;
            _pendingDrawSourceType = null;
            return;
        }

        var (sourceId, sourceType) = ResolveSource(null);
        if (sourceId != null)
            GetOrCreate(sourceId, sourceType).CardsDrawn++;
    }

    // ── Energy Gained ───────────────────────────────────────

    public void OnEnergyGained(int amount)
    {
        if (amount <= 0) return;

        var (sourceId, sourceType) = ResolveSource(null);
        if (sourceId != null)
            GetOrCreate(sourceId, sourceType).EnergyGained += amount;
    }

    /// <summary>Called when player gains Stars (GainStars hook).</summary>
    public void OnStarsGained(int amount)
    {
        if (amount <= 0) return;
        var (sourceId, sourceType) = ResolveSource(null);
        if (sourceId != null)
            GetOrCreate(sourceId, sourceType).StarsContribution += amount;
    }

    // ── Cost Savings (NEW-1/NEW-2: centralized play-time attribution) ──

    /// <summary>
    /// Called from CardPlayStarted to attribute energy/star savings at play time.
    /// Compares card's canonical cost vs actual cost spent.
    /// </summary>
    /// <summary>
    /// Source IDs whose cost-savings attribution is allowed to be NEGATIVE (PRD-04 §4.2).
    /// SneckoEye / FakeSneckoEye / SneckoOil randomize card costs — the change can
    /// raise the cost, in which case the energy "savings" is a negative contribution.
    /// </summary>
    private static readonly HashSet<string> _negativeSavingsAllowedSources = new()
    {
        "SNECKO_EYE",
        "FAKE_SNECKO_EYE",
        "SNECKO_OIL",
    };

    public void AttributeCostSavings(int cardHash, int canonicalEnergy, int energySpent,
        int canonicalStars, int starsSpent, bool costsX)
    {
        if (costsX) return; // X-cost cards excluded

        int energySaved = canonicalEnergy - energySpent;
        int starsSaved = canonicalStars - starsSpent;

        // Exception rule: generated-and-freed cards only tracked via sub-bar
        if (ContributionMap.Instance.IsCardGeneratedAndFree(cardHash))
        {
            ContributionMap.Instance.ClearCostReductionSourceTag(cardHash);
            return;
        }

        // Look up source tag
        var tag = ContributionMap.Instance.GetCostReductionSourceTag(cardHash);
        if (tag == null) return;

        var (sourceId, sourceType) = tag.Value;
        bool allowNegative = _negativeSavingsAllowedSources.Contains(sourceId);

        // Default: only credit positive savings.
        // Snecko-family sources may also credit negative (cost increase) per PRD §4.2.
        if (allowNegative)
        {
            if (energySaved != 0) GetOrCreate(sourceId, sourceType).EnergyGained += energySaved;
            if (starsSaved != 0) GetOrCreate(sourceId, sourceType).StarsContribution += starsSaved;
        }
        else
        {
            if (energySaved <= 0 && starsSaved <= 0)
            {
                ContributionMap.Instance.ClearCostReductionSourceTag(cardHash);
                return;
            }
            if (energySaved > 0) GetOrCreate(sourceId, sourceType).EnergyGained += energySaved;
            if (starsSaved > 0) GetOrCreate(sourceId, sourceType).StarsContribution += starsSaved;
        }

        ContributionMap.Instance.ClearCostReductionSourceTag(cardHash);
    }

    // ── Extra Hand Draw ──────────────────────────────────────

    /// <summary>
    /// Called after hand draw completes to attribute extra draws from powers
    /// (PaleBlueDot, Tyranny) that modify hand draw count.
    /// </summary>
    public void FlushPendingHandDrawBonus()
    {
        var bonuses = ContributionMap.Instance.ConsumePendingHandDrawBonus();
        foreach (var (sourceId, sourceType, count) in bonuses)
        {
            GetOrCreate(sourceId, sourceType).CardsDrawn += count;
        }
    }

    // ── SeekingEdge Context ──────────────────────────────────

    public void SetSeekingEdgeContext(int primaryTargetHash)
        => ContributionMap.Instance.SetSeekingEdgeContext(primaryTargetHash);

    public void ClearSeekingEdgeContext()
        => ContributionMap.Instance.ClearSeekingEdgeContext();

    // ── Osty Damage Attribution ───────────────────────────────
    // When Osty deals damage to enemies, attribute to the specific attack card
    // but set OriginSourceId = "OSTY" for sub-bar display under OSTY parent.

    private const string OstyParentId = "OSTY";

    /// <summary>
    /// Called when Osty deals damage to an enemy. The cardSourceId is the attack card
    /// (Unleash, Poke, etc.). Damage is attributed to the card with OSTY as parent.
    /// </summary>
    public void OnOstyDamageDealt(int totalDamage, string cardSourceId)
    {
        if (totalDamage <= 0 || string.IsNullOrEmpty(cardSourceId)) return;

        // Ensure OSTY parent entry exists
        GetOrCreate(OstyParentId, "osty");

        // Attribute damage to the specific card
        var accum = GetOrCreate(cardSourceId, "card");
        accum.DirectDamage += totalDamage;
        accum.OriginSourceId = OstyParentId;

        // Also consume modifier context if present
        var modifiers = ContributionMap.Instance.LastDamageModifiers;
        if (modifiers.Count > 0)
        {
            foreach (var mod in modifiers)
            {
                if (mod.Amount > 0)
                    GetOrCreate(mod.SourceId, mod.SourceType).ModifierDamage += mod.Amount;
            }
            modifiers.Clear();
        }
    }

    // ── Osty Defense (HP absorption) ────────────────────────
    // When Osty absorbs damage for the player, attribute defense to summon sources.

    public void OnOstyAbsorbedDamage(int damage)
    {
        if (damage <= 0) return;

        // Ensure OSTY parent entry exists
        GetOrCreate(OstyParentId, "osty");

        // Consume from LIFO stack to find which summon sources contributed
        var consumed = ContributionMap.Instance.ConsumeOstyHp(damage);
        foreach (var (sourceId, sourceType, amount) in consumed)
        {
            var accum = GetOrCreate(sourceId, sourceType);
            accum.EffectiveBlock += amount;
            accum.OriginSourceId = OstyParentId;
        }

        // If stack was insufficient (shouldn't normally happen), attribute remainder to OSTY itself
        int totalConsumed = 0;
        foreach (var (_, _, amount) in consumed) totalConsumed += amount;
        int remainder = damage - totalConsumed;
        if (remainder > 0)
        {
            GetOrCreate(OstyParentId, "osty").EffectiveBlock += remainder;
        }
    }

    // ── Osty Summon Tracking ────────────────────────────────

    public void OnOstySummoned(string sourceId, string sourceType, int hpAmount)
    {
        ContributionMap.Instance.PushOstyHp(sourceId, sourceType, hpAmount);
    }

    /// <summary>
    /// Called when Osty is killed (BoneShards sacrifice, Sacrifice card, etc.).
    /// Subtracts remaining Osty HP from the killer card's defense contribution
    /// (killing Osty destroys its HP pool, removing that defensive resource).
    /// Then clears the LIFO stack.
    /// </summary>
    public void OnOstyKilled()
    {
        int remainingHp = ContributionMap.Instance.GetRemainingOstyHpTotal();

        if (remainingHp > 0 && _activeCardId != null)
        {
            // H3: Defer negative defense until next enemy attack hits player
            _pendingOstyDeathDefense = (_activeCardId, "card", remainingHp);
        }

        ContributionMap.Instance.ClearOstyHpStack();
    }

    // ── Doom Damage Attribution ─────────────────────────────

    /// <summary>
    /// Called before Doom kills enemies. Records each target's current HP.
    /// </summary>
    public void OnDoomTargetCapture(int creatureHash, int currentHp)
    {
        ContributionMap.Instance.RecordDoomTargetHp(creatureHash, currentHp);
    }

    /// <summary>
    /// Called after Doom kills. Attributes total HP as damage to the Doom power source.
    /// </summary>
    public void OnDoomKillsCompleted()
    {
        var targets = ContributionMap.Instance.ConsumeDoomTargetHp();
        if (targets.Count == 0) return;

        int totalDamage = 0;
        foreach (var (_, hp) in targets) totalDamage += hp;

        if (totalDamage <= 0) return;

        // Find what card/relic applied the Doom power
        var source = ContributionMap.Instance.GetPowerSource("DOOM_POWER");
        if (source != null)
        {
            GetOrCreate(source.SourceId, source.SourceType).AttributedDamage += totalDamage;
        }
        else
        {
            // Fallback: attribute to generic "DOOM" entry
            GetOrCreate("DOOM", "card").AttributedDamage += totalDamage;
        }
    }

    // ── Card Origin (for sub-bar display) ───────────────────

    public void RecordCardOrigin(int cardHash, string originId, string originType)
    {
        ContributionMap.Instance.RecordCardOrigin(cardHash, originId, originType);
    }

    /// <summary>
    /// If a card has a known origin (generated/transformed by another card),
    /// tag its ContributionAccum so the UI can display it as a sub-bar.
    /// </summary>
    public void TagCardOrigin(string cardId, int cardHash)
    {
        var origin = ContributionMap.Instance.GetCardOrigin(cardHash);
        if (origin != null && _currentCombat.TryGetValue(cardId, out var accum))
        {
            accum.OriginSourceId = origin.Value.originId;
        }
    }

    // ── Forge Tracking ────────────────────────────────────────

    /// <summary>
    /// Called from ForgeCmd.Forge postfix. Records source and amount of each forge event.
    /// </summary>
    public void OnForge(string sourceId, string sourceType, int amount)
    {
        if (amount <= 0) return;
        _forgeLog.Add((sourceId, sourceType, amount));
    }

    /// <summary>
    /// Called from SovereignBlade.OnPlay prefix. Writes accumulated forge sources
    /// as sub-bar entries under SOVEREIGN_BLADE so the chart shows breakdown.
    /// Each forge source gets a ContributionAccum with OriginSourceId = "SOVEREIGN_BLADE"
    /// and DirectDamage = total forged amount from that source.
    /// </summary>
    public void FlushForgeSubBars()
    {
        if (_forgeLog.Count == 0) return;

        // Aggregate by sourceId
        var aggregated = new Dictionary<string, (string sourceType, int count, int totalAmount)>();
        foreach (var (srcId, srcType, amt) in _forgeLog)
        {
            if (aggregated.TryGetValue(srcId, out var existing))
                aggregated[srcId] = (srcType, existing.count + 1, existing.totalAmount + amt);
            else
                aggregated[srcId] = (srcType, 1, amt);
        }

        // Write sub-bar entries: "FORGE:SOURCE_ID" with OriginSourceId = "SOVEREIGN_BLADE"
        foreach (var (srcId, (srcType, count, totalAmt)) in aggregated)
        {
            string key = $"FORGE:{srcId}";
            var accum = GetOrCreate(key, srcType);
            accum.OriginSourceId = "SOVEREIGN_BLADE";
            accum.DirectDamage += totalAmt;
            accum.TimesPlayed += count;
        }

        // Also write base damage entry (SovereignBlade starts at 10)
        const int baseDamage = 10;
        var baseAccum = GetOrCreate("FORGE:BASE", "card");
        baseAccum.OriginSourceId = "SOVEREIGN_BLADE";
        baseAccum.DirectDamage = baseDamage; // fixed, not additive
        baseAccum.TimesPlayed = 1;
    }

    // ── Healing ──────────────────────────────────────────────

    /// <summary>
    /// Called when player is healed. Attributes to active card/relic/potion/power source.
    /// Healing can occur both during combat (e.g. Reaper card) and after combat ends
    /// (e.g. BurningBlood.AfterCombatVictory fires AFTER OnCombatEnded).
    /// When combat data has been flushed, writes directly to RunContributionAggregator.
    /// fallbackId/fallbackType: used when no active context (rest site, event, merchant).
    /// </summary>
    public void OnHealingReceived(int amount, string? fallbackId = null, string? fallbackType = null)
    {
        if (amount <= 0) return;
        var (sourceId, sourceType) = ResolveSource(null);

        // Use fallback when no active source context (UNTRACKED means ResolveSource found nothing)
        if (sourceId == "UNTRACKED" && fallbackId != null)
        {
            sourceId = fallbackId;
            sourceType = fallbackType ?? "other";
        }

        if (_currentCombat.Count > 0)
        {
            // During combat — write to per-combat data
            GetOrCreate(sourceId, sourceType).HpHealed += amount;
        }
        else
        {
            // Outside combat (e.g. AfterCombatVictory, rest site, events)
            // Write directly to run-level aggregator
            RunContributionAggregator.Instance.AddHealing(sourceId, sourceType, amount);
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
