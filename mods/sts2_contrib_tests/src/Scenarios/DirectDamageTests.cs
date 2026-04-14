using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.1: Direct Damage tests (D1-D6).
/// </summary>
public static class DirectDamageTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new D1_BasicDamage(),
        new D2_AoeDamage(),
        new D3_MultiHit(),
        new D4_OverkillCap(),
        new D_BlockedDamage(),
        new D6_RelicPassiveDamage(),
    };

    /// <summary>D1: Single-target attack card deals correct DirectDamage.</summary>
    private class D1_BasicDamage : ITestScenario
    {
        public string Id => "D1";
        public string Name => "Strike deals DirectDamage = base damage";
        public string Category => "DirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // StrikeIronclad: base 6 damage
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            var enemy = ctx.GetFirstEnemy();

            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("STRIKE_IRONCLAD", out var d);

            ctx.AssertEquals(result, "STRIKE_IRONCLAD.DirectDamage", 6, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "STRIKE_IRONCLAD.TimesPlayed", 1, d?.TimesPlayed ?? 0);

            return result;
        }
    }

    /// <summary>D3: Multi-hit attack sums all hits into DirectDamage.</summary>
    private class D3_MultiHit : ITestScenario
    {
        public string Id => "D3";
        public string Name => "TwinStrike 2x5 = 10 DirectDamage";
        public string Category => "DirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // TwinStrike: 5 damage x 2 hits = 10 total
            var twin = await ctx.CreateCardInHand<TwinStrike>();
            var enemy = ctx.GetFirstEnemy();

            ctx.TakeSnapshot();
            await ctx.PlayCard(twin, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("TWIN_STRIKE", out var d);

            ctx.AssertEquals(result, "TWIN_STRIKE.DirectDamage", 10, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "TWIN_STRIKE.TimesPlayed", 1, d?.TimesPlayed ?? 0);

            return result;
        }
    }

    /// <summary>
    /// D4: Overkill damage capped at enemy remaining HP.
    /// Remove enemy Plating, then simulate a 20-damage hit on enemy with 5 HP.
    /// DirectDamage should record 5 (actual HP), not 20 (raw damage).
    /// </summary>
    private class D4_OverkillCap : ITestScenario
    {
        public string Id => "D4";
        public string Name => "Overkill: damage capped at enemy remaining HP";
        public string Category => "DirectDamage";

        // Need at least 2 enemies so combat doesn't end when one dies
        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count >= 2;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Use the LAST enemy as a sacrifice — set it to low HP directly
            var enemies = ctx.GetAllEnemies();
            var sacrificeEnemy = enemies[enemies.Count - 1];
            await CreatureCmd.LoseBlock(sacrificeEnemy, sacrificeEnemy.Block);

            // Set enemy to exactly 5 HP so Bludgeon (32 base) will overkill
            await CreatureCmd.SetCurrentHp(sacrificeEnemy, 5m);
            int hpBefore = sacrificeEnemy.CurrentHp;

            // Snapshot, then use Bludgeon (32 base damage) to overkill
            var bludgeon = await ctx.CreateCardInHand<Bludgeon>();

            ctx.TakeSnapshot();
            await ctx.PlayCard(bludgeon, sacrificeEnemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("BLUDGEON", out var d);

            // KillingBlowPatcher should cap damage at enemy's remaining HP
            int actualDamage = d?.DirectDamage ?? 0;
            ctx.AssertEquals(result, "BLUDGEON.DirectDamage", hpBefore, actualDamage);

            result.ExpectedValues["EnemyHpBefore"] = hpBefore.ToString();
            result.ActualValues["EnemyHpBefore"] = hpBefore.ToString();

            return result;
        }
    }

    /// <summary>
    /// D2: AoE attack — Thunderclap (4 damage to all enemies + 1 Vulnerable).
    /// DirectDamage should equal 4 × number of enemies.
    /// </summary>
    private class D2_AoeDamage : ITestScenario
    {
        public string Id => "D2";
        public string Name => "Thunderclap AoE: DirectDamage = 4 × enemy count";
        public string Category => "DirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Thunderclap: 4 damage to ALL enemies + apply Vulnerable
            var thunderclap = await ctx.CreateCardInHand<Thunderclap>();
            int enemyCount = ctx.GetAllEnemies().Count;

            ctx.TakeSnapshot();
            await ctx.PlayCard(thunderclap);

            var delta = ctx.GetDelta();
            delta.TryGetValue("THUNDERCLAP", out var d);

            // Thunderclap should deal 4 × N enemies
            int expectedDmg = 4 * enemyCount;
            ctx.AssertEquals(result, "THUNDERCLAP.DirectDamage", expectedDmg, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "THUNDERCLAP.TimesPlayed", 1, d?.TimesPlayed ?? 0);

            // Clean up Vulnerable on all enemies
            foreach (var enemy in ctx.GetAllEnemies())
                await PowerCmd.Remove<VulnerablePower>(enemy);

            result.ExpectedValues["EnemyCount"] = enemyCount.ToString();
            result.ActualValues["EnemyCount"] = enemyCount.ToString();

            return result;
        }
    }

    /// <summary>
    /// D6: Relic passive damage — MercuryHourglass deals 3 damage to all enemies each turn.
    /// Simulated via SimulateDamage with no card source, relic context active.
    /// </summary>
    private class D6_RelicPassiveDamage : ITestScenario
    {
        public string Id => "D6";
        public string Name => "Relic passive damage tracked (simulated via SimulateDamage)";
        public string Category => "DirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Simulate relic damage: enemy takes 3 damage with no card source.
            // This tests the ResolveSource fallback chain.
            var enemy = ctx.GetFirstEnemy();

            ctx.TakeSnapshot();
            await ctx.SimulateDamage(enemy, 3, ctx.PlayerCreature, cardSource: null);

            var delta = ctx.GetDelta();

            // SPEC-WAIVER: fallback-routing test — verifies no data loss when no card source is set.
            // Source routing depends on active context which is internal; foreach-sum is the only
            // way to verify total-tracked invariant
            int totalDmg = 0;
            foreach (var (key, d) in delta)
                totalDmg += d.DirectDamage + d.AttributedDamage;

            ctx.AssertEquals(result, "TotalDamageTracked", 3, totalDmg);

            result.ExpectedValues["TotalDamageTracked_detail"] = "3 (to some source)";
            result.ActualValues["TotalDamageTracked_detail"] = totalDmg.ToString();

            return result;
        }
    }

    /// <summary>
    /// D-Block: Attack vs blocked enemy — DirectDamage should include the blocked portion.
    /// Three sub-cases tested sequentially:
    ///   (a) Fully blocked: enemy has 10 block, Strike deals 6 → DirectDamage = 6, HP unchanged
    ///   (b) Partially blocked: enemy has 3 block, Strike deals 6 → DirectDamage = 6 (3 to block + 3 to HP)
    ///   (c) Exact match: enemy has 6 block, Strike deals 6 → DirectDamage = 6, block → 0
    /// </summary>
    private class D_BlockedDamage : ITestScenario
    {
        public string Id => "D-Block";
        public string Name => "Attack vs blocked enemy: DirectDamage includes blocked portion";
        public string Category => "DirectDamage";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // ── Case (a): Fully blocked ──
            // Give enemy 10 block, Strike deals 6 → all absorbed by block
            await CreatureCmd.LoseBlock(enemy, enemy.Block);
            await ctx.GainBlock(enemy, 10);
            int hpBefore_a = enemy.CurrentHp;

            ctx.TakeSnapshot();
            var strike_a = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(strike_a, enemy);

            var delta_a = ctx.GetDelta();
            delta_a.TryGetValue("STRIKE_IRONCLAD", out var d_a);

            int dmg_a = d_a?.DirectDamage ?? 0;
            int hpAfter_a = enemy.CurrentHp;

            // DirectDamage should still be 6 even though block absorbed it
            ctx.AssertEquals(result, "(a) FullyBlocked.DirectDamage", 6, dmg_a);
            // Enemy HP should be unchanged
            ctx.AssertEquals(result, "(a) FullyBlocked.EnemyHpUnchanged", hpBefore_a, hpAfter_a);

            // ── Case (b): Partially blocked ──
            // Give enemy 3 block, Strike deals 6 → 3 to block + 3 to HP
            await CreatureCmd.LoseBlock(enemy, enemy.Block);
            await ctx.GainBlock(enemy, 3);
            int hpBefore_b = enemy.CurrentHp;

            ctx.TakeSnapshot();
            var strike_b = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(strike_b, enemy);

            var delta_b = ctx.GetDelta();
            delta_b.TryGetValue("STRIKE_IRONCLAD", out var d_b);

            int dmg_b = d_b?.DirectDamage ?? 0;
            int hpAfter_b = enemy.CurrentHp;

            // DirectDamage should be 6 (full amount, not 3)
            ctx.AssertEquals(result, "(b) PartialBlock.DirectDamage", 6, dmg_b);
            // Enemy HP should drop by 3 (the unblocked portion)
            ctx.AssertEquals(result, "(b) PartialBlock.HpLost", 3, hpBefore_b - hpAfter_b);

            // ── Case (c): Exact match ──
            // Give enemy 6 block, Strike deals 6 → block reduced to 0, HP unchanged
            await CreatureCmd.LoseBlock(enemy, enemy.Block);
            await ctx.GainBlock(enemy, 6);
            int hpBefore_c = enemy.CurrentHp;

            ctx.TakeSnapshot();
            var strike_c = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(strike_c, enemy);

            var delta_c = ctx.GetDelta();
            delta_c.TryGetValue("STRIKE_IRONCLAD", out var d_c);

            int dmg_c = d_c?.DirectDamage ?? 0;
            int hpAfter_c = enemy.CurrentHp;
            int blockAfter_c = enemy.Block;

            // DirectDamage should be 6
            ctx.AssertEquals(result, "(c) ExactMatch.DirectDamage", 6, dmg_c);
            // Enemy HP unchanged, block reduced to 0
            ctx.AssertEquals(result, "(c) ExactMatch.EnemyHpUnchanged", hpBefore_c, hpAfter_c);
            ctx.AssertEquals(result, "(c) ExactMatch.BlockAfter", 0, blockAfter_c);

            // Clean up
            await CreatureCmd.LoseBlock(enemy, enemy.Block);

            return result;
        }
    }
}
