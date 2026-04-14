using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §11 CardsDrawn — cards that draw extra cards during the turn.
/// Normal: play card, verify CardsDrawn increment on the source.
/// Boundary: draw when deck empty / NoDraw applied.
/// </summary>
public static class Catalog_DrawTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_Acrobatics_Draw(),           // normal
        new CAT_PommelStrike_Draw(),         // normal — +1 draw
        new CAT_EscapePlan_Draw(),           // normal — +1 draw
        new CAT_FlashOfSteel_Draw(),         // normal — +1 draw
        new CAT_Finesse_Draw(),              // normal — +1 draw + Block
        new CAT_BattleTrance_Draw(),         // normal — +3 draw (boundary: NoDraw applied after)
        new CAT_Draw_BoundaryNoDrawBlocked(),// boundary: play with NoDraw power → no draws
    };

    private class CAT_Acrobatics_Draw : ITestScenario
    {
        public string Id => "CAT-DRAW-Acrobatics";
        public string Name => "Catalog §11: Acrobatics CardsDrawn=3";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearHand(); // Round 14 B5: Acrobatics draws 3, prevent hand overflow
            var card = await ctx.CreateCardInHand<Acrobatics>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ACROBATICS", out var d);
            int cd = d?.CardsDrawn ?? 0;
            ctx.AssertEquals(result, "ACROBATICS.CardsDrawn", 3, cd);
            result.ActualValues["CardsDrawn"] = cd.ToString();
            return result;
        }
    }

    private class CAT_PommelStrike_Draw : ITestScenario
    {
        public string Id => "CAT-DRAW-Pommel";
        public string Name => "Catalog §11: PommelStrike CardsDrawn=1";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            var card = await ctx.CreateCardInHand<PommelStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("POMMEL_STRIKE", out var d);
            ctx.AssertEquals(result, "POMMEL_STRIKE.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class CAT_EscapePlan_Draw : ITestScenario
    {
        public string Id => "CAT-DRAW-EscapePlan";
        public string Name => "Catalog §11: EscapePlan CardsDrawn=1";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile();
            var card = await ctx.CreateCardInHand<EscapePlan>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ESCAPE_PLAN", out var d);
            int cd = d?.CardsDrawn ?? 0;
            ctx.AssertEquals(result, "ESCAPE_PLAN.CardsDrawn", 1, cd);
            return result;
        }
    }

    private class CAT_FlashOfSteel_Draw : ITestScenario
    {
        public string Id => "CAT-DRAW-FlashOfSteel";
        public string Name => "Catalog §11: FlashOfSteel CardsDrawn=1";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile();
            var enemy = ctx.GetFirstEnemy();
            var card = await ctx.CreateCardInHand<FlashOfSteel>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FLASH_OF_STEEL", out var d);
            ctx.AssertEquals(result, "FLASH_OF_STEEL.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class CAT_Finesse_Draw : ITestScenario
    {
        public string Id => "CAT-DRAW-Finesse";
        public string Name => "Catalog §11: Finesse CardsDrawn=1";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile();
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            var card = await ctx.CreateCardInHand<Finesse>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FINESSE", out var d);
            ctx.AssertEquals(result, "FINESSE.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            return result;
        }
    }

    private class CAT_BattleTrance_Draw : ITestScenario
    {
        public string Id => "CAT-DRAW-BattleTrance";
        public string Name => "Catalog §11: BattleTrance CardsDrawn=3 + NoDraw applied";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile();
            await PowerCmd.Remove<NoDrawPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<BattleTrance>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BATTLE_TRANCE", out var d);
            int cd = d?.CardsDrawn ?? 0;
            ctx.AssertEquals(result, "BATTLE_TRANCE.CardsDrawn", 3, cd);
            result.ActualValues["CardsDrawn"] = cd.ToString();
            await PowerCmd.Remove<NoDrawPower>(ctx.PlayerCreature);
            return result;
        }
    }

    /// <summary>Boundary: NoDrawPower blocks further draws — PommelStrike should not credit a draw.</summary>
    private class CAT_Draw_BoundaryNoDrawBlocked : ITestScenario
    {
        public string Id => "CAT-DRAW-NoDraw";
        public string Name => "Catalog §11 boundary: NoDrawPower → PommelStrike CardsDrawn=0";
        public string Category => "Catalog_Draw";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await ctx.ApplyPower<NoDrawPower>(ctx.PlayerCreature, 1);
            var card = await ctx.CreateCardInHand<PommelStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("POMMEL_STRIKE", out var d);
            // With NoDraw active, should draw 0
            int cd = d?.CardsDrawn ?? 0;
            ctx.AssertEquals(result, "POMMEL_STRIKE.CardsDrawn (NoDraw)", 0, cd);
            await PowerCmd.Remove<NoDrawPower>(ctx.PlayerCreature);
            return result;
        }
    }
}
