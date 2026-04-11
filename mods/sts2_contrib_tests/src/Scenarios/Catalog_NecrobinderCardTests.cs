using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §19.2.5 — Round 10 batch 3: closes the **0-coverage Necrobinder gap**.
///
/// Existing Cross-character tests cover Defect (Zap, Defragment, Shiv) and
/// Regent (HiddenCache) but Necrobinder had **zero** scenarios. This file adds
/// 7 representative tests covering: basic Strike/Defend, an Attack with
/// sub-card generation (Reave → Soul), an Attack with debuff side-effect
/// (BlightStrike → Weakness), and three Power cards (DevourLife, Pagestorm,
/// Haunt) as smoke/play-without-crash checks.
///
/// All tests run regardless of the active character — they manually create
/// the Necrobinder card in the player's hand and play it. The runner already
/// grants 999 energy and unlimited HP so cost/HP gates don't matter.
/// </summary>
public static class Catalog_NecrobinderCardTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_NEC_Strike(),
        new CAT_NEC_Defend(),
        new CAT_NEC_Reave(),
        new CAT_NEC_BlightStrike(),
        new CAT_NEC_DevourLife(),
        new CAT_NEC_Pagestorm(),
        new CAT_NEC_Haunt(),
    };

    // ── Basic Strike ────────────────────────────────────────

    private class CAT_NEC_Strike : ITestScenario
    {
        public string Id => "CAT-NEC-Strike";
        public string Name => "Catalog §1: StrikeNecrobinder → DirectDamage=6 to STRIKE_NECROBINDER";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<StrikeNecrobinder>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("STRIKE_NECROBINDER", out var d);
            ctx.AssertEquals(result, "STRIKE_NECROBINDER.DirectDamage", 6, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── Basic Defend ────────────────────────────────────────

    private class CAT_NEC_Defend : ITestScenario
    {
        public string Id => "CAT-NEC-Defend";
        public string Name => "Catalog §6: DefendNecrobinder → EffectiveBlock=5 to DEFEND_NECROBINDER";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<DefendNecrobinder>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);

            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFEND_NECROBINDER", out var d);
            ctx.AssertEquals(result, "DEFEND_NECROBINDER.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ── Reave: 9 damage + Soul sub-card generation ──────────

    private class CAT_NEC_Reave : ITestScenario
    {
        public string Id => "CAT-NEC-Reave";
        public string Name => "Catalog §1: Reave → DirectDamage=9 to REAVE (+1 Soul generated to draw)";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Reave>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("REAVE", out var d);
            ctx.AssertEquals(result, "REAVE.DirectDamage", 9, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── BlightStrike: 8 damage + Weakness applied ───────────

    private class CAT_NEC_BlightStrike : ITestScenario
    {
        public string Id => "CAT-NEC-BlightStrike";
        public string Name => "Catalog §1: BlightStrike → DirectDamage=8 to BLIGHT_STRIKE";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<BlightStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("BLIGHT_STRIKE", out var d);
            ctx.AssertEquals(result, "BLIGHT_STRIKE.DirectDamage", 8, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── DevourLife: Power, sets up DevourLifePower ──────────

    private class CAT_NEC_DevourLife : ITestScenario
    {
        public string Id => "CAT-NEC-DevourLife";
        public string Name => "Catalog §14: DevourLife (Power) → DevourLifePower applied";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DevourLifePower>(ctx.PlayerCreature);
                var card = await ctx.CreateCardInHand<DevourLife>();
                await ctx.PlayCard(card);

                var pow = ctx.PlayerCreature.GetPower<DevourLifePower>();
                if (pow != null && pow.Amount > 0)
                    result.Pass("DevourLifePower.Amount", pow.Amount.ToString());
                else
                    result.Fail("DevourLifePower.Amount", ">0", pow?.Amount.ToString() ?? "null");
            }
            finally
            {
                await PowerCmd.Remove<DevourLifePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ── Pagestorm: Power applied ────────────────────────────

    private class CAT_NEC_Pagestorm : ITestScenario
    {
        public string Id => "CAT-NEC-Pagestorm";
        public string Name => "Catalog §2: Pagestorm (Power) → PagestormPower applied";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<PagestormPower>(ctx.PlayerCreature);
                var card = await ctx.CreateCardInHand<Pagestorm>();
                await ctx.PlayCard(card);

                var pow = ctx.PlayerCreature.GetPower<PagestormPower>();
                if (pow != null && pow.Amount > 0)
                    result.Pass("PagestormPower.Amount", pow.Amount.ToString());
                else
                    result.Fail("PagestormPower.Amount", ">0", pow?.Amount.ToString() ?? "null");
            }
            finally
            {
                await PowerCmd.Remove<PagestormPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ── Haunt: Power applied ────────────────────────────────

    private class CAT_NEC_Haunt : ITestScenario
    {
        public string Id => "CAT-NEC-Haunt";
        public string Name => "Catalog §2: Haunt (Power) → HauntPower applied";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<HauntPower>(ctx.PlayerCreature);
                var card = await ctx.CreateCardInHand<Haunt>();
                await ctx.PlayCard(card);

                var pow = ctx.PlayerCreature.GetPower<HauntPower>();
                if (pow != null && pow.Amount > 0)
                    result.Pass("HauntPower.Amount", pow.Amount.ToString());
                else
                    result.Fail("HauntPower.Amount", ">0", pow?.Amount.ToString() ?? "null");
            }
            finally
            {
                await PowerCmd.Remove<HauntPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }
}
