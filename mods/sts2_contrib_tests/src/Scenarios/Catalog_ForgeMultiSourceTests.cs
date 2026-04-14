using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog — Multi-source Forge sub-bar tests.
///
/// Extends the single-source <c>RG_ForgeSubBar</c> test (Bulwark only) to
/// verify that FORGE:BULWARK, FORGE:FURNACE and FORGE:BASE all accumulate
/// correctly onto the same SovereignBlade card play. Per spec §Forge:
///
///   1. PlayCard(Bulwark)                       → Forge 10, logged as BULWARK
///   2. PlayCard(Furnace) → manually invoke FurnacePower.AfterSideTurnStart
///                                              → Forge 4, logged as FURNACE
///   3. PlayCard(SovereignBlade, enemy)         → Flush forge log into sub-bars
///        • SOVEREIGN_BLADE.DirectDamage  == 10 + 10 + 4 == 24
///        • FORGE:BULWARK.DirectDamage    == 10
///        • FORGE:FURNACE.DirectDamage    == 4
///        • FORGE:BASE.DirectDamage       == 10 (SovereignBlade initial)
/// </summary>
public static class Catalog_ForgeMultiSourceTests
{
    private const string Cat = "Catalog_ForgeMulti";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new RG_ForgeMultiSource(),
    };

    private class RG_ForgeMultiSource : ITestScenario
    {
        public string Id => "CAT-RG-ForgeMultiSource";
        public string Name => "ForgeMultiSource: Bulwark(10)+Furnace(4) → SovereignBlade=24, FORGE:BULWARK=10, FORGE:FURNACE=4, FORGE:BASE=10";
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
                await PowerCmd.Remove<FurnacePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
                await ctx.ClearBlock();

                // Step 1: Bulwark (block 13 + forge 10, logged as BULWARK)
                var bulwark = await ctx.CreateCardInHand<Bulwark>();
                await ctx.PlayCard(bulwark);
                await Task.Delay(150);

                // Step 2: Furnace (apply FurnacePower Amount=4), then manually
                // drive AfterSideTurnStart so it Forges 4 with the FURNACE source
                // set via the power's active context.
                var furnace = await ctx.CreateCardInHand<Furnace>();
                await ctx.PlayCard(furnace);
                await Task.Delay(150);

                var fPower = ctx.PlayerCreature.GetPower<FurnacePower>();
                if (fPower == null)
                {
                    result.Fail("FurnacePower", "applied", "null");
                    return result;
                }
                // Trigger the Forge via the power's turn-start hook. The power's
                // AfterSideTurnStart issues ForgeCmd.Forge with itself as source,
                // so ForgeCmdPatch.AfterForge records it as FURNACE.
                CombatTracker.Instance.SetActivePowerSource(fPower.Id.Entry);
                await fPower.AfterSideTurnStart(ctx.PlayerCreature.Side, ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();
                await Task.Delay(150);

                // Step 3: Get / create SovereignBlade and play it.
                var hand = PileType.Hand.GetPile(ctx.Player);
                var blade = hand.Cards.FirstOrDefault(c => c is SovereignBlade) as SovereignBlade;
                if (blade == null)
                {
                    blade = ctx.CombatState.CreateCard<SovereignBlade>(ctx.Player);
                    await CardPileCmd.Add(blade, PileType.Hand, skipVisuals: true);
                    await Task.Delay(100);
                }

                ctx.TakeSnapshot();
                await ctx.PlayCard(blade, enemy);
                var delta = ctx.GetDelta();

                // SOVEREIGN_BLADE DirectDamage = 10 base + 10 Bulwark + 4 Furnace = 24
                delta.TryGetValue("SOVEREIGN_BLADE", out var dSB);
                ctx.AssertEquals(result, "SOVEREIGN_BLADE.DirectDamage", 24, dSB?.DirectDamage ?? 0);

                delta.TryGetValue("FORGE:BULWARK", out var dForgeBulwark);
                ctx.AssertEquals(result, "FORGE:BULWARK.DirectDamage", 10, dForgeBulwark?.DirectDamage ?? 0);

                delta.TryGetValue("FORGE:FURNACE", out var dForgeFurnace);
                ctx.AssertEquals(result, "FORGE:FURNACE.DirectDamage", 4, dForgeFurnace?.DirectDamage ?? 0);

                delta.TryGetValue("FORGE:BASE", out var dForgeBase);
                ctx.AssertEquals(result, "FORGE:BASE.DirectDamage", 10, dForgeBase?.DirectDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<FurnacePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
