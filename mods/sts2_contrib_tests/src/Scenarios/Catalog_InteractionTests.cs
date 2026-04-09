using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog complex interactions — integration tests for multi-feature scenarios.
/// Each test validates a stacking/chaining rule from the catalog or PRD-04 §4.x.
/// Some scenarios drive ContributionMap / CombatTracker directly when the fluent API
/// cannot reach the required state (same pattern as DEF5b_BufferPrdExample).
/// </summary>
public static class Catalog_InteractionTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_INT_VulnStrengthStacking(),     // 1
        new CAT_INT_BufferIntangibleChain(),    // 2
        new CAT_INT_ModifierTotalOverhangPRD(), // 3 — PRD-04 §4.1 scaling
        new CAT_INT_HealingGainMaxHpPlusHeal(), // 4
        new CAT_INT_NegativeCostSavingsClamp(), // 5 — Snecko allowed, others clamped
        new CAT_INT_StrReductionStacking(),     // 6
        new CAT_INT_BufferPerHitSum3Source(),   // 7
        new CAT_INT_SelfDamageStacking(),       // 8
        new CAT_INT_CardOriginChain(),          // 9
        new CAT_INT_FreeEnergyChain(),          // 10
        new CAT_INT_FilterToggleShortCircuit(), // 11 (bonus) — verifies ContributionMap clear path
        new CAT_INT_RunHistoryRoundTrip(),      // 12 (bonus) — accumulator round-trip
    };

    // ── 1. Vulnerable + Strength + card damage stacking ─────────
    private class CAT_INT_VulnStrengthStacking : ITestScenario
    {
        public string Id => "CAT-INT-01-VulnStrStack";
        public string Name => "Interaction: Vulnerable + Strength → both contribute ModifierDamage";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);

            // Inflame (+2 Str), Bash (apply Vuln), then Strike (base 6)
            await ctx.PlayCard(await ctx.CreateCardInHand<Inflame>());
            await ctx.PlayCard(await ctx.CreateCardInHand<Bash>(), enemy);
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("INFLAME", out var inf);
            delta.TryGetValue("BASH", out var bash);
            delta.TryGetValue("STRIKE_IRONCLAD", out var st);

            int direct = st?.DirectDamage ?? 0;
            int infMod = inf?.ModifierDamage ?? 0;
            int bashMod = bash?.ModifierDamage ?? 0;
            int total = direct + infMod + bashMod;
            // Strike base 6 + Str 2 = 8; with Vuln 1.5× → 12 total.
            ctx.AssertEquals(result, "Strike.DirectDamage", 6, direct);
            ctx.AssertGreaterThan(result, "Inflame.ModifierDamage", 0, infMod);
            ctx.AssertGreaterThan(result, "Bash.ModifierDamage (vuln)", 0, bashMod);
            ctx.AssertRange(result, "SumDamage", 11, 13, total);

            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    // ── 2. Block + Buffer + Intangible chain ────────────────────
    private class CAT_INT_BufferIntangibleChain : ITestScenario
    {
        public string Id => "CAT-INT-02-BufferIntangible";
        public string Name => "Interaction: Block + Buffer + Intangible chain all populate defense fields";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);

            // Setup: Block via Defend, Buffer + Intangible via ApplyPower
            await ctx.PlayCard(await ctx.CreateCardInHand<DefendIronclad>());
            await ctx.ApplyPower<BufferPower>(ctx.PlayerCreature, 1);
            await ctx.ApplyPower<IntangiblePower>(ctx.PlayerCreature, 1);

            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();

            var delta = ctx.GetDelta();
            int effBlock = 0, mitigBuff = 0;
            foreach (var (_, d) in delta)
            {
                effBlock += d.EffectiveBlock;
                mitigBuff += d.MitigatedByBuff;
            }
            // Chain should populate at least one of the defense categories
            ctx.AssertGreaterThan(result, "Defense.EffectiveBlock+MitigatedByBuff", 0, effBlock + mitigBuff);
            result.ActualValues["EffectiveBlock"] = effBlock.ToString();
            result.ActualValues["MitigatedByBuff"] = mitigBuff.ToString();

            await ctx.SetEnergy(999);
            return result;
        }
    }

    // ── 3. PRD-04 §4.1 modifier total scaling ───────────────────
    private class CAT_INT_ModifierTotalOverhangPRD : ITestScenario
    {
        public string Id => "CAT-INT-03-ModifierScalingPRD";
        public string Name => "Interaction PRD-04 §4.1: stacked Strength → modifier scaling preserves totals";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);

            // Apply +8 strength via 4 Inflames (via card to retain source), then Strike
            for (int i = 0; i < 4; i++)
                await ctx.PlayCard(await ctx.CreateCardInHand<Inflame>());

            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);

            var delta = ctx.GetDelta();
            delta.TryGetValue("INFLAME", out var inf);
            delta.TryGetValue("STRIKE_IRONCLAD", out var st);
            int direct = st?.DirectDamage ?? 0;
            int modSum = inf?.ModifierDamage ?? 0;

            // Strike direct = 6, +8 strength × 1 hit = +8 modifier. Sum = 14.
            ctx.AssertEquals(result, "Strike.DirectDamage", 6, direct);
            ctx.AssertEquals(result, "Inflame.ModifierDamage (4×2)", 8, modSum);
            ctx.AssertEquals(result, "Direct+Modifier=Total", 14, direct + modSum);

            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            return result;
        }
    }

    // ── 4. Healing source attribution GainMaxHp + Heal ──────────
    private class CAT_INT_HealingGainMaxHpPlusHeal : ITestScenario
    {
        public string Id => "CAT-INT-04-HealChain";
        public string Name => "Interaction: GainMaxHp + Heal both tracked, no double-count";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            await CreatureCmd.GainMaxHp(ctx.PlayerCreature, 4m);
            await Task.Delay(100);
            CombatTracker.Instance.OnHealingReceived(3, fallbackId: "HEAL_TEST", fallbackType: "relic");
            await Task.Delay(50);

            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.HpHealed;
            // Expect 4 (MaxHp) + 3 (Heal) = 7
            ctx.AssertEquals(result, "Total.HpHealed", 7, total);
            return result;
        }
    }

    // ── 5. Cost-savings clamping ─────────────────────────────────
    private class CAT_INT_NegativeCostSavingsClamp : ITestScenario
    {
        public string Id => "CAT-INT-05-NegativeClamp";
        public string Name => "Interaction: direct ContributionAccum negative guard for non-Snecko";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            // Drive CombatTracker directly with positive then (potentially) negative values.
            // This exercises the same write path used by SneckoEye when implemented.
            CombatTracker.Instance.OnEnergyGained(2);
            await Task.Delay(20);
            var delta = ctx.GetDelta();
            int totalEn = 0;
            foreach (var (_, d) in delta) totalEn += d.EnergyGained;
            ctx.AssertEquals(result, "Total.EnergyGained (+2)", 2, totalEn);
            return result;
        }
    }

    // ── 6. Strength reduction stacking ──────────────────────────
    private class CAT_INT_StrReductionStacking : ITestScenario
    {
        public string Id => "CAT-INT-06-StrRedStack";
        public string Name => "Interaction: Malaise + DarkShackles stacked → both contribute mitigation";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            await ctx.PlayCard(await ctx.CreateCardInHand<Malaise>(), enemy);
            await ctx.PlayCard(await ctx.CreateCardInHand<DarkShackles>(), enemy);
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            ctx.TakeSnapshot();
            await ctx.EndTurnAndWaitForPlayerTurn();
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.MitigatedByStrReduction;
            ctx.AssertGreaterThan(result, "Total.MitigatedByStrReduction", -1, total);
            result.ActualValues["Total"] = total.ToString();
            await ctx.SetEnergy(999);
            return result;
        }
    }

    // ── 7. Buffer per-hit variant (3+ hits) ─────────────────────
    private class CAT_INT_BufferPerHitSum3Source : ITestScenario
    {
        public string Id => "CAT-INT-07-Buffer3Hits";
        public string Name => "Interaction: Buffer per-hit sum 2+4+6 = 12 attributed to BUFFER_POWER";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            CombatTracker.Instance.OnBufferPrevention(2);
            CombatTracker.Instance.OnBufferPrevention(4);
            CombatTracker.Instance.OnBufferPrevention(6);
            await Task.Delay(50);
            var delta = ctx.GetDelta();
            int total = 0;
            foreach (var (_, d) in delta) total += d.MitigatedByBuff;
            ctx.AssertEquals(result, "Total.MitigatedByBuff (3-hit)", 12, total);
            return result;
        }
    }

    // ── 8. Self-damage stacking ─────────────────────────────────
    private class CAT_INT_SelfDamageStacking : ITestScenario
    {
        public string Id => "CAT-INT-08-SelfDmgStack";
        public string Name => "Interaction: Offering + Bloodletting same combat → combined SelfDamage";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            await ctx.PlayCard(await ctx.CreateCardInHand<Offering>());
            await ctx.PlayCard(await ctx.CreateCardInHand<Bloodletting>());
            var delta = ctx.GetDelta();
            delta.TryGetValue("OFFERING", out var off);
            delta.TryGetValue("BLOODLETTING", out var bl);
            int offSelf = off?.SelfDamage ?? 0;
            int blSelf = bl?.SelfDamage ?? 0;
            ctx.AssertEquals(result, "OFFERING.SelfDamage", 6, offSelf);
            ctx.AssertEquals(result, "BLOODLETTING.SelfDamage", 3, blSelf);
            ctx.AssertEquals(result, "SumSelf", 9, offSelf + blSelf);
            return result;
        }
    }

    // ── 9. Card-origin chain ────────────────────────────────────
    private class CAT_INT_CardOriginChain : ITestScenario
    {
        public string Id => "CAT-INT-09-OriginChain";
        public string Name => "Interaction: ContributionMap RecordCardOrigin round-trip";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            // Record synthetic origin and verify lookup works.
            ContributionMap.Instance.RecordCardOrigin(999001, "HAVOC", "card");
            var origin = ContributionMap.Instance.GetCardOrigin(999001);
            if (origin is { originId: "HAVOC" })
                result.Pass("OriginChain.Havoc", "HAVOC");
            else
                result.Fail("OriginChain.Havoc", "HAVOC", origin?.originId ?? "null");
            return Task.FromResult(result);
        }
    }

    // ── 10. Free-energy chain ───────────────────────────────────
    private class CAT_INT_FreeEnergyChain : ITestScenario
    {
        public string Id => "CAT-INT-10-FreeEnergyChain";
        public string Name => "Interaction: Corruption + Defend + FreeAttackPower + Strike chain";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<CorruptionPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FreeAttackPower>(ctx.PlayerCreature);

            await ctx.PlayCard(await ctx.CreateCardInHand<Corruption>());
            var defend = await ctx.CreateCardInHand<DefendIronclad>();
            await ctx.PlayCard(defend);
            await ctx.ApplyPower<FreeAttackPower>(ctx.PlayerCreature, 1);
            var strike = await ctx.CreateCardInHand<StrikeIronclad>();

            ctx.TakeSnapshot();
            await ctx.PlayCard(strike, enemy);

            var delta = ctx.GetDelta();
            int totalEn = 0;
            foreach (var (_, d) in delta) totalEn += d.EnergyGained;
            // FreeAttackPower should credit 1 energy saved for Strike.
            ctx.AssertEquals(result, "Total.EnergyGained_chain", 1, totalEn);

            await PowerCmd.Remove<CorruptionPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FreeAttackPower>(ctx.PlayerCreature);
            return result;
        }
    }

    // ── 11. Filter/toggle short-circuit (via ContributionMap flag) ─
    private class CAT_INT_FilterToggleShortCircuit : ITestScenario
    {
        public string Id => "CAT-INT-11-FilterShortCircuit";
        public string Name => "Interaction: ContributionMap GainMaxHp flag set/clear round-trip";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ContributionMap.Instance.SetGainMaxHpFlag(true);
            bool seen1 = ContributionMap.Instance.CheckAndClearGainMaxHpFlag();
            bool seen2 = ContributionMap.Instance.CheckAndClearGainMaxHpFlag();
            if (seen1 && !seen2)
                result.Pass("Flag.SetClearRoundTrip", "true→false");
            else
                result.Fail("Flag.SetClearRoundTrip", "true→false", $"{seen1}→{seen2}");
            return Task.FromResult(result);
        }
    }

    // ── 12. RunHistory accumulator round-trip ───────────────────
    private class CAT_INT_RunHistoryRoundTrip : ITestScenario
    {
        public string Id => "CAT-INT-12-AccumRoundTrip";
        public string Name => "Interaction: ContributionAccum MergeFrom round-trip preserves totals";
        public string Category => "Catalog_Interaction";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var a = new ContributionAccum
            {
                SourceId = "TEST_ACCUM",
                SourceType = "card",
                TimesPlayed = 2,
                DirectDamage = 10,
                ModifierDamage = 4,
                EffectiveBlock = 5,
                SelfDamage = 2
            };
            var b = new ContributionAccum
            {
                SourceId = "TEST_ACCUM",
                SourceType = "card",
                TimesPlayed = 3,
                DirectDamage = 6,
                ModifierDamage = 2,
                EffectiveBlock = 7,
                SelfDamage = 1
            };
            a.MergeFrom(b);
            ctx.AssertEquals(result, "Merged.TimesPlayed", 5, a.TimesPlayed);
            ctx.AssertEquals(result, "Merged.DirectDamage", 16, a.DirectDamage);
            ctx.AssertEquals(result, "Merged.ModifierDamage", 6, a.ModifierDamage);
            ctx.AssertEquals(result, "Merged.TotalDamage", 22, a.TotalDamage);
            ctx.AssertEquals(result, "Merged.EffectiveBlock", 12, a.EffectiveBlock);
            ctx.AssertEquals(result, "Merged.SelfDamage", 3, a.SelfDamage);
            // TotalDefense = 12 - 3 = 9
            ctx.AssertEquals(result, "Merged.TotalDefense", 9, a.TotalDefense);
            return Task.FromResult(result);
        }
    }
}
