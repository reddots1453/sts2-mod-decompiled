using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Colorless cards — contribution tracking verification (spec v3).
/// Every test validates delta fields with exact values from KnowledgeBase.
/// Cards already tested in other files (DarkShackles, Finesse, FlashOfSteel, Clash) are excluded.
/// Cards that open CardSelectCmd grids are SKIPPED.
/// </summary>
public static class Catalog_ColorlessCardTests
{
    private const string Cat = "Catalog_ColorlessCards";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // A: Attack → exact DirectDamage
        new CL_DramaticEntrance(),  // 11 AoE dmg
        new CL_Fisticuffs(),        // 7 dmg
        new CL_Omnislice(),         // 8 dmg (single target only)
        new CL_UltimateStrike(),    // 14 dmg
        new CL_Volley(),            // 10 dmg
        new CL_HandOfGreed(),       // 20 dmg
        new CL_Salvo(),             // 12 dmg
        new CL_TagTeam(),           // 11 dmg
        new CL_Knockdown(),         // 10 dmg
        // B: Block → exact EffectiveBlock
        new CL_Equilibrium(),       // 13 block
        // Intercept REMOVED — MultiplayerOnly (TargetType.AnyAlly)
        // Lift REMOVED — MultiplayerOnly (TargetType.AnyAlly)
        new CL_UltimateDefend(),    // 11 block
        new CL_PanicButton(),       // 30 block
        new CL_Rally(),             // 12 block
        new CL_Entrench(),          // doubles existing 10 block → 10 from Entrench
        // C: Draw → exact CardsDrawn
        new CL_MasterOfStrategy(),  // 3 cards
        new CL_HuddleUp(),         // 2 cards
        new CL_Impatience(),        // 2 cards
        // D: Energy → exact EnergyGained
        new CL_Production(),        // 2 energy
        // BelieveInYou REMOVED — MultiplayerOnly (TargetType.AnyAlly)
        // E: Combo
        new CL_Restlessness(),      // CardsDrawn=2 + EnergyGained=2
        new CL_SeekerStrike(),      // DirectDamage=6 (Cards=3 is CardSelectCmd selection)
        new CL_Jackpot(),           // DirectDamage=25 (Cards=3 generates random 0-cost cards)
        // F: Debuff application (Shockwave)
        new CL_Shockwave(),         // Weak=3 + Vuln=3 on all enemies
        // G: Power contribution chain (template F)
        new CL_Prowess(),           // +1 Str +1 Dex → play attack → ModifierDamage=1
        // Coordinate REMOVED — MultiplayerOnly (TargetType.AnyAlly, applies CoordinatePower)
        // H: EndTurn enemy-attack powers
        new CL_Caltrops_Thorns(),   // +3 Thorns → EndTurn → enemy attacks → AttributedDamage≥3
        new CL_Apparition_Intang(), // +1 Intangible → EndTurn → enemy attacks → MitigatedByBuff>0
        // I: Calculated damage (qualitative — deck/state-dependent)
        new CL_MindBlast_Qual(),    // Damage = draw pile size → DirectDamage > 0
        new CL_RipAndTear_Qual(),   // 2-hit random → DirectDamage > 0
        // GENUINELY SKIPPED: Apotheosis (upgrade-all, no contribution field)
        // GENUINELY SKIPPED: DualWield, ThinkingAhead, Scrawl, SecretTechnique, SecretWeapon,
        //   Discovery, Anointed, Alchemize, BeatDown, JackOfAllTrades, Purity, Splash,
        //   Catastrophe (open CardSelectCmd — user manual select; but damage/block not guaranteed)
        // GENUINELY SKIPPED: TheBomb (3 EndTurns), GangUp/GoldAxe (state-dependent),
        //   TheGambit (no-attack condition), Automation/Entropy/Fasten/Mayhem/Nostalgia/
        //   PrepTime/Stratagem/BeaconOfHope/Calamity/EternalArmor (tested in PowerContrib or no contrib field)
    };

    // ═══════════════════════════════════════════════════════════
    // A: Attack → DirectDamage
    // ═══════════════════════════════════════════════════════════

    private class CL_DramaticEntrance : ITestScenario
    {
        public string Id => "CAT-CL-DramaticEntrance";
        public string Name => "DramaticEntrance: AoE DirectDamage=11×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            foreach (var e in ctx.GetAllEnemies())
                await PowerCmd.Remove<VulnerablePower>(e);
            var card = await ctx.CreateCardInHand<DramaticEntrance>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("DRAMATIC_ENTRANCE", out var d);
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "DRAMATIC_ENTRANCE.DirectDamage", 11 * enemies, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_Fisticuffs : ITestScenario
    {
        public string Id => "CAT-CL-Fisticuffs";
        public string Name => "Fisticuffs: DirectDamage=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Fisticuffs>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("FISTICUFFS", out var d);
            ctx.AssertEquals(result, "FISTICUFFS.DirectDamage", 7, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_Omnislice : ITestScenario
    {
        public string Id => "CAT-CL-Omnislice";
        public string Name => "Omnislice: DirectDamage=8";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            // Omnislice deals 8 to target + same to other enemies. With 1 enemy, DirectDamage=8.
            // With multiple, it splashes. We assert on total DirectDamage tracked.
            var card = await ctx.CreateCardInHand<Omnislice>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("OMNISLICE", out var d);
            // With multiple enemies: 8 to target + 8 to each other = 8 × enemyCount
            int enemies = ctx.GetAllEnemies().Count;
            ctx.AssertEquals(result, "OMNISLICE.DirectDamage", 8 * enemies, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_UltimateStrike : ITestScenario
    {
        public string Id => "CAT-CL-UltimateStrike";
        public string Name => "UltimateStrike: DirectDamage=14";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<UltimateStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("ULTIMATE_STRIKE", out var d);
            ctx.AssertEquals(result, "ULTIMATE_STRIKE.DirectDamage", 14, d?.DirectDamage ?? 0);
            return result;
        }
    }

    /// <summary>
    /// Volley: X-cost, 10 dmg × X hits (random target).
    /// SetEnergy(3) → X=3, DirectDamage = 10×3 = 30.
    /// </summary>
    private class CL_Volley : ITestScenario
    {
        public string Id => "CAT-CL-Volley";
        public string Name => "Volley: X-cost, Energy=3 → DirectDamage=30 (10×3)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            // X-cost: set energy to 3 to avoid animation freeze
            await ctx.SetEnergy(3);
            var card = await ctx.CreateCardInHand<Volley>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("VOLLEY", out var d);
            // 10 damage × 3 hits = 30 (random target, all hit same enemy in single-enemy fight)
            ctx.AssertEquals(result, "VOLLEY.DirectDamage", 30, d?.DirectDamage ?? 0);
            await ctx.SetEnergy(999);
            return result;
        }
    }

    private class CL_HandOfGreed : ITestScenario
    {
        public string Id => "CAT-CL-HandOfGreed";
        public string Name => "HandOfGreed: DirectDamage=20";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<HandOfGreed>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("HAND_OF_GREED", out var d);
            ctx.AssertEquals(result, "HAND_OF_GREED.DirectDamage", 20, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_Salvo : ITestScenario
    {
        public string Id => "CAT-CL-Salvo";
        public string Name => "Salvo: DirectDamage=12";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Salvo>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SALVO", out var d);
            ctx.AssertEquals(result, "SALVO.DirectDamage", 12, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_TagTeam : ITestScenario
    {
        public string Id => "CAT-CL-TagTeam";
        public string Name => "TagTeam: DirectDamage=11";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<TagTeam>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("TAG_TEAM", out var d);
            ctx.AssertEquals(result, "TAG_TEAM.DirectDamage", 11, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_Knockdown : ITestScenario
    {
        public string Id => "CAT-CL-Knockdown";
        public string Name => "Knockdown: DirectDamage=10";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Knockdown>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("KNOCKDOWN", out var d);
            ctx.AssertEquals(result, "KNOCKDOWN.DirectDamage", 10, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // B: Block → EffectiveBlock
    // ═══════════════════════════════════════════════════════════

    private class CL_Equilibrium : ITestScenario
    {
        public string Id => "CAT-CL-Equilibrium";
        public string Name => "Equilibrium: EffectiveBlock=13";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Equilibrium>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("EQUILIBRIUM", out var d);
            ctx.AssertEquals(result, "EQUILIBRIUM.EffectiveBlock", 13, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class CL_Intercept : ITestScenario
    {
        public string Id => "CAT-CL-Intercept";
        public string Name => "Intercept: EffectiveBlock=9";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Intercept>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("INTERCEPT", out var d);
            ctx.AssertEquals(result, "INTERCEPT.EffectiveBlock", 9, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class CL_Lift : ITestScenario
    {
        public string Id => "CAT-CL-Lift";
        public string Name => "Lift: EffectiveBlock=11";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Lift>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("LIFT", out var d);
            ctx.AssertEquals(result, "LIFT.EffectiveBlock", 11, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class CL_UltimateDefend : ITestScenario
    {
        public string Id => "CAT-CL-UltimateDefend";
        public string Name => "UltimateDefend: EffectiveBlock=11";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<UltimateDefend>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("ULTIMATE_DEFEND", out var d);
            ctx.AssertEquals(result, "ULTIMATE_DEFEND.EffectiveBlock", 11, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class CL_PanicButton : ITestScenario
    {
        public string Id => "CAT-CL-PanicButton";
        public string Name => "PanicButton: EffectiveBlock=30";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<PanicButton>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("PANIC_BUTTON", out var d);
            ctx.AssertEquals(result, "PANIC_BUTTON.EffectiveBlock", 30, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class CL_Rally : ITestScenario
    {
        public string Id => "CAT-CL-Rally";
        public string Name => "Rally: EffectiveBlock=12";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            var card = await ctx.CreateCardInHand<Rally>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("RALLY", out var d);
            ctx.AssertEquals(result, "RALLY.EffectiveBlock", 12, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    private class CL_Entrench : ITestScenario
    {
        public string Id => "CAT-CL-Entrench";
        public string Name => "Entrench: EffectiveBlock=10 (doubles 10 existing block)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
            // Give 10 block first (unattributed)
            await ctx.GainBlock(ctx.PlayerCreature, 10);
            var card = await ctx.CreateCardInHand<Entrench>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            // Total block now 20 (10 old + 10 from Entrench). SimulateDamage consumes all.
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("ENTRENCH", out var d);
            // Only the 10 block added by Entrench is attributed to ENTRENCH
            ctx.AssertEquals(result, "ENTRENCH.EffectiveBlock", 10, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // C: Draw → CardsDrawn
    // ═══════════════════════════════════════════════════════════

    private class CL_MasterOfStrategy : ITestScenario
    {
        public string Id => "CAT-CL-MasterOfStrategy";
        public string Name => "MasterOfStrategy: CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<MasterOfStrategy>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("MASTER_OF_STRATEGY", out var d);
            ctx.AssertEquals(result, "MASTER_OF_STRATEGY.CardsDrawn", 3, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class CL_HuddleUp : ITestScenario
    {
        public string Id => "CAT-CL-HuddleUp";
        public string Name => "HuddleUp: CardsDrawn=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<HuddleUp>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("HUDDLE_UP", out var d);
            ctx.AssertEquals(result, "HUDDLE_UP.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class CL_Impatience : ITestScenario
    {
        public string Id => "CAT-CL-Impatience";
        public string Name => "Impatience: CardsDrawn=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Impatience>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("IMPATIENCE", out var d);
            ctx.AssertEquals(result, "IMPATIENCE.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D: Energy → EnergyGained
    // ═══════════════════════════════════════════════════════════

    private class CL_Production : ITestScenario
    {
        public string Id => "CAT-CL-Production";
        public string Name => "Production: EnergyGained=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<Production>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("PRODUCTION", out var d);
            ctx.AssertEquals(result, "PRODUCTION.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    private class CL_BelieveInYou : ITestScenario
    {
        public string Id => "CAT-CL-BelieveInYou";
        public string Name => "BelieveInYou: EnergyGained=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<BelieveInYou>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("BELIEVE_IN_YOU", out var d);
            ctx.AssertEquals(result, "BELIEVE_IN_YOU.EnergyGained", 3, d?.EnergyGained ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // E: Combo (damage + draw/energy)
    // ═══════════════════════════════════════════════════════════

    private class CL_Restlessness : ITestScenario
    {
        public string Id => "CAT-CL-Restlessness";
        public string Name => "Restlessness: CardsDrawn=2 + EnergyGained=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Restlessness>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("RESTLESSNESS", out var d);
            ctx.AssertEquals(result, "RESTLESSNESS.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            ctx.AssertEquals(result, "RESTLESSNESS.EnergyGained", 2, d?.EnergyGained ?? 0);
            return result;
        }
    }

    private class CL_SeekerStrike : ITestScenario
    {
        public string Id => "CAT-CL-SeekerStrike";
        public string Name => "SeekerStrike: DirectDamage=6 + CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<SeekerStrike>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("SEEKER_STRIKE", out var d);
            ctx.AssertEquals(result, "SEEKER_STRIKE.DirectDamage", 6, d?.DirectDamage ?? 0);
            return result;
        }
    }

    private class CL_Jackpot : ITestScenario
    {
        public string Id => "CAT-CL-Jackpot";
        public string Name => "Jackpot: DirectDamage=25 + CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.EnsureDrawPile(8);
            var card = await ctx.CreateCardInHand<Jackpot>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("JACKPOT", out var d);
            ctx.AssertEquals(result, "JACKPOT.DirectDamage", 25, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // F: Debuff application (exact values)
    // ═══════════════════════════════════════════════════════════

    private class CL_Shockwave : ITestScenario
    {
        public string Id => "CAT-CL-Shockwave";
        public string Name => "Shockwave: Weak=3 + Vuln=3 on enemy";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            var card = await ctx.CreateCardInHand<Shockwave>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var weak = enemy.GetPower<WeakPower>();
            ctx.AssertEquals(result, "enemy.WeakPower", 3, weak?.Amount ?? 0);
            var vuln = enemy.GetPower<VulnerablePower>();
            ctx.AssertEquals(result, "enemy.VulnerablePower", 3, vuln?.Amount ?? 0);
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // G: Power contribution chain (template F)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Prowess: +1 Str +1 Dex. Play Prowess → play Strike → delta["PROWESS"].ModifierDamage == 1
    /// Template F: power indirect effect → contribution attributed to source card.
    /// </summary>
    private class CL_Prowess : ITestScenario
    {
        public string Id => "CAT-CL-Prowess";
        public string Name => "Prowess: ModifierDamage=1 (Str+1 from power)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);

                var prowess = await ctx.CreateCardInHand<Prowess>();
                await ctx.PlayCard(prowess);

                // Now play a Strike to trigger the Str contribution chain
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(strike, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("PROWESS", out var d);
                ctx.AssertEquals(result, "PROWESS.ModifierDamage", 1, d?.ModifierDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Coordinate: +5 Str. Play Coordinate → play Strike → delta["COORDINATE"].ModifierDamage == 5
    /// Template F: power indirect effect → contribution attributed to source card.
    /// </summary>
    private class CL_Coordinate : ITestScenario
    {
        public string Id => "CAT-CL-Coordinate";
        public string Name => "Coordinate: ModifierDamage=5 (Str+5 from power)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);

                var coordinate = await ctx.CreateCardInHand<Coordinate>();
                await ctx.PlayCard(coordinate);

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(strike, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("COORDINATE", out var d);
                ctx.AssertEquals(result, "COORDINATE.ModifierDamage", 5, d?.ModifierDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // H: EndTurn enemy-attack powers
    // ═══════════════════════════════════════════════════════════

    /// <summary>Caltrops: +3 Thorns → EndTurn → enemy attacks → Thorns reflects 3 damage per hit.</summary>
    private class CL_Caltrops_Thorns : ITestScenario
    {
        public string Id => "CAT-CL-Caltrops-Thorns";
        public string Name => "Caltrops: +3 Thorns → EndTurn → enemy attacks → AttributedDamage≥3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<ThornsPower>(ctx.PlayerCreature);
                var card = await ctx.CreateCardInHand<Caltrops>();
                await ctx.PlayCard(card);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Enemy attacked → Thorns fires → 3 damage per attack
                var delta = ctx.GetDelta();
                delta.TryGetValue("CALTROPS", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "CALTROPS.TotalDamage", 0, totalDmg);
            }
            finally
            {
                await PowerCmd.Remove<ThornsPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>Apparition: +1 Intangible → EndTurn → enemy attacks → MitigatedByBuff > 0.</summary>
    private class CL_Apparition_Intang : ITestScenario
    {
        public string Id => "CAT-CL-Apparition-Intang";
        public string Name => "Apparition: +1 Intangible → EndTurn → enemy attacks → MitigatedByBuff>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                var card = await ctx.CreateCardInHand<Apparition>();
                await ctx.PlayCard(card);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Intangible reduces all damage to 1 → MitigatedByBuff = damage - 1
                var delta = ctx.GetDelta();
                delta.TryGetValue("APPARITION", out var d);
                ctx.AssertGreaterThan(result, "APPARITION.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // I: Calculated damage (qualitative — deck/state-dependent)
    // ═══════════════════════════════════════════════════════════

    /// <summary>MindBlast: Damage = draw pile size. Qualitative: DirectDamage > 0.</summary>
    private class CL_MindBlast_Qual : ITestScenario
    {
        public string Id => "CAT-CL-MindBlast";
        public string Name => "MindBlast: DirectDamage > 0 (deck-dependent)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            await ctx.EnsureDrawPile(5);
            var card = await ctx.CreateCardInHand<MindBlast>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card, enemy);
            var delta = ctx.GetDelta();
            delta.TryGetValue("MIND_BLAST", out var d);
            ctx.AssertGreaterThan(result, "MIND_BLAST.DirectDamage", 0, d?.DirectDamage ?? 0);
            return result;
        }
    }

    /// <summary>RipAndTear: 2 hits on random enemies. Qualitative: DirectDamage > 0.</summary>
    private class CL_RipAndTear_Qual : ITestScenario
    {
        public string Id => "CAT-CL-RipAndTear";
        public string Name => "RipAndTear: DirectDamage > 0 (random target)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var card = await ctx.CreateCardInHand<RipAndTear>();
            ctx.TakeSnapshot();
            await ctx.PlayCard(card);
            var delta = ctx.GetDelta();
            delta.TryGetValue("RIP_AND_TEAR", out var d);
            ctx.AssertGreaterThan(result, "RIP_AND_TEAR.DirectDamage", 0, d?.DirectDamage ?? 0);
            return result;
        }
    }
}
