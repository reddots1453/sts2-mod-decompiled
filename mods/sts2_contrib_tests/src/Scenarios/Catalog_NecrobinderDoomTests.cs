using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog — Necrobinder Doom-family card contribution tests.
///
/// Round 14 R2 rewrite: all pseudo-PASS tests converted to real Doom-kill
/// attribution checks per AUDIT_PSEUDO_PASS_R14.md.
///
/// Pattern: drop enemy HP ≤ Doom stacks, apply Doom via the card under test,
/// EndTurn to trigger OnDoomKillsCompleted, then assert AttributedDamage on
/// the card's ContributionAccum.
///
/// WARNING: these tests kill the first enemy. TestRunner.BuildScenarioList
/// must schedule Catalog_NecrobinderDoom AFTER all other categories.
/// </summary>
public static class Catalog_NecrobinderDoomTests
{
    private const string Cat = "Catalog_NecrobinderDoom";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new NB_Doom_BorrowedTime(),
        new NB_Doom_Deathbringer(),
        new NB_Doom_EndOfDays(),
        new NB_Doom_NegativePulse(),
        new NB_Doom_NoEscape(),
        new NB_Doom_Scourge(),
        new NB_Doom_ReaperForm(),
        new NB_Doom_Countdown(),
        new NB_Doom_TimesUp(),
    };

    // Helper: reduce enemy HP to `targetHp` using CreatureCmd.SetCurrentHp.
    private static async Task LowerEnemyHp(Creature enemy, int targetHp)
    {
        if (enemy.CurrentHp > targetHp)
            await CreatureCmd.SetCurrentHp(enemy, targetHp);
    }

    // ── BorrowedTime: apply 3 Doom to SELF + gain 1 energy ──
    // B-class補足: EnergyGained stays the primary assertion; self-damage and
    // DoomPower stacking aren't tracked as contribution fields.
    private class NB_Doom_BorrowedTime : ITestScenario
    {
        public string Id => "NB-DOOM-BorrowedTime";
        public string Name => "BorrowedTime → EnergyGained=1 + self DoomPower(3)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DoomPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(3);
                var card = await ctx.CreateCardInHand<BorrowedTime>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);

                var delta = ctx.GetDelta();
                delta.TryGetValue("BORROWED_TIME", out var d);
                ctx.AssertEquals(result, "BORROWED_TIME.EnergyGained", 1, d?.EnergyGained ?? 0);

                // supplementary check: 3 Doom now stacked on the player
                var selfDoom = ctx.PlayerCreature.GetPower<DoomPower>();
                ctx.AssertEquals(result, "Player.DoomPower.Amount", 3, selfDoom?.Amount ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── Deathbringer: 21 Doom + 1 Weak AoE → kill low-HP enemy ──
    private class NB_Doom_Deathbringer : ITestScenario
    {
        public string Id => "NB-DOOM-Deathbringer";
        public string Name => "Deathbringer → Doom-kill AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await PowerCmd.Remove<WeakPower>(enemy);
                await LowerEnemyHp(enemy, 5);

                var card = await ctx.CreateCardInHand<Deathbringer>();
                await ctx.PlayCard(card, enemy);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEATHBRINGER", out var d);
                ctx.AssertGreaterThan(result, "DEATHBRINGER.AttributedDamage", 0, d?.AttributedDamage ?? 0);
                // non-deterministic: exact kill amount depends on enemy HP at snapshot time
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await PowerCmd.Remove<WeakPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── EndOfDays: 29 Doom AoE + kill if Doom ≥ HP ──
    private class NB_Doom_EndOfDays : ITestScenario
    {
        public string Id => "NB-DOOM-EndOfDays";
        public string Name => "EndOfDays → Doom-kill AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
                await LowerEnemyHp(enemy, 5);

                var card = await ctx.CreateCardInHand<EndOfDays>();
                await ctx.PlayCard(card);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("END_OF_DAYS", out var d);
                ctx.AssertGreaterThan(result, "END_OF_DAYS.AttributedDamage", 0, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── NegativePulse: 5 Block + apply 7 Doom to all (keeps existing Block assertion) ──
    private class NB_Doom_NegativePulse : ITestScenario
    {
        public string Id => "NB-DOOM-NegativePulse";
        public string Name => "NegativePulse → EffectiveBlock=5 to NEGATIVE_PULSE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                var card = await ctx.CreateCardInHand<NegativePulse>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("NEGATIVE_PULSE", out var d);
                ctx.AssertEquals(result, "NEGATIVE_PULSE.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            }
            finally { }
            return result;
        }
    }

    // ── NoEscape: 10 Doom single-target → kill ──
    private class NB_Doom_NoEscape : ITestScenario
    {
        public string Id => "NB-DOOM-NoEscape";
        public string Name => "NoEscape → Doom-kill AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await LowerEnemyHp(enemy, 5);

                var card = await ctx.CreateCardInHand<NoEscape>();
                await ctx.PlayCard(card, enemy);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("NO_ESCAPE", out var d);
                ctx.AssertGreaterThan(result, "NO_ESCAPE.AttributedDamage", 0, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── Scourge: 13 Doom + draw 1. Retain CardsDrawn assertion + add Doom-kill. ──
    private class NB_Doom_Scourge : ITestScenario
    {
        public string Id => "NB-DOOM-Scourge";
        public string Name => "Scourge → CardsDrawn=1 + Doom-kill AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.EnsureDrawPile(5);
                await LowerEnemyHp(enemy, 5);

                var card = await ctx.CreateCardInHand<Scourge>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card, enemy);

                // First delta captures CardsDrawn from the card-play phase
                var delta1 = ctx.GetDelta();
                delta1.TryGetValue("SCOURGE", out var d1);
                ctx.AssertEquals(result, "SCOURGE.CardsDrawn", 1, d1?.CardsDrawn ?? 0);

                // EndTurn → Doom tick should kill the enemy → AttributedDamage
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta2 = ctx.GetDelta();
                delta2.TryGetValue("SCOURGE", out var d2);
                ctx.AssertGreaterThan(result, "SCOURGE.AttributedDamage", 0, d2?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── ReaperForm: Attack dmg → equal Doom → Doom tick kill ──
    private class NB_Doom_ReaperForm : ITestScenario
    {
        public string Id => "NB-DOOM-ReaperForm";
        public string Name => "ReaperForm → Strike → Doom-kill AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<ReaperFormPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.SetEnergy(999);

                var rf = await ctx.CreateCardInHand<ReaperForm>();
                await ctx.PlayCard(rf);

                if (ctx.PlayerCreature.GetPower<ReaperFormPower>() == null)
                {
                    result.Fail("ReaperFormPower", "applied", "null");
                    return result;
                }

                // Lower HP so that 6 Strike dmg + 6 Doom will finish the enemy
                await LowerEnemyHp(enemy, 8);

                var strike = await ctx.CreateCardInHand<StrikeNecrobinder>();
                await ctx.PlayCard(strike, enemy);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("REAPER_FORM", out var d);
                ctx.AssertGreaterThan(result, "REAPER_FORM.AttributedDamage", 0, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<ReaperFormPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── Countdown: Power — start-of-turn apply 6 Doom to random enemy ──
    // Play Countdown → EndTurn → next turn start fires CountdownPower →
    // applies 6 Doom → with enemy at low HP, Doom ticks kill → AttributedDamage.
    private class NB_Doom_Countdown : ITestScenario
    {
        public string Id => "NB-DOOM-Countdown";
        public string Name => "Countdown → turn-start Doom → AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<CountdownPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);

                var card = await ctx.CreateCardInHand<Countdown>();
                await ctx.PlayCard(card);

                await LowerEnemyHp(enemy, 3);

                ctx.TakeSnapshot();
                // Two EndTurn cycles: first fires CountdownPower (applies Doom),
                // second lets the Doom tick resolve and attribute the kill.
                await ctx.EndTurnAndWaitForPlayerTurn();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("COUNTDOWN", out var d);
                ctx.AssertGreaterThan(result, "COUNTDOWN.AttributedDamage", 0, d?.AttributedDamage ?? 0);
                // non-deterministic: Countdown targets random enemy; may miss the one we lowered
            }
            finally
            {
                await PowerCmd.Remove<CountdownPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── TimesUp: 2-cost attack, damage = enemy's Doom ──
    // Existing test is already compliant (asserts TIMES_UP.DirectDamage=15).
    private class NB_Doom_TimesUp : ITestScenario
    {
        public string Id => "NB-DOOM-TimesUp";
        public string Name => "TimesUp → DirectDamage=15 to TIMES_UP (Doom=15)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.ApplyPower<DoomPower>(enemy, 15);
                await ctx.ResetEnemyHp();

                var card = await ctx.CreateCardInHand<TimesUp>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("TIMES_UP", out var d);
                ctx.AssertEquals(result, "TIMES_UP.DirectDamage", 15, d?.DirectDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
            }
            return result;
        }
    }
}
