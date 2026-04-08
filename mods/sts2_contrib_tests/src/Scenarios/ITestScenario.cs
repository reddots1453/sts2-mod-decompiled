namespace ContribTests.Scenarios;

/// <summary>
/// Interface for automated combat contribution test scenarios.
/// Each scenario sets up a specific game state, performs actions,
/// then verifies CombatTracker data against expected values.
/// </summary>
public interface ITestScenario
{
    /// <summary>PRD test case ID, e.g. "D1", "DEF-4c".</summary>
    string Id { get; }

    /// <summary>Human-readable description.</summary>
    string Name { get; }

    /// <summary>Category for grouping: "DirectDamage", "ModifierDamage", "Defense", etc.</summary>
    string Category { get; }

    /// <summary>
    /// Check whether this test can run in the current combat state.
    /// Return false if prerequisites aren't met (e.g., wrong character, no enemies).
    /// </summary>
    bool CanRun(TestContext ctx);

    /// <summary>
    /// Execute the test scenario. Use ctx to play cards, apply powers, simulate damage,
    /// then assert contribution data via ctx.AssertEquals / ctx.AssertGreaterThan.
    /// </summary>
    Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct);
}
