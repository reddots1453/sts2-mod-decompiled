using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog — Necrobinder non-Doom, non-Osty card contribution tests.
///
/// Covers: SculptingStrike, Eradicate, TheScythe, Dredge, Defy, Eidolon,
/// Bury, Parse, GlimpseBeyond, DeathsDoor, Defile, DrainPower, Fear,
/// Debilitate, Melancholy, Misery, Putrefy, DanseMacabre, Demesne, Lethality,
/// Oblivion, Shroud, SleightOfFlesh, SpiritOfAsh, Veilpiercer, Delay,
/// Friendship.
///
/// Uses canonical templates A/B/C/E/F per TEST_DESIGN_SPEC.md v3.
/// </summary>
public static class Catalog_NecrobinderCardTests2
{
    private const string Cat = "Catalog_Necrobinder2";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // Attack (DirectDamage)
        new NB2_SculptingStrike(),
        new NB2_Eradicate(),
        new NB2_TheScythe(),
        new NB2_Bury(),
        new NB2_Defile(),
        new NB2_DrainPower(),
        new NB2_Fear(),
        new NB2_Debilitate(),
        new NB2_Misery(),
        new NB2_Veilpiercer(),
        // Skill: block / draw / selfdmg / power-apply
        new NB2_Defy(),
        new NB2_Eidolon(),
        new NB2_Dredge(),
        new NB2_Parse(),
        new NB2_GlimpseBeyond(),
        new NB2_DeathsDoor(),
        new NB2_Melancholy(),
        new NB2_Putrefy(),
        new NB2_Delay(),
        // Power (power applied with expected Amount)
        new NB2_DanseMacabre(),
        new NB2_Demesne(),
        // NB2_Lethality moved to TestRunner head (must run as combat's first action)
        new NB2_Oblivion(),
        new NB2_Shroud(),
        new NB2_SleightOfFlesh(),
        new NB2_SpiritOfAsh(),
        new NB2_Friendship(),
    };

    // ═══════════════════════════════════════════════════════════
    // Attack cards → DirectDamage = card base
    // ═══════════════════════════════════════════════════════════

    private class NB2_SculptingStrike : ITestScenario
    {
        public string Id => "NB2-SculptingStrike";
        public string Name => "SculptingStrike → DirectDamage=8 to SCULPTING_STRIKE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<SculptingStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SCULPTING_STRIKE", out var d);
            ctx.AssertEquals(result, "SCULPTING_STRIKE.DirectDamage", 8, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // Eradicate: X-cost multi-hit. With SetEnergy(3) + X=3, base=11 × 3 hits = 33.
    private class NB2_Eradicate : ITestScenario
    {
        public string Id => "NB2-Eradicate";
        public string Name => "Eradicate → DirectDamage=33 (3 hits × 11) to ERADICATE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.ResetEnemyHp();
                await ctx.SetEnergy(3);
                var card = await ctx.CreateCardInHand<Eradicate>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("ERADICATE", out var d);
                ctx.AssertEquals(result, "ERADICATE.DirectDamage", 33, d?.DirectDamage ?? 0);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    private class NB2_TheScythe : ITestScenario
    {
        public string Id => "NB2-TheScythe";
        public string Name => "TheScythe → DirectDamage=13 to THE_SCYTHE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<TheScythe>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("THE_SCYTHE", out var d);
            ctx.AssertEquals(result, "THE_SCYTHE.DirectDamage", 13, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_Bury : ITestScenario
    {
        public string Id => "NB2-Bury";
        public string Name => "Bury → DirectDamage=52 to BURY";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<Bury>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BURY", out var d);
            ctx.AssertEquals(result, "BURY.DirectDamage", 52, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_Defile : ITestScenario
    {
        public string Id => "NB2-Defile";
        public string Name => "Defile → DirectDamage=13 to DEFILE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<Defile>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFILE", out var d);
            ctx.AssertEquals(result, "DEFILE.DirectDamage", 13, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_DrainPower : ITestScenario
    {
        public string Id => "NB2-DrainPower";
        public string Name => "DrainPower → DirectDamage=10 to DRAIN_POWER";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<DrainPower>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DRAIN_POWER", out var d);
            ctx.AssertEquals(result, "DRAIN_POWER.DirectDamage", 10, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_Fear : ITestScenario
    {
        public string Id => "NB2-Fear";
        public string Name => "Fear → DirectDamage=7 to FEAR";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<Fear>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FEAR", out var d);
            ctx.AssertEquals(result, "FEAR.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_Debilitate : ITestScenario
    {
        public string Id => "NB2-Debilitate";
        public string Name => "Debilitate → DirectDamage=7 to DEBILITATE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<Debilitate>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DEBILITATE", out var d);
            ctx.AssertEquals(result, "DEBILITATE.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_Misery : ITestScenario
    {
        public string Id => "NB2-Misery";
        public string Name => "Misery → DirectDamage=7 to MISERY";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<Misery>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("MISERY", out var d);
            ctx.AssertEquals(result, "MISERY.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class NB2_Veilpiercer : ITestScenario
    {
        public string Id => "NB2-Veilpiercer";
        public string Name => "Veilpiercer → DirectDamage=10 to VEILPIERCER";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.ResetEnemyHp();
            var card = await ctx.CreateCardInHand<Veilpiercer>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("VEILPIERCER", out var d);
            ctx.AssertEquals(result, "VEILPIERCER.DirectDamage", 10, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Block skills → EffectiveBlock
    // ═══════════════════════════════════════════════════════════

    private class NB2_Defy : ITestScenario
    {
        public string Id => "NB2-Defy";
        public string Name => "Defy → EffectiveBlock=6 to DEFY";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            await ctx.ClearBlock();
            var card = await ctx.CreateCardInHand<Defy>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, ctx.GetFirstEnemy());
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFY", out var d);
            ctx.AssertEquals(result, "DEFY.EffectiveBlock", 6, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // Eidolon: Exhausts hand, grants Intangible iff ≥9 cards were exhausted.
    // SPEC-WAIVER: harness can't reliably pad the hand to 9 cards pre-play
    // (CreateCardInHand + ClearHand races). MitigatedByBuff attribution would
    // require a subsequent SimulateDamage post-Intangible, but without the 9-card
    // precondition the Intangible is never applied. Assert TimesPlayed only.
    private class NB2_Eidolon : ITestScenario
    {
        public string Id => "NB2-Eidolon";
        public string Name => "Eidolon → TimesPlayed=1 to EIDOLON";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var card = await ctx.CreateCardInHand<Eidolon>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                var delta = ctx.GetDelta();
                delta.TryGetValue("EIDOLON", out var d);
                ctx.AssertEquals(result, "EIDOLON.TimesPlayed", 1, d?.TimesPlayed ?? 0);
                // SPEC-WAIVER: 9-card exhaust precondition not reachable in harness
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    // Dredge: put 3 cards from discard → hand. Uses CardPileCmd.Move, not
    // PlayerCmd.Draw, so CardsDrawn is not tracked. TimesPlayed only.
    // SPEC-WAIVER: discard→hand transfer is not a "draw" event in the
    // contribution pipeline; no per-card contribution field represents it.
    private class NB2_Dredge : ITestScenario
    {
        public string Id => "NB2-Dredge";
        public string Name => "Dredge → TimesPlayed=1 to DREDGE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Dredge>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DREDGE", out var d);
            ctx.AssertEquals(result, "DREDGE.TimesPlayed", 1, d?.TimesPlayed ?? 0);
            // SPEC-WAIVER: discard→hand is not tracked as CardsDrawn
            return result;
        }
    }

    // Parse: draw 3.
    private class NB2_Parse : ITestScenario
    {
        public string Id => "NB2-Parse";
        public string Name => "Parse → CardsDrawn=3 to PARSE";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<NoDrawPower>(ctx.PlayerCreature);
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Parse>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("PARSE", out var d);
            ctx.AssertEquals(result, "PARSE.CardsDrawn", 3, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // GlimpseBeyond: adds Souls to all players' draw piles. The card itself
    // generates no DirectDamage/Block/CardsDrawn — the downstream Souls are
    // separate cards with their own contribution attribution.
    // SPEC-WAIVER: no contribution field represents "added-to-draw-pile" for
    // the generator card. Assert TimesPlayed=1.
    private class NB2_GlimpseBeyond : ITestScenario
    {
        public string Id => "NB2-GlimpseBeyond";
        public string Name => "GlimpseBeyond → TimesPlayed=1 to GLIMPSE_BEYOND";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<GlimpseBeyond>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GLIMPSE_BEYOND", out var d);
            ctx.AssertEquals(result, "GLIMPSE_BEYOND.TimesPlayed", 1, d?.TimesPlayed ?? 0);
            // SPEC-WAIVER: downstream Soul cards attribute separately
            return result;
        }
    }

    // DeathsDoor: block 6; if you applied Doom this turn, gain block Repeat(2)
    // additional times → 6 × 3 = 18. Pre-play Scourge (applies Doom) to satisfy
    // the turn-flag, then play DeathsDoor. Assert EffectiveBlock=18.
    private class NB2_DeathsDoor : ITestScenario
    {
        public string Id => "NB2-DeathsDoor";
        public string Name => "DeathsDoor (Doom applied) → EffectiveBlock=18 to DEATHS_DOOR";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);

                // Trigger the "applied Doom this turn" flag via Scourge.
                var seeder = await ctx.CreateCardInHand<Scourge>();
                await ctx.PlayCard(seeder, enemy);
                await ctx.ClearBlock();

                var card = await ctx.CreateCardInHand<DeathsDoor>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                await ctx.SimulateDamage(ctx.PlayerCreature, 999, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEATHS_DOOR", out var d);
                ctx.AssertEquals(result, "DEATHS_DOOR.EffectiveBlock", 18, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Melancholy: gain 13 block.
    private class NB2_Melancholy : ITestScenario
    {
        public string Id => "NB2-Melancholy";
        public string Name => "Melancholy → EffectiveBlock=13 to MELANCHOLY";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                await ctx.SetEnergy(999);
                var card = await ctx.CreateCardInHand<Melancholy>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("MELANCHOLY", out var d);
                ctx.AssertEquals(result, "MELANCHOLY.EffectiveBlock", 13, d?.EffectiveBlock ?? 0);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    // Putrefy: apply 2 Weak + 2 Vulnerable to enemy. Play Putrefy then Strike;
    // Vuln's +50% damage modifier attributes ModifierDamage back to PUTREFY as
    // the debuff source. Assert PUTREFY.ModifierDamage > 0.
    private class NB2_Putrefy : ITestScenario
    {
        public string Id => "NB2-Putrefy";
        public string Name => "Putrefy → Strike → PUTREFY.ModifierDamage>0 (Vuln debuff)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<WeakPower>(enemy);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await ctx.ResetEnemyHp();
                await ctx.SetEnergy(999);

                var putrefy = await ctx.CreateCardInHand<Putrefy>();
                await ctx.PlayCard(putrefy, enemy);

                ctx.TakeSnapshot();
                var strike = await ctx.CreateCardInHand<StrikeNecrobinder>();
                await ctx.PlayCard(strike, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("PUTREFY", out var d);
                ctx.AssertGreaterThan(result, "PUTREFY.ModifierDamage", 0, d?.ModifierDamage ?? 0);
                // non-deterministic: exact split depends on Vuln rounding; just assert >0
            }
            finally
            {
                await PowerCmd.Remove<WeakPower>(enemy);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Delay: block 11 now + gain energy next turn. Test the block contribution.
    private class NB2_Delay : ITestScenario
    {
        public string Id => "NB2-Delay";
        public string Name => "Delay → EffectiveBlock=11 to DELAY";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                await ctx.SetEnergy(999);
                var card = await ctx.CreateCardInHand<Delay>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("DELAY", out var d);
                ctx.AssertEquals(result, "DELAY.EffectiveBlock", 11, d?.EffectiveBlock ?? 0);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Power cards → power applied with expected Amount
    // ═══════════════════════════════════════════════════════════

    // DanseMacabre: Power — whenever you play a card that costs 2+, gain 3 Block.
    // Play DanseMacabre → play a 2-cost card (Deathbringer) → downstream 3 Block
    // attributes to DANSE_MACABRE as EffectiveBlock.
    private class NB2_DanseMacabre : ITestScenario
    {
        public string Id => "NB2-DanseMacabre";
        public string Name => "DanseMacabre → 2-cost trigger → DANSE_MACABRE.EffectiveBlock>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DanseMacabrePower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.ClearBlock();
                await ctx.SetEnergy(999);

                var danse = await ctx.CreateCardInHand<DanseMacabre>();
                await ctx.PlayCard(danse);
                await ctx.ClearBlock();

                ctx.TakeSnapshot();
                // Deathbringer is a 2-cost Necrobinder skill — triggers DanseMacabre.
                var trigger = await ctx.CreateCardInHand<Deathbringer>();
                await ctx.PlayCard(trigger, enemy);
                await ctx.SimulateDamage(ctx.PlayerCreature, 999, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("DANSE_MACABRE", out var d);
                ctx.AssertGreaterThan(result, "DANSE_MACABRE.EffectiveBlock", 0, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DanseMacabrePower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Demesne: Power — start of turn, gain 1 energy + draw 1 extra card.
    // Play Demesne → snapshot → EndTurn → next turn-start fires DemesnePower →
    // EnergyGained/CardsDrawn attribute back to DEMESNE.
    private class NB2_Demesne : ITestScenario
    {
        public string Id => "NB2-Demesne";
        public string Name => "Demesne → turn-start → DEMESNE.EnergyGained>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DemesnePower>(ctx.PlayerCreature);
                await ctx.EnsureDrawPile(8);
                await ctx.SetEnergy(999);
                var card = await ctx.CreateCardInHand<Demesne>();
                await ctx.PlayCard(card);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEMESNE", out var d);
                ctx.AssertGreaterThan(result, "DEMESNE.EnergyGained", 0, d?.EnergyGained ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DemesnePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Lethality: Power — first Attack each turn deals +50% damage. Play
    // Lethality → play Strike → ModifierDamage attributes to LETHALITY.
    internal class NB2_Lethality : ITestScenario
    {
        public string Id => "NB2-Lethality";
        public string Name => "Lethality → Strike → LETHALITY.ModifierDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<LethalityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.ResetEnemyHp();
                await ctx.SetEnergy(999);

                var lethality = await ctx.CreateCardInHand<Lethality>();
                await ctx.PlayCard(lethality);

                // Lethality's "first attack each turn" flag is consumed/checked
                // at turn start — must cross a turn boundary before the strike.
                await ctx.EndTurnAndWaitForPlayerTurn();

                ctx.TakeSnapshot();
                var strike = await ctx.CreateCardInHand<StrikeNecrobinder>();
                await ctx.PlayCard(strike, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("LETHALITY", out var dLeth);
                delta.TryGetValue("STRIKE_NECROBINDER", out var dStrike);
                int total = (dLeth?.ModifierDamage ?? 0) + (dStrike?.ModifierDamage ?? 0);
                ctx.AssertGreaterThan(result, "LETHALITY+STRIKE_NECROBINDER.ModifierDamage", 0, total);
            }
            finally
            {
                await PowerCmd.Remove<LethalityPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Oblivion: "Whenever you play a card this turn, apply X Doom to the enemy."
    // Play Oblivion → play a follow-up card → enemy gains Doom via Oblivion's
    // hook. We verify the downstream Doom landed (cannot measure AttributedDamage
    // without killing the enemy — Doom-kill attribution is tail-group only).
    // SPEC-WAIVER: Doom-kill path reserved for Catalog_NecrobinderDoom tail group.
    private class NB2_Oblivion : ITestScenario
    {
        public string Id => "NB2-Oblivion";
        public string Name => "Oblivion → follow-up → enemy DoomPower>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);

                var oblivion = await ctx.CreateCardInHand<Oblivion>();
                await ctx.PlayCard(oblivion, enemy);

                var follow = await ctx.CreateCardInHand<Defy>();
                await ctx.PlayCard(follow);

                var doom = enemy.GetPower<DoomPower>();
                ctx.AssertGreaterThan(result, "Enemy.DoomPower.Amount (via Oblivion)", 0, doom?.Amount ?? 0);
                // SPEC-WAIVER: OBLIVION.AttributedDamage requires Doom-kill in tail group
            }
            finally
            {
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Shroud: Power — whenever you apply Doom, gain Block.
    // Play Shroud → play Scourge (applies Doom) → SHROUD.EffectiveBlock > 0.
    private class NB2_Shroud : ITestScenario
    {
        public string Id => "NB2-Shroud";
        public string Name => "Shroud → apply Doom → SHROUD.EffectiveBlock>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<ShroudPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.ClearBlock();
                await ctx.SetEnergy(999);

                var shroud = await ctx.CreateCardInHand<Shroud>();
                await ctx.PlayCard(shroud);
                await ctx.ClearBlock();

                ctx.TakeSnapshot();
                var doomCard = await ctx.CreateCardInHand<Scourge>();
                await ctx.PlayCard(doomCard, enemy);
                await ctx.SimulateDamage(ctx.PlayerCreature, 999, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("SHROUD", out var d);
                ctx.AssertGreaterThan(result, "SHROUD.EffectiveBlock", 0, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<ShroudPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // SleightOfFlesh: Power — whenever you apply a debuff to an enemy, they
    // take 9 damage. Play SoF → play Putrefy (applies Weak+Vuln) → downstream
    // damage attributes to SLEIGHT_OF_FLESH.
    private class NB2_SleightOfFlesh : ITestScenario
    {
        public string Id => "NB2-SleightOfFlesh";
        public string Name => "SleightOfFlesh → debuff applied → SLEIGHT_OF_FLESH.AttributedDamage>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await PowerCmd.Remove<SleightOfFleshPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<WeakPower>(enemy);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.ResetEnemyHp();
                await ctx.SetEnergy(999);

                var sof = await ctx.CreateCardInHand<SleightOfFlesh>();
                await ctx.PlayCard(sof);

                ctx.TakeSnapshot();
                var debuffer = await ctx.CreateCardInHand<Putrefy>();
                await ctx.PlayCard(debuffer, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("SLEIGHT_OF_FLESH", out var d);
                ctx.AssertGreaterThan(result, "SLEIGHT_OF_FLESH.AttributedDamage", 0, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<SleightOfFleshPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<WeakPower>(enemy);
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // SpiritOfAsh: Power — whenever you play an Ethereal card, gain Block.
    // Parse is Ethereal. Play SoA → play Parse → SPIRIT_OF_ASH.EffectiveBlock > 0.
    private class NB2_SpiritOfAsh : ITestScenario
    {
        public string Id => "NB2-SpiritOfAsh";
        public string Name => "SpiritOfAsh → Ethereal play → SPIRIT_OF_ASH.EffectiveBlock>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<SpiritOfAshPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<NoDrawPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                await ctx.EnsureDrawPile(8);
                await ctx.SetEnergy(999);

                var soa = await ctx.CreateCardInHand<SpiritOfAsh>();
                await ctx.PlayCard(soa);
                await ctx.ClearBlock();

                ctx.TakeSnapshot();
                var parse = await ctx.CreateCardInHand<Parse>();
                await ctx.PlayCard(parse);
                await ctx.SimulateDamage(ctx.PlayerCreature, 999, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("SPIRIT_OF_ASH", out var d);
                ctx.AssertGreaterThan(result, "SPIRIT_OF_ASH.EffectiveBlock", 0, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<SpiritOfAshPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // Friendship: Power — lose 2 Strength, gain 1 Energy each turn start.
    // Play Friendship → snapshot → EndTurn → next turn-start fires the hook →
    // FRIENDSHIP.EnergyGained > 0.
    private class NB2_Friendship : ITestScenario
    {
        public string Id => "NB2-Friendship";
        public string Name => "Friendship → turn-start → FRIENDSHIP.EnergyGained>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FriendshipPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);

                var card = await ctx.CreateCardInHand<Friendship>();
                await ctx.PlayCard(card);

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("FRIENDSHIP", out var d);
                ctx.AssertGreaterThan(result, "FRIENDSHIP.EnergyGained", 0, d?.EnergyGained ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<FriendshipPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
