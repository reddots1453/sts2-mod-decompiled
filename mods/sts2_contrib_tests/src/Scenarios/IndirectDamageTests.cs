using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.3: Indirect Damage tests (I1-I5).
/// </summary>
public static class IndirectDamageTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new I1_PoisonDamage(),
        new I2_ThornsDamage(),
        new I5_SelfDamage(),
    };

    /// <summary>
    /// I1: Poison indirect damage.
    /// Apply Poison via NoxiousFumes (or direct power apply), then trigger enemy turn.
    /// Poison damage should go to AttributedDamage of the poison source.
    /// CRITICAL: Tests fix C3 (indirect vs direct damage classification).
    /// </summary>
    private class I1_PoisonDamage : ITestScenario
    {
        public string Id => "I1";
        public string Name => "Poison damage → AttributedDamage to poison source";
        public string Category => "IndirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // Apply 5 Poison via DeadlyPoison card
            var poison = await ctx.CreateCardInHand<DeadlyPoison>();
            await ctx.PlayCard(poison, enemy);

            ctx.TakeSnapshot();

            // EndTurn → enemy turn start → Poison ticks (AfterSideTurnStart)
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();

            // Check that DEADLY_POISON got AttributedDamage (not DirectDamage)
            delta.TryGetValue("DEADLY_POISON", out var poisonDelta);

            int attrDmg = poisonDelta?.AttributedDamage ?? 0;
            int directDmg = poisonDelta?.DirectDamage ?? 0;

            // Poison should be AttributedDamage, NOT DirectDamage (C3 fix)
            ctx.AssertGreaterThan(result, "DEADLY_POISON.AttributedDamage", 0, attrDmg);
            ctx.AssertEquals(result, "DEADLY_POISON.DirectDamage (should be 0)", 0, directDmg);

            result.ExpectedValues["PoisonAmount"] = "5 (applied)";
            result.ActualValues["AttributedDamage"] = attrDmg.ToString();

            // Clean up: remove poison, refresh energy
            await PowerCmd.Remove<PoisonPower>(enemy);
            await ctx.SetEnergy(999);

            return result;
        }
    }

    /// <summary>
    /// I2: Thorns indirect damage.
    /// Apply Thorns to player, simulate enemy attacking player.
    /// Thorns damage should go to AttributedDamage of the thorns source.
    /// CRITICAL: Tests fix C3.
    /// </summary>
    private class I2_ThornsDamage : ITestScenario
    {
        public string Id => "I2";
        public string Name => "Thorns damage → AttributedDamage to thorns source";
        public string Category => "IndirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // Remove player block
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Apply Flame Barrier (Thorns-like: deals damage when attacked)
            var flameBarrier = await ctx.CreateCardInHand<FlameBarrier>();
            await ctx.PlayCard(flameBarrier);

            ctx.TakeSnapshot();

            // EndTurn → enemy attacks player → triggers Flame Barrier counter-damage
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            delta.TryGetValue("FLAME_BARRIER", out var fbDelta);

            // FlameBarrier counter-damage should be classified as AttributedDamage
            int attrDmg = fbDelta?.AttributedDamage ?? 0;
            int directDmg = fbDelta?.DirectDamage ?? 0;

            ctx.AssertGreaterThan(result, "FLAME_BARRIER.AttributedDamage", 0, attrDmg);

            result.ExpectedValues["FlameBarrier_detail"] = "> 0 (counter-damage as AttributedDamage)";
            result.ActualValues["AttributedDamage"] = attrDmg.ToString();
            result.ActualValues["DirectDamage"] = directDmg.ToString();

            // Clean up
            await PowerCmd.Remove<FlameBarrierPower>(ctx.PlayerCreature);
            await ctx.SetEnergy(999);

            return result;
        }
    }

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

            var bloodletting = await ctx.CreateCardInHand<Bloodletting>();

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
