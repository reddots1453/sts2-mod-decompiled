using System.Diagnostics;
using ContribTests.Scenarios;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace ContribTests;

/// <summary>
/// Orchestrates execution of all test scenarios within an active combat.
/// </summary>
public sealed class TestRunner
{
    private static readonly List<ITestScenario> AllScenarios = BuildScenarioList();

    private static List<ITestScenario> BuildScenarioList()
    {
        var list = new List<ITestScenario>();

        // Phase 2: Core tests
        list.AddRange(DirectDamageTests.All);
        list.AddRange(ModifierDamageTests.All);
        list.AddRange(DefenseTests.All);

        // Phase 3: Expanded tests (indirect, consistency, etc.)
        list.AddRange(IndirectDamageTests.All);
        list.AddRange(ConsistencyTests.All);

        return list;
    }

    public async Task RunAllAsync(CancellationToken ct)
    {
        GD.Print("[ContribTest] ══════════════════════════════════════");
        GD.Print($"[ContribTest] Running {AllScenarios.Count} test scenarios...");
        GD.Print("[ContribTest] ══════════════════════════════════════");

        // Get combat state and player
        var combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null)
        {
            GD.PrintErr("[ContribTest] Cannot get CombatState.");
            return;
        }

        var player = LocalContext.GetMe(combatState);
        if (player == null)
        {
            GD.PrintErr("[ContribTest] Cannot get Player.");
            return;
        }

        var ctx = new TestContext(combatState, player);

        // Setup: give player unlimited energy
        await ctx.SetEnergy(999);

        // Setup: protect enemies from dying (999 Plating on each)
        foreach (var enemy in ctx.GetAllEnemies())
        {
            await PowerCmd.Apply<PlatingPower>(
                enemy, 999m, ctx.PlayerCreature, null, silent: true);
        }

        // Also give player plenty of HP to survive test damage
        await PowerCmd.Apply<PlatingPower>(
            ctx.PlayerCreature, 999m, ctx.PlayerCreature, null, silent: true);
        await PowerCmd.Apply<RegenPower>(
            ctx.PlayerCreature, 50m, ctx.PlayerCreature, null, silent: true);

        var report = new TestReport
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            TotalTests = AllScenarios.Count
        };

        var totalSw = Stopwatch.StartNew();

        foreach (var scenario in AllScenarios)
        {
            ct.ThrowIfCancellationRequested();

            // Check combat is still active
            if (!CombatManager.Instance.IsInProgress)
            {
                GD.Print($"[ContribTest] [SKIP] {scenario.Id} - {scenario.Name} (combat ended)");
                var skipResult = new TestResult
                {
                    ScenarioId = scenario.Id,
                    ScenarioName = scenario.Name,
                    Category = scenario.Category,
                    SkipReason = "Combat ended"
                };
                report.Results.Add(skipResult);
                report.Skipped++;
                continue;
            }

            // Check prerequisites
            if (!scenario.CanRun(ctx))
            {
                GD.Print($"[ContribTest] [SKIP] {scenario.Id} - {scenario.Name} (prerequisites not met)");
                var skipResult = new TestResult
                {
                    ScenarioId = scenario.Id,
                    ScenarioName = scenario.Name,
                    Category = scenario.Category,
                    SkipReason = "Prerequisites not met"
                };
                report.Results.Add(skipResult);
                report.Skipped++;
                continue;
            }

            // Run the scenario
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await scenario.RunAsync(ctx, ct);
                sw.Stop();
                result.DurationMs = sw.ElapsedMilliseconds;
                report.Results.Add(result);

                if (result.Passed)
                {
                    GD.Print($"[ContribTest] [PASS] {scenario.Id} - {scenario.Name} ({sw.ElapsedMilliseconds}ms)");
                    report.Passed++;
                }
                else
                {
                    GD.Print($"[ContribTest] [FAIL] {scenario.Id} - {scenario.Name}");
                    GD.Print($"[ContribTest]   {result.FailureReason}");
                    report.Failed++;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                GD.PrintErr($"[ContribTest] [ERROR] {scenario.Id} - {scenario.Name}: {ex.Message}");
                var errorResult = new TestResult
                {
                    ScenarioId = scenario.Id,
                    ScenarioName = scenario.Name,
                    Category = scenario.Category,
                    DurationMs = sw.ElapsedMilliseconds
                };
                errorResult.Fail("Exception", "none", ex.Message);
                report.Results.Add(errorResult);
                report.Failed++;
            }

            // Refresh energy between tests
            if (CombatManager.Instance.IsInProgress)
                await ctx.SetEnergy(999);
        }

        totalSw.Stop();

        // Print summary
        GD.Print("[ContribTest] ══════════════════════════════════════");
        GD.Print($"[ContribTest] Results: {report.Passed} passed, {report.Failed} failed, {report.Skipped} skipped ({totalSw.ElapsedMilliseconds}ms)");

        // Write JSON report
        try
        {
            var reportPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(ContribTestMod).Assembly.Location) ?? ".",
                "test_results.json");
            var json = report.ToJson();
            await System.IO.File.WriteAllTextAsync(reportPath, json, ct);
            GD.Print($"[ContribTest] Report saved to: {reportPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ContribTest] Failed to save report: {ex.Message}");
        }

        GD.Print("[ContribTest] ══════════════════════════════════════");
    }
}
