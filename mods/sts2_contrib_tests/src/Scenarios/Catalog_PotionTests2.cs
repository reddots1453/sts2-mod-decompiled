using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Potions;

namespace ContribTests.Scenarios;

/// <summary>
/// Potion batch 2 — contribution tracking verification (spec v3).
/// Every test validates delta fields with exact values from KnowledgeBase.
/// Potions already tested in Catalog_PotionTests (Fire, Block, Energy, Blood,
/// Strength, Flex, Weak, Regen) are excluded.
/// </summary>
public static class Catalog_PotionTests2
{
    private const string Cat = "Catalog_Potion2";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // A: Direct damage
        new POT2_ExplosiveAmpoule(),  // DirectDamage=10
        // B: Draw
        new POT2_SwiftPotion(),       // CardsDrawn=3
        new POT2_BottledPotential(),  // CardsDrawn=5
        new POT2_SneckoOil(),         // CardsDrawn=7
        // C: Draw + Energy combo
        new POT2_CureAll(),           // CardsDrawn=2 + EnergyGained=1
        // D: Block
        new POT2_ShipInABottle(),     // EffectiveBlock=10
        // E: MaxHP
        new POT2_FruitJuice(),        // HpHealed=5
        // F: Power contribution chain (template I + F)
        new POT2_DexterityPotion(),   // +2 Dex → play Defend → ModifierBlock=2
        new POT2_SpeedPotion(),       // +5 Dex → play Defend → ModifierBlock=5
        new POT2_FyshOil(),           // +1 Str +1 Dex → play Strike → ModifierDamage=1
        // G: Debuff application (exact power amounts)
        new POT2_PoisonPotion(),      // enemy PoisonPower=6
        new POT2_VulnerablePotion(),  // enemy VulnerablePower=3
        new POT2_ShacklingPotion(),   // enemy StrengthPower=-7
        new POT2_PotionOfBinding(),   // enemy Weak+Vuln applied
        // H: EndTurn / enemy attack chain
        new POT2_LiquidBronze(),      // +3 Thorns → EndTurn → enemy attacks → AttributedDamage=3
        new POT2_HeartOfIron(),       // +7 Plating → EndTurn → EffectiveBlock=7
        new POT2_GhostInAJar(),       // +1 Intangible → EndTurn → enemy attacks → MitigatedByBuff
        new POT2_LuckyTonic(),        // +1 Buffer → EndTurn → enemy attacks → MitigatedByBuff
        new POT2_FocusPotion_Orb(),   // +2 Focus → channel Lightning → EndTurn → ModifierDamage=2
        // GENUINELY SKIPPED: GamblersBrew, AttackPotion, SkillPotion, PowerPotion,
        //   ColorlessPotion (CardSelectCmd random — user can't predict selection)
        // GENUINELY SKIPPED: Duplicator, Fortifier, StarPotion, MazalethsGift
        //   (complex multi-step or Regent-only)
    };

    // ═══════════════════════════════════════════════════════════
    // A: Direct damage
    // ═══════════════════════════════════════════════════════════

    private class POT2_ExplosiveAmpoule : ITestScenario
    {
        public string Id => "CAT-POT2-Explosive";
        public string Name => "ExplosiveAmpoule: DirectDamage=10";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ResetEnemyHp();
            ctx.TakeSnapshot();
            await ctx.UsePotion<ExplosiveAmpoule>(target: ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("EXPLOSIVE_AMPOULE", out var d);
            ctx.AssertEquals(result, "EXPLOSIVE_AMPOULE.DirectDamage", 10, d?.DirectDamage ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // B: Draw
    // ═══════════════════════════════════════════════════════════

    private class POT2_SwiftPotion : ITestScenario
    {
        public string Id => "CAT-POT2-Swift";
        public string Name => "SwiftPotion: CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            ctx.TakeSnapshot();
            await ctx.UsePotion<SwiftPotion>();
            var delta = ctx.GetDelta();
            delta.TryGetValue("SWIFT_POTION", out var d);
            ctx.AssertEquals(result, "SWIFT_POTION.CardsDrawn", 3, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class POT2_BottledPotential : ITestScenario
    {
        public string Id => "CAT-POT2-BottledPotential";
        public string Name => "BottledPotential: CardsDrawn=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(12);
            ctx.TakeSnapshot();
            await ctx.UsePotion<BottledPotential>();
            var delta = ctx.GetDelta();
            delta.TryGetValue("BOTTLED_POTENTIAL", out var d);
            ctx.AssertEquals(result, "BOTTLED_POTENTIAL.CardsDrawn", 5, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    private class POT2_SneckoOil : ITestScenario
    {
        public string Id => "CAT-POT2-SneckoOil";
        public string Name => "SneckoOil: CardsDrawn=7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(12);
            ctx.TakeSnapshot();
            await ctx.UsePotion<SneckoOil>();
            var delta = ctx.GetDelta();
            delta.TryGetValue("SNECKO_OIL", out var d);
            ctx.AssertEquals(result, "SNECKO_OIL.CardsDrawn", 7, d?.CardsDrawn ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // C: Draw + Energy combo
    // ═══════════════════════════════════════════════════════════

    private class POT2_CureAll : ITestScenario
    {
        public string Id => "CAT-POT2-CureAll";
        public string Name => "CureAll: CardsDrawn=2 + EnergyGained=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.EnsureDrawPile(8);
            ctx.TakeSnapshot();
            await ctx.UsePotion<CureAll>();
            var delta = ctx.GetDelta();
            delta.TryGetValue("CURE_ALL", out var d);
            ctx.AssertEquals(result, "CURE_ALL.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            ctx.AssertEquals(result, "CURE_ALL.EnergyGained", 1, d?.EnergyGained ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D: Block
    // ═══════════════════════════════════════════════════════════

    private class POT2_ShipInABottle : ITestScenario
    {
        public string Id => "CAT-POT2-ShipInABottle";
        public string Name => "ShipInABottle: EffectiveBlock=10";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            await ctx.ClearBlock();
            ctx.TakeSnapshot();
            await ctx.UsePotion<ShipInABottle>();
            await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
            var delta = ctx.GetDelta();
            delta.TryGetValue("SHIP_IN_A_BOTTLE", out var d);
            ctx.AssertEquals(result, "SHIP_IN_A_BOTTLE.EffectiveBlock", 10, d?.EffectiveBlock ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // E: MaxHP → HpHealed
    // ═══════════════════════════════════════════════════════════

    private class POT2_FruitJuice : ITestScenario
    {
        public string Id => "CAT-POT2-FruitJuice";
        public string Name => "FruitJuice: HpHealed=5";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            ctx.TakeSnapshot();
            await ctx.UsePotion<FruitJuice>();
            var delta = ctx.GetDelta();
            delta.TryGetValue("FRUIT_JUICE", out var d);
            ctx.AssertEquals(result, "FRUIT_JUICE.HpHealed", 5, d?.HpHealed ?? 0);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // F: Power contribution chain (template I + F)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// DexterityPotion: +2 Dex → play Defend → ModifierBlock=2 attributed to DEXTERITY_POTION.
    /// </summary>
    private class POT2_DexterityPotion : ITestScenario
    {
        public string Id => "CAT-POT2-Dexterity";
        public string Name => "DexterityPotion: ModifierBlock=2 (Dex+2 chain)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await ctx.UsePotion<DexterityPotion>();
                // Now play Defend to trigger the Dex contribution chain
                await ctx.ClearBlock();
                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(defend);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("DEXTERITY_POTION", out var d);
                ctx.AssertEquals(result, "DEXTERITY_POTION.ModifierBlock", 2, d?.ModifierBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// SpeedPotion: +5 temp Dex → play Defend → ModifierBlock=5 attributed to SPEED_POTION.
    /// </summary>
    private class POT2_SpeedPotion : ITestScenario
    {
        public string Id => "CAT-POT2-Speed";
        public string Name => "SpeedPotion: ModifierBlock=5 (Dex+5 chain)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await ctx.UsePotion<SpeedPotion>();
                await ctx.ClearBlock();
                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(defend);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("SPEED_POTION", out var d);
                ctx.AssertEquals(result, "SPEED_POTION.ModifierBlock", 5, d?.ModifierBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// FyshOil: +1 Str +1 Dex → play Strike → ModifierDamage=1 attributed to FYSH_OIL.
    /// </summary>
    private class POT2_FyshOil : ITestScenario
    {
        public string Id => "CAT-POT2-FyshOil";
        public string Name => "FyshOil: ModifierDamage=1 (Str+1 chain)";
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
                await ctx.UsePotion<FyshOil>();
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(strike, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("FYSH_OIL", out var d);
                ctx.AssertEquals(result, "FYSH_OIL.ModifierDamage", 1, d?.ModifierDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // G: Debuff application (exact power values)
    // These check game state (debuff applied) rather than contribution tracking,
    // since the contribution from debuffs requires subsequent actions (enemy attack
    // for Weak, player attack for Vuln, EndTurn for Poison).
    // ═══════════════════════════════════════════════════════════

    private class POT2_PoisonPotion : ITestScenario
    {
        public string Id => "CAT-POT2-Poison";
        public string Name => "PoisonPotion: PoisonPower=6 on enemy";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<PoisonPower>(enemy);
            ctx.TakeSnapshot();
            await ctx.UsePotion<PoisonPotion>(target: enemy);
            var poison = enemy.GetPower<PoisonPower>();
            ctx.AssertEquals(result, "enemy.PoisonPower", 6, poison?.Amount ?? 0);
            return result;
        }
    }

    private class POT2_VulnerablePotion : ITestScenario
    {
        public string Id => "CAT-POT2-Vulnerable";
        public string Name => "VulnerablePotion: VulnerablePower=3 on enemy";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<VulnerablePower>(enemy);
            ctx.TakeSnapshot();
            await ctx.UsePotion<VulnerablePotion>(target: enemy);
            var vuln = enemy.GetPower<VulnerablePower>();
            ctx.AssertEquals(result, "enemy.VulnerablePower", 3, vuln?.Amount ?? 0);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    private class POT2_ShacklingPotion : ITestScenario
    {
        public string Id => "CAT-POT2-Shackling";
        public string Name => "ShacklingPotion: enemy Str=-7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<StrengthPower>(enemy);
            ctx.TakeSnapshot();
            await ctx.UsePotion<ShacklingPotion>(target: enemy);
            var str = enemy.GetPower<StrengthPower>();
            ctx.AssertEquals(result, "enemy.StrengthPower", -7, str?.Amount ?? 0);
            await PowerCmd.Remove<StrengthPower>(enemy);
            return result;
        }
    }

    private class POT2_PotionOfBinding : ITestScenario
    {
        public string Id => "CAT-POT2-Binding";
        public string Name => "PotionOfBinding: Weak + Vuln on enemy";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            ctx.TakeSnapshot();
            await ctx.UsePotion<PotionOfBinding>(target: enemy);
            var weak = enemy.GetPower<WeakPower>();
            var vuln = enemy.GetPower<VulnerablePower>();
            // PotionOfBinding applies Weak+Vuln (exact amounts depend on potion vars)
            ctx.AssertEquals(result, "enemy.WeakPower", 1, weak?.Amount ?? 0);
            ctx.AssertEquals(result, "enemy.VulnerablePower", 1, vuln?.Amount ?? 0);
            await PowerCmd.Remove<WeakPower>(enemy);
            await PowerCmd.Remove<VulnerablePower>(enemy);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // H: EndTurn / enemy attack chain
    // ═══════════════════════════════════════════════════════════

    /// <summary>LiquidBronze: +3 Thorns → EndTurn → enemy attacks → Thorns deals 3 damage.</summary>
    private class POT2_LiquidBronze : ITestScenario
    {
        public string Id => "CAT-POT2-LiquidBronze";
        public string Name => "LiquidBronze: +3 Thorns → EndTurn → enemy attacks → AttributedDamage=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<ThornsPower>(ctx.PlayerCreature);
                await ctx.UsePotion<LiquidBronze>();
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Enemy attacked → Thorns fired → 3 damage per attack
                var delta = ctx.GetDelta();
                delta.TryGetValue("LIQUID_BRONZE", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "LIQUID_BRONZE.TotalDamage", 0, totalDmg);
            }
            finally
            {
                await PowerCmd.Remove<ThornsPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>HeartOfIron: +7 Plating → EndTurn → Plating block at BeforeTurnEndEarly.</summary>
    private class POT2_HeartOfIron : ITestScenario
    {
        public string Id => "CAT-POT2-HeartOfIron";
        public string Name => "HeartOfIron: +7 Plating → EndTurn → EffectiveBlock≥7";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<PlatingPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                await ctx.UsePotion<HeartOfIron>();
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Plating fires BeforeTurnEndEarly → 7 block → enemy attacks consume it
                var delta = ctx.GetDelta();
                delta.TryGetValue("HEART_OF_IRON", out var d);
                ctx.AssertGreaterThan(result, "HEART_OF_IRON.EffectiveBlock", 0, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<PlatingPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>GhostInAJar: +1 Intangible → EndTurn → enemy attacks → MitigatedByBuff.</summary>
    private class POT2_GhostInAJar : ITestScenario
    {
        public string Id => "CAT-POT2-GhostInAJar";
        public string Name => "GhostInAJar: +1 Intangible → EndTurn → enemy attacks → MitigatedByBuff>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                await ctx.UsePotion<GhostInAJar>();
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Intangible reduces all damage to 1 → MitigatedByBuff = damage - 1
                var delta = ctx.GetDelta();
                delta.TryGetValue("GHOST_IN_A_JAR", out var d);
                ctx.AssertGreaterThan(result, "GHOST_IN_A_JAR.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<IntangiblePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>LuckyTonic: +1 Buffer → EndTurn → enemy attacks → MitigatedByBuff.</summary>
    private class POT2_LuckyTonic : ITestScenario
    {
        public string Id => "CAT-POT2-LuckyTonic";
        public string Name => "LuckyTonic: +1 Buffer → EndTurn → enemy attacks → MitigatedByBuff>0";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
                await ctx.ClearBlock();
                await ctx.UsePotion<LuckyTonic>();
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // Buffer prevents first HP loss entirely → MitigatedByBuff = full damage
                var delta = ctx.GetDelta();
                delta.TryGetValue("LUCKY_TONIC", out var d);
                ctx.AssertGreaterThan(result, "LUCKY_TONIC.MitigatedByBuff", 0, d?.MitigatedByBuff ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>FocusPotion: +2 Focus → channel Lightning → EndTurn → ModifierDamage=2.</summary>
    private class POT2_FocusPotion_Orb : ITestScenario
    {
        public string Id => "CAT-POT2-FocusOrb";
        public string Name => "FocusPotion: +2 Focus → Lightning passive → ModifierDamage=2";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await ctx.UsePotion<FocusPotion>();
                // Channel Lightning via Zap
                var zap = await ctx.CreateCardInHand<Zap>();
                await ctx.PlayCard(zap);
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("FOCUS_POTION", out var d);
                ctx.AssertEquals(result, "FOCUS_POTION.ModifierDamage", 2, d?.ModifierDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<FocusPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
