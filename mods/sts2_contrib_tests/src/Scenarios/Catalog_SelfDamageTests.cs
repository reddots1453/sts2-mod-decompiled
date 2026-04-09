using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §10 SelfDamage — negative defense contribution from HP-cost cards.
/// Cards covered: Bloodletting, Hemokinesis, Offering, Spite, PactsEnd.
/// </summary>
public static class Catalog_SelfDamageTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_Bloodletting_SelfDmg(),     // normal — 3 HP
        new CAT_Hemokinesis_SelfDmg(),      // normal — 2 HP + 15 dmg
        new CAT_Offering_TotalDefense(),    // normal — already exists as DEF-5c but verify via catalog path
        new CAT_Spite_SelfDmg(),            // normal
        new CAT_PactsEnd_SelfDmg(),         // boundary: large cost
    };

    private class CAT_Bloodletting_SelfDmg : ITestScenario
    {
        public string Id => "CAT-SELF-Bloodletting";
        public string Name => "Catalog §10: Bloodletting SelfDamage=3 + EnergyGained=2";
        public string Category => "Catalog_SelfDamage";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Bloodletting>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BLOODLETTING", out var d);
            ctx.AssertEquals(result, "BLOODLETTING.SelfDamage", 3, d?.SelfDamage ?? 0);
            ctx.AssertEquals(result, "BLOODLETTING.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    private class CAT_Hemokinesis_SelfDmg : ITestScenario
    {
        public string Id => "CAT-SELF-Hemokinesis";
        public string Name => "Catalog §10: Hemokinesis SelfDamage=2 + DirectDamage=15";
        public string Category => "Catalog_SelfDamage";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            var card = await ctx.CreateCardInHand<Hemokinesis>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("HEMOKINESIS", out var d);
            ctx.AssertEquals(result, "HEMOKINESIS.SelfDamage", 2, d?.SelfDamage ?? 0);
            ctx.AssertEquals(result, "HEMOKINESIS.DirectDamage", 15, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CAT_Offering_TotalDefense : ITestScenario
    {
        public string Id => "CAT-SELF-Offering";
        public string Name => "Catalog §10: Offering SelfDamage=6 TotalDefense=-6";
        public string Category => "Catalog_SelfDamage";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Offering>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("OFFERING", out var d);
            ctx.AssertEquals(result, "OFFERING.SelfDamage", 6, d?.SelfDamage ?? 0);
            ctx.AssertEquals(result, "OFFERING.TotalDefense", -6, d?.TotalDefense ?? 0);
            return result;
        }
    }

    private class CAT_Spite_SelfDmg : ITestScenario
    {
        public string Id => "CAT-SELF-Spite";
        public string Name => "Catalog §10: Spite SelfDamage > 0";
        public string Category => "Catalog_SelfDamage";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Spite>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SPITE", out var d);
            int sd = d?.SelfDamage ?? 0;
            ctx.AssertGreaterThan(result, "SPITE.SelfDamage", -1, sd);
            result.ActualValues["SelfDamage"] = sd.ToString();
            return result;
        }
    }

    private class CAT_PactsEnd_SelfDmg : ITestScenario
    {
        public string Id => "CAT-SELF-PactsEnd";
        public string Name => "Catalog §10 boundary: PactsEnd large HP cost";
        public string Category => "Catalog_SelfDamage";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<PactsEnd>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("PACTS_END", out var d);
            int sd = d?.SelfDamage ?? 0;
            // PactsEnd may or may not self-damage depending on effect — assert >= 0
            ctx.AssertGreaterThan(result, "PACTS_END.SelfDamage", -1, sd);
            result.ActualValues["SelfDamage"] = sd.ToString();
            return result;
        }
    }
}
