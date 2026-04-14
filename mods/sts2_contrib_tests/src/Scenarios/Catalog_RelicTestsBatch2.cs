using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §19.3 — Round 10 batch 2: writes tests for the relics that **already
/// have patches** (so contributions route correctly) but lacked any
/// `Catalog_RelicTests.cs` coverage. Splits into:
///
/// - **食物类 (§14)**: BurningBlood / BlackBlood / MeatOnTheBone fire heal
///   inside `AfterCombatVictory(Early)` so we manually invoke the hook after
///   wounding the player so the heal has somewhere to go.
///
/// - **格挡类 (§6)**: Orichalcum / RippleBasin / CloakClasp / ToughBandages /
///   CaptainsWheel / HornCleat. The first two and CloakClasp drive
///   `BeforeTurnEnd` directly. ToughBandages drives `CardCmd.Discard`.
///   CaptainsWheel/HornCleat reach into `CombatState.RoundNumber` to satisfy
///   the round guard before invoking `AfterBlockCleared`.
///
/// All contributions should route to the relic id under §19.3.5/19.3.6.
/// </summary>
public static class Catalog_RelicTestsBatch2
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // §14 food / heal
        new CAT_REL_BurningBlood_Heal(),
        new CAT_REL_BlackBlood_Heal(),
        new CAT_REL_MeatOnTheBone_Heal(),

        // §6 block
        new CAT_REL_Orichalcum_Block(),
        // RippleBasin moved to TestRunner head (must run as combat's first action)
        new CAT_REL_CloakClasp_Block(),
        new CAT_REL_ToughBandages_Block(),
        new CAT_REL_CaptainsWheel_Block(),
        new CAT_REL_HornCleat_Block(),
    };

    // ── Helper: wound the player so heal has room to land ──

    private static async Task WoundPlayer(TestContext ctx, int amount)
    {
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            ctx.PlayerCreature, amount,
            ValueProp.Move,
            ctx.PlayerCreature, null);
        await Task.Delay(80);
    }

    private static async Task ClearPlayerBlock(TestContext ctx)
    {
        await ctx.ClearBlock();
    }

    // ── §14 BurningBlood AfterCombatVictory ────────────────

    private class CAT_REL_BurningBlood_Heal : ITestScenario
    {
        public string Id => "CAT-REL-BurningBlood";
        public string Name => "Catalog §14: BurningBlood AfterCombatVictory → HpHealed=6 to BURNING_BLOOD";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            BurningBlood? relic = null;
            try
            {
                await WoundPlayer(ctx, 30);
                relic = await ctx.ObtainRelic<BurningBlood>();
                ctx.TakeSnapshot();

                var room = ctx.GetCurrentRoom() as CombatRoom;
                if (room == null) { result.Fail("Prereq", "CombatRoom", "null"); return result; }
                await ctx.TriggerRelicHook(() => relic.AfterCombatVictory(room));

                var delta = ctx.GetDelta();
                delta.TryGetValue("BURNING_BLOOD", out var d);
                ctx.AssertEquals(result, "BURNING_BLOOD.HpHealed", 6, d?.HpHealed ?? 0);
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    private class CAT_REL_BlackBlood_Heal : ITestScenario
    {
        public string Id => "CAT-REL-BlackBlood";
        public string Name => "Catalog §14: BlackBlood AfterCombatVictory → HpHealed=12 to BLACK_BLOOD";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            BlackBlood? relic = null;
            try
            {
                await WoundPlayer(ctx, 30);
                relic = await ctx.ObtainRelic<BlackBlood>();
                ctx.TakeSnapshot();
                var room = ctx.GetCurrentRoom() as CombatRoom;
                if (room == null) { result.Fail("Prereq", "CombatRoom", "null"); return result; }
                await ctx.TriggerRelicHook(() => relic.AfterCombatVictory(room));

                var delta = ctx.GetDelta();
                delta.TryGetValue("BLACK_BLOOD", out var d);
                ctx.AssertEquals(result, "BLACK_BLOOD.HpHealed", 12, d?.HpHealed ?? 0);
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    private class CAT_REL_MeatOnTheBone_Heal : ITestScenario
    {
        public string Id => "CAT-REL-MeatOnTheBone";
        public string Name => "Catalog §14: MeatOnTheBone (HP<50%) AfterCombatVictoryEarly → HpHealed=12";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            MeatOnTheBone? relic = null;
            try
            {
                // Need CurrentHp ≤ 50% MaxHp. Player has 9999+ from runner; wound heavily.
                await WoundPlayer(ctx, ctx.PlayerCreature.MaxHp / 2 + 50);

                relic = await ctx.ObtainRelic<MeatOnTheBone>();
                ctx.TakeSnapshot();
                var room = ctx.GetCurrentRoom() as CombatRoom;
                if (room == null) { result.Fail("Prereq", "CombatRoom", "null"); return result; }
                await ctx.TriggerRelicHook(() => relic.AfterCombatVictoryEarly(room));

                var delta = ctx.GetDelta();
                delta.TryGetValue("MEAT_ON_THE_BONE", out var d);
                ctx.AssertEquals(result, "MEAT_ON_THE_BONE.HpHealed", 12, d?.HpHealed ?? 0);
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    // ── §6 Block: Orichalcum BeforeTurnEnd ────────────────────

    private class CAT_REL_Orichalcum_Block : ITestScenario
    {
        public string Id => "CAT-REL-Orichalcum";
        public string Name => "Catalog §6: Orichalcum BeforeTurnEnd (no block) → EffectiveBlock=6 to ORICHALCUM";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            Orichalcum? relic = null;
            try
            {
                await ClearPlayerBlock(ctx);
                relic = await ctx.ObtainRelic<Orichalcum>();
                ctx.TakeSnapshot();

                var choiceCtx = new BlockingPlayerChoiceContext();
                var side = ctx.PlayerCreature.Side;
                await ctx.TriggerRelicHook(() => relic.BeforeTurnEndVeryEarly(choiceCtx, side));
                await ctx.TriggerRelicHook(() => relic.BeforeTurnEnd(choiceCtx, side));
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("ORICHALCUM", out var d);
                ctx.AssertEquals(result, "ORICHALCUM.EffectiveBlock", 6, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    // ── §6 Block: RippleBasin BeforeTurnEnd ───────────────────

    internal class CAT_REL_RippleBasin_Block : ITestScenario
    {
        public string Id => "CAT-REL-RippleBasin";
        public string Name => "Catalog §6: RippleBasin BeforeTurnEnd → EffectiveBlock=4 to RIPPLE_BASIN";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            RippleBasin? relic = null;
            try
            {
                await ClearPlayerBlock(ctx);
                relic = await ctx.ObtainRelic<RippleBasin>();
                ctx.TakeSnapshot();
                var choiceCtx = new BlockingPlayerChoiceContext();
                await ctx.TriggerRelicHook(() => relic.BeforeTurnEnd(choiceCtx, ctx.PlayerCreature.Side));
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("RIPPLE_BASIN", out var d);
                int got = d?.EffectiveBlock ?? 0;
                if (got >= 1)
                    ctx.AssertEquals(result, "RIPPLE_BASIN.EffectiveBlock", 4, got);
                else
                    result.Fail("RIPPLE_BASIN.EffectiveBlock", "≥1", "0 — guard not satisfied");
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    // ── §6 Block: CloakClasp BeforeTurnEnd (per card in hand) ──

    private class CAT_REL_CloakClasp_Block : ITestScenario
    {
        public string Id => "CAT-REL-CloakClasp";
        public string Name => "Catalog §6: CloakClasp BeforeTurnEnd → EffectiveBlock=N(hand) to CLOAK_CLASP";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            CloakClasp? relic = null;
            try
            {
                // Stuff a few cards into hand so CloakClasp has something to count.
                await ctx.CreateCardInHand<DefendIronclad>();
                await ctx.CreateCardInHand<DefendIronclad>();
                await ctx.CreateCardInHand<DefendIronclad>();

                await ClearPlayerBlock(ctx);
                int handSize = PileType.Hand.GetPile(ctx.Player).Cards.Count;
                relic = await ctx.ObtainRelic<CloakClasp>();
                ctx.TakeSnapshot();
                await ctx.TriggerRelicHook(() =>
                    relic.BeforeTurnEnd(new BlockingPlayerChoiceContext(), ctx.PlayerCreature.Side));
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("CLOAK_CLASP", out var d);
                ctx.AssertEquals(result, "CLOAK_CLASP.EffectiveBlock (=hand size)", handSize, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    // ── §6 Block: ToughBandages AfterCardDiscarded ────────────

    private class CAT_REL_ToughBandages_Block : ITestScenario
    {
        public string Id => "CAT-REL-ToughBandages";
        public string Name => "Catalog §6: ToughBandages AfterCardDiscarded → EffectiveBlock=3 to TOUGH_BANDAGES";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ToughBandages? relic = null;
            try
            {
                await ClearPlayerBlock(ctx);
                relic = await ctx.ObtainRelic<ToughBandages>();
                var card = await ctx.CreateCardInHand<DefendIronclad>();
                ctx.TakeSnapshot();

                await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
                await Task.Delay(150);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("TOUGH_BANDAGES", out var d);
                ctx.AssertEquals(result, "TOUGH_BANDAGES.EffectiveBlock", 3, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    // ── §6 Block: CaptainsWheel AfterBlockCleared (round=3) ───

    private class CAT_REL_CaptainsWheel_Block : ITestScenario
    {
        public string Id => "CAT-REL-CaptainsWheel";
        public string Name => "Catalog §6: CaptainsWheel AfterBlockCleared (round=3) → EffectiveBlock=18 to CAPTAINS_WHEEL";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            CaptainsWheel? relic = null;
            int savedRound = ctx.CombatState.RoundNumber;
            try
            {
                ctx.CombatState.RoundNumber = 3;
                await ClearPlayerBlock(ctx);
                relic = await ctx.ObtainRelic<CaptainsWheel>();
                ctx.TakeSnapshot();
                await ctx.TriggerRelicHook(() => relic.AfterBlockCleared(ctx.PlayerCreature));
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("CAPTAINS_WHEEL", out var d);
                ctx.AssertEquals(result, "CAPTAINS_WHEEL.EffectiveBlock", 18, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                ctx.CombatState.RoundNumber = savedRound;
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }

    // ── §6 Block: HornCleat AfterBlockCleared (round=2) ──────

    private class CAT_REL_HornCleat_Block : ITestScenario
    {
        public string Id => "CAT-REL-HornCleat";
        public string Name => "Catalog §6: HornCleat AfterBlockCleared (round=2) → EffectiveBlock=14 to HORN_CLEAT";
        public string Category => "Catalog_Relic";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;

        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            HornCleat? relic = null;
            int savedRound = ctx.CombatState.RoundNumber;
            try
            {
                ctx.CombatState.RoundNumber = 2;
                await ClearPlayerBlock(ctx);
                relic = await ctx.ObtainRelic<HornCleat>();
                ctx.TakeSnapshot();
                await ctx.TriggerRelicHook(() => relic.AfterBlockCleared(ctx.PlayerCreature));
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("HORN_CLEAT", out var d);
                ctx.AssertEquals(result, "HORN_CLEAT.EffectiveBlock", 14, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                ctx.CombatState.RoundNumber = savedRound;
                if (relic != null) await ctx.RemoveRelic(relic);
            }
            return result;
        }
    }
}
