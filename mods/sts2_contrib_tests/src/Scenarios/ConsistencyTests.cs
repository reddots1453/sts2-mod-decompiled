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
        new F1_UntrackedLogging(),
        new F4_DamageSumConsistency(),
        new F5_DefenseSumConsistency(),
    };

    /// <summary>
    /// F1: UNTRACKED entries should not contain large amounts of unattributed damage.
    /// If UNTRACKED exists and has significant values, it means the attribution chain has gaps.
    /// </summary>
    private class F1_UntrackedLogging : ITestScenario
    {
        public string Id => "F1";
        public string Name => "UNTRACKED damage should be minimal (attribution chain working)";
        public string Category => "Consistency";

        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var data = CombatTracker.Instance.GetCurrentCombatData();
            data.TryGetValue("UNTRACKED", out var untracked);

            int untrackedDmg = untracked?.TotalDamage ?? 0;
            int untrackedDef = untracked?.TotalDefense ?? 0;

            // Calculate total across all sources for proportion
            int grandTotalDmg = 0;
            foreach (var (_, accum) in data)
                grandTotalDmg += accum.TotalDamage;

            // UNTRACKED should be < 10% of total damage (if there is any total)
            if (grandTotalDmg > 0)
            {
                double untrackedPct = (double)untrackedDmg / grandTotalDmg * 100;
                if (untrackedPct < 10)
                    result.Pass("UntrackedDamage%", $"{untrackedPct:F1}% ({untrackedDmg}/{grandTotalDmg})");
                else
                    result.Fail("UntrackedDamage%", "< 10%", $"{untrackedPct:F1}% ({untrackedDmg}/{grandTotalDmg})");
            }
            else
            {
                result.Pass("UntrackedDamage", "0 (no damage dealt yet)");
            }

            result.ExpectedValues["UntrackedDefense"] = "minimal";
            result.ActualValues["UntrackedDefense"] = untrackedDef.ToString();

            await Task.CompletedTask;
            return result;
        }
    }

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

    /// <summary>
    /// F5: Defense sum consistency — total defense across all sources should be non-negative
    /// (excluding SelfDamage entries), and each individual defense field should be ≥ 0.
    /// </summary>
    private class F5_DefenseSumConsistency : ITestScenario
    {
        public string Id => "F5";
        public string Name => "Defense fields non-negative for all sources (except SelfDamage)";
        public string Category => "Consistency";

        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            var data = CombatTracker.Instance.GetCurrentCombatData();
            var violations = new List<string>();

            foreach (var (sourceId, accum) in data)
            {
                if (accum.EffectiveBlock < 0)
                    violations.Add($"{sourceId}: EffectiveBlock = {accum.EffectiveBlock}");
                if (accum.ModifierBlock < 0)
                    violations.Add($"{sourceId}: ModifierBlock = {accum.ModifierBlock}");
                if (accum.MitigatedByDebuff < 0)
                    violations.Add($"{sourceId}: MitigatedByDebuff = {accum.MitigatedByDebuff}");
                if (accum.MitigatedByBuff < 0)
                    violations.Add($"{sourceId}: MitigatedByBuff = {accum.MitigatedByBuff}");
                if (accum.MitigatedByStrReduction < 0)
                    violations.Add($"{sourceId}: MitigatedByStrReduction = {accum.MitigatedByStrReduction}");
                if (accum.SelfDamage < 0)
                    violations.Add($"{sourceId}: SelfDamage = {accum.SelfDamage} (should be ≥ 0)");
            }

            if (violations.Count == 0)
                result.Pass("AllDefenseFields", $"{data.Count} sources checked, all non-negative");
            else
                result.Fail("DefenseFieldViolations",
                    "0 violations",
                    $"{violations.Count}: {string.Join("; ", violations)}");

            await Task.CompletedTask;
            return result;
        }
    }
}
