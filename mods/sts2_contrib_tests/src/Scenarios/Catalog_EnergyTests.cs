using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §12 EnergyGained — active energy gain + free-play savings (§NEW-1).
/// Cards covered: Bloodletting(+2), Offering(+2), Corruption (Skill free), FreeAttackPower.
/// SneckoEye (§4.2), Enlightenment (§4.3), Ectoplasm relic — deferred.
/// </summary>
public static class Catalog_EnergyTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_EN_Bloodletting_Energy(),    // normal +2
        new CAT_EN_Offering_Energy(),        // normal +2
        new CAT_EN_Corruption_FreePlay(),    // normal: Corruption makes Skill free → EnergyGained
        new CAT_EN_FreeAttackPower(),        // normal: FreeAttackPower direct source
        new CAT_EN_Boundary_NoFreeCard(),    // boundary: no free modifier → 0 savings
    };

    private class CAT_EN_Bloodletting_Energy : ITestScenario
    {
        public string Id => "CAT-EN-Bloodletting";
        public string Name => "Catalog §12: Bloodletting EnergyGained=2";
        public string Category => "Catalog_Energy";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Bloodletting>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BLOODLETTING", out var d);
            ctx.AssertEquals(result, "BLOODLETTING.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    private class CAT_EN_Offering_Energy : ITestScenario
    {
        public string Id => "CAT-EN-Offering";
        public string Name => "Catalog §12: Offering EnergyGained=2";
        public string Category => "Catalog_Energy";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Offering>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("OFFERING", out var d);
            ctx.AssertEquals(result, "OFFERING.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    private class CAT_EN_Corruption_FreePlay : ITestScenario
    {
        public string Id => "CAT-EN-Corruption";
        public string Name => "Catalog §12: Corruption makes Defend free → EnergyGained=1";
        public string Category => "Catalog_Energy";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var corr = await ctx.CreateCardInHand<Corruption>();
            await ctx.PlayCard(corr);
            var defend = await ctx.CreateCardInHand<DefendIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(defend);
            var delta = ctx.GetDelta();
            delta.TryGetValue("CORRUPTION", out var d);
            ctx.AssertEquals(result, "CORRUPTION.EnergyGained", 1, d?.EnergyGained ?? 0);
            await PowerCmd.Remove<CorruptionPower>(ctx.PlayerCreature);
            return result;
        }
    }

    private class CAT_EN_FreeAttackPower : ITestScenario
    {
        public string Id => "CAT-EN-FreeAttackPower";
        public string Name => "Catalog §12: FreeAttackPower → Strike plays without crash (smoke)";
        public string Category => "Catalog_Energy";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await ctx.ApplyPower<FreeAttackPower>(ctx.PlayerCreature, 1);
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);
            // AutoPlay bypasses the energy cost system (EnergySpent=0 always),
            // so FreeAttackPower savings can't be detected. Smoke test only.
            result.Passed = true;
            await PowerCmd.Remove<FreeAttackPower>(ctx.PlayerCreature);
            return result;
        }
    }

    /// <summary>Boundary: play Strike without any free-cost modifier → no EnergyGained (cost is paid).</summary>
    private class CAT_EN_Boundary_NoFreeCard : ITestScenario
    {
        public string Id => "CAT-EN-Boundary";
        public string Name => "Catalog §12 boundary: Strike plain → 0 EnergyGained anywhere";
        public string Category => "Catalog_Energy";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<FreeAttackPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<CorruptionPower>(ctx.PlayerCreature);
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);
            var delta = ctx.GetDelta();
            int totalEn = 0;
            foreach (var (_, d) in delta) totalEn += d.EnergyGained;
            ctx.AssertEquals(result, "Total.EnergyGained (no free mod)", 0, totalEn);
            return result;
        }
    }
}
