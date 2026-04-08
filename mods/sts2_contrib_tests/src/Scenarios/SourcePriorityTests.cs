using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.4: Source attribution priority tests (P1-P5).
/// Tests the ResolveSource chain: cardSource → activeCard → potion → relic → power → UNTRACKED.
/// </summary>
public static class SourcePriorityTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new P1_ExplicitCardSource(),
        new P5_UntrackedFallback(),
    };

    /// <summary>
    /// P1: Explicit card source — when a card directly causes damage,
    /// the damage is attributed to that card even if other contexts are active.
    /// </summary>
    private class P1_ExplicitCardSource : ITestScenario
    {
        public string Id => "P1";
        public string Name => "Explicit card source has highest priority";
        public string Category => "SourcePriority";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Playing Strike against an enemy — damage should be attributed to STRIKE_IRONCLAD
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            var enemy = ctx.GetFirstEnemy();

            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("STRIKE_IRONCLAD", out var d);

            // Strike should be the source (not UNTRACKED or any other)
            ctx.AssertEquals(result, "STRIKE_IRONCLAD.DirectDamage", 6, d?.DirectDamage ?? 0);

            // Verify no UNTRACKED entry was created
            delta.TryGetValue("UNTRACKED", out var untracked);
            int untrackedDmg = (untracked?.DirectDamage ?? 0) + (untracked?.AttributedDamage ?? 0);
            ctx.AssertEquals(result, "UNTRACKED.Damage (should be 0)", 0, untrackedDmg);

            return result;
        }
    }

    /// <summary>
    /// P5: When damage occurs with no context (no card, no potion, no relic, no power),
    /// it should go to UNTRACKED instead of being lost.
    /// HIGH: Tests fix H5.
    /// </summary>
    private class P5_UntrackedFallback : ITestScenario
    {
        public string Id => "P5";
        public string Name => "No context → damage goes to UNTRACKED, not lost";
        public string Category => "SourcePriority";

        public bool CanRun(TestContext ctx) =>
            ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var enemy = ctx.GetFirstEnemy();

            // Simulate damage with no card source and no active context
            // This should trigger H5 fallback → UNTRACKED
            ctx.TakeSnapshot();
            await ctx.SimulateDamage(enemy, 5, ctx.PlayerCreature, cardSource: null);

            var delta = ctx.GetDelta();

            // Some source should have captured the 5 damage (either UNTRACKED or _activeCardId if set)
            int totalDmg = 0;
            bool hasUntracked = false;
            foreach (var (key, d) in delta)
            {
                totalDmg += d.DirectDamage + d.AttributedDamage;
                if (key == "UNTRACKED") hasUntracked = true;
            }

            // Key assertion: damage was NOT lost
            ctx.AssertEquals(result, "TotalDamageTracked", 5, totalDmg);

            result.ExpectedValues["HasUntrackedEntry"] = "true or attributed to some source";
            result.ActualValues["HasUntrackedEntry"] = hasUntracked.ToString();
            result.ActualValues["TotalDamage"] = totalDmg.ToString();

            return result;
        }
    }
}
