using CommunityStats.Collection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
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

    /// <summary>
    /// Create a card and add it to the player's hand (with skipVisuals to avoid UI crash).
    /// Use this instead of CreateCard when you intend to PlayCard afterwards.
    /// </summary>
    public async Task<T> CreateCardInHand<T>() where T : CardModel
    {
        var card = CombatState.CreateCard<T>(Player);
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        await Task.Delay(100);
        return card;
    }

    /// <summary>
    /// Remove all player block AND clear the FIFO block pool so fresh block
    /// from the next card/relic is the only entry in the queue.
    /// </summary>
    public async Task ClearBlock()
    {
        await CreatureCmd.LoseBlock(PlayerCreature, PlayerCreature.Block);
        ContributionMap.Instance.ClearBlockPool();
    }

    /// <summary>
    /// Ensure the draw pile has at least `count` cards.
    /// If not, creates Strike cards in draw pile so card-draw effects work.
    /// </summary>
    public async Task EnsureDrawPile(int count = 5)
    {
        var drawPile = PileType.Draw.GetPile(Player);
        int needed = count - drawPile.Cards.Count;
        for (int i = 0; i < needed; i++)
        {
            var card = CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(Player);
            await CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);
        }
    }

    /// <summary>
    /// Discard all cards from hand to prevent hand overflow (10-card limit).
    /// Must be called between tests to ensure draw effects have room.
    /// </summary>
    public async Task ClearHand()
    {
        var hand = PileType.Hand.GetPile(Player);
        var cards = hand.Cards.ToList();
        foreach (var card in cards)
        {
            await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
        }
    }

    /// <summary>Clear all orbs from the player's orb queue to prevent accumulation.</summary>
    public void ClearOrbs()
    {
        Player.PlayerCombatState?.OrbQueue?.Clear();
    }

    /// <summary>Play a card against an optional target.</summary>
    public async Task PlayCard(CardModel card, Creature? target = null)
    {
        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, target,
            skipCardPileVisuals: true);
        await Task.Delay(300); // Allow patches to fire + Godot scene tree to process
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

    // ── Relic Helpers ───────────────────────────────────────
    // For relic-source contribution testing. Pattern:
    //   var relic = await ctx.ObtainRelic<Akabeko>();
    //   await ctx.TriggerRelicHook(() => relic.AfterSideTurnStart(...));
    //   ctx.TakeSnapshot();
    //   ... play card ...
    //   await ctx.RemoveRelic(relic);

    /// <summary>Obtain a relic for the player. Triggers AfterObtained automatically.</summary>
    public async Task<T> ObtainRelic<T>() where T : RelicModel
    {
        var relic = (T)await RelicCmd.Obtain(ModelDb.Relic<T>().ToMutable(), Player);
        await Task.Delay(80);
        return relic;
    }

    /// <summary>Remove a relic from the player.</summary>
    public async Task RemoveRelic(RelicModel relic)
    {
        if (relic == null) return;
        await RelicCmd.Remove(relic);
        await Task.Delay(50);
    }

    /// <summary>
    /// Reset all enemies to full HP. Call before tests that check exact damage values
    /// to avoid overkill-cap from accumulated damage across tests.
    /// </summary>
    public async Task ResetEnemyHp()
    {
        foreach (var enemy in GetAllEnemies())
            await CreatureCmd.Heal(enemy, 9999m, playAnim: false);
    }

    /// <summary>
    /// Manually invoke a relic hook (e.g. BeforeCombatStart, AfterRoomEntered, AfterSideTurnStart)
    /// since RelicCmd.Obtain only fires AfterObtained. Wrap the call in a lambda.
    /// </summary>
    public async Task TriggerRelicHook(Func<Task> hookInvocation)
    {
        await hookInvocation();
        await Task.Delay(80);
    }

    /// <summary>Get the current combat room (for AfterRoomEntered triggers).</summary>
    public AbstractRoom? GetCurrentRoom()
        => Player.RunState.CurrentRoom;

    // ── Potion Helpers ──────────────────────────────────────
    // Round 10 batch 1: closes the §19.4 zero-coverage gap. Pattern:
    //   ctx.TakeSnapshot();
    //   await ctx.UsePotion<FirePotion>(target: enemy);
    //   var d = ctx.GetDelta();
    //   ctx.AssertEquals(result, "FIRE_POTION.DirectDamage", 20, d["FIRE_POTION"].DirectDamage);

    /// <summary>
    /// Procure a potion into the player's first open slot, then immediately
    /// consume it via OnUseWrapper. Going through OnUseWrapper is required so
    /// PotionContextPatch.BeforePotionUse fires SetActivePotion — without that,
    /// none of the contribution events get attributed to the potion source.
    /// </summary>
    public async Task<T> UsePotion<T>(Creature? target = null) where T : PotionModel
    {
        // Ensure a free slot. CombatOnly potions auto-remove when consumed,
        // but defensive: clear slot 0 if something stale is parked there.
        if (!Player.HasOpenPotionSlots && Player.PotionSlots.Count > 0)
        {
            var stale = Player.PotionSlots[0];
            if (stale != null) await PotionCmd.Discard(stale);
        }

        var procureResult = await PotionCmd.TryToProcure<T>(Player);
        if (!procureResult.success || procureResult.potion is not T potion)
        {
            throw new InvalidOperationException(
                $"UsePotion<{typeof(T).Name}> failed to procure: {procureResult.failureReason}");
        }
        await Task.Delay(80);

        // Default targeting: self for AnyPlayer/AllPlayers, first enemy otherwise.
        var resolvedTarget = target ?? ResolveDefaultPotionTarget(potion);

        var choiceCtx = new BlockingPlayerChoiceContext();
        await potion.OnUseWrapper(choiceCtx, resolvedTarget);
        await Task.Delay(300);
        return potion;
    }

    private Creature? ResolveDefaultPotionTarget(PotionModel potion)
    {
        var tt = potion.TargetType;
        if (tt == TargetType.AnyPlayer || tt == TargetType.Self
            || tt == TargetType.AnyAlly || tt == TargetType.AllAllies
            || tt == TargetType.None)
            return PlayerCreature;
        // AnyEnemy / AllEnemies / RandomEnemy — pick first hittable enemy
        return CombatState.HittableEnemies.FirstOrDefault();
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

    /// <summary>
    /// End turn and wait for enemy turn to fully resolve.
    /// Polls until it's the player's turn again (or timeout).
    /// </summary>
    public async Task EndTurnAndWaitForPlayerTurn(int timeoutMs = 15000)
    {
        bool isReady = CombatManager.Instance.IsPlayerReadyToEndTurn(Player);
        var currentAction = MegaCrit.Sts2.Core.Runs.RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
        Godot.GD.Print($"[ContribTest] EndTurn: before call, CurrentSide={CombatState.CurrentSide}, isReady={isReady}, runningAction={currentAction?.GetType().Name ?? "null"}");

        // PHASE 1: Call EndTurn and wait for side to flip to Enemy
        PlayerCmd.EndTurn(Player, canBackOut: false);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool sideFlipped = false;
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(200);
            if (!CombatManager.Instance.IsInProgress) break;
            if (CombatState.CurrentSide != MegaCrit.Sts2.Core.Combat.CombatSide.Player)
            {
                sideFlipped = true;
                break;
            }
        }
        Godot.GD.Print($"[ContribTest] EndTurn: sideFlipped={sideFlipped} ({sw.ElapsedMilliseconds}ms)");
        if (!sideFlipped)
        {
            bool readyNow = CombatManager.Instance.IsPlayerReadyToEndTurn(Player);
            var action2 = MegaCrit.Sts2.Core.Runs.RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
            bool inProgress = CombatManager.Instance.IsInProgress;
            Godot.GD.PrintErr($"[ContribTest] EndTurn: FAILED! ready={readyNow}, action={action2?.GetType().Name ?? "null"}, inProgress={inProgress}, side={CombatState.CurrentSide}");
            return;
        }

        // PHASE 2: Wait for side to flip back to Player
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(200);
            if (!CombatManager.Instance.IsInProgress) break;
            if (CombatState.CurrentSide == MegaCrit.Sts2.Core.Combat.CombatSide.Player) break;
        }

        // PHASE 3: Wait for _playersReadyToEndTurn to be cleared by BeforeSideTurnStart.
        // CurrentSide flips to Player BEFORE the clear happens, so we must explicitly wait.
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (!CombatManager.Instance.IsPlayerReadyToEndTurn(Player)) break;
            await Task.Delay(100);
        }
        await Task.Delay(3000); // extra buffer for AfterPlayerTurnStart hooks (CrimsonMantle etc.)

        // Heal player to prevent death from accumulated enemy attacks
        if (CombatManager.Instance.IsInProgress)
            await CreatureCmd.Heal(PlayerCreature, 9999m, playAnim: false);

        Godot.GD.Print($"[ContribTest] EndTurn: done ({sw.ElapsedMilliseconds}ms), CurrentSide={CombatState.CurrentSide}, ready={CombatManager.Instance.IsPlayerReadyToEndTurn(Player)}");
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
