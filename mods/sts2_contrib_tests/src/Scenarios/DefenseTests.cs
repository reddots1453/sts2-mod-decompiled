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
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Play DarkShackles from hand (preserves card source attribution)
            var shackles = await ctx.CreateCardInHand<DarkShackles>();
            await ctx.PlayCard(shackles, enemy);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int totalMitigated = 0;
            foreach (var (key, d) in delta)
                totalMitigated += d.MitigatedByStrReduction;

            ctx.AssertGreaterThan(result, "Total.MitigatedByStrReduction", 0, totalMitigated);
            result.ActualValues["MitigatedByStrReduction"] = totalMitigated.ToString();

            await ctx.SetEnergy(999);
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
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Play Uppercut from hand to apply Weak (preserves card source attribution)
            var uppercut = await ctx.CreateCardInHand<Uppercut>();
            await ctx.PlayCard(uppercut, enemy);

            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int totalMitigated = 0;
            foreach (var (key, d) in delta)
                totalMitigated += d.MitigatedByDebuff;

            ctx.AssertGreaterThan(result, "Total.MitigatedByDebuff", 0, totalMitigated);
            result.ActualValues["MitigatedByDebuff"] = totalMitigated.ToString();

            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.SetEnergy(999);
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
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            await ctx.ApplyPower<ColossusPower>(ctx.PlayerCreature, 1);
            await ctx.ApplyPower<VulnerablePower>(enemy, 3, ctx.PlayerCreature);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int totalMitigated = 0;
            foreach (var (key, d) in delta)
                totalMitigated += d.MitigatedByBuff;

            ctx.AssertGreaterThan(result, "Total.MitigatedByBuff", 0, totalMitigated);
            result.ActualValues["MitigatedByBuff"] = totalMitigated.ToString();

            await PowerCmd.Remove<ColossusPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.SetEnergy(999);
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

            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            await ctx.ApplyPower<IntangiblePower>(ctx.PlayerCreature, 1);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int totalMitigated = 0;
            foreach (var (key, d) in delta)
                totalMitigated += d.MitigatedByBuff;

            ctx.AssertGreaterThan(result, "Total.MitigatedByBuff", 0, totalMitigated);
            result.ActualValues["MitigatedByBuff"] = totalMitigated.ToString();

            await ctx.SetEnergy(999);
            return result;
        }
    }

    /// <summary>
    /// DEF-4a: Basic block — play Defend, EndTurn, verify EffectiveBlock > 0.
    /// </summary>
    private class DEF4a_BasicBlock : ITestScenario
    {
        public string Id => "DEF-4a";
        public string Name => "Defend(5), EndTurn → EffectiveBlock > 0";
        public string Category => "Defense";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            var defend = await ctx.CreateCardInHand<DefendIronclad>();
            await ctx.PlayCard(defend);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int totalEffective = 0;
            foreach (var (key, d) in delta)
                totalEffective += d.EffectiveBlock;

            ctx.AssertGreaterThan(result, "Total.EffectiveBlock", 0, totalEffective);
            result.ActualValues["EffectiveBlock"] = totalEffective.ToString();

            await ctx.SetEnergy(999);
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

            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            var defend = await ctx.CreateCardInHand<DefendIronclad>();
            var shrug = await ctx.CreateCardInHand<ShrugItOff>();
            await ctx.PlayCard(defend);
            await ctx.PlayCard(shrug);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFEND_IRONCLAD", out var defendDelta);
            delta.TryGetValue("SHRUG_IT_OFF", out var shrugDelta);

            int defendBlock = defendDelta?.EffectiveBlock ?? 0;
            int shrugBlock = shrugDelta?.EffectiveBlock ?? 0;
            int totalBlock = defendBlock + shrugBlock;

            ctx.AssertGreaterThan(result, "Total.EffectiveBlock", 0, totalBlock);

            if (totalBlock > 5)
                ctx.AssertEquals(result, "DEFEND.EffectiveBlock (FIFO)", 5, defendBlock);
            else if (totalBlock > 0)
                ctx.AssertEquals(result, "DEFEND.EffectiveBlock (FIFO: all to first)", totalBlock, defendBlock);

            result.ActualValues["Defend.EffectiveBlock"] = defendBlock.ToString();
            result.ActualValues["Shrug.EffectiveBlock"] = shrugBlock.ToString();

            await ctx.SetEnergy(999);
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

            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            await ctx.ApplyPower<BufferPower>(ctx.PlayerCreature, 1);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int totalMitigated = 0;
            foreach (var (key, d) in delta)
                totalMitigated += d.MitigatedByBuff;

            ctx.AssertGreaterThan(result, "Total.MitigatedByBuff", 0, totalMitigated);
            result.ActualValues["MitigatedByBuff"] = totalMitigated.ToString();

            await ctx.SetEnergy(999);
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
            int totalMitigated = 0;
            foreach (var (key, d) in delta) totalMitigated += d.MitigatedByBuff;

            ctx.AssertEquals(result, "Total.MitigatedByBuff (PRD §4.4)", 8, totalMitigated);
            result.ActualValues["MitigatedByBuff"] = totalMitigated.ToString();

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
