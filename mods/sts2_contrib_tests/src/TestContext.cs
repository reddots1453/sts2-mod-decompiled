using CommunityStats.Collection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ContribTests;

/// <summary>
/// Provides game state access, command wrappers, and snapshot-based
/// delta verification for automated contribution tests.
/// </summary>
public sealed class TestContext
{
    public CombatState CombatState { get; }
    public Player Player { get; }
    public Creature PlayerCreature { get; }

    // Snapshot of CombatTracker data taken before each test's actions
    private Dictionary<string, SnapshotEntry>? _snapshot;

    public TestContext(CombatState combatState, Player player)
    {
        CombatState = combatState;
        Player = player;
        PlayerCreature = player.Creature;
    }

    // ── Snapshot / Delta ────────────────────────────────────

    /// <summary>
    /// Take a deep-copy snapshot of current CombatTracker data.
    /// Call this before executing test actions.
    /// </summary>
    public void TakeSnapshot()
    {
        var liveData = CombatTracker.Instance.GetCurrentCombatData();
        _snapshot = new Dictionary<string, SnapshotEntry>();
        foreach (var (key, accum) in liveData)
        {
            _snapshot[key] = new SnapshotEntry(accum);
        }
    }

    /// <summary>
    /// Compute the delta between current CombatTracker data and the snapshot.
    /// Returns field-level differences for each source that changed.
    /// </summary>
    public Dictionary<string, DeltaEntry> GetDelta()
    {
        var liveData = CombatTracker.Instance.GetCurrentCombatData();
        var delta = new Dictionary<string, DeltaEntry>();

        foreach (var (key, accum) in liveData)
        {
            if (_snapshot != null && _snapshot.TryGetValue(key, out var snap))
            {
                var d = new DeltaEntry
                {
                    SourceId = accum.SourceId,
                    SourceType = accum.SourceType,
                    TimesPlayed = accum.TimesPlayed - snap.TimesPlayed,
                    DirectDamage = accum.DirectDamage - snap.DirectDamage,
                    AttributedDamage = accum.AttributedDamage - snap.AttributedDamage,
                    ModifierDamage = accum.ModifierDamage - snap.ModifierDamage,
                    UpgradeDamage = accum.UpgradeDamage - snap.UpgradeDamage,
                    EffectiveBlock = accum.EffectiveBlock - snap.EffectiveBlock,
                    ModifierBlock = accum.ModifierBlock - snap.ModifierBlock,
                    MitigatedByDebuff = accum.MitigatedByDebuff - snap.MitigatedByDebuff,
                    MitigatedByBuff = accum.MitigatedByBuff - snap.MitigatedByBuff,
                    MitigatedByStrReduction = accum.MitigatedByStrReduction - snap.MitigatedByStrReduction,
                    SelfDamage = accum.SelfDamage - snap.SelfDamage,
                    CardsDrawn = accum.CardsDrawn - snap.CardsDrawn,
                    EnergyGained = accum.EnergyGained - snap.EnergyGained,
                    HpHealed = accum.HpHealed - snap.HpHealed,
                    StarsContribution = accum.StarsContribution - snap.StarsContribution,
                    UpgradeBlock = accum.UpgradeBlock - snap.UpgradeBlock,
                    OriginSourceId = accum.OriginSourceId
                };
                // Only include if something changed
                if (d.HasAnyChange())
                    delta[key] = d;
            }
            else
            {
                // New entry since snapshot — entire value is the delta
                delta[key] = new DeltaEntry
                {
                    SourceId = accum.SourceId,
                    SourceType = accum.SourceType,
                    TimesPlayed = accum.TimesPlayed,
                    DirectDamage = accum.DirectDamage,
                    AttributedDamage = accum.AttributedDamage,
                    ModifierDamage = accum.ModifierDamage,
                    UpgradeDamage = accum.UpgradeDamage,
                    EffectiveBlock = accum.EffectiveBlock,
                    ModifierBlock = accum.ModifierBlock,
                    MitigatedByDebuff = accum.MitigatedByDebuff,
                    MitigatedByBuff = accum.MitigatedByBuff,
                    MitigatedByStrReduction = accum.MitigatedByStrReduction,
                    SelfDamage = accum.SelfDamage,
                    CardsDrawn = accum.CardsDrawn,
                    EnergyGained = accum.EnergyGained,
                    HpHealed = accum.HpHealed,
                    StarsContribution = accum.StarsContribution,
                    UpgradeBlock = accum.UpgradeBlock,
                    OriginSourceId = accum.OriginSourceId
                };
            }
        }

        return delta;
    }

    // ── Game Command Wrappers ───────────────────────────────

    /// <summary>Create a card instance in the current combat.</summary>
    public T CreateCard<T>() where T : CardModel
        => CombatState.CreateCard<T>(Player);

    /// <summary>Play a card against an optional target.</summary>
    public async Task PlayCard(CardModel card, Creature? target = null)
    {
        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, target);
        await Task.Delay(100); // Allow patches to fire
    }

    /// <summary>Apply a power to a creature.</summary>
    public async Task ApplyPower<T>(Creature target, int amount,
        Creature? applier = null, CardModel? cardSource = null) where T : PowerModel
    {
        await PowerCmd.Apply<T>(target, amount, applier ?? PlayerCreature, cardSource);
        await Task.Delay(50);
    }

    /// <summary>
    /// Simulate damage from a dealer to a target.
    /// This triggers the full DamageReceived hook chain (Weak, Block, etc.).
    /// </summary>
    public async Task SimulateDamage(Creature target, int amount, Creature dealer,
        CardModel? cardSource = null)
    {
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            target,
            amount,
            ValueProp.Move, // Standard powered attack — triggers Weak/Vulnerable modifiers
            dealer,
            cardSource);
        await Task.Delay(100);
    }

    /// <summary>Gain block on a creature.</summary>
    public async Task GainBlock(Creature creature, int amount, CardPlay? cardPlay = null)
    {
        await CreatureCmd.GainBlock(creature, amount, ValueProp.Move, cardPlay, fast: true);
        await Task.Delay(50);
    }

    /// <summary>Set player energy to a specific value.</summary>
    public async Task SetEnergy(int amount)
    {
        await PlayerCmd.SetEnergy(amount, Player);
    }

    /// <summary>End the player's turn.</summary>
    public void EndTurn()
    {
        PlayerCmd.EndTurn(Player, canBackOut: false);
    }

    /// <summary>Get the first hittable enemy.</summary>
    public Creature GetFirstEnemy()
        => CombatState.HittableEnemies.First();

    /// <summary>Get all hittable enemies.</summary>
    public IReadOnlyList<Creature> GetAllEnemies()
        => CombatState.HittableEnemies.ToList();

    /// <summary>Check if combat is still active.</summary>
    public bool IsCombatActive => CombatManager.Instance.IsInProgress;

    // ── Assertion Helpers ───────────────────────────────────

    public void AssertEquals(TestResult result, string field, int expected, int actual)
    {
        if (expected == actual)
            result.Pass(field, expected.ToString());
        else
            result.Fail(field, expected.ToString(), actual.ToString());
    }

    public void AssertGreaterThan(TestResult result, string field, int threshold, int actual)
    {
        if (actual > threshold)
            result.Pass(field, actual.ToString());
        else
            result.Fail(field, $"> {threshold}", actual.ToString());
    }

    public void AssertRange(TestResult result, string field, int min, int max, int actual)
    {
        if (actual >= min && actual <= max)
            result.Pass(field, actual.ToString());
        else
            result.Fail(field, $"[{min}, {max}]", actual.ToString());
    }

    // ── Internal Types ──────────────────────────────────────

    private record SnapshotEntry(
        int TimesPlayed,
        int DirectDamage,
        int AttributedDamage,
        int ModifierDamage,
        int UpgradeDamage,
        int EffectiveBlock,
        int ModifierBlock,
        int MitigatedByDebuff,
        int MitigatedByBuff,
        int MitigatedByStrReduction,
        int SelfDamage,
        int CardsDrawn,
        int EnergyGained,
        int HpHealed,
        int StarsContribution,
        int UpgradeBlock)
    {
        public SnapshotEntry(ContributionAccum a) : this(
            a.TimesPlayed, a.DirectDamage, a.AttributedDamage,
            a.ModifierDamage, a.UpgradeDamage,
            a.EffectiveBlock, a.ModifierBlock,
            a.MitigatedByDebuff, a.MitigatedByBuff, a.MitigatedByStrReduction,
            a.SelfDamage, a.CardsDrawn, a.EnergyGained,
            a.HpHealed, a.StarsContribution, a.UpgradeBlock) { }
    }
}

/// <summary>Field-level delta for a single contribution source.</summary>
public class DeltaEntry
{
    public string SourceId { get; set; } = "";
    public string SourceType { get; set; } = "";
    public int TimesPlayed { get; set; }
    public int DirectDamage { get; set; }
    public int AttributedDamage { get; set; }
    public int ModifierDamage { get; set; }
    public int UpgradeDamage { get; set; }
    public int EffectiveBlock { get; set; }
    public int ModifierBlock { get; set; }
    public int MitigatedByDebuff { get; set; }
    public int MitigatedByBuff { get; set; }
    public int MitigatedByStrReduction { get; set; }
    public int SelfDamage { get; set; }
    public int CardsDrawn { get; set; }
    public int EnergyGained { get; set; }
    public int HpHealed { get; set; }
    public int StarsContribution { get; set; }
    public int UpgradeBlock { get; set; }
    public string? OriginSourceId { get; set; }

    public int TotalDamage => DirectDamage + AttributedDamage + ModifierDamage + UpgradeDamage;
    public int TotalDefense => EffectiveBlock + ModifierBlock + MitigatedByDebuff
        + MitigatedByBuff + MitigatedByStrReduction + UpgradeBlock - SelfDamage;

    public bool HasAnyChange() =>
        TimesPlayed != 0 || DirectDamage != 0 || AttributedDamage != 0 ||
        ModifierDamage != 0 || UpgradeDamage != 0 || EffectiveBlock != 0 ||
        ModifierBlock != 0 || MitigatedByDebuff != 0 || MitigatedByBuff != 0 ||
        MitigatedByStrReduction != 0 || SelfDamage != 0 || CardsDrawn != 0 ||
        EnergyGained != 0 || HpHealed != 0 || StarsContribution != 0 || UpgradeBlock != 0;
}
