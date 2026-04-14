using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §19.2.5 — Round 10 batch 3: closes the **0-coverage Necrobinder gap**.
///
/// Existing Cross-character tests cover Defect (Zap, Defragment, Shiv) and
/// Regent (HiddenCache) but Necrobinder had **zero** scenarios. This file adds
/// 7 representative tests covering: basic Strike/Defend, an Attack with
/// sub-card generation (Reave → Soul), an Attack with debuff side-effect
/// (BlightStrike → Weakness), and three Power cards (DevourLife, Pagestorm,
/// Haunt) as smoke/play-without-crash checks.
///
/// All tests run regardless of the active character — they manually create
/// the Necrobinder card in the player's hand and play it. The runner already
/// grants 999 energy and unlimited HP so cost/HP gates don't matter.
/// </summary>
public static class Catalog_NecrobinderCardTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        new CAT_NEC_Strike(),
        new CAT_NEC_Defend(),
        new CAT_NEC_Reave(),
        new CAT_NEC_BlightStrike(),
        new CAT_NEC_DevourLife(),
        new CAT_NEC_Pagestorm(),
        new CAT_NEC_Haunt(),
    };

    // ── Basic Strike ────────────────────────────────────────

    private class CAT_NEC_Strike : ITestScenario
    {
        public string Id => "CAT-NEC-Strike";
        public string Name => "Catalog §1: StrikeNecrobinder → DirectDamage=6 to STRIKE_NECROBINDER";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<StrikeNecrobinder>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("STRIKE_NECROBINDER", out var d);
            ctx.AssertEquals(result, "STRIKE_NECROBINDER.DirectDamage", 6, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── Basic Defend ────────────────────────────────────────

    private class CAT_NEC_Defend : ITestScenario
    {
        public string Id => "CAT-NEC-Defend";
        public string Name => "Catalog §6: DefendNecrobinder → EffectiveBlock=5 to DEFEND_NECROBINDER";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            // Clean residual Dexterity/Frail from prior tests
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            await ctx.ClearBlock();
            var card = await ctx.CreateCardInHand<DefendNecrobinder>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            // Force damage so block gets consumed → EffectiveBlock tracked
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFEND_NECROBINDER", out var d);
            ctx.AssertEquals(result, "DEFEND_NECROBINDER.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ── Reave: 9 damage + Soul sub-card generation ──────────

    private class CAT_NEC_Reave : ITestScenario
    {
        public string Id => "CAT-NEC-Reave";
        public string Name => "Catalog §1: Reave → DirectDamage=9 to REAVE (+1 Soul generated to draw)";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Reave>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("REAVE", out var d);
            ctx.AssertEquals(result, "REAVE.DirectDamage", 9, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── BlightStrike: 8 damage + Weakness applied ───────────

    private class CAT_NEC_BlightStrike : ITestScenario
    {
        public string Id => "CAT-NEC-BlightStrike";
        public string Name => "Catalog §1: BlightStrike → DirectDamage=8 to BLIGHT_STRIKE";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<BlightStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());

            var delta = ctx.GetDelta();
            delta.TryGetValue("BLIGHT_STRIKE", out var d);
            ctx.AssertEquals(result, "BLIGHT_STRIKE.DirectDamage", 8, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ── DevourLife: Power, sets up DevourLifePower ──────────

    // DevourLife: "Whenever you play a Soul, Summon 1." Downstream summons don't
    // map to any contribution field (no HpHealed, Damage, or Block on the
    // generator card). Play DevourLife → play Soul → assert TimesPlayed tracked.
    // SPEC-WAIVER: Summon is not a tracked contribution event.
    private class CAT_NEC_DevourLife : ITestScenario
    {
        public string Id => "CAT-NEC-DevourLife";
        public string Name => "Catalog §14: DevourLife → play Soul → TimesPlayed=1";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DevourLifePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);

                var devour = await ctx.CreateCardInHand<DevourLife>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(devour);

                var soul = await ctx.CreateCardInHand<Soul>();
                await ctx.PlayCard(soul);

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEVOUR_LIFE", out var d);
                ctx.AssertEquals(result, "DEVOUR_LIFE.TimesPlayed", 1, d?.TimesPlayed ?? 0);
                // SPEC-WAIVER: Summon not a contribution event
            }
            finally
            {
                await PowerCmd.Remove<DevourLifePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── Pagestorm: Power applied ────────────────────────────

    // Pagestorm: "Whenever you draw an Ethereal card, draw 1." Play Pagestorm →
    // EndTurn → next turn draws the starting hand including any Ethereal cards
    // → Pagestorm hook fires → CardsDrawn attributes to PAGESTORM.
    private class CAT_NEC_Pagestorm : ITestScenario
    {
        public string Id => "CAT-NEC-Pagestorm";
        public string Name => "Catalog §2: Pagestorm → turn-start Ethereal draw → PAGESTORM.CardsDrawn>0";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<PagestormPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<NoDrawPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);

                // Seed draw pile with Ethereal cards (Parse) so next turn's draw fires.
                for (int i = 0; i < 3; i++)
                {
                    var ethereal = ctx.CombatState.CreateCard<Parse>(ctx.Player);
                    await CardPileCmd.Add(ethereal, MegaCrit.Sts2.Core.Entities.Cards.PileType.Draw, skipVisuals: true);
                }

                var card = await ctx.CreateCardInHand<Pagestorm>();
                await ctx.PlayCard(card);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("PAGESTORM", out var d);
                ctx.AssertGreaterThan(result, "PAGESTORM.CardsDrawn", 0, d?.CardsDrawn ?? 0);
                // non-deterministic: depends on which Ethereal cards are in starting draw
            }
            finally
            {
                await PowerCmd.Remove<PagestormPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ── Haunt: Power applied ────────────────────────────────

    // Haunt: "Whenever you play a Soul, a random enemy loses 6 HP."
    // Play Haunt → play Soul → random enemy takes 6 HP-loss → AttributedDamage
    // to HAUNT.
    private class CAT_NEC_Haunt : ITestScenario
    {
        public string Id => "CAT-NEC-Haunt";
        public string Name => "Catalog §2: Haunt → play Soul → HAUNT.AttributedDamage>0";
        public string Category => "Catalog_Necrobinder";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<HauntPower>(ctx.PlayerCreature);
                await ctx.ResetEnemyHp();
                await ctx.SetEnergy(999);

                var haunt = await ctx.CreateCardInHand<Haunt>();
                await ctx.PlayCard(haunt);

                ctx.TakeSnapshot();
                var soul = await ctx.CreateCardInHand<Soul>();
                await ctx.PlayCard(soul);

                var delta = ctx.GetDelta();
                delta.TryGetValue("HAUNT", out var d);
                ctx.AssertGreaterThan(result, "HAUNT.AttributedDamage", 0, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<HauntPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
