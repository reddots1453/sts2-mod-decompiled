using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.5: Defense tests.
/// Uses EndTurn for enemy attacks. Uses ApplyPower instead of PlayCard for buff/debuff
/// setup to avoid Godot UI crashes from background thread card plays.
/// </summary>
public static class DefenseTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new DEF1a_StrengthReduction(),
        new DEF2a_WeakMitigation(),
        new DEF2d_ColossusMitigation(),
        new DEF3a_IntangibleReduction(),
        new DEF4a_BasicBlock(),
        new DEF4c_FIFOBlock(),
        new DEF5a_BufferMitigation(),
        new DEF5b_BufferPrdExample(),
        new DEF5c_SelfDamageDefense(),
    };

    /// <summary>
    /// DEF-1a: DarkShackles reduces enemy strength.
    /// Apply temporary Str reduction via ApplyPower, EndTurn, verify MitigatedByStrReduction > 0.
    /// </summary>
    private class DEF1a_StrengthReduction : ITestScenario
    {
        public string Id => "DEF-1a";
        public string Name => "DarkShackles -Str, EndTurn → MitigatedByStrReduction > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<StrengthPower>(enemy);

                var shackles = await ctx.CreateCardInHand<DarkShackles>();
                await ctx.PlayCard(shackles, enemy);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("DARK_SHACKLES", out var d);
                // non-deterministic: enemy intent + base Str unknown, mitigation capped by both.
                ctx.AssertGreaterThan(result, "DARK_SHACKLES.MitigatedByStrReduction", 0, d?.MitigatedByStrReduction ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<StrengthPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-2a: Weak on enemy reduces attack damage.
    /// Apply Weak via ApplyPower (not PlayCard), EndTurn, verify MitigatedByDebuff > 0.
    /// </summary>
    private class DEF2a_WeakMitigation : ITestScenario
    {
        public string Id => "DEF-2a";
        public string Name => "Uppercut applies Weak, EndTurn → MitigatedByDebuff > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<WeakPower>(enemy);
                await PowerCmd.Remove<VulnerablePower>(enemy);

                var uppercut = await ctx.CreateCardInHand<Uppercut>();
                await ctx.PlayCard(uppercut, enemy);
                await ctx.ClearBlock();

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("UPPERCUT", out var d);
                // non-deterministic: enemy intent determines attack count/damage, Weak 25% cut
                // applies only if enemy attacks this turn.
                ctx.AssertGreaterThan(result, "UPPERCUT.MitigatedByDebuff", 0, d?.MitigatedByDebuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<WeakPower>(enemy);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-2d: Colossus halves damage from Vulnerable enemies.
    /// Apply Colossus + Vulnerable via ApplyPower, EndTurn, verify MitigatedByBuff > 0.
    /// </summary>
    private class DEF2d_ColossusMitigation : ITestScenario
    {
        public string Id => "DEF-2d";
        public string Name => "Colossus + Vuln enemy, EndTurn → MitigatedByBuff > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<ColossusPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);

                await ctx.ApplyPower<ColossusPower>(ctx.PlayerCreature, 1);
                await ctx.ApplyPower<VulnerablePower>(enemy, 3, ctx.PlayerCreature);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("COLOSSUS_POWER", out var d);
                // non-deterministic: enemy intent determines whether (and how much) Vuln attack
                // damage is halved by Colossus.
                ctx.AssertGreaterThan(result, "COLOSSUS_POWER.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<ColossusPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-3a: Intangible reduces all HP loss to 1.
    /// Apply Intangible via ApplyPower, EndTurn, verify MitigatedByBuff > 0.
    /// </summary>
    private class DEF3a_IntangibleReduction : ITestScenario
    {
        public string Id => "DEF-3a";
        public string Name => "Intangible, EndTurn → MitigatedByBuff > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await ctx.ApplyPower<IntangiblePower>(ctx.PlayerCreature, 1);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("INTANGIBLE_POWER", out var d);
                // non-deterministic: enemy intent determines raw hit size; mitigation = hit-1
                // only if enemy actually attacks.
                ctx.AssertGreaterThan(result, "INTANGIBLE_POWER.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-4a: Basic block — play Defend, EndTurn, verify EffectiveBlock=5.
    /// </summary>
    private class DEF4a_BasicBlock : ITestScenario
    {
        public string Id => "DEF-4a";
        public string Name => "Defend(5), EndTurn → EffectiveBlock=5";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);

                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                await ctx.PlayCard(defend);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEFEND_IRONCLAD", out var d);
                ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-4c: FIFO block — Defend + ShrugItOff, EndTurn, verify Defend consumed first.
    /// </summary>
    private class DEF4c_FIFOBlock : ITestScenario
    {
        public string Id => "DEF-4c";
        public string Name => "FIFO: Defend(5)+Shrug(8), EndTurn → Defend consumed first";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);

                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                var shrug = await ctx.CreateCardInHand<ShrugItOff>();
                await ctx.PlayCard(defend); // 5 block first (FIFO: consumed first)
                await ctx.PlayCard(shrug);  // 8 block second

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEFEND_IRONCLAD", out var defendDelta);
                delta.TryGetValue("SHRUG_IT_OFF", out var shrugDelta);
                // FIFO: Defend's 5 block consumed before Shrug's 8 → DEFEND.EffectiveBlock=5 (if enemy hit ≥5)
                ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock (FIFO first)", 5, defendDelta?.EffectiveBlock ?? 0);
                // non-deterministic: enemy intent determines how many of Shrug's 8 blocks are
                // actually consumed (after Defend's 5 are burned first by FIFO).
                ctx.AssertGreaterThan(result, "SHRUG_IT_OFF.EffectiveBlock (FIFO second)", 0, shrugDelta?.EffectiveBlock ?? 0);
            }
            finally
            {
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-5a: Buffer prevents entire hit. Apply Buffer via ApplyPower, EndTurn.
    /// </summary>
    private class DEF5a_BufferMitigation : ITestScenario
    {
        public string Id => "DEF-5a";
        public string Name => "Buffer(1), EndTurn → MitigatedByBuff > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
                await ctx.ApplyPower<BufferPower>(ctx.PlayerCreature, 1);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("BUFFER_POWER", out var d);
                // non-deterministic: enemy intent; Buffer only absorbs the first HP-loss event
                // which may or may not happen this turn.
                ctx.AssertGreaterThan(result, "BUFFER_POWER.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DEF-5b: PRD-04 §4.4 Buffer prevention worked example.
    /// Drives CombatTracker.OnBufferPrevention(int) directly with the 3 + 5 = 8
    /// per-hit sequence (5 dmg × 4 enemy attacks vs 2 Buffer + 7 block):
    ///   hit1: 5 fully blocked → no buffer call
    ///   hit2: 3 unblocked → Buffer prevents 3
    ///   hit3: 5 unblocked → Buffer prevents 5
    ///   hit4: no buffer left → no call
    /// Expected: BUFFER_POWER source MitigatedByBuff = 8.
    /// </summary>
    private class DEF5b_BufferPrdExample : ITestScenario
    {
        public string Id => "DEF-5b";
        public string Name => "Buffer PRD §4.4 example: 3 + 5 = 8";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            ctx.TakeSnapshot();

            // Drive the public API directly — bypasses the harder-to-control
            // turn timing of EndTurn-based tests and validates the per-hit
            // summation invariant.
            CommunityStats.Collection.CombatTracker.Instance.OnBufferPrevention(3);
            CommunityStats.Collection.CombatTracker.Instance.OnBufferPrevention(5);

            await Task.Delay(50);

            var delta = ctx.GetDelta();
            // OnBufferPrevention falls back to BUFFER_POWER sourceId when no buff source
            // is registered in ContributionMap, so target that specific key per spec.
            delta.TryGetValue("BUFFER_POWER", out var d);
            int mitigated = d?.MitigatedByBuff ?? 0;

            ctx.AssertEquals(result, "BUFFER_POWER.MitigatedByBuff (PRD §4.4)", 8, mitigated);
            result.ActualValues["MitigatedByBuff"] = mitigated.ToString();

            return result;
        }
    }

    /// <summary>
    /// DEF-5c: Self-damage as negative defense. Offering: 6 HP, 2 energy, draws.
    /// </summary>
    private class DEF5c_SelfDamageDefense : ITestScenario
    {
        public string Id => "DEF-5c";
        public string Name => "Offering: SelfDamage = 6, negative defense";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var offering = await ctx.CreateCardInHand<Offering>();

            ctx.TakeSnapshot();
            await ctx.PlayCard(offering);

            var delta = ctx.GetDelta();
            delta.TryGetValue("OFFERING", out var d);

            ctx.AssertEquals(result, "OFFERING.SelfDamage", 6, d?.SelfDamage ?? 0);
            ctx.AssertEquals(result, "OFFERING.EnergyGained", 2, d?.EnergyGained ?? 0);
            ctx.AssertEquals(result, "OFFERING.TotalDefense", -6, d?.TotalDefense ?? 0);

            return result;
        }
    }
}
