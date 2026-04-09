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
    // Tests that have already passed and should be skipped to save time.
    // Remove an ID from this set to re-run it.
    private static readonly HashSet<string> PassedSkipList = new()
    {
        "D1", "D2", "D3", "D4", "D-Block", "D6", // DirectDamage — passed
        "M1", "M2", "M3", "M5", "M6",     // ModifierDamage — passed
        "DEF-1a", "DEF-4a", "DEF-4c",      // Defense block — passed
        "I1", "I2", "I5",                   // IndirectDamage — passed
        "P1", "P5",                         // SourcePriority — passed
        "I3", "S4",                          // CrossCharacter — passed
        "DEF-2d",                           // Defense Colossus — passed
        "DEF-2a", "DEF-3a", "DEF-5a", "DEF-5c", // Defense mitigation — passed
        "NEW-1b",                           // NewFeature free energy — passed
        "NEW-1a", "NEW-1c", "NEW-3a",      // NewFeature — passed
        "F1", "F4", "F5",                  // Consistency — passed
    };

    private static readonly List<ITestScenario> AllScenarios = BuildScenarioList();

    private static List<ITestScenario> BuildScenarioList()
    {
        var list = new List<ITestScenario>();

        // Phase 2: Core tests
        list.AddRange(DirectDamageTests.All);
        list.AddRange(ModifierDamageTests.All);
        list.AddRange(DefenseTests.All);

        // Phase 3: Expanded tests (indirect, source priority, cross-character)
        list.AddRange(IndirectDamageTests.All);
        list.AddRange(SourcePriorityTests.All);
        list.AddRange(CrossCharacterTests.All);

        // Phase 4: NEW feature tests (free energy/stars, max HP healing)
        list.AddRange(NewFeatureTests.All);

        // Phase 5: Consistency checks (run last, validate accumulated data)
        list.AddRange(ConsistencyTests.All);

        // Phase 6: Catalog-driven tests (per CONTRIBUTION_CATALOG.md sections).
        // Each Catalog_* file targets one catalog section with normal + boundary cases.
        list.AddRange(Catalog_AttackCardTests.All);
        list.AddRange(Catalog_PowerIndirectTests.All);
        list.AddRange(Catalog_ModifierTests.All);
        list.AddRange(Catalog_DefenseBlockTests.All);
        list.AddRange(Catalog_DefenseDebuffTests.All);
        list.AddRange(Catalog_DefenseStrReductionTests.All);
        list.AddRange(Catalog_SelfDamageTests.All);
        list.AddRange(Catalog_DrawTests.All);
        list.AddRange(Catalog_EnergyTests.All);
        list.AddRange(Catalog_HealingTests.All);
        list.AddRange(Catalog_InteractionTests.All);

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

        // Setup: protect enemies from dying — give massive HP directly
        foreach (var enemy in ctx.GetAllEnemies())
        {
            await CreatureCmd.GainMaxHp(enemy, 9999m);
            await CreatureCmd.Heal(enemy, 9999m, playAnim: false);
        }

        // Also give player plenty of HP to survive test damage
        await CreatureCmd.GainMaxHp(ctx.PlayerCreature, 9999m);
        await CreatureCmd.Heal(ctx.PlayerCreature, 9999m, playAnim: false);
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

            // Skip already-passed tests
            if (PassedSkipList.Contains(scenario.Id))
            {
                GD.Print($"[ContribTest] [SKIP] {scenario.Id} - {scenario.Name} (already passed)");
                report.Skipped++;
                continue;
            }

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
