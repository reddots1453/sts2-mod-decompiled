using MegaCrit.Sts2.Core.Models.Cards;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.3: Indirect Damage tests (I1-I5).
/// </summary>
public static class IndirectDamageTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new I5_SelfDamage(),
    };

    /// <summary>
    /// I5: Self-damage card (Bloodletting: 3 HP loss, gain 2 energy).
    /// Bloodletting.SelfDamage = 3 (positive value representing HP lost).
    /// </summary>
    private class I5_SelfDamage : ITestScenario
    {
        public string Id => "I5";
        public string Name => "Bloodletting: SelfDamage = 3";
        public string Category => "IndirectDamage";

        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var bloodletting = ctx.CreateCard<Bloodletting>();

            ctx.TakeSnapshot();
            await ctx.PlayCard(bloodletting);

            var delta = ctx.GetDelta();
            delta.TryGetValue("BLOODLETTING", out var d);

            // Bloodletting self-inflicts 3 HP
            ctx.AssertEquals(result, "BLOODLETTING.SelfDamage", 3, d?.SelfDamage ?? 0);
            // Also grants 2 energy
            ctx.AssertEquals(result, "BLOODLETTING.EnergyGained", 2, d?.EnergyGained ?? 0);

            return result;
        }
    }
}
