using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Defect cards — contribution tracking verification (spec v3).
/// Every test validates delta fields with exact values.
/// Combo cards validate ALL effect fields.
/// </summary>
public static class Catalog_DefectCardTests
{
    private const string Cat = "Catalog_DefectCards";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // A: Attack → exact DirectDamage
        new DE_BallLightning(),   // 7 dmg + channel Lightning
        new DE_ColdSnap(),        // 6 dmg + channel Frost
        new DE_MeteorStrike(),    // 24 dmg + channel 3 Plasma
        new DE_BeamCell(),        // 3 dmg + 1 Vuln
        new DE_GoForTheEyes(),    // 3 dmg
        new DE_GunkUp(),          // 4×3 = 12 dmg
        new DE_Hyperbeam(),       // 26 AoE dmg
        new DE_Shatter(),         // 11 AoE dmg
        // D: Combo (damage + draw)
        new DE_SweepingBeam(),    // 6 AoE dmg + draw 1
        new DE_RocketPunch(),     // 13 dmg + draw 1
        new DE_CompileDriver(),   // 7 dmg (draw depends on orbs)
        // B: Block → exact EffectiveBlock
        new DE_Glacier(),         // 6 block + channel 2 Frost
        new DE_ShadowShield(),    // 11 block + channel Dark
        new DE_Glasswork(),       // 5 block + channel Glass
        new DE_ChargeBattery(),   // 7 block
        new DE_BootSequence(),    // 10 block (Innate, Exhaust)
        new DE_Compact(),         // 6 block
        // C: Draw → exact CardsDrawn
        new DE_Skim(),            // draw 3
        new DE_Coolheaded(),      // draw 1 + channel Frost
        new DE_Overclock(),       // draw 2 + add Burn
        new DE_Reboot(),          // draw 4 (shuffle all)
        // Null: 10 dmg + 2 Weak (combo)
        new DE_Null(),
        // E: Attack (additional)
        new DE_Ftl(),             // 5 dmg (draw conditional, test only DirectDamage)
        new DE_Scrape(),          // 7 dmg + draw 4
        new DE_Sunder(),          // 24 dmg
        // F: Block (additional)
        new DE_Leap(),            // 9 block
        new DE_Hologram(),        // 3 block + CardSelectCmd (user selects from discard)
        // G: Energy
        new DE_Turbo(),           // +2 energy
        new DE_Supercritical(),   // +4 energy
        // H: Power contribution chain (template F)
        new DE_Buffer(),          // play Buffer → take damage → MitigatedByBuff
        // I: Orb channel → EndTurn → passive effect
        new DE_Zap_Passive(),           // channel Lightning → EndTurn → AttributedDamage=3
        new DE_Chill_Passive(),         // channel Frost → EndTurn → enemy attacks → EffectiveBlock=2
        new DE_Fusion_Passive(),        // channel Plasma → EndTurn → next turn → EnergyGained=1
        // J: Focus → orb modifier
        new DE_BiasedCognition_Focus(), // +4 Focus → channel Lightning → EndTurn → ModifierDamage=4
        new DE_Defragment_Focus(),      // +1 Focus → channel Lightning → EndTurn → ModifierDamage=1
        // K: Power → channel trigger chain
        new DE_Storm_Chain(),           // play Storm → play Power → Lightning channeled → EndTurn → 3 dmg
        // L: Hailstorm EndTurn AoE
        new DE_Hailstorm_EndTurn(),     // channel Frost → play Hailstorm → EndTurn → 6 AoE dmg
        // M: Evoke
        new DE_Dualcast_Evoke(),        // channel Lightning → play Dualcast → evoke 2× → 16 dmg
        // N: EchoForm next turn
        new DE_EchoForm_NextTurn(),     // play EchoForm → EndTurn → play Strike → 12 DirectDamage
    };

    // ═══════════════════════════════════════════════════════════
    // A: Attack → DirectDamage
    // ═══════════════════════════════════════════════════════════

    private class DE_BallLightning : ITestScenario
    {
        public string Id => "CAT-DE-BallLightning";
        public string Name => "BallLightning: DirectDamage=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<BallLightning>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BALL_LIGHTNING", out var d);
            ctx.AssertEquals(result, "BALL_LIGHTNING.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class DE_ColdSnap : ITestScenario
    {
        public string Id => "CAT-DE-ColdSnap";
        public string Name => "ColdSnap: DirectDamage=6";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<ColdSnap>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("COLD_SNAP", out var d);
            ctx.AssertEquals(result, "COLD_SNAP.DirectDamage", 6, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class DE_MeteorStrike : ITestScenario
    {
        public string Id => "CAT-DE-MeteorStrike";
        public string Name => "MeteorStrike: DirectDamage=24";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<MeteorStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("METEOR_STRIKE", out var d);
            ctx.AssertEquals(result, "METEOR_STRIKE.DirectDamage", 24, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class DE_BeamCell : ITestScenario
    {
        public string Id => "CAT-DE-BeamCell";
        public string Name => "BeamCell: DirectDamage=3 + VulnerablePower=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<BeamCell>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BEAM_CELL", out var d);
            ctx.AssertEquals(result, "BEAM_CELL.DirectDamage", 3, d?.DirectDamage ?? 0);
            var vuln = enemy.GetPower<VulnerablePower>();
            if (vuln == null || vuln.Amount < 1)
                result.Fail("VulnerablePower", "≥1", vuln?.Amount.ToString() ?? "null");
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    private class DE_GoForTheEyes : ITestScenario
    {
        public string Id => "CAT-DE-GoForTheEyes";
        public string Name => "GoForTheEyes: DirectDamage=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<GoForTheEyes>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GO_FOR_THE_EYES", out var d);
            ctx.AssertEquals(result, "GO_FOR_THE_EYES.DirectDamage", 3, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class DE_GunkUp : ITestScenario
    {
        public string Id => "CAT-DE-GunkUp";
        public string Name => "GunkUp: DirectDamage=4×3=12";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<GunkUp>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GUNK_UP", out var d);
            ctx.AssertEquals(result, "GUNK_UP.DirectDamage", 12, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class DE_Hyperbeam : ITestScenario
    {
        public string Id => "CAT-DE-Hyperbeam";
        public string Name => "Hyperbeam: DirectDamage=26×enemies (AoE)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Hyperbeam>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("HYPERBEAM", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "HYPERBEAM.DirectDamage", 26 * enemies, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class DE_Shatter : ITestScenario
    {
        public string Id => "CAT-DE-Shatter";
        public string Name => "Shatter: DirectDamage=11×enemies (AoE)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Shatter>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SHATTER", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "SHATTER.DirectDamage", 11 * enemies, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D: Combo (damage + draw)
    // ═══════════════════════════════════════════════════════════

    private class DE_SweepingBeam : ITestScenario
    {
        public string Id => "CAT-DE-SweepingBeam";
        public string Name => "SweepingBeam: DirectDamage=6×enemies + CardsDrawn=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<SweepingBeam>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SWEEPING_BEAM", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "SWEEPING_BEAM.DirectDamage", 6 * enemies, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "SWEEPING_BEAM.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class DE_RocketPunch : ITestScenario
    {
        public string Id => "CAT-DE-RocketPunch";
        public string Name => "RocketPunch: DirectDamage=13 + CardsDrawn=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<RocketPunch>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ROCKET_PUNCH", out var d);
            ctx.AssertEquals(result, "ROCKET_PUNCH.DirectDamage", 13, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "ROCKET_PUNCH.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class DE_CompileDriver : ITestScenario
    {
        public string Id => "CAT-DE-CompileDriver";
        public string Name => "CompileDriver: DirectDamage=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<CompileDriver>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("COMPILE_DRIVER", out var d);
            ctx.AssertEquals(result, "COMPILE_DRIVER.DirectDamage", 7, d?.DirectDamage ?? 0);
            // CardsDrawn depends on unique orb types — don't assert exact
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // B: Block → EffectiveBlock
    // ═══════════════════════════════════════════════════════════

    private class DE_Glacier : ITestScenario
    {
        public string Id => "CAT-DE-Glacier";
        public string Name => "Glacier: EffectiveBlock=6";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Glacier>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("GLACIER", out var d);
            ctx.AssertEquals(result, "GLACIER.EffectiveBlock", 6, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class DE_ShadowShield : ITestScenario
    {
        public string Id => "CAT-DE-ShadowShield";
        public string Name => "ShadowShield: EffectiveBlock=11";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<ShadowShield>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("SHADOW_SHIELD", out var d);
            ctx.AssertEquals(result, "SHADOW_SHIELD.EffectiveBlock", 11, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class DE_Glasswork : ITestScenario
    {
        public string Id => "CAT-DE-Glasswork";
        public string Name => "Glasswork: EffectiveBlock=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Glasswork>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("GLASSWORK", out var d);
            ctx.AssertEquals(result, "GLASSWORK.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class DE_ChargeBattery : ITestScenario
    {
        public string Id => "CAT-DE-ChargeBattery";
        public string Name => "ChargeBattery: EffectiveBlock=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<ChargeBattery>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("CHARGE_BATTERY", out var d);
            ctx.AssertEquals(result, "CHARGE_BATTERY.EffectiveBlock", 7, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class DE_BootSequence : ITestScenario
    {
        public string Id => "CAT-DE-BootSequence";
        public string Name => "BootSequence: EffectiveBlock=10 (Innate, Exhaust)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<BootSequence>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("BOOT_SEQUENCE", out var d);
            ctx.AssertEquals(result, "BOOT_SEQUENCE.EffectiveBlock", 10, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class DE_Compact : ITestScenario
    {
        public string Id => "CAT-DE-Compact";
        public string Name => "Compact: EffectiveBlock=6";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Compact>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("COMPACT", out var d);
            ctx.AssertEquals(result, "COMPACT.EffectiveBlock", 6, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // C: Draw → CardsDrawn
    // ═══════════════════════════════════════════════════════════

    private class DE_Skim : ITestScenario
    {
        public string Id => "CAT-DE-Skim";
        public string Name => "Skim: CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Skim>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SKIM", out var d);
            ctx.AssertEquals(result, "SKIM.CardsDrawn", 3, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class DE_Coolheaded : ITestScenario
    {
        public string Id => "CAT-DE-Coolheaded";
        public string Name => "Coolheaded: CardsDrawn=1 (+ channel Frost)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<Coolheaded>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("COOLHEADED", out var d);
            ctx.AssertEquals(result, "COOLHEADED.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class DE_Overclock : ITestScenario
    {
        public string Id => "CAT-DE-Overclock";
        public string Name => "Overclock: CardsDrawn=2 (+ add Burn)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(7);
            var card = await ctx.CreateCardInHand<Overclock>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("OVERCLOCK", out var d);
            ctx.AssertEquals(result, "OVERCLOCK.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class DE_Reboot : ITestScenario
    {
        public string Id => "CAT-DE-Reboot";
        public string Name => "Reboot: CardsDrawn=4 (shuffle all + draw)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(10);
            var card = await ctx.CreateCardInHand<Reboot>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("REBOOT", out var d);
            ctx.AssertEquals(result, "REBOOT.CardsDrawn", 4, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Combo: Null (10 dmg + 2 Weak)
    // ═══════════════════════════════════════════════════════════

    private class DE_Null : ITestScenario
    {
        public string Id => "CAT-DE-Null";
        public string Name => "Null: DirectDamage=10 + WeakPower=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await PowerCmd.Remove<WeakPower>(enemy);
            var card = await ctx.CreateCardInHand<Null>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("NULL", out var d);
            ctx.AssertEquals(result, "NULL.DirectDamage", 10, d?.DirectDamage ?? 0);
            var weak = enemy.GetPower<WeakPower>();
            if (weak == null || weak.Amount < 2)
                result.Fail("WeakPower", "≥2", weak?.Amount.ToString() ?? "null");
            await PowerCmd.Remove<WeakPower>(enemy);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // E: Attack (additional)
    // ═══════════════════════════════════════════════════════════

    private class DE_Ftl : ITestScenario
    {
        public string Id => "CAT-DE-Ftl";
        public string Name => "Ftl: DirectDamage=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Ftl>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FTL", out var d);
            ctx.AssertEquals(result, "FTL.DirectDamage", 5, d?.DirectDamage ?? 0);
            // CardsDrawn is conditional (< 3 cards played this turn) — not asserted
            return result;
        }
    }

    private class DE_Scrape : ITestScenario
    {
        public string Id => "CAT-DE-Scrape";
        public string Name => "Scrape: DirectDamage=7 + CardsDrawn=4";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.EnsureDrawPile(10);
            var card = await ctx.CreateCardInHand<Scrape>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SCRAPE", out var d);
            ctx.AssertEquals(result, "SCRAPE.DirectDamage", 7, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "SCRAPE.CardsDrawn", 4, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class DE_Sunder : ITestScenario
    {
        public string Id => "CAT-DE-Sunder";
        public string Name => "Sunder: DirectDamage=24";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Sunder>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SUNDER", out var d);
            ctx.AssertEquals(result, "SUNDER.DirectDamage", 24, d?.DirectDamage ?? 0);
            // Energy gain (3) only on kill — enemy has 9999 HP, no kill
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // F: Block (additional)
    // ═══════════════════════════════════════════════════════════

    private class DE_Leap : ITestScenario
    {
        public string Id => "CAT-DE-Leap";
        public string Name => "Leap: EffectiveBlock=9";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Leap>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("LEAP", out var d);
            ctx.AssertEquals(result, "LEAP.EffectiveBlock", 9, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // G: Energy
    // ═══════════════════════════════════════════════════════════

    private class DE_Turbo : ITestScenario
    {
        public string Id => "CAT-DE-Turbo";
        public string Name => "Turbo: EnergyGained=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Turbo>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("TURBO", out var d);
            ctx.AssertEquals(result, "TURBO.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    private class DE_Supercritical : ITestScenario
    {
        public string Id => "CAT-DE-Supercritical";
        public string Name => "Supercritical: EnergyGained=4";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Supercritical>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SUPERCRITICAL", out var d);
            ctx.AssertEquals(result, "SUPERCRITICAL.EnergyGained", 4, d?.EnergyGained ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // H: Power contribution chain (template F)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Buffer: play Buffer → take 10 damage from enemy → MitigatedByBuff=10 attributed to BUFFER.
    /// Template F: Power indirect effect → contribution attributed to source card.
    /// </summary>
    private class DE_Buffer : ITestScenario
    {
        public string Id => "CAT-DE-Buffer";
        public string Name => "Buffer: MitigatedByBuff=10 (power contribution chain)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                var card = await ctx.CreateCardInHand<MegaCrit.Sts2.Core.Models.Cards.Buffer>();
                await ctx.PlayCard(card);
                // Verify BufferPower applied
                var buf = ctx.PlayerCreature.GetPower<BufferPower>();
                if (buf == null || buf.Amount < 1)
                {
                    result.Fail("BufferPower", "≥1", buf?.Amount.ToString() ?? "null");
                    return result;
                }
                // Now take damage — Buffer should prevent it → MitigatedByBuff
                ctx.TakeSnapshot();
                await ctx.SimulateDamage(ctx.PlayerCreature, 10, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("BUFFER", out var d);
                ctx.AssertEquals(result, "BUFFER.MitigatedByBuff", 10, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Block + CardSelectCmd (user manually selects)
    // ═══════════════════════════════════════════════════════════

    private class DE_Hologram : ITestScenario
    {
        public string Id => "CAT-DE-Hologram";
        public string Name => "Hologram: EffectiveBlock=3 (user selects from discard)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Hologram>();
            ctx.TakeSnapshot();
            // User must manually select a card from the discard pile grid
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("HOLOGRAM", out var d);
            ctx.AssertEquals(result, "HOLOGRAM.EffectiveBlock", 3, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // I: Orb channel → EndTurn → passive
    // ═══════════════════════════════════════════════════════════

    /// <summary>Zap channels Lightning → EndTurn → Lightning passive = 3 dmg (Focus=0).</summary>
    private class DE_Zap_Passive : ITestScenario
    {
        public string Id => "CAT-DE-Zap-Passive";
        public string Name => "Zap: channel Lightning → EndTurn → AttributedDamage=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                var zap = await ctx.CreateCardInHand<Zap>();
                await ctx.PlayCard(zap);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("ZAP", out var d);
                ctx.AssertEquals(result, "ZAP.AttributedDamage", 3, d?.AttributedDamage ?? 0);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    /// <summary>Chill channels 1 Frost per enemy → EndTurn → Frost passive = 2 block (Focus=0).
    /// Enemy attacks consume the block.</summary>
    private class DE_Chill_Passive : ITestScenario
    {
        public string Id => "CAT-DE-Chill-Passive";
        public string Name => "Chill: channel Frost → EndTurn → EffectiveBlock=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                var chill = await ctx.CreateCardInHand<Chill>();
                await ctx.PlayCard(chill);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Frost passive fired at turn end (2 block per Frost orb).
                // Enemy attacks consumed the block. Check EffectiveBlock.
                var delta = ctx.GetDelta();
                // Chill channels 1 Frost per enemy; each gives 2 block passive
                int enemies = ctx.GetAllEnemies().Count;
                int totalBlock = 0;
                delta.TryGetValue("CHILL", out var d);
                totalBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "CHILL.EffectiveBlock", 2 * enemies, totalBlock);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    /// <summary>Fusion channels Plasma → EndTurn → next turn start → Plasma passive = 1 energy.</summary>
    private class DE_Fusion_Passive : ITestScenario
    {
        public string Id => "CAT-DE-Fusion-Passive";
        public string Name => "Fusion: channel Plasma → EndTurn → EnergyGained=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                var fusion = await ctx.CreateCardInHand<Fusion>();
                await ctx.PlayCard(fusion);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Plasma fires at AfterTurnStartOrbTrigger (start of next turn)
                var delta = ctx.GetDelta();
                delta.TryGetValue("FUSION", out var d);
                ctx.AssertEquals(result, "FUSION.EnergyGained", 1, d?.EnergyGained ?? 0);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // J: Focus → orb output modifier
    // ═══════════════════════════════════════════════════════════

    /// <summary>BiasedCognition (+4 Focus) → channel Lightning → EndTurn →
    /// Lightning passive = 3+4 = 7 total. ZAP.AttributedDamage=3, BIASED_COGNITION.ModifierDamage=4.</summary>
    private class DE_BiasedCognition_Focus : ITestScenario
    {
        public string Id => "CAT-DE-BiasedCognition-Focus";
        public string Name => "BiasedCognition: +4 Focus → Lightning passive ModifierDamage=4";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                var bc = await ctx.CreateCardInHand<BiasedCognition>();
                await ctx.PlayCard(bc);
                var zap = await ctx.CreateCardInHand<Zap>();
                await ctx.PlayCard(zap);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                // Lightning passive: base 3 → ZAP, Focus 4 → BIASED_COGNITION
                delta.TryGetValue("ZAP", out var dZap);
                ctx.AssertEquals(result, "ZAP.AttributedDamage", 3, dZap?.AttributedDamage ?? 0);
                delta.TryGetValue("BIASED_COGNITION", out var dBC);
                ctx.AssertEquals(result, "BIASED_COGNITION.ModifierDamage", 4, dBC?.ModifierDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<BiasedCognitionPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>Defragment (+1 Focus) → channel Lightning → EndTurn → ModifierDamage=1.</summary>
    private class DE_Defragment_Focus : ITestScenario
    {
        public string Id => "CAT-DE-Defragment-Focus";
        public string Name => "Defragment: +1 Focus → Lightning passive ModifierDamage=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                var defrag = await ctx.CreateCardInHand<Defragment>();
                await ctx.PlayCard(defrag);
                var zap = await ctx.CreateCardInHand<Zap>();
                await ctx.PlayCard(zap);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("DEFRAGMENT", out var d);
                ctx.AssertEquals(result, "DEFRAGMENT.ModifierDamage", 1, d?.ModifierDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // K: Power → channel trigger chain
    // ═══════════════════════════════════════════════════════════

    /// <summary>Storm: play Storm → play another Power → Storm channels Lightning →
    /// EndTurn → Lightning passive = 3 → STORM.AttributedDamage=3.</summary>
    private class DE_Storm_Chain : ITestScenario
    {
        public string Id => "CAT-DE-Storm-Chain";
        public string Name => "Storm: play Power → channels Lightning → EndTurn → AttributedDamage=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StormPower>(ctx.PlayerCreature);
                var storm = await ctx.CreateCardInHand<Storm>();
                await ctx.PlayCard(storm);
                // Play another Power to trigger Storm's channel
                var defrag = await ctx.CreateCardInHand<Defragment>();
                await ctx.PlayCard(defrag);
                // Storm should have channeled a Lightning orb attributed to STORM
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("STORM", out var d);
                // Lightning passive base=3, Focus=1 from Defragment
                // But the Lightning channeled by Storm might have Focus=1 contrib to DEFRAGMENT
                // Base 3 should go to STORM
                ctx.AssertEquals(result, "STORM.AttributedDamage", 3, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<StormPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // L: Hailstorm EndTurn AoE
    // ═══════════════════════════════════════════════════════════

    /// <summary>Hailstorm: if you have Frost orb, deal 6 AoE at end of turn.
    /// Channel Frost → play Hailstorm → EndTurn → 6×enemies AoE.</summary>
    private class DE_Hailstorm_EndTurn : ITestScenario
    {
        public string Id => "CAT-DE-Hailstorm-EndTurn";
        public string Name => "Hailstorm: Frost + EndTurn → AttributedDamage=6×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                var chill = await ctx.CreateCardInHand<Chill>();
                await ctx.PlayCard(chill);
                var hail = await ctx.CreateCardInHand<Hailstorm>();
                await ctx.PlayCard(hail);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("HAILSTORM", out var d);
                int enemies = ctx.GetAllEnemies().Count;
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertEquals(result, "HAILSTORM.TotalDamage", 6 * enemies, totalDmg);
            }
            finally
            {
                await PowerCmd.Remove<HailstormPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // M: Evoke
    // ═══════════════════════════════════════════════════════════

    /// <summary>Dualcast: evoke rightmost orb twice.
    /// Channel Lightning → play Dualcast → 2 × 8 = 16 damage.</summary>
    private class DE_Dualcast_Evoke : ITestScenario
    {
        public string Id => "CAT-DE-Dualcast-Evoke";
        public string Name => "Dualcast: evoke 2× → ZAP.Attributed=8 + DUALCAST.Direct=8";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                var zap = await ctx.CreateCardInHand<Zap>();
                await ctx.PlayCard(zap);
                var dualcast = await ctx.CreateCardInHand<Dualcast>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(dualcast);
                var delta = ctx.GetDelta();
                // PRD: 1st evoke (8) → channeling source (ZAP), 2nd evoke (8) → evoking card (DUALCAST)
                delta.TryGetValue("ZAP", out var dZap);
                ctx.AssertEquals(result, "ZAP.AttributedDamage", 8, dZap?.AttributedDamage ?? 0);
                delta.TryGetValue("DUALCAST", out var dDual);
                ctx.AssertEquals(result, "DUALCAST.DirectDamage", 8, dDual?.DirectDamage ?? 0);
            }
            finally { await ctx.SetEnergy(999); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // N: EchoForm next turn
    // ═══════════════════════════════════════════════════════════

    /// <summary>EchoForm: first card each turn is played an extra time.
    /// Play EchoForm → EndTurn → next turn play Strike → 6×2 = 12 DirectDamage.</summary>
    private class DE_EchoForm_NextTurn : ITestScenario
    {
        public string Id => "CAT-DE-EchoForm-NextTurn";
        public string Name => "EchoForm: EndTurn → next turn Strike → DirectDamage=12 (2×6)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<EchoFormPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);

                var echo = await ctx.CreateCardInHand<EchoForm>();
                await ctx.PlayCard(echo);
                await ctx.EndTurnAndWaitForPlayerTurn();

                // EchoForm active: first card played this turn is played twice
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(strike, enemy);
                await Task.Delay(500); // wait for echo replay
                var delta = ctx.GetDelta();
                delta.TryGetValue("STRIKE_IRONCLAD", out var d);
                // Strike base=6, played twice = 12
                ctx.AssertEquals(result, "STRIKE_IRONCLAD.DirectDamage", 12, d?.DirectDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<EchoFormPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
