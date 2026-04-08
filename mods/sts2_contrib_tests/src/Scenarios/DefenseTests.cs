using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.5: Defense tests (DEF-1 through DEF-5).
/// </summary>
public static class DefenseTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new DEF4a_BasicBlock(),
        new DEF4c_FIFOBlock(),
        new DEF2a_WeakMitigation(),
        new DEF2d_ColossusMitigation(),
        new DEF5a_BufferMitigation(),
    };

    /// <summary>
    /// DEF-4a: Basic block — play Defend (5 block), take 3 damage.
    /// Defend.EffectiveBlock = 3 (consumed).
    /// </summary>
    private class DEF4a_BasicBlock : ITestScenario
    {
        public string Id => "DEF-4a";
        public string Name => "Defend(5 block), take 3 → EffectiveBlock = 3";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Remove existing block on player first
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Play DefendIronclad: 5 block
            var defend = ctx.CreateCard<DefendIronclad>();
            await ctx.PlayCard(defend);

            ctx.TakeSnapshot();

            // Simulate enemy attacking player for 3 damage
            var enemy = ctx.GetFirstEnemy();
            await ctx.SimulateDamage(ctx.PlayerCreature, 3, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFEND_IRONCLAD", out var d);

            // 3 of 5 block consumed
            ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock", 3, d?.EffectiveBlock ?? 0);

            // Clean up remaining block
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            return result;
        }
    }

    /// <summary>
    /// DEF-4c: FIFO block — Defend(5) then ShrugItOff(8), take 7.
    /// Defend.EffectiveBlock = 5 (all consumed first), ShrugItOff.EffectiveBlock = 2.
    /// </summary>
    private class DEF4c_FIFOBlock : ITestScenario
    {
        public string Id => "DEF-4c";
        public string Name => "FIFO: Defend(5)+Shrug(8), take 7 → 5+2";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Remove existing block
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Play Defend (5 block) then ShrugItOff (8 block)
            var defend = ctx.CreateCard<DefendIronclad>();
            var shrug = ctx.CreateCard<ShrugItOff>();
            await ctx.PlayCard(defend);
            await ctx.PlayCard(shrug);

            ctx.TakeSnapshot();

            // Simulate enemy attack for 7 damage → FIFO: Defend absorbs 5, Shrug absorbs 2
            var enemy = ctx.GetFirstEnemy();
            await ctx.SimulateDamage(ctx.PlayerCreature, 7, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFEND_IRONCLAD", out var defendDelta);
            delta.TryGetValue("SHRUG_IT_OFF", out var shrugDelta);

            ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock", 5, defendDelta?.EffectiveBlock ?? 0);
            ctx.AssertEquals(result, "SHRUG_IT_OFF.EffectiveBlock", 2, shrugDelta?.EffectiveBlock ?? 0);

            // Clean up
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            return result;
        }
    }

    /// <summary>
    /// DEF-2a: Weak on enemy reduces its attack damage. Mitigation attributed to Weak source.
    /// Apply Weak via Uppercut, then simulate enemy attacking player.
    /// </summary>
    private class DEF2a_WeakMitigation : ITestScenario
    {
        public string Id => "DEF-2a";
        public string Name => "Weak on enemy → MitigatedByDebuff > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // Remove player block so damage goes through to weak mitigation calc
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Apply Weak to enemy via Uppercut (also deals damage and applies Vuln)
            var uppercut = ctx.CreateCard<Uppercut>();
            await ctx.PlayCard(uppercut, enemy);

            // Remove player block again after Plating regen etc.
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            ctx.TakeSnapshot();

            // Simulate weakened enemy attacking player for 10 damage
            // With Weak (0.75x), actual = floor(10*0.75) = 7, prevented = ~3
            await ctx.SimulateDamage(ctx.PlayerCreature, 10, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("UPPERCUT", out var uppercutDelta);

            // Weak mitigation should be attributed to UPPERCUT (the Weak source)
            ctx.AssertGreaterThan(result, "UPPERCUT.MitigatedByDebuff", 0, uppercutDelta?.MitigatedByDebuff ?? 0);

            // Record actual value for diagnosis
            result.ExpectedValues["MitigatedByDebuff_detail"] = "~2-3 (Weak 0.75x on 10 dmg)";
            result.ActualValues["MitigatedByDebuff_detail"] = (uppercutDelta?.MitigatedByDebuff ?? 0).ToString();

            // Clean up
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);

            return result;
        }
    }

    /// <summary>
    /// DEF-2d: Colossus buff — player buff that halves damage from Vulnerable enemies.
    /// Apply Colossus to player + Vulnerable to enemy, simulate attack.
    /// </summary>
    private class DEF2d_ColossusMitigation : ITestScenario
    {
        public string Id => "DEF-2d";
        public string Name => "Colossus: halves damage from Vulnerable enemy → MitigatedByBuff";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // Remove player block
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Apply Colossus power to player and Vulnerable to enemy
            await ctx.ApplyPower<ColossusPower>(ctx.PlayerCreature, 1);
            await ctx.ApplyPower<VulnerablePower>(enemy, 2, ctx.PlayerCreature);

            ctx.TakeSnapshot();

            // Simulate vulnerable enemy attacking player for 10 damage
            // Colossus: 0.5x → actual = 5, prevented = 5
            await ctx.SimulateDamage(ctx.PlayerCreature, 10, enemy);

            var delta = ctx.GetDelta();

            // Find the Colossus contribution — it may be keyed by the card that gave Colossus,
            // or by COLOSSUS_POWER if applied directly. Check both patterns.
            int totalMitigatedByBuff = 0;
            foreach (var (key, d) in delta)
            {
                totalMitigatedByBuff += d.MitigatedByBuff;
            }

            ctx.AssertGreaterThan(result, "Total.MitigatedByBuff", 0, totalMitigatedByBuff);

            // Record details
            result.ExpectedValues["MitigatedByBuff_detail"] = "~5 (Colossus 0.5x on 10 dmg)";
            result.ActualValues["MitigatedByBuff_detail"] = totalMitigatedByBuff.ToString();

            // Clean up
            await PowerCmd.Remove<ColossusPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);

            return result;
        }
    }

    /// <summary>
    /// DEF-5a: Buffer prevents entire hit. MitigatedByBuff = hit damage.
    /// </summary>
    private class DEF5a_BufferMitigation : ITestScenario
    {
        public string Id => "DEF-5a";
        public string Name => "Buffer(1) prevents 20 dmg hit → MitigatedByBuff = 20";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // Remove player block
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Apply 1 stack of Buffer to player
            await ctx.ApplyPower<BufferPower>(ctx.PlayerCreature, 1);

            ctx.TakeSnapshot();

            // Simulate enemy attacking player for 20 damage — Buffer should absorb the entire hit
            await ctx.SimulateDamage(ctx.PlayerCreature, 20, enemy);

            var delta = ctx.GetDelta();

            int totalMitigatedByBuff = 0;
            foreach (var (key, d) in delta)
            {
                totalMitigatedByBuff += d.MitigatedByBuff;
            }

            // Buffer should mitigate the entire 20 damage hit
            ctx.AssertEquals(result, "Total.MitigatedByBuff", 20, totalMitigatedByBuff);

            return result;
        }
    }
}
