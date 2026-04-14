using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §19.4 — Round 10 batch 1: closes the **0% potion coverage** gap.
///
/// Each test uses the new <see cref="TestContext.UsePotion{T}"/> helper which
/// procures a potion into the player's first open slot then immediately drives
/// it through OnUseWrapper, so PotionContextPatch.BeforePotionUse fires
/// SetActivePotion before any contribution event is recorded.
///
/// The test set deliberately starts with the simplest contribution shapes
/// (DirectDamage / EffectiveBlock / EnergyGained / HpHealed) before chaining
/// into the §3 modifier paths (Strength + FlexPotion → ModifierDamage on a
/// Strike played afterwards).
/// </summary>
public static class Catalog_PotionTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_POTION_FireDirectDamage(),
        new CAT_POTION_BlockEffectiveBlock(),
        new CAT_POTION_EnergyGained(),
        new CAT_POTION_BloodHpHealed(),
        new CAT_POTION_StrengthThenStrike(),
        new CAT_POTION_FlexThenStrike(),
        new CAT_POTION_WeakAppliedNoCrash(),
        new CAT_POTION_RegenHpHealed(),
    };

    // ── §1: FirePotion → DirectDamage ───────────────────────

    private class CAT_POTION_FireDirectDamage : ITestScenario
    {
        public string Id => "CAT-POTION-Fire";
        public string Name => "Catalog §19.4: FirePotion → DirectDamage=20 to FIRE_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ResetEnemyHp();
            ctx.TakeSnapshot();
            await ctx.UsePotion<FirePotion>(target: ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("FIRE_POTION", out var d);
            ctx.AssertEquals(result, "FIRE_POTION.DirectDamage", 20, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── §6: BlockPotion → EffectiveBlock ────────────────────

    private class CAT_POTION_BlockEffectiveBlock : ITestScenario
    {
        public string Id => "CAT-POTION-Block";
        public string Name => "Catalog §19.4: BlockPotion → EffectiveBlock=12 to BLOCK_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            ctx.TakeSnapshot();
            await ctx.UsePotion<BlockPotion>();
            // Force damage so block gets consumed → EffectiveBlock tracked
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("BLOCK_POTION", out var d);
            ctx.AssertEquals(result, "BLOCK_POTION.EffectiveBlock", 12, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ── §12: EnergyPotion → EnergyGained ────────────────────

    private class CAT_POTION_EnergyGained : ITestScenario
    {
        public string Id => "CAT-POTION-Energy";
        public string Name => "Catalog §19.4: EnergyPotion → EnergyGained=2 to ENERGY_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            await ctx.UsePotion<EnergyPotion>();

            var delta = ctx.GetDelta();
            delta.TryGetValue("ENERGY_POTION", out var d);
            ctx.AssertEquals(result, "ENERGY_POTION.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    // ── §14: BloodPotion → HpHealed ─────────────────────────

    private class CAT_POTION_BloodHpHealed : ITestScenario
    {
        public string Id => "CAT-POTION-Blood";
        public string Name => "Catalog §19.4: BloodPotion → HpHealed > 0 to BLOOD_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            // BloodPotion heals for 20% MaxHp. With the runner's 9999 MaxHp grant
            // this is ~2000 HP — but the player is already at max from the runner's
            // pre-heal, so the actual HP delta is 0 (Heal clamps to MaxHp). Force a
            // small wound first so the heal has somewhere to go.
            await CreatureCmd.Damage(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
                ctx.PlayerCreature, 50,
                MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,
                ctx.PlayerCreature, null);
            await Task.Delay(100);

            ctx.TakeSnapshot();
            await ctx.UsePotion<BloodPotion>();

            var delta = ctx.GetDelta();
            delta.TryGetValue("BLOOD_POTION", out var d);
            // BloodPotion heals 20% of MaxHp, clamped to the current wound.
            // Runner's MaxHp grant (9999) * 20% ≫ wound of 50, so HpHealed == 50 exactly.
            ctx.AssertEquals(result, "BLOOD_POTION.HpHealed", 50, d?.HpHealed ?? 0);
            return result;
        }
    }

    // ── §3: StrengthPotion → +Strength → ModifierDamage on next Strike

    private class CAT_POTION_StrengthThenStrike : ITestScenario
    {
        public string Id => "CAT-POTION-StrengthMod";
        public string Name => "Catalog §19.4: StrengthPotion(+2) → next Strike DirectDamage+2 attributed to STRENGTH_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Clean residual Strength from prior tests
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await ctx.ResetEnemyHp();
            await ctx.UsePotion<StrengthPotion>();
            // Snapshot AFTER potion so we only see the delta from Strike's modifier path.
            ctx.TakeSnapshot();

            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(strike, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("STRENGTH_POTION", out var d);
            ctx.AssertEquals(result, "STRENGTH_POTION.ModifierDamage", 2, d?.ModifierDamage ?? 0);
            // Cleanup
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            return result;
        }
    }

    private class CAT_POTION_FlexThenStrike : ITestScenario
    {
        public string Id => "CAT-POTION-FlexMod";
        public string Name => "Catalog §19.4: FlexPotion(+5 temp Str) → next Strike ModifierDamage=5 to FLEX_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };

            // Clean residual Strength from prior tests
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await ctx.ResetEnemyHp();
            await ctx.UsePotion<FlexPotion>();
            ctx.TakeSnapshot();

            var strike = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(strike, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("FLEX_POTION", out var d);
            ctx.AssertEquals(result, "FLEX_POTION.ModifierDamage", 5, d?.ModifierDamage ?? 0);
            // Cleanup
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            return result;
        }
    }

    // ── §7: WeakPotion → smoke test (no contribution expected, just no crash)

    private class CAT_POTION_WeakAppliedNoCrash : ITestScenario
    {
        public string Id => "CAT-POTION-Weak";
        public string Name => "Catalog §19.4: WeakPotion applies WeakPower to enemy without crash";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();

            ctx.TakeSnapshot();
            await ctx.UsePotion<WeakPotion>(target: enemy);

            // SPEC-WAIVER: WeakPotion's only contribution path is MitigatedByDebuff on a
            // subsequent enemy attack, which is enemy-intent dependent and non-deterministic
            // in this harness. Verify power application as a stand-in.
            var weak = enemy.GetPower<WeakPower>();
            int weakAmt = weak?.Amount ?? 0;
            ctx.AssertEquals(result, "WeakPower.Amount (from WeakPotion)", 3, weakAmt);
            result.ActualValues["WeakPower.Amount"] = weakAmt.ToString();
            return result;
        }
    }

    // ── §14: RegenPotion → applies RegenPower → HpHealed over time
    // We end the turn once so RegenPower ticks; the runner already wounded the
    // player to 50 HP earlier so there's room for healing. Smoke-level: just
    // verify a non-zero HpHealed gets credited to REGEN_POTION (which can fail
    // if RegenPower contributions aren't routed through active potion source).

    private class CAT_POTION_RegenHpHealed : ITestScenario
    {
        public string Id => "CAT-POTION-Regen";
        public string Name => "Catalog §19.4: RegenPotion → RegenPower → HpHealed credited to REGEN_POTION";
        public string Category => "Catalog_Potion";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<RegenPower>(ctx.PlayerCreature);

                // Open a wound so heal has somewhere to go.
                await CreatureCmd.Damage(
                    new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
                    ctx.PlayerCreature, 30,
                    MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,
                    ctx.PlayerCreature, null);
                await Task.Delay(100);

                await ctx.UsePotion<RegenPotion>();

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("REGEN_POTION", out var d);
                // RegenPotion applies RegenPower(5); a single EndTurn ticks once → heal 5,
                // clamped by the 30 HP wound opened above, so HpHealed == 5.
                ctx.AssertEquals(result, "REGEN_POTION.HpHealed", 5, d?.HpHealed ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<RegenPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
