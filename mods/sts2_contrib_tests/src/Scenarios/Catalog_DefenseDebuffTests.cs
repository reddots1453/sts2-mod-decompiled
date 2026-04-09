using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §6/§7 MitigatedByDebuff — Weak application cards reduce enemy damage.
/// Normal: apply Weak via card, end turn, verify MitigatedByDebuff > 0 attributed to the card.
/// Boundary: no enemy attack intent → 0 mitigation.
/// </summary>
public static class Catalog_DefenseDebuffTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_Neutralize_WeakMitigation(),   // normal
        new CAT_PiercingWail_WeakAoE(),        // normal
        new CAT_LegSweep_WeakAndBlock(),       // normal
    };

    private class CAT_Neutralize_WeakMitigation : ITestScenario
    {
        public string Id => "CAT-DBF-Neutralize";
        public string Name => "Catalog §7: Neutralize applies Weak → MitigatedByDebuff > 0";
        public string Category => "Catalog_DefenseDebuff";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var card = await ctx.CreateCardInHand<Neutralize>();
            await ctx.PlayCard(card, enemy);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.MitigatedByDebuff;
            ctx.AssertGreaterThan(result, "Total.MitigatedByDebuff", 0, total);
            result.ActualValues["MitigatedByDebuff"] = total.ToString();
            await PowerCmd.Remove<WeakPower>(enemy);
            await ctx.SetEnergy(999);
            return result;
        }
    }

    private class CAT_PiercingWail_WeakAoE : ITestScenario
    {
        public string Id => "CAT-DBF-PiercingWail";
        public string Name => "Catalog §7: PiercingWail AoE Weak → MitigatedByDebuff > 0";
        public string Category => "Catalog_DefenseDebuff";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var card = await ctx.CreateCardInHand<PiercingWail>();
            await ctx.PlayCard(card);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.MitigatedByDebuff;
            ctx.AssertGreaterThan(result, "Total.MitigatedByDebuff", 0, total);
            foreach (var enemy in ctx.GetAllEnemies())
                await PowerCmd.Remove<WeakPower>(enemy);
            await ctx.SetEnergy(999);
            return result;
        }
    }

    private class CAT_LegSweep_WeakAndBlock : ITestScenario
    {
        public string Id => "CAT-DBF-LegSweep";
        public string Name => "Catalog §7: LegSweep Weak + Block → MitigatedByDebuff > 0";
        public string Category => "Catalog_DefenseDebuff";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var card = await ctx.CreateCardInHand<LegSweep>();
            await ctx.PlayCard(card, enemy);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.MitigatedByDebuff;
            ctx.AssertGreaterThan(result, "Total.MitigatedByDebuff", 0, total);
            await PowerCmd.Remove<WeakPower>(enemy);
            await ctx.SetEnergy(999);
            return result;
        }
    }
}
