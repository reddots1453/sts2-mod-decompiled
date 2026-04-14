using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §4 EffectiveBlock — per-character Defend variants + ShrugItOff + TrueGrit.
/// Validates OnBlockGained → ContributionAccum.EffectiveBlock attribution.
/// Relic block sources (Anchor, BronzeScales) deferred — not reachable via card API.
/// </summary>
public static class Catalog_DefenseBlockTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_DefendIronclad_Basic(),
        new CAT_DefendSilent_Basic(),
        new CAT_DefendDefect_Basic(),
        new CAT_DefendRegent_Basic(),
        new CAT_DefendNecrobinder_Basic(),
        new CAT_ShrugItOff_Basic(),
        new CAT_TrueGrit_Basic(),
        new CAT_Defend_BoundaryUnconsumed(),  // boundary: gain block then immediately another source
    };

    private abstract class DefendBase : ITestScenario
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public string Category => "Catalog_DefenseBlock";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public abstract Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct);

        protected async Task<int> PlayAndConsume<T>(TestContext ctx, string cardKey) where T : MegaCrit.Sts2.Core.Models.CardModel
        {
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var card = await ctx.CreateCardInHand<T>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            // Force damage so block gets consumed (OnBlockGained accumulates gained, consume tracks effective)
            var enemy = ctx.GetFirstEnemy();
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue(cardKey, out var d);
            return d?.EffectiveBlock ?? 0;
        }
    }

    private class CAT_DefendIronclad_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-Defend-IC";
        public override string Name => "Catalog §4: DefendIronclad EffectiveBlock=5";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<DefendIronclad>(ctx, "DEFEND_IRONCLAD");
            ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock", 5, eff);
            return result;
        }
    }

    private class CAT_DefendSilent_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-Defend-SI";
        public override string Name => "Catalog §4: DefendSilent EffectiveBlock=5";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<DefendSilent>(ctx, "DEFEND_SILENT");
            ctx.AssertEquals(result, "DEFEND_SILENT.EffectiveBlock", 5, eff);
            return result;
        }
    }

    private class CAT_DefendDefect_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-Defend-DE";
        public override string Name => "Catalog §4: DefendDefect EffectiveBlock=5";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<DefendDefect>(ctx, "DEFEND_DEFECT");
            ctx.AssertEquals(result, "DEFEND_DEFECT.EffectiveBlock", 5, eff);
            return result;
        }
    }

    private class CAT_DefendRegent_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-Defend-RE";
        public override string Name => "Catalog §4: DefendRegent EffectiveBlock=5";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<DefendRegent>(ctx, "DEFEND_REGENT");
            ctx.AssertEquals(result, "DEFEND_REGENT.EffectiveBlock", 5, eff);
            return result;
        }
    }

    private class CAT_DefendNecrobinder_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-Defend-NE";
        public override string Name => "Catalog §4: DefendNecrobinder EffectiveBlock=5";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<DefendNecrobinder>(ctx, "DEFEND_NECROBINDER");
            ctx.AssertEquals(result, "DEFEND_NECROBINDER.EffectiveBlock", 5, eff);
            return result;
        }
    }

    private class CAT_ShrugItOff_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-Shrug";
        public override string Name => "Catalog §4: ShrugItOff EffectiveBlock=8";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<ShrugItOff>(ctx, "SHRUG_IT_OFF");
            ctx.AssertEquals(result, "SHRUG_IT_OFF.EffectiveBlock", 8, eff);
            return result;
        }
    }

    private class CAT_TrueGrit_Basic : DefendBase
    {
        public override string Id => "CAT-BLK-TrueGrit";
        public override string Name => "Catalog §4: TrueGrit EffectiveBlock=7";
        public override async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            int eff = await PlayAndConsume<TrueGrit>(ctx, "TRUE_GRIT");
            // KB: TrueGrit base block = 7
            ctx.AssertEquals(result, "TRUE_GRIT.EffectiveBlock", 7, eff);
            result.ActualValues["block"] = eff.ToString();
            return result;
        }
    }

    /// <summary>Boundary: block gained but no damage comes → EffectiveBlock should still track gained via OnBlockGained.</summary>
    private class CAT_Defend_BoundaryUnconsumed : ITestScenario
    {
        public string Id => "CAT-BLK-DefendUnconsumed";
        public string Name => "Catalog §4 boundary: Defend not yet consumed tracks gained block";
        public string Category => "Catalog_DefenseBlock";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var defend = await ctx.CreateCardInHand<DefendIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(defend);
            // Do NOT consume — verify player block went up by 5
            ctx.AssertEquals(result, "PlayerBlockGained", 5, ctx.PlayerCreature.Block);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            return result;
        }
    }
}
