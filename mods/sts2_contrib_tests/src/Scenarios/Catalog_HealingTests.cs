using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §14 HpHealed — heal / +MaxHp sources.
/// We exercise the CombatTracker.OnHealingReceived API directly with fallback source IDs
/// because most in-catalog healing comes from relics (Pear/Mango/Strawberry/MealTicket/BloodVial)
/// that can't be installed at runtime via the fluent API, or Feed (requires kill).
/// This validates the attribution path for each source ID used in the catalog.
/// </summary>
public static class Catalog_HealingTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_HEAL_Pear(),           // normal: +MaxHp fallback to PEAR
        new CAT_HEAL_Strawberry(),     // normal
        new CAT_HEAL_Mango(),          // normal
        new CAT_HEAL_BloodVial(),      // normal
        new CAT_HEAL_MealTicket(),     // normal
        new CAT_HEAL_Feed_MaxHpGain(), // normal: direct GainMaxHp attributed to FEED
        new CAT_HEAL_Zero_Boundary(),  // boundary: OnHealingReceived(0) is a no-op
    };

    private abstract class HealFallbackBase : ITestScenario
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string SourceId { get; }
        public virtual int Amount => 3;
        public string Category => "Catalog_Healing";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            CombatTracker.Instance.OnHealingReceived(Amount, fallbackId: SourceId, fallbackType: "relic");
            await Task.Delay(50);
            var delta = ctx.GetDelta();
            delta.TryGetValue(SourceId, out var d);
            ctx.AssertEquals(result, $"{SourceId}.HpHealed", Amount, d?.HpHealed ?? 0);
            return result;
        }
    }

    private class CAT_HEAL_Pear : HealFallbackBase
    {
        public override string Id => "CAT-HEAL-Pear";
        public override string Name => "Catalog §14: Pear HpHealed attribution";
        public override string SourceId => "PEAR";
    }

    private class CAT_HEAL_Strawberry : HealFallbackBase
    {
        public override string Id => "CAT-HEAL-Strawberry";
        public override string Name => "Catalog §14: Strawberry HpHealed attribution";
        public override string SourceId => "STRAWBERRY";
    }

    private class CAT_HEAL_Mango : HealFallbackBase
    {
        public override string Id => "CAT-HEAL-Mango";
        public override string Name => "Catalog §14: Mango HpHealed attribution";
        public override string SourceId => "MANGO";
    }

    private class CAT_HEAL_BloodVial : HealFallbackBase
    {
        public override string Id => "CAT-HEAL-BloodVial";
        public override string Name => "Catalog §14: BloodVial HpHealed attribution";
        public override string SourceId => "BLOOD_VIAL";
        public override int Amount => 2;
    }

    private class CAT_HEAL_MealTicket : HealFallbackBase
    {
        public override string Id => "CAT-HEAL-MealTicket";
        public override string Name => "Catalog §14: MealTicket HpHealed attribution";
        public override string SourceId => "MEAL_TICKET";
        public override int Amount => 15;
    }

    /// <summary>Feed: directly call GainMaxHp so the MaxHpGainPatch fires and attributes healing.</summary>
    private class CAT_HEAL_Feed_MaxHpGain : ITestScenario
    {
        public string Id => "CAT-HEAL-FeedMaxHp";
        public string Name => "Catalog §14: GainMaxHp +3 → HpHealed=3 via MaxHpGainPatch";
        public string Category => "Catalog_Healing";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            await CreatureCmd.GainMaxHp(ctx.PlayerCreature, 3m);
            await Task.Delay(150);
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.HpHealed;
            ctx.AssertEquals(result, "Total.HpHealed (GainMaxHp)", 3, total);
            return result;
        }
    }

    /// <summary>Boundary: 0-amount healing → no tracking record emitted.</summary>
    private class CAT_HEAL_Zero_Boundary : ITestScenario
    {
        public string Id => "CAT-HEAL-Zero";
        public string Name => "Catalog §14 boundary: Heal(0) → 0 HpHealed";
        public string Category => "Catalog_Healing";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            CombatTracker.Instance.OnHealingReceived(0, fallbackId: "ZERO_TEST", fallbackType: "relic");
            await Task.Delay(50);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ZERO_TEST", out var d);
            ctx.AssertEquals(result, "ZERO_TEST.HpHealed", 0, d?.HpHealed ?? 0);
            return result;
        }
    }
}
