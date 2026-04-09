using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §3 ModifierDamage (additive) — tests Strength-based ModifierDamage attribution
/// for card sources. Relic sources (Vajra, Girya, DataDisk, StrikeDummy, PenNib, IronClub)
/// are deferred since the fluent API cannot install relics into a combat.
/// </summary>
public static class Catalog_ModifierTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_Inflame_StrengthMod(),            // normal
        new CAT_Inflame_MultiHit_ScaledMod(),     // normal (multi-hit Twin Strike)
        new CAT_Inflame_BoundaryNoAttack(),       // boundary — apply +Str, no attack → 0 ModifierDamage
        new CAT_Strength_StackedMultipleSources(), // normal — two Inflames both contribute
    };

    /// <summary>Catalog §3: Inflame +2 Str, then Strike → Inflame.ModifierDamage=2.</summary>
    private class CAT_Inflame_StrengthMod : ITestScenario
    {
        public string Id => "CAT-MOD-Inflame";
        public string Name => "Catalog §3: Inflame +2 Str → Strike gains +2 ModifierDamage to Inflame";
        public string Category => "Catalog_Modifier";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);

            var inflame = await ctx.CreateCardInHand<Inflame>();
            await ctx.PlayCard(inflame);

            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("INFLAME", out var d);
            ctx.AssertEquals(result, "INFLAME.ModifierDamage", 2, d?.ModifierDamage ?? 0);

            await ctx.ApplyPower<StrengthPower>(ctx.PlayerCreature, -2);
            return result;
        }
    }

    /// <summary>Catalog §3: Inflame +2 Str, then TwinStrike (2 hits) → Inflame.ModifierDamage=4.</summary>
    private class CAT_Inflame_MultiHit_ScaledMod : ITestScenario
    {
        public string Id => "CAT-MOD-InflameMulti";
        public string Name => "Catalog §3: Inflame +2 Str × TwinStrike(2 hits) → ModifierDamage=4";
        public string Category => "Catalog_Modifier";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);

            var inflame = await ctx.CreateCardInHand<Inflame>();
            await ctx.PlayCard(inflame);

            var twin = await ctx.CreateCardInHand<TwinStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(twin, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("INFLAME", out var d);
            ctx.AssertEquals(result, "INFLAME.ModifierDamage", 4, d?.ModifierDamage ?? 0);

            await ctx.ApplyPower<StrengthPower>(ctx.PlayerCreature, -2);
            return result;
        }
    }

    /// <summary>Catalog §3 boundary: apply +Str but play no attack → no ModifierDamage recorded.</summary>
    private class CAT_Inflame_BoundaryNoAttack : ITestScenario
    {
        public string Id => "CAT-MOD-InflameNoAttack";
        public string Name => "Catalog §3 boundary: Inflame alone → 0 ModifierDamage";
        public string Category => "Catalog_Modifier";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);

            var inflame = await ctx.CreateCardInHand<Inflame>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(inflame);
            var delta = ctx.GetDelta();
            delta.TryGetValue("INFLAME", out var d);
            ctx.AssertEquals(result, "INFLAME.ModifierDamage", 0, d?.ModifierDamage ?? 0);

            await ctx.ApplyPower<StrengthPower>(ctx.PlayerCreature, -2);
            return result;
        }
    }

    /// <summary>Catalog §3: two Inflames (source stacking) → first-applier attribution.</summary>
    private class CAT_Strength_StackedMultipleSources : ITestScenario
    {
        public string Id => "CAT-MOD-InflameStack";
        public string Name => "Catalog §3: two Inflames → combined ModifierDamage distribution";
        public string Category => "Catalog_Modifier";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);

            await ctx.PlayCard(await ctx.CreateCardInHand<Inflame>());
            await ctx.PlayCard(await ctx.CreateCardInHand<Inflame>());
            // Player now has +4 Strength; source was Inflame both times (same ID merged).
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("INFLAME", out var d);
            // Two stacks of +2 Str × 1 hit = +4 modifier damage total → all to INFLAME
            ctx.AssertEquals(result, "INFLAME.ModifierDamage (stacked)", 4, d?.ModifierDamage ?? 0);

            await ctx.ApplyPower<StrengthPower>(ctx.PlayerCreature, -4);
            return result;
        }
    }
}
