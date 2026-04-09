using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §9 MitigatedByStrReduction — cards that reduce enemy Strength.
/// Pattern: play card, EndTurn, verify total MitigatedByStrReduction > 0.
/// Card sources covered: Malaise, DarkShackles, EnfeeblingTouch.
/// Potion/relic sources (PotionOfBinding, TeaOfDiscourtesy, WhisperingEarring) deferred.
/// </summary>
public static class Catalog_DefenseStrReductionTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_Malaise_StrReduction(),
        new CAT_DarkShackles_StrReduction(),
        new CAT_EnfeeblingTouch_StrReduction(),
        new CAT_StrReduction_BoundaryNoAttack(), // boundary
    };

    private abstract class StrRedBase<T> : ITestScenario where T : MegaCrit.Sts2.Core.Models.CardModel
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public string Category => "Catalog_StrReduction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var card = await ctx.CreateCardInHand<T>();
            await ctx.PlayCard(card, enemy);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.MitigatedByStrReduction;
            ctx.AssertGreaterThan(result, "Total.MitigatedByStrReduction", 0, total);
            result.ActualValues["MitigatedByStrReduction"] = total.ToString();
            await ctx.SetEnergy(999);
            return result;
        }
    }

    private class CAT_Malaise_StrReduction : StrRedBase<Malaise>
    {
        public override string Id => "CAT-STR-Malaise";
        public override string Name => "Catalog §9: Malaise reduces enemy Strength → MitigatedByStrReduction > 0";
    }

    private class CAT_DarkShackles_StrReduction : StrRedBase<DarkShackles>
    {
        public override string Id => "CAT-STR-DarkShackles";
        public override string Name => "Catalog §9: DarkShackles -Str → MitigatedByStrReduction > 0";
    }

    private class CAT_EnfeeblingTouch_StrReduction : StrRedBase<EnfeeblingTouch>
    {
        public override string Id => "CAT-STR-EnfeeblingTouch";
        public override string Name => "Catalog §9: EnfeeblingTouch -Str → MitigatedByStrReduction > 0";
    }

    /// <summary>Boundary: apply StrReduction to enemy without intent to attack → no mitigation recorded.</summary>
    private class CAT_StrReduction_BoundaryNoAttack : ITestScenario
    {
        public string Id => "CAT-STR-Boundary";
        public string Name => "Catalog §9 boundary: DarkShackles without EndTurn → 0 mitigation recorded";
        public string Category => "Catalog_StrReduction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            var card = await ctx.CreateCardInHand<DarkShackles>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            // Don't EndTurn — mitigation should not be recorded yet
            var delta = ctx.GetDelta();
            delta.TryGetValue("DARK_SHACKLES", out var d);
            ctx.AssertEquals(result, "DARK_SHACKLES.MitigatedByStrReduction_preEndTurn", 0, d?.MitigatedByStrReduction ?? 0);
            return result;
        }
    }
}
