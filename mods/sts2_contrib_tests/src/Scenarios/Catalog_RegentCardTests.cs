using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Regent cards — contribution tracking verification (spec v3).
/// Every test validates delta fields with exact values from KnowledgeBase.
/// Power cards that require EndTurn to produce contribution are SKIPPED.
/// Cards that open CardSelectCmd grids are SKIPPED.
/// </summary>
public static class Catalog_RegentCardTests
{
    private const string Cat = "Catalog_RegentCards";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // A: Attack → exact DirectDamage
        new RG_SolarStrike(),       // 8 dmg + StarsContribution=1
        new RG_WroughtInWar(),      // 7 dmg
        new RG_BeatIntoShape(),     // 5 dmg
        new RG_Bombardment(),       // 18 dmg
        new RG_CollisionCourse(),   // 9 dmg
        new RG_CelestialMight(),    // 6×3 = 18 dmg
        new RG_AstralPulse(),       // 14 dmg
        new RG_KinglyKick(),        // 24 dmg
        new RG_Stardust(),          // 5 dmg
        new RG_ShiningStrike(),     // 8 dmg + StarsContribution=2
        // B: Attack + debuff
        new RG_FallingStar(),       // 7 dmg + Weak=1 + Vuln=1
        new RG_Comet(),             // 33 dmg + Weak=3 + Vuln=3
        new RG_GammaBlast(),        // 13 dmg + Weak=2 + Vuln=2
        new RG_CrushUnder(),        // 7 AoE dmg
        new RG_MeteorShower(),      // 14 AoE dmg
        new RG_DyingStar(),         // 9 AoE dmg
        // C: Block → exact EffectiveBlock
        new RG_DefendRegent(),      // 5 block
        new RG_GatherLight(),       // 7 block + StarsContribution=1
        new RG_Bulwark(),           // 13 block
        new RG_ParticleWall(),      // 9 block
        new RG_IAmInvincible(),     // 9 block
        new RG_CloakOfStars(),      // 7 block
        new RG_ManifestAuthority(), // 7 block
        new RG_Glitterstream(),     // 11 block
        new RG_Reflect(),           // 17 block + ReflectPower
        new RG_Patter(),            // 8 block + Vigor=2
        // D: Draw → exact CardsDrawn
        new RG_Glow(),              // StarsContribution=1 + CardsDrawn=2
        // Charge REMOVED — CardSelectCmd transformation, not draw
        // BundleOfJoy REMOVED — generates random cards to hand, not draw
        new RG_Prophesize(),        // CardsDrawn=6
        // E: Stars → exact StarsContribution
        new RG_Venerate(),          // StarsContribution=2
        new RG_RoyalGamble(),       // StarsContribution=9
        // F: Energy → exact EnergyGained
        new RG_Alignment(),         // EnergyGained=2
        // Convergence REMOVED — EnergyNextTurnPower + StarNextTurnPower, both next turn
        // RefineBlade REMOVED — EnergyNextTurnPower, next turn (Forge effect not tracked as contribution)
        // G: Combo (damage + draw/energy)
        new RG_GuidingStar(),       // 12 dmg (draw 2 is next turn via DrawCardsNextTurnPower)
        new RG_MakeItSo(),          // 6 dmg (Cards=3 is self-return cycle, not draw)
        new RG_HeavenlyDrill(),     // 8 dmg + EnergyGained=4
        new RG_BigBang(),           // CardsDrawn=1 + EnergyGained=1 + StarsContribution=1
        // H: Forge sub-bar integration
        new RG_ForgeSubBar(),       // Bulwark(Forge 10) → SovereignBlade sub-bar
        // SKIPPED power cards — no direct contribution tracking without EndTurn:
        // Arsenal, BlackHole, ChildOfTheStars, Furnace, Genesis, TheSealedThrone,
        // MonarchsGaze, Orbit, Parry, SwordSage, PaleBlueDot, PillarOfCreation,
        // SpectrumShift — tested in Catalog_PowerContribTests via EndTurn chain
        // SKIPPED cards opening CardSelectCmd: CosmicIndifference, Glimmer
        // SKIPPED deck-dependent calculated: CrescentSpear, Supermassive, LunarBlast
    };

    // ═══════════════════════════════════════════════════════════
    // A: Attack → DirectDamage
    // ═══════════════════════════════════════════════════════════

    private class RG_SolarStrike : ITestScenario
    {
        public string Id => "CAT-RG-SolarStrike";
        public string Name => "SolarStrike: DirectDamage=8 + StarsContribution=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<SolarStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SOLAR_STRIKE", out var d);
            ctx.AssertEquals(result, "SOLAR_STRIKE.DirectDamage", 8, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "SOLAR_STRIKE.StarsContribution", 1, d?.StarsContribution ?? 0);
            return result;
        }
    }

    private class RG_WroughtInWar : ITestScenario
    {
        public string Id => "CAT-RG-WroughtInWar";
        public string Name => "WroughtInWar: DirectDamage=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<WroughtInWar>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("WROUGHT_IN_WAR", out var d);
            ctx.AssertEquals(result, "WROUGHT_IN_WAR.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_BeatIntoShape : ITestScenario
    {
        public string Id => "CAT-RG-BeatIntoShape";
        public string Name => "BeatIntoShape: DirectDamage=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<BeatIntoShape>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BEAT_INTO_SHAPE", out var d);
            ctx.AssertEquals(result, "BEAT_INTO_SHAPE.DirectDamage", 5, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_Bombardment : ITestScenario
    {
        public string Id => "CAT-RG-Bombardment";
        public string Name => "Bombardment: DirectDamage=18";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Bombardment>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BOMBARDMENT", out var d);
            ctx.AssertEquals(result, "BOMBARDMENT.DirectDamage", 18, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_CollisionCourse : ITestScenario
    {
        public string Id => "CAT-RG-CollisionCourse";
        public string Name => "CollisionCourse: DirectDamage=9";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<CollisionCourse>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("COLLISION_COURSE", out var d);
            ctx.AssertEquals(result, "COLLISION_COURSE.DirectDamage", 9, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_CelestialMight : ITestScenario
    {
        public string Id => "CAT-RG-CelestialMight";
        public string Name => "CelestialMight: DirectDamage=18 (6×3)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<CelestialMight>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("CELESTIAL_MIGHT", out var d);
            ctx.AssertEquals(result, "CELESTIAL_MIGHT.DirectDamage", 18, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_AstralPulse : ITestScenario
    {
        public string Id => "CAT-RG-AstralPulse";
        public string Name => "AstralPulse: DirectDamage=14";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<AstralPulse>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ASTRAL_PULSE", out var d);
            ctx.AssertEquals(result, "ASTRAL_PULSE.DirectDamage", 14, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_KinglyKick : ITestScenario
    {
        public string Id => "CAT-RG-KinglyKick";
        public string Name => "KinglyKick: DirectDamage=24";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<KinglyKick>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("KINGLY_KICK", out var d);
            ctx.AssertEquals(result, "KINGLY_KICK.DirectDamage", 24, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_Stardust : ITestScenario
    {
        public string Id => "CAT-RG-Stardust";
        public string Name => "Stardust: DirectDamage=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Stardust>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("STARDUST", out var d);
            ctx.AssertEquals(result, "STARDUST.DirectDamage", 5, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class RG_ShiningStrike : ITestScenario
    {
        public string Id => "CAT-RG-ShiningStrike";
        public string Name => "ShiningStrike: DirectDamage=8 + StarsContribution=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<ShiningStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SHINING_STRIKE", out var d);
            ctx.AssertEquals(result, "SHINING_STRIKE.DirectDamage", 8, d?.DirectDamage ?? 0);
            ctx.AssertEquals(result, "SHINING_STRIKE.StarsContribution", 2, d?.StarsContribution ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // B: Attack + debuff
    // ═══════════════════════════════════════════════════════════

    private class RG_FallingStar : ITestScenario
    {
        public string Id => "CAT-RG-FallingStar";
        public string Name => "FallingStar: DirectDamage=7 + Weak=1 + Vuln=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await PowerCmd.Remove<WeakPower>(enemy);
            var card = await ctx.CreateCardInHand<FallingStar>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FALLING_STAR", out var d);
            ctx.AssertEquals(result, "FALLING_STAR.DirectDamage", 7, d?.DirectDamage ?? 0);
            var weak = enemy.GetPower<WeakPower>();
            ctx.AssertEquals(result, "enemy.WeakPower", 1, weak?.Amount ?? 0);
            var vuln = enemy.GetPower<VulnerablePower>();
            ctx.AssertEquals(result, "enemy.VulnerablePower", 1, vuln?.Amount ?? 0);
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    private class RG_Comet : ITestScenario
    {
        public string Id => "CAT-RG-Comet";
        public string Name => "Comet: DirectDamage=33";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await PowerCmd.Remove<WeakPower>(enemy);
            var card = await ctx.CreateCardInHand<Comet>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("COMET", out var d);
            ctx.AssertEquals(result, "COMET.DirectDamage", 33, d?.DirectDamage ?? 0);
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    private class RG_GammaBlast : ITestScenario
    {
        public string Id => "CAT-RG-GammaBlast";
        public string Name => "GammaBlast: DirectDamage=13 + Weak=2 + Vuln=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await PowerCmd.Remove<WeakPower>(enemy);
            var card = await ctx.CreateCardInHand<GammaBlast>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GAMMA_BLAST", out var d);
            ctx.AssertEquals(result, "GAMMA_BLAST.DirectDamage", 13, d?.DirectDamage ?? 0);
            var weak = enemy.GetPower<WeakPower>();
            ctx.AssertEquals(result, "enemy.WeakPower", 2, weak?.Amount ?? 0);
            var vuln = enemy.GetPower<VulnerablePower>();
            ctx.AssertEquals(result, "enemy.VulnerablePower", 2, vuln?.Amount ?? 0);
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    private class RG_CrushUnder : ITestScenario
    {
        public string Id => "CAT-RG-CrushUnder";
        public string Name => "CrushUnder: AoE DirectDamage=7×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            foreach (var e in ctx.GetAllEnemies())
                await PowerCmd.Remove<VulnerablePower>(e);
            var card = await ctx.CreateCardInHand<CrushUnder>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("CRUSH_UNDER", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "CRUSH_UNDER.DirectDamage", 7 * enemies, d?.DirectDamage ?? 0);
            foreach (var e in ctx.GetAllEnemies())
                await PowerCmd.Remove<CrushUnderPower>(e);
            return result;
        }
    }

    private class RG_MeteorShower : ITestScenario
    {
        public string Id => "CAT-RG-MeteorShower";
        public string Name => "MeteorShower: AoE DirectDamage=14×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            foreach (var e in ctx.GetAllEnemies())
            {
                await PowerCmd.Remove<VulnerablePower>(e);
                await PowerCmd.Remove<WeakPower>(e);
            }
            var card = await ctx.CreateCardInHand<MeteorShower>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("METEOR_SHOWER", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "METEOR_SHOWER.DirectDamage", 14 * enemies, d?.DirectDamage ?? 0);
            foreach (var e in ctx.GetAllEnemies())
            {
                await PowerCmd.Remove<WeakPower>(e);
                await PowerCmd.Remove<VulnerablePower>(e);
            }
            return result;
        }
    }

    private class RG_DyingStar : ITestScenario
    {
        public string Id => "CAT-RG-DyingStar";
        public string Name => "DyingStar: AoE DirectDamage=9×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            foreach (var e in ctx.GetAllEnemies())
                await PowerCmd.Remove<VulnerablePower>(e);
            var card = await ctx.CreateCardInHand<DyingStar>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DYING_STAR", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "DYING_STAR.DirectDamage", 9 * enemies, d?.DirectDamage ?? 0);
            foreach (var e in ctx.GetAllEnemies())
                await PowerCmd.Remove<DyingStarPower>(e);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // C: Block → EffectiveBlock
    // ═══════════════════════════════════════════════════════════

    private class RG_DefendRegent : ITestScenario
    {
        public string Id => "CAT-RG-DefendRegent";
        public string Name => "DefendRegent: EffectiveBlock=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<DefendRegent>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("DEFEND_REGENT", out var d);
            ctx.AssertEquals(result, "DEFEND_REGENT.EffectiveBlock", 5, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_GatherLight : ITestScenario
    {
        public string Id => "CAT-RG-GatherLight";
        public string Name => "GatherLight: EffectiveBlock=7 + StarsContribution=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<GatherLight>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("GATHER_LIGHT", out var d);
            ctx.AssertEquals(result, "GATHER_LIGHT.EffectiveBlock", 7, d?.EffectiveBlock ?? 0);
            ctx.AssertEquals(result, "GATHER_LIGHT.StarsContribution", 1, d?.StarsContribution ?? 0);
            return result;
        }
    }

    private class RG_Bulwark : ITestScenario
    {
        public string Id => "CAT-RG-Bulwark";
        public string Name => "Bulwark: EffectiveBlock=13";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Bulwark>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("BULWARK", out var d);
            ctx.AssertEquals(result, "BULWARK.EffectiveBlock", 13, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_ParticleWall : ITestScenario
    {
        public string Id => "CAT-RG-ParticleWall";
        public string Name => "ParticleWall: EffectiveBlock=9";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<ParticleWall>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("PARTICLE_WALL", out var d);
            ctx.AssertEquals(result, "PARTICLE_WALL.EffectiveBlock", 9, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_IAmInvincible : ITestScenario
    {
        public string Id => "CAT-RG-IAmInvincible";
        public string Name => "IAmInvincible: EffectiveBlock=9";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<IAmInvincible>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("I_AM_INVINCIBLE", out var d);
            ctx.AssertEquals(result, "I_AM_INVINCIBLE.EffectiveBlock", 9, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_CloakOfStars : ITestScenario
    {
        public string Id => "CAT-RG-CloakOfStars";
        public string Name => "CloakOfStars: EffectiveBlock=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<CloakOfStars>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("CLOAK_OF_STARS", out var d);
            ctx.AssertEquals(result, "CLOAK_OF_STARS.EffectiveBlock", 7, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_ManifestAuthority : ITestScenario
    {
        public string Id => "CAT-RG-ManifestAuthority";
        public string Name => "ManifestAuthority: EffectiveBlock=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<ManifestAuthority>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("MANIFEST_AUTHORITY", out var d);
            ctx.AssertEquals(result, "MANIFEST_AUTHORITY.EffectiveBlock", 7, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_Glitterstream : ITestScenario
    {
        public string Id => "CAT-RG-Glitterstream";
        public string Name => "Glitterstream: EffectiveBlock=11";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Glitterstream>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("GLITTERSTREAM", out var d);
            ctx.AssertEquals(result, "GLITTERSTREAM.EffectiveBlock", 11, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class RG_Reflect : ITestScenario
    {
        public string Id => "CAT-RG-Reflect";
        public string Name => "Reflect: EffectiveBlock=17";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<ReflectPower>(ctx.PlayerCreature);
                var card = await ctx.CreateCardInHand<Reflect>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("REFLECT", out var d);
                ctx.AssertEquals(result, "REFLECT.EffectiveBlock", 17, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<ReflectPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    private class RG_Patter : ITestScenario
    {
        public string Id => "CAT-RG-Patter";
        public string Name => "Patter: EffectiveBlock=8";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<VigorPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Patter>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("PATTER", out var d);
            ctx.AssertEquals(result, "PATTER.EffectiveBlock", 8, d?.EffectiveBlock ?? 0);
            await PowerCmd.Remove<VigorPower>(ctx.PlayerCreature);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D: Draw → CardsDrawn
    // ═══════════════════════════════════════════════════════════

    private class RG_Glow : ITestScenario
    {
        public string Id => "CAT-RG-Glow";
        public string Name => "Glow: StarsContribution=1 + CardsDrawn=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Glow>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GLOW", out var d);
            ctx.AssertEquals(result, "GLOW.StarsContribution", 1, d?.StarsContribution ?? 0);
            ctx.AssertEquals(result, "GLOW.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class RG_Charge : ITestScenario
    {
        public string Id => "CAT-RG-Charge";
        public string Name => "Charge: CardsDrawn=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Charge>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("CHARGE", out var d);
            ctx.AssertEquals(result, "CHARGE.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class RG_BundleOfJoy : ITestScenario
    {
        public string Id => "CAT-RG-BundleOfJoy";
        public string Name => "BundleOfJoy: CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<BundleOfJoy>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BUNDLE_OF_JOY", out var d);
            ctx.AssertEquals(result, "BUNDLE_OF_JOY.CardsDrawn", 3, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class RG_Prophesize : ITestScenario
    {
        public string Id => "CAT-RG-Prophesize";
        public string Name => "Prophesize: CardsDrawn=6";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(12);
            var card = await ctx.CreateCardInHand<Prophesize>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("PROPHESIZE", out var d);
            ctx.AssertEquals(result, "PROPHESIZE.CardsDrawn", 6, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // E: Stars → StarsContribution
    // ═══════════════════════════════════════════════════════════

    private class RG_Venerate : ITestScenario
    {
        public string Id => "CAT-RG-Venerate";
        public string Name => "Venerate: StarsContribution=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Venerate>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("VENERATE", out var d);
            ctx.AssertEquals(result, "VENERATE.StarsContribution", 2, d?.StarsContribution ?? 0);
            return result;
        }
    }

    private class RG_RoyalGamble : ITestScenario
    {
        public string Id => "CAT-RG-RoyalGamble";
        public string Name => "RoyalGamble: StarsContribution=9";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<RoyalGamble>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ROYAL_GAMBLE", out var d);
            ctx.AssertEquals(result, "ROYAL_GAMBLE.StarsContribution", 9, d?.StarsContribution ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // F: Energy → EnergyGained
    // ═══════════════════════════════════════════════════════════

    private class RG_Alignment : ITestScenario
    {
        public string Id => "CAT-RG-Alignment";
        public string Name => "Alignment: EnergyGained=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Alignment>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ALIGNMENT", out var d);
            ctx.AssertEquals(result, "ALIGNMENT.EnergyGained", 2, d?.EnergyGained ?? 0);
            await ctx.SetEnergy(999);
            return result;
        }
    }

    private class RG_Convergence : ITestScenario
    {
        public string Id => "CAT-RG-Convergence";
        public string Name => "Convergence: EnergyGained=1 + StarsContribution=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Convergence>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("CONVERGENCE", out var d);
            ctx.AssertEquals(result, "CONVERGENCE.EnergyGained", 1, d?.EnergyGained ?? 0);
            ctx.AssertEquals(result, "CONVERGENCE.StarsContribution", 1, d?.StarsContribution ?? 0);
            return result;
        }
    }

    private class RG_RefineBlade : ITestScenario
    {
        public string Id => "CAT-RG-RefineBlade";
        public string Name => "RefineBlade: EnergyGained=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<RefineBlade>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("REFINE_BLADE", out var d);
            ctx.AssertEquals(result, "REFINE_BLADE.EnergyGained", 1, d?.EnergyGained ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // G: Combo (damage + draw/energy)
    // ═══════════════════════════════════════════════════════════

    private class RG_GuidingStar : ITestScenario
    {
        public string Id => "CAT-RG-GuidingStar";
        public string Name => "GuidingStar: DirectDamage=12 (draw 2 is next turn)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<GuidingStar>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("GUIDING_STAR", out var d);
            ctx.AssertEquals(result, "GUIDING_STAR.DirectDamage", 12, d?.DirectDamage ?? 0);
            // CardsDrawn via DrawCardsNextTurnPower — tested in CAT-PWR-DrawCardsNextTurn
            return result;
        }
    }

    private class RG_MakeItSo : ITestScenario
    {
        public string Id => "CAT-RG-MakeItSo";
        public string Name => "MakeItSo: DirectDamage=6 (Cards=3 is self-return cycle)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<MakeItSo>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("MAKE_IT_SO", out var d);
            ctx.AssertEquals(result, "MAKE_IT_SO.DirectDamage", 6, d?.DirectDamage ?? 0);
            return result;
        }
    }

    /// <summary>
    /// HeavenlyDrill: X-cost, 8 dmg × X hits (doubled if X≥4).
    /// SetEnergy(3) → X=3 (not doubled), DirectDamage = 8×3 = 24.
    /// </summary>
    private class RG_HeavenlyDrill : ITestScenario
    {
        public string Id => "CAT-RG-HeavenlyDrill";
        public string Name => "HeavenlyDrill: X-cost, Energy=3 → DirectDamage=24 (8×3)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            // X-cost: set energy to 3 to avoid animation freeze (3 < 4 threshold, no doubling)
            await ctx.SetEnergy(3);
            var card = await ctx.CreateCardInHand<HeavenlyDrill>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("HEAVENLY_DRILL", out var d);
            // 8 damage × 3 hits = 24
            ctx.AssertEquals(result, "HEAVENLY_DRILL.DirectDamage", 24, d?.DirectDamage ?? 0);
            await ctx.SetEnergy(999);
            return result;
        }
    }

    private class RG_BigBang : ITestScenario
    {
        public string Id => "CAT-RG-BigBang";
        public string Name => "BigBang: CardsDrawn=1 + EnergyGained=1 + StarsContribution=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<BigBang>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BIG_BANG", out var d);
            ctx.AssertEquals(result, "BIG_BANG.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            ctx.AssertEquals(result, "BIG_BANG.EnergyGained", 1, d?.EnergyGained ?? 0);
            ctx.AssertEquals(result, "BIG_BANG.StarsContribution", 1, d?.StarsContribution ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // H: Forge sub-bar integration
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Play Bulwark (Forge 10) then SovereignBlade. Verify:
    /// SOVEREIGN_BLADE has DirectDamage (base 10 + forge 10 = 20)
    /// Sub-bar entry FORGE:BULWARK with DirectDamage = 10
    /// </summary>
    private class RG_ForgeSubBar : ITestScenario
    {
        public string Id => "CAT-RG-ForgeSubBar";
        public string Name => "ForgeSubBar: Bulwark(Forge 10) → SovereignBlade=20, FORGE:BULWARK=10";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);

            // Step 1: Play Bulwark (Block 13 + Forge 10)
            await ctx.ClearBlock();
            var bulwark = await ctx.CreateCardInHand<Bulwark>();
            await ctx.PlayCard(bulwark);
            await Task.Delay(200);

            // Step 2: Get or create SovereignBlade in hand
            var hand = PileType.Hand.GetPile(ctx.Player);
            var blade = hand.Cards.FirstOrDefault(c => c is SovereignBlade) as SovereignBlade;
            if (blade == null)
            {
                blade = ctx.CombatState.CreateCard<SovereignBlade>(ctx.Player);
                await CardPileCmd.Add(blade, PileType.Hand, skipVisuals: true);
                await Task.Delay(100);
            }

            ctx.TakeSnapshot();
            await ctx.PlayCard(blade, enemy);
            var delta = ctx.GetDelta();

            // SovereignBlade base 10 + forge 10 = 20
            delta.TryGetValue("SOVEREIGN_BLADE", out var sbData);
            ctx.AssertEquals(result, "SOVEREIGN_BLADE.DirectDamage", 20, sbData?.DirectDamage ?? 0);

            // Forge sub-bar: FORGE:BULWARK should have 10
            delta.TryGetValue("FORGE:BULWARK", out var forgeData);
            ctx.AssertEquals(result, "FORGE:BULWARK.DirectDamage", 10, forgeData?.DirectDamage ?? 0);

            return result;
        }
    }
}
