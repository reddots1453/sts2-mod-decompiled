using CommunityStats.Collection;

namespace ContribTests.Scenarios;

/// <summary>
/// PRD §4.10: Consistency tests (F1-F6).
/// Verifies data integrity invariants across the contribution system.
/// </summary>
public static class ConsistencyTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new F4_DamageSumConsistency(),
    };

    /// <summary>
    /// F4: For each source, TotalDamage = DirectDamage + AttributedDamage + ModifierDamage + UpgradeDamage.
    /// Checks the current combat data for any source where this invariant is violated.
    /// </summary>
    private class F4_DamageSumConsistency : ITestScenario
    {
        public string Id => "F4";
        public string Name => "TotalDamage = Direct + Attributed + Modifier + Upgrade for all sources";
        public string Category => "Consistency";

        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var data = CombatTracker.Instance.GetCurrentCombatData();
            var violations = new List<string>();

            foreach (var (sourceId, accum) in data)
            {
                int expected = accum.DirectDamage + accum.AttributedDamage
                    + accum.ModifierDamage + accum.UpgradeDamage;
                int actual = accum.TotalDamage;

                if (expected != actual)
                {
                    violations.Add($"{sourceId}: expected {expected}, TotalDamage = {actual}");
                }
            }

            if (violations.Count == 0)
            {
                result.Pass("AllSources", $"{data.Count} sources checked, all consistent");
            }
            else
            {
                result.Fail("DamageSumViolations",
                    "0 violations",
                    $"{violations.Count} violations: {string.Join("; ", violations)}");
            }

            await Task.CompletedTask;
            return result;
        }
    }
}
