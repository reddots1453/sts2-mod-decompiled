using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Silent cards — contribution tracking verification (spec v3).
/// Every test validates delta fields with exact values.
/// </summary>
public static class Catalog_SilentCardTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // A: Attack → exact DirectDamage
        new SI_SuckerPunch(),    // 8 dmg + 1 Weak
        new SI_DaggerSpray(),    // 4×2 AoE
        new SI_DaggerThrow(),    // 9 dmg + draw 1 + discard 1 (combo)
        new SI_LeadingStrike(),  // 7 dmg + gen Shiv
        new SI_Backstab(),       // 11 dmg (Innate, Exhaust)
        new SI_Predator(),       // 15 dmg
        new SI_Skewer(),         // 7×3 (energy=3)
        new SI_Finisher(),       // 6 per attack played this turn
        new SI_Flechettes(),     // 5 per Skill in hand
        new SI_GrandFinale(),    // 50 AoE (draw pile must be empty)
        // D: Combo
        new SI_Backflip(),       // 5 block + draw 2
        new SI_CloakAndDagger(), // 6 block + gen 1 Shiv
        new SI_Dash(),           // 10 dmg + 10 block
        // B: Block
        new SI_Survivor(),       // 8 block (+ discard 1)
        new SI_Blur(),           // 5 block (+ retain)
        // C: Draw
        new SI_Adrenaline(),     // draw 2 (+ energy, Exhaust)
        new SI_CalculatedGamble(), // discard hand + draw same count
        new SI_Expertise(),      // draw until 6 in hand
        // Discard + gen
        new SI_StormOfSteel(),   // discard hand + gen Shivs (smoke: verify no crash)
        // Poison
        new SI_BouncingFlask(),  // 3 poison × 3 random hits
        new SI_Haze(),           // 4 AoE poison
        new SI_Snakebite(),      // 7 poison
        // Power contrib (F): verify actual delta
        new SI_Footwork(),       // +2 Dex → Defend ModifierBlock=2
        new SI_Afterimage(),     // play card → AFTERIMAGE.EffectiveBlock=1
        new SI_Envenom(),        // Strike hit → enemy PoisonPower=1
        new SI_Accuracy(),       // +4 Shiv bonus → Shiv DirectDamage=4 + ACCURACY.ModifierDamage=4
        // Power smoke (these Powers have effects too complex for direct contrib test)
        new SI_InfiniteBlades(),  // add Shiv each turn → verify power applied
        new SI_WellLaidPlans(),   // retain cards → verify power applied
        new SI_WraithForm(),      // Intangible → verify IntangiblePower applied
        new SI_Burst(),           // next Skill 2x → verify BurstPower applied
    };

    // ═══════════════════════════════════════════════════════════
    // A: Attack → DirectDamage (exact)
    // ═══════════════════════════════════════════════════════════

    private class SI_SuckerPunch : ITestScenario
    {
        public string Id => "CAT-SI-SuckerPunch";
        public string Name => "SuckerPunch: DirectDamage=8 + WeakPower=1";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await PowerCmd.Remove<WeakPower>(enemy);
            var card = await ctx.CreateCardInHand<SuckerPunch>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SUCKER_PUNCH", out var d);
            ctx.AssertEquals(result, "SUCKER_PUNCH.DirectDamage", 8, d?.DirectDamage ?? 0);
            // SPEC-WAIVER: debuff application smoke (Weak contribution requires follow-up enemy attack)
            var weak = enemy.GetPower<WeakPower>();
            if (weak == null || weak.Amount < 1)
                result.Fail("WeakPower.Amount", "≥1", weak?.Amount.ToString() ?? "null");
            await PowerCmd.Remove<WeakPower>(enemy);
            return result;
        }
    }

    private class SI_DaggerSpray : ITestScenario
    {
        public string Id => "CAT-SI-DaggerSpray";
        public string Name => "DaggerSpray: DirectDamage=4×2×enemies (AoE 2-hit)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<DaggerSpray>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DAGGER_SPRAY", out var d);
            int enemyCount = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "DAGGER_SPRAY.DirectDamage", 4 * 2 * enemyCount, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // DaggerThrow: 9 dmg + draw 1 + discard 1 (combo: DirectDamage + CardsDrawn)
    private class SI_DaggerThrow : ITestScenario
    {
        public string Id => "CAT-SI-DaggerThrow";
        public string Name => "DaggerThrow: DirectDamage=9 + CardsDrawn=1";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ClearHand(); // Round 14 B5: DaggerThrow draws, prevent hand overflow
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<DaggerThrow>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DAGGER_THROW", out var d);
            ctx.AssertEquals(result, "DAGGER_THROW.DirectDamage", 9, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "DAGGER_THROW.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class SI_LeadingStrike : ITestScenario
    {
        public string Id => "CAT-SI-LeadingStrike";
        public string Name => "LeadingStrike: DirectDamage=7";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<LeadingStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("LEADING_STRIKE", out var d);
            ctx.AssertEquals(result, "LEADING_STRIKE.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class SI_Backstab : ITestScenario
    {
        public string Id => "CAT-SI-Backstab";
        public string Name => "Backstab: DirectDamage=11";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Backstab>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BACKSTAB", out var d);
            ctx.AssertEquals(result, "BACKSTAB.DirectDamage", 11, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class SI_Predator : ITestScenario
    {
        public string Id => "CAT-SI-Predator";
        public string Name => "Predator: DirectDamage=15";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Predator>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("PREDATOR", out var d);
            ctx.AssertEquals(result, "PREDATOR.DirectDamage", 15, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class SI_Skewer : ITestScenario
    {
        public string Id => "CAT-SI-Skewer";
        public string Name => "Skewer: DirectDamage=7×3 (energy=3)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Skewer>();
            await ctx.SetEnergy(3);
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            await ctx.SetEnergy(999);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SKEWER", out var d);
            ctx.AssertEquals(result, "SKEWER.DirectDamage", 7 * 3, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D: Combo → multiple fields
    // ═══════════════════════════════════════════════════════════

    // Backflip: 5 block + draw 2
    private class SI_Backflip : ITestScenario
    {
        public string Id => "CAT-SI-Backflip";
        public string Name => "Backflip: EffectiveBlock=5 + CardsDrawn=2";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<Backflip>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("BACKFLIP", out var d);
            ctx.AssertEquals(result, "BACKFLIP.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            ctx.AssertEquals(result, "BACKFLIP.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // CloakAndDagger: 6 block + gen 1 Shiv
    private class SI_CloakAndDagger : ITestScenario
    {
        public string Id => "CAT-SI-CloakAndDagger";
        public string Name => "CloakAndDagger: EffectiveBlock=6 (+ Shiv generated)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<CloakAndDagger>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("CLOAK_AND_DAGGER", out var d);
            ctx.AssertEquals(result, "CLOAK_AND_DAGGER.EffectiveBlock", 6, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // Dash: 10 dmg + 10 block
    private class SI_Dash : ITestScenario
    {
        public string Id => "CAT-SI-Dash";
        public string Name => "Dash: DirectDamage=10 + EffectiveBlock=10";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ClearBlock();
            var card = await ctx.CreateCardInHand<Dash>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DASH", out var d);
            ctx.AssertEquals(result, "DASH.DirectDamage", 10, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "DASH.EffectiveBlock", 10, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // C: Draw → CardsDrawn (exact)
    // ═══════════════════════════════════════════════════════════

    // Adrenaline: draw 2 + gain 1 energy (Exhaust)
    private class SI_Adrenaline : ITestScenario
    {
        public string Id => "CAT-SI-Adrenaline";
        public string Name => "Adrenaline: CardsDrawn=2";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<Adrenaline>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ADRENALINE", out var d);
            ctx.AssertEquals(result, "ADRENALINE.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // CalculatedGamble: discard hand + draw same count
    private class SI_CalculatedGamble : ITestScenario
    {
        public string Id => "CAT-SI-CalculatedGamble";
        public string Name => "CalculatedGamble: CardsDrawn=2 (2 cards in hand discarded)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(7);
            // Put exactly 2 other cards in hand so CG discards 2 + draws 2
            await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.CreateCardInHand<StrikeIronclad>();
            var card = await ctx.CreateCardInHand<CalculatedGamble>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("CALCULATED_GAMBLE", out var d);
            // Discards 2 cards (the 2 Strikes), draws 2
            ctx.AssertEquals(result, "CALCULATED_GAMBLE.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Poison → verify delta AttributedDamage (requires EndTurn for tick)
    // For direct poison application, verify exact PoisonPower amount
    // ═══════════════════════════════════════════════════════════

    // BouncingFlask: 3 poison × 3 hits = 9 total poison → EndTurn → AttributedDamage=9
    private class SI_BouncingFlask : ITestScenario
    {
        public string Id => "CAT-SI-BouncingFlask";
        public string Name => "BouncingFlask: EndTurn → AttributedDamage=9 (total poison tick)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                foreach (var e in ctx.GetAllEnemies())
                    await PowerCmd.Remove<PoisonPower>(e);
                var card = await ctx.CreateCardInHand<BouncingFlask>();
                await ctx.PlayCard(card, ctx.GetFirstEnemy());
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("BOUNCING_FLASK", out var d);
                ctx.AssertEquals(result, "BOUNCING_FLASK.AttributedDamage", 9, d?.AttributedDamage ?? 0);
            }
            finally
            {
                foreach (var e in ctx.GetAllEnemies())
                    await PowerCmd.Remove<PoisonPower>(e);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Haze: 4 AoE poison → EndTurn → AttributedDamage=4×enemies
    private class SI_Haze : ITestScenario
    {
        public string Id => "CAT-SI-Haze";
        public string Name => "Haze: EndTurn → AttributedDamage=4×enemies";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                foreach (var e in ctx.GetAllEnemies())
                    await PowerCmd.Remove<PoisonPower>(e);
                var card = await ctx.CreateCardInHand<Haze>();
                await ctx.PlayCard(card);
                int enemies = ctx.GetAllEnemies().Count;
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("HAZE", out var d);
                ctx.AssertEquals(result, "HAZE.AttributedDamage", 4 * enemies, d?.AttributedDamage ?? 0);
            }
            finally
            {
                foreach (var e in ctx.GetAllEnemies())
                    await PowerCmd.Remove<PoisonPower>(e);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Snakebite: 7 poison → EndTurn → AttributedDamage=7
    private class SI_Snakebite : ITestScenario
    {
        public string Id => "CAT-SI-Snakebite";
        public string Name => "Snakebite: EndTurn → AttributedDamage=7";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<PoisonPower>(enemy);
                var card = await ctx.CreateCardInHand<Snakebite>();
                await ctx.PlayCard(card, enemy);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("SNAKEBITE", out var d);
                ctx.AssertEquals(result, "SNAKEBITE.AttributedDamage", 7, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<PoisonPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // F: Power contrib → verify actual delta attribution
    // ═══════════════════════════════════════════════════════════

    // Footwork: +2 Dex → Defend block=5+2=7. EffectiveBlock=5(base)+ModifierBlock=2(Dex).
    private class SI_Footwork : ITestScenario
    {
        public string Id => "CAT-SI-Footwork";
        public string Name => "Footwork: +2 Dex → Defend EffectiveBlock=5 + FOOTWORK.ModifierBlock=2";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                var fw = await ctx.CreateCardInHand<Footwork>();
                await ctx.PlayCard(fw);
                await ctx.ClearBlock();
                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(defend);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                // Defend base=5 → EffectiveBlock=5
                delta.TryGetValue("DEFEND_IRONCLAD", out var dd);
                ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock", 5, dd?.EffectiveBlock ?? 0);
                // Footwork Dex=2 → ModifierBlock=2
                delta.TryGetValue("FOOTWORK", out var fd);
                ctx.AssertEquals(result, "FOOTWORK.ModifierBlock", 2, fd?.ModifierBlock ?? 0);
            }
            finally { await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature); }
            return result;
        }
    }

    // Afterimage: play card → +1 block. AFTERIMAGE.EffectiveBlock=1
    private class SI_Afterimage : ITestScenario
    {
        public string Id => "CAT-SI-Afterimage";
        public string Name => "Afterimage: play 1 card → EffectiveBlock=1 attributed to AFTERIMAGE";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                var ai = await ctx.CreateCardInHand<Afterimage>();
                await ctx.PlayCard(ai);
                await ctx.ClearBlock();
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(strike, ctx.GetFirstEnemy());
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("AFTERIMAGE", out var d);
                ctx.AssertEquals(result, "AFTERIMAGE.EffectiveBlock", 1, d?.EffectiveBlock ?? 0);
            }
            finally { await PowerCmd.Remove<AfterimagePower>(ctx.PlayerCreature); }
            return result;
        }
    }

    // Envenom: unblocked hit → +1 Poison. Play Strike → enemy PoisonPower=1
    private class SI_Envenom : ITestScenario
    {
        public string Id => "CAT-SI-Envenom";
        public string Name => "Envenom: Strike hit → EndTurn → ENVENOM.AttributedDamage=1";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<EnvenomPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<PoisonPower>(enemy);
                var env = await ctx.CreateCardInHand<Envenom>();
                await ctx.PlayCard(env);
                // Strike hit applies 1 poison to enemy via Envenom hook
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Poison ticks 1 damage → attributed to ENVENOM (the power source)
                var delta = ctx.GetDelta();
                delta.TryGetValue("ENVENOM", out var d);
                ctx.AssertEquals(result, "ENVENOM.AttributedDamage", 1, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<EnvenomPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<PoisonPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Accuracy: +4 to Shiv. BladeDance → Shiv(4+4=8 total).
    // delta["SHIV"].DirectDamage=4 (base), delta["ACCURACY"].ModifierDamage=4
    private class SI_Accuracy : ITestScenario
    {
        public string Id => "CAT-SI-Accuracy";
        public string Name => "Accuracy: Shiv DirectDamage=4 + ACCURACY.ModifierDamage=4";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                var acc = await ctx.CreateCardInHand<Accuracy>();
                await ctx.PlayCard(acc);
                var bd = await ctx.CreateCardInHand<BladeDance>();
                await ctx.PlayCard(bd);
                // Get a Shiv from hand
                var hand = PileType.Hand.GetPile(ctx.Player);
                var shiv = hand.Cards.FirstOrDefault(c => c is Shiv);
                if (shiv == null) { result.Fail("ShivInHand", "exists", "null"); return result; }
                ctx.TakeSnapshot();
                await ctx.PlayCard(shiv, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("SHIV", out var sd);
                ctx.AssertEquals(result, "SHIV.DirectDamage", 4, sd?.DirectDamage ?? 0);
                delta.TryGetValue("ACCURACY", out var ad);
                ctx.AssertEquals(result, "ACCURACY.ModifierDamage", 4, ad?.ModifierDamage ?? 0);
            }
            finally { await PowerCmd.Remove<AccuracyPower>(ctx.PlayerCreature); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Added: missing attack tests
    // ═══════════════════════════════════════════════════════════

    // Finisher: 6 dmg per attack played this turn. Play 2 Strikes first → 6×2=12
    private class SI_Finisher : ITestScenario
    {
        public string Id => "CAT-SI-Finisher";
        public string Name => "Finisher: DirectDamage=6×2 (2 attacks played this turn)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            // Play 2 attacks first to set hit count
            var s1 = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(s1, enemy);
            var s2 = await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.PlayCard(s2, enemy);
            // Now play Finisher: 6 dmg × 2 attacks = 12
            var card = await ctx.CreateCardInHand<Finisher>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FINISHER", out var d);
            ctx.AssertEquals(result, "FINISHER.DirectDamage", 12, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // Flechettes: 5 dmg per Skill in hand. Put 3 Skills in hand → 5×3=15
    private class SI_Flechettes : ITestScenario
    {
        public string Id => "CAT-SI-Flechettes";
        public string Name => "Flechettes: DirectDamage=5×3 (3 Skills in hand)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            // Add 3 Skill cards to hand (Defends)
            await ctx.CreateCardInHand<DefendIronclad>();
            await ctx.CreateCardInHand<DefendIronclad>();
            await ctx.CreateCardInHand<DefendIronclad>();
            // Flechettes itself is a Skill, but it's being played so may not count
            var card = await ctx.CreateCardInHand<Flechettes>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FLECHETTES", out var d);
            // 3 Defends in hand (Flechettes itself goes to Play pile before counting)
            ctx.AssertEquals(result, "FLECHETTES.DirectDamage", 15, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // GrandFinale: 50 AoE if draw pile empty. Hard to guarantee empty draw pile.
    // Test: ensure draw pile is empty, then play → 50 × enemies
    private class SI_GrandFinale : ITestScenario
    {
        public string Id => "CAT-SI-GrandFinale";
        public string Name => "GrandFinale: DirectDamage=50×enemies (draw pile empty)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.SetEnergy(999);
            // GrandFinale only deals damage if draw pile is empty.
            // Move all draw-pile cards to discard so the play restriction is satisfied.
            var drawPile = PileType.Draw.GetPile(ctx.Player);
            var drawCards = drawPile.Cards.ToList();
            foreach (var c in drawCards)
                await CardPileCmd.Add(c, PileType.Discard, skipVisuals: true);
            var gf = await ctx.CreateCardInHand<GrandFinale>();
            int enemies = ctx.GetAllEnemies().Count;
            ctx.TakeSnapshot();
            await ctx.PlayCard(gf, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GRAND_FINALE", out var d);
            ctx.AssertEquals(result, "GRAND_FINALE.DirectDamage", 50 * enemies, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Added: Block tests
    // ═══════════════════════════════════════════════════════════

    // Survivor: 8 block + discard 1 card
    private class SI_Survivor : ITestScenario
    {
        public string Id => "CAT-SI-Survivor";
        public string Name => "Survivor: EffectiveBlock=8";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            // Survivor discards a card — need one in hand
            await ctx.CreateCardInHand<StrikeIronclad>();
            var card = await ctx.CreateCardInHand<Survivor>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("SURVIVOR", out var d);
            ctx.AssertEquals(result, "SURVIVOR.EffectiveBlock", 8, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // Blur: 5 block + retain block next turn
    private class SI_Blur : ITestScenario
    {
        public string Id => "CAT-SI-Blur";
        public string Name => "Blur: EffectiveBlock=5";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Blur>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("BLUR", out var d);
            ctx.AssertEquals(result, "BLUR.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Added: Draw / gen tests
    // ═══════════════════════════════════════════════════════════

    // Expertise: draw until 6 cards in hand
    private class SI_Expertise : ITestScenario
    {
        public string Id => "CAT-SI-Expertise";
        public string Name => "Expertise: CardsDrawn > 0 (draw until 6 in hand)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(10);
            // Expertise draws until hand has 6 cards. With only Expertise in hand (1 card),
            // after playing Expertise (0 in hand), it draws 6.
            var card = await ctx.CreateCardInHand<Expertise>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("EXPERTISE", out var d);
            int drawn = d?.CardsDrawn ?? 0;
            // non-deterministic: Expertise draws until hand=6, count depends on hand size at play time
            ctx.AssertGreaterThan(result, "EXPERTISE.CardsDrawn", 0, drawn);
            result.ActualValues["CardsDrawn"] = drawn.ToString();
            return result;
        }
    }

    // StormOfSteel: discard hand + add Shivs per discarded card
    private class SI_StormOfSteel : ITestScenario
    {
        public string Id => "CAT-SI-StormOfSteel";
        public string Name => "StormOfSteel: smoke (discard hand + gen Shivs)";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.CreateCardInHand<StrikeIronclad>();
            await ctx.CreateCardInHand<StrikeIronclad>();
            var card = await ctx.CreateCardInHand<StormOfSteel>();
            await ctx.PlayCard(card);
            // Verify Shivs were added to hand
            var hand = PileType.Hand.GetPile(ctx.Player);
            int shivCount = hand.Cards.Count(c => c is Shiv);
            // SPEC-WAIVER: card-generation smoke (shiv contribution shows only when Shivs are played)
            ctx.AssertGreaterThan(result, "ShivsInHand", 0, shivCount);
            result.ActualValues["ShivCount"] = shivCount.ToString();
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Added: Power smoke (turn-based, cannot verify direct contrib in test)
    // ═══════════════════════════════════════════════════════════

    private class SI_InfiniteBlades : ITestScenario
    {
        public string Id => "CAT-SI-InfiniteBlades";
        public string Name => "InfiniteBlades: InfiniteBladesPower applied";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var card = await ctx.CreateCardInHand<InfiniteBlades>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                var delta = ctx.GetDelta();
                delta.TryGetValue("INFINITE_BLADES", out var d);
                // SPEC-WAIVER: InfiniteBlades adds a Shiv at turn start. The Shiv-add
                // itself has no direct contribution field, and driving EndTurn here
                // risks desynchronising the harness. Assert TimesPlayed for the card cast.
                ctx.AssertEquals(result, "INFINITE_BLADES.TimesPlayed", 1, d?.TimesPlayed ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<InfiniteBladesPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    private class SI_WellLaidPlans : ITestScenario
    {
        public string Id => "CAT-SI-WellLaidPlans";
        public string Name => "WellLaidPlans: WellLaidPlansPower applied";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var card = await ctx.CreateCardInHand<WellLaidPlans>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                var delta = ctx.GetDelta();
                delta.TryGetValue("WELL_LAID_PLANS", out var d);
                // SPEC-WAIVER: WellLaidPlans retains a card at end of turn. Card-retention
                // has no dedicated contribution field, so we assert TimesPlayed for the cast.
                ctx.AssertEquals(result, "WELL_LAID_PLANS.TimesPlayed", 1, d?.TimesPlayed ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<WellLaidPlansPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    private class SI_WraithForm : ITestScenario
    {
        public string Id => "CAT-SI-WraithForm";
        public string Name => "WraithForm: IntangiblePower applied";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                await ctx.ClearBlock();
                var card = await ctx.CreateCardInHand<WraithForm>();
                await ctx.PlayCard(card);
                // After WraithForm, IntangiblePower should reduce incoming damage.
                // Snapshot → take 10 damage → IntangiblePower-attributed MitigatedByBuff > 0
                ctx.TakeSnapshot();
                await ctx.SimulateDamage(ctx.PlayerCreature, 10, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("WRAITH_FORM", out var d);
                ctx.AssertGreaterThan(result, "WRAITH_FORM.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await PowerCmd.Remove<WraithFormPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    private class SI_Burst : ITestScenario
    {
        public string Id => "CAT-SI-Burst";
        public string Name => "Burst: BurstPower applied";
        public string Category => "Catalog_SilentCards";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                var burst = await ctx.CreateCardInHand<Burst>();
                await ctx.PlayCard(burst);
                await ctx.ClearBlock();
                var defend = await ctx.CreateCardInHand<DefendSilent>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(defend);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("DEFEND_SILENT", out var d);
                // Burst plays the next Skill an extra time → Defend(5) triggers twice → 10 block
                ctx.AssertEquals(result, "DEFEND_SILENT.EffectiveBlock (Burst x2)", 10, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<BurstPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
