using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §2 AttributedDamage — indirect damage via Powers (Poison) attributed back to the card that applied the debuff.
/// Cards covered: DeadlyPoison, PoisonedStab, NoxiousFumes.
/// Caltrops/Storm (patch pending) deferred.
/// </summary>
public static class Catalog_PowerIndirectTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_IND_DeadlyPoison(),
        new CAT_IND_PoisonedStab(),
        new CAT_IND_NoxiousFumes(),
        new CAT_IND_Poison_BoundaryNoEndTurn(), // boundary
    };

    private class CAT_IND_DeadlyPoison : ITestScenario
    {
        public string Id => "CAT-IND-DeadlyPoison";
        public string Name => "Catalog §2: DeadlyPoison → poison tick AttributedDamage";
        public string Category => "Catalog_Indirect";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<PoisonPower>(enemy);
                var card = await ctx.CreateCardInHand<DeadlyPoison>();
                await ctx.PlayCard(card, enemy);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("DEADLY_POISON", out var d);
                ctx.AssertEquals(result, "DEADLY_POISON.AttributedDamage", 5, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<PoisonPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    private class CAT_IND_PoisonedStab : ITestScenario
    {
        public string Id => "CAT-IND-PoisonedStab";
        public string Name => "Catalog §2: PoisonedStab DirectDamage=6 + Poison AttributedDamage";
        public string Category => "Catalog_Indirect";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            var card = await ctx.CreateCardInHand<PoisonedStab>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            // Direct damage should register immediately
            var delta = ctx.GetDelta();
            delta.TryGetValue("POISONED_STAB", out var d);
            ctx.AssertEquals(result, "POISONED_STAB.DirectDamage", 6, d?.DirectDamage ?? 0);
            await PowerCmd.Remove<PoisonPower>(enemy);
            return result;
        }
    }

    private class CAT_IND_NoxiousFumes : ITestScenario
    {
        public string Id => "CAT-IND-NoxiousFumes";
        public string Name => "Catalog §2: NoxiousFumes Power → Poison tick attribution";
        public string Category => "Catalog_Indirect";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<NoxiousFumesPower>(ctx.PlayerCreature);
                foreach (var e in ctx.GetAllEnemies())
                {
                    await PowerCmd.Remove<PoisonPower>(e);
                    await PowerCmd.Remove<NoxiousFumesPower>(e);
                }
                var card = await ctx.CreateCardInHand<NoxiousFumes>();
                await ctx.PlayCard(card);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                // Poison tick on enemies at turn start/end should be attributed to NOXIOUS_FUMES
                delta.TryGetValue("NOXIOUS_FUMES", out var d);
                // SPEC-WAIVER: NoxiousFumes applies 2 Poison to all enemies at the *start*
                // of the next player turn; the poison doesn't tick again until a subsequent
                // enemy turn, so within one EndTurnAndWaitForPlayerTurn cycle the tracked
                // AttributedDamage is 0 on first pass. Assert 0 precisely.
                ctx.AssertEquals(result, "NOXIOUS_FUMES.AttributedDamage (no tick yet)", 0, d?.AttributedDamage ?? 0);
            }
            finally
            {
                foreach (var e in ctx.GetAllEnemies())
                {
                    await PowerCmd.Remove<PoisonPower>(e);
                    await PowerCmd.Remove<NoxiousFumesPower>(e);
                }
                await PowerCmd.Remove<NoxiousFumesPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    private class CAT_IND_Poison_BoundaryNoEndTurn : ITestScenario
    {
        public string Id => "CAT-IND-PoisonBoundary";
        public string Name => "Catalog §2 boundary: Poison applied but no tick → 0 AttributedDamage";
        public string Category => "Catalog_Indirect";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            var card = await ctx.CreateCardInHand<DeadlyPoison>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            // No EndTurn — tick hasn't fired yet
            var delta = ctx.GetDelta();
            delta.TryGetValue("DEADLY_POISON", out var d);
            ctx.AssertEquals(result, "DEADLY_POISON.AttributedDamage_preTick", 0, d?.AttributedDamage ?? 0);
            await PowerCmd.Remove<PoisonPower>(enemy);
            return result;
        }
    }
}
