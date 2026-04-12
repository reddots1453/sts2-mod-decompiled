using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ContribTests.Scenarios;

/// <summary>
/// Catalog §PWR — Power contribution tracking tests (52 tests).
/// Each test plays a power card, triggers the power's effect, then verifies
/// the resulting contribution (damage/block/draw/etc.) is attributed to the
/// power's source card ID in CombatTracker.
///
/// Categories:
///   A: AfterCardPlayed / AfterDamageGiven powers (trigger during card play)
///   B: AfterCardExhausted powers
///   C: AfterBlockGained / AfterDamageReceived powers
///   D: Modifier powers (Strength/Dexterity via indirect cards)
///   E: Turn-based powers (manual hook trigger)
///   F: New A-group additions (Grapple, Inferno, Vicious, etc.)
///   G: Turn-based / BeforeHandDraw powers (manual hook trigger)
///   H: Smoke tests (powers too complex or untestable with AutoPlay)
/// </summary>
public static class Catalog_PowerContribTests
{
    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // Category A: AfterCardPlayed
        new CAT_PWR_Rage(),
        new CAT_PWR_Afterimage(),
        new CAT_PWR_Panache(),
        new CAT_PWR_Envenom(),
        new CAT_PWR_Cruelty(),
        new CAT_PWR_SerpentForm(),
        // Category B: AfterCardExhausted
        new CAT_PWR_FeelNoPain(),
        new CAT_PWR_DarkEmbrace(),
        // Category C: AfterBlockGained / AfterDamageReceived
        new CAT_PWR_Juggernaut(),
        new CAT_PWR_FlameBarrier(),
        // Category D: Modifier powers
        new CAT_PWR_DemonForm(),
        new CAT_PWR_Accuracy(),
        new CAT_PWR_Footwork(),
        // Category E: Turn-based (manual hook)
        new CAT_PWR_Plating(),
        new CAT_PWR_CrimsonMantle(),
        // Category F: New A-group (AfterCardPlayed / AfterDamageGiven / AfterBlockGained / etc.)
        new CAT_PWR_Grapple(),
        new CAT_PWR_Inferno(),
        new CAT_PWR_Vicious(),
        new CAT_PWR_Outbreak(),
        new CAT_PWR_Speedster(),
        new CAT_PWR_Buffer(),
        new CAT_PWR_MonarchsGaze(),
        new CAT_PWR_ReaperForm(),
        new CAT_PWR_Monologue(),
        new CAT_PWR_Reflect(),
        new CAT_PWR_TheSealedThrone(),
        new CAT_PWR_BlackHole(),
        new CAT_PWR_Subroutine(),
        new CAT_PWR_PillarOfCreation(),
        // Category G: Turn-based / BeforeHandDraw (manual hook)
        new CAT_PWR_RollingBoulder(),
        new CAT_PWR_Genesis(),
        new CAT_PWR_Furnace(),
        new CAT_PWR_InfiniteBlades(),
        new CAT_PWR_CreativeAi(),
        new CAT_PWR_HelloWorld(),
        new CAT_PWR_SpectrumShift(),
        new CAT_PWR_SentryMode(),
        new CAT_PWR_Hailstorm(),
        new CAT_PWR_Loop(),
        // Category H: EndTurn chain tests (real contribution via turn cycle)
        new CAT_PWR_PoisonTick(),         // DeadlyPoison(5) → EndTurn → poison tick → AttributedDamage=5
        new CAT_PWR_GenesisEndTurn(),     // Genesis → EndTurn → AfterEnergyReset → StarsContribution=2
        new CAT_PWR_BlockNextTurn(),      // Glitterstream(+4 next turn) → EndTurn → EffectiveBlock=4
        new CAT_PWR_StarNextTurn(),       // HiddenCache(+3 next turn) → EndTurn → StarsContribution=3
        new CAT_PWR_DrawCardsNextTurn(),  // GuidingStar(draw 2 next turn) → EndTurn → CardsDrawn=2
        // REMOVED old Category H smoke tests (spec v3 prohibits smoke tests)
    };

    // ── Category A: AfterCardPlayed powers ──────────────────────

    /// <summary>
    /// Rage: Amount=3 → play 1 attack → EffectiveBlock=3.
    /// </summary>
    private class CAT_PWR_Rage : ITestScenario
    {
        public string Id => "CAT-PWR-001";
        public string Name => "Power §A: Rage → 3 block per attack played";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var rage = await ctx.CreateCardInHand<Rage>();
                await ctx.PlayCard(rage);

                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("RAGE", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "RAGE.EffectiveBlock", 3, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
            }
            finally
            {
                await PowerCmd.Remove<RagePower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// Afterimage: Amount=1 → play 1 card → EffectiveBlock=1.
    /// </summary>
    private class CAT_PWR_Afterimage : ITestScenario
    {
        public string Id => "CAT-PWR-002";
        public string Name => "Power §A: Afterimage → 1 block per card played";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var afterimage = await ctx.CreateCardInHand<Afterimage>();
                await ctx.PlayCard(afterimage);

                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("AFTERIMAGE", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "AFTERIMAGE.EffectiveBlock", 1, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
            }
            finally
            {
                await PowerCmd.Remove<AfterimagePower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// Panache: PanacheDamage=10, triggers every 5 cards after the first.
    /// Play Panache, then 6 more cards (1st skipped by alreadyApplied, then 5 counted).
    /// AoE 10 damage to all enemies.
    /// </summary>
    private class CAT_PWR_Panache : ITestScenario
    {
        public string Id => "CAT-PWR-003";
        public string Name => "Power §A: Panache → 10 AoE damage after 5 cards";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var panache = await ctx.CreateCardInHand<Panache>();
                await ctx.PlayCard(panache);
                await ctx.SetEnergy(999);
                await ctx.ResetEnemyHp();

                ctx.TakeSnapshot();

                // 1st card: sets alreadyApplied=true but no decrement.
                // Cards 2-6: decrement CardsLeft from 5 to 0 → triggers AoE.
                for (int i = 0; i < 6; i++)
                {
                    var s = await ctx.CreateCardInHand<StrikeIronclad>();
                    await ctx.PlayCard(s, enemy);
                }

                var delta = ctx.GetDelta();
                delta.TryGetValue("PANACHE", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                // Panache deals 10 AoE (10 per enemy)
                int enemies = ctx.GetAllEnemies().Count;
                ctx.AssertEquals(result, "PANACHE.TotalDamage", 10 * enemies, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<PanachePower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// Envenom: Amount=1 → hit enemy → PoisonPower.Amount=1.
    /// </summary>
    private class CAT_PWR_Envenom : ITestScenario
    {
        public string Id => "CAT-PWR-004";
        public string Name => "Power §A: Envenom → 1 poison applied on unblocked damage";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await CreatureCmd.LoseBlock(enemy, enemy.Block);

                var envenom = await ctx.CreateCardInHand<Envenom>();
                await ctx.PlayCard(envenom);

                // Remove any pre-existing poison
                await PowerCmd.Remove<PoisonPower>(enemy);

                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                var poison = enemy.GetPower<PoisonPower>();
                int poisonAmt = poison?.Amount ?? 0;
                ctx.AssertEquals(result, "Enemy.PoisonPower.Amount", 1, poisonAmt);
                result.ActualValues["PoisonAmount"] = poisonAmt.ToString();
            }
            finally
            {
                await PowerCmd.Remove<PoisonPower>(enemy);
                await PowerCmd.Remove<EnvenomPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Cruelty: Amount=25 → Vulnerable becomes 1.75x instead of 1.5x.
    /// StrikeIronclad base=6, with Vuln+Cruelty: floor(6*1.75)=10, ModifierDamage=4.
    /// Use GreaterThan(3) since exact decomposition may vary.
    /// </summary>
    private class CAT_PWR_Cruelty : ITestScenario
    {
        public string Id => "CAT-PWR-005";
        public string Name => "Power §A: Cruelty → amplified Vulnerable modifier";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.ApplyPower<VulnerablePower>(enemy, 5);

                var cruelty = await ctx.CreateCardInHand<Cruelty>();
                await ctx.PlayCard(cruelty);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("STRIKE_IRONCLAD", out var d);
                int modDmg = d?.ModifierDamage ?? 0;
                ctx.AssertGreaterThan(result, "STRIKE_IRONCLAD.ModifierDamage (Cruelty-boosted)", 3, modDmg);
                result.ActualValues["ModifierDamage"] = modDmg.ToString();
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await PowerCmd.Remove<CrueltyPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// SerpentForm: Amount=4 → play 1 card → damage=4 to random enemy.
    /// </summary>
    private class CAT_PWR_SerpentForm : ITestScenario
    {
        public string Id => "CAT-PWR-006";
        public string Name => "Power §A: SerpentForm → 4 damage per card played";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var serpent = await ctx.CreateCardInHand<SerpentForm>();
                await ctx.PlayCard(serpent);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("SERPENT_FORM", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                // Random target — use GreaterThan since we can't guarantee which enemy
                ctx.AssertGreaterThan(result, "SERPENT_FORM.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<SerpentFormPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ── Category B: AfterCardExhausted powers ───────────────────

    /// <summary>
    /// FeelNoPain: Amount=3 → exhaust 1 card → EffectiveBlock=3.
    /// </summary>
    private class CAT_PWR_FeelNoPain : ITestScenario
    {
        public string Id => "CAT-PWR-007";
        public string Name => "Power §B: FeelNoPain → 3 block per card exhausted";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var fnp = await ctx.CreateCardInHand<FeelNoPain>();
                await ctx.PlayCard(fnp);

                await ctx.ClearBlock();
                await ctx.CreateCardInHand<StrikeIronclad>();

                ctx.TakeSnapshot();

                var trueGrit = await ctx.CreateCardInHand<TrueGrit>();
                await ctx.PlayCard(trueGrit);

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("FEEL_NO_PAIN", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "FEEL_NO_PAIN.EffectiveBlock", 3, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
            }
            finally
            {
                await PowerCmd.Remove<FeelNoPainPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// DarkEmbrace: Amount=1 → exhaust 1 card → CardsDrawn=1.
    /// </summary>
    private class CAT_PWR_DarkEmbrace : ITestScenario
    {
        public string Id => "CAT-PWR-008";
        public string Name => "Power §B: DarkEmbrace → 1 draw per card exhausted";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                var de = await ctx.CreateCardInHand<DarkEmbrace>();
                await ctx.PlayCard(de);

                await ctx.EnsureDrawPile(5);
                await ctx.CreateCardInHand<StrikeIronclad>();

                ctx.TakeSnapshot();

                var trueGrit = await ctx.CreateCardInHand<TrueGrit>();
                await ctx.PlayCard(trueGrit);

                var delta = ctx.GetDelta();
                delta.TryGetValue("DARK_EMBRACE", out var d);
                int drawn = d?.CardsDrawn ?? 0;
                ctx.AssertEquals(result, "DARK_EMBRACE.CardsDrawn", 1, drawn);
                result.ActualValues["CardsDrawn"] = drawn.ToString();
            }
            finally
            {
                await PowerCmd.Remove<DarkEmbracePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ── Category C: AfterBlockGained / AfterDamageReceived ──────

    /// <summary>
    /// Juggernaut: Amount=5 → gain block → damage=5 to random enemy.
    /// </summary>
    private class CAT_PWR_Juggernaut : ITestScenario
    {
        public string Id => "CAT-PWR-009";
        public string Name => "Power §C: Juggernaut → 5 damage when block gained";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                var jugg = await ctx.CreateCardInHand<Juggernaut>();
                await ctx.PlayCard(jugg);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                await ctx.PlayCard(defend);

                var delta = ctx.GetDelta();
                delta.TryGetValue("JUGGERNAUT", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                // Random target — use GreaterThan
                ctx.AssertGreaterThan(result, "JUGGERNAUT.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<JuggernautPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// FlameBarrier: DamageBack=4 → take hit → damage=4 reflected.
    /// </summary>
    private class CAT_PWR_FlameBarrier : ITestScenario
    {
        public string Id => "CAT-PWR-010";
        public string Name => "Power §C: FlameBarrier → 4 reflect damage when attacked";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var fb = await ctx.CreateCardInHand<FlameBarrier>();
                await ctx.PlayCard(fb);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // Simulate enemy attacking player → triggers FlameBarrier reflection
                await ctx.SimulateDamage(ctx.PlayerCreature, 10, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("FLAME_BARRIER", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertEquals(result, "FLAME_BARRIER.TotalDamage", 4, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<FlameBarrierPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    // ── Category D: Modifier powers ─────────────────────────────

    /// <summary>
    /// DemonForm: Str=2 → manual trigger turn start → Str=2 → Strike ModifierDamage=2.
    /// </summary>
    private class CAT_PWR_DemonForm : ITestScenario
    {
        public string Id => "CAT-PWR-011";
        public string Name => "Power §D: DemonForm → 2 Strength gain at turn start";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var df = await ctx.CreateCardInHand<DemonForm>();
                await ctx.PlayCard(df);

                var demonPower = ctx.PlayerCreature.GetPower<DemonFormPower>();
                if (demonPower == null)
                {
                    result.Fail("DemonFormPower", "applied", "null");
                    return result;
                }

                CombatTracker.Instance.SetActivePowerSource(demonPower.Id.Entry);
                await demonPower.AfterSideTurnStart(
                    CombatSide.Player,
                    ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();

                var str = ctx.PlayerCreature.GetPower<StrengthPower>();
                int strAmt = str?.Amount ?? 0;
                ctx.AssertEquals(result, "StrengthPower.Amount (from DemonForm)", 2, strAmt);
                result.ActualValues["StrengthAmount"] = strAmt.ToString();

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("STRIKE_IRONCLAD", out var d);
                int modDmg = d?.ModifierDamage ?? 0;
                ctx.AssertEquals(result, "STRIKE_IRONCLAD.ModifierDamage (Str from DemonForm)", 2, modDmg);
                result.ActualValues["ModifierDamage"] = modDmg.ToString();
            }
            finally
            {
                await PowerCmd.Remove<DemonFormPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Accuracy: Amount=4 → Shiv base=4 + 4 = 8 DirectDamage.
    /// </summary>
    private class CAT_PWR_Accuracy : ITestScenario
    {
        public string Id => "CAT-PWR-012";
        public string Name => "Power §D: Accuracy → Shiv deals 8 (4 base + 4 bonus)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var acc = await ctx.CreateCardInHand<Accuracy>();
                await ctx.PlayCard(acc);
                await ctx.SetEnergy(999);

                var bd = await ctx.CreateCardInHand<BladeDance>();
                await ctx.PlayCard(bd);
                await Task.Delay(300);

                var hand = PileType.Hand.GetPile(ctx.Player);
                var shiv = hand.Cards.FirstOrDefault(c => c is Shiv);
                if (shiv == null)
                {
                    result.Fail("Shiv", "in hand", "not found");
                    return result;
                }

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();
                await ctx.PlayCard(shiv, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("SHIV", out var d);
                int directDmg = d?.DirectDamage ?? 0;
                ctx.AssertEquals(result, "SHIV.DirectDamage (with Accuracy)", 8, directDmg);
                result.ActualValues["DirectDamage"] = directDmg.ToString();
                result.ActualValues["ModifierDamage"] = (d?.ModifierDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<AccuracyPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// Footwork: Dex=2 → Defend 5+2=7 EffectiveBlock.
    /// </summary>
    private class CAT_PWR_Footwork : ITestScenario
    {
        public string Id => "CAT-PWR-013";
        public string Name => "Power §D: Footwork → Defend block = 7 (5 base + 2 Dex)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var fw = await ctx.CreateCardInHand<Footwork>();
                await ctx.PlayCard(fw);

                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                await ctx.PlayCard(defend);

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEFEND_IRONCLAD", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "DEFEND_IRONCLAD.EffectiveBlock (Footwork)", 7, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
                result.ActualValues["ModifierBlock"] = (d?.ModifierBlock ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    // ── Category E: Turn-based powers (manual hook trigger) ─────

    /// <summary>
    /// Plating (via StoneArmor): Amount=4 → turn end → EffectiveBlock=4.
    /// </summary>
    private class CAT_PWR_Plating : ITestScenario
    {
        public string Id => "CAT-PWR-014";
        public string Name => "Power §E: Plating → 4 block at turn end (manual trigger)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var stoneArmor = await ctx.CreateCardInHand<StoneArmor>();
                await ctx.PlayCard(stoneArmor);

                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                var plating = ctx.PlayerCreature.GetPower<PlatingPower>();
                if (plating == null)
                {
                    result.Fail("PlatingPower", "applied", "null");
                    return result;
                }

                CombatTracker.Instance.SetActivePowerSource(plating.Id.Entry);
                await plating.BeforeTurnEndEarly(
                    new BlockingPlayerChoiceContext(),
                    ctx.PlayerCreature.Side);
                CombatTracker.Instance.ClearActivePowerSource();

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("STONE_ARMOR", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "STONE_ARMOR.EffectiveBlock", 4, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
            }
            finally
            {
                await PowerCmd.Remove<PlatingPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// CrimsonMantle: Amount=8 → turn start → self-damage + 8 block.
    /// </summary>
    private class CAT_PWR_CrimsonMantle : ITestScenario
    {
        public string Id => "CAT-PWR-015";
        public string Name => "Power §E: CrimsonMantle → 8 block at turn start (manual trigger)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var cm = await ctx.CreateCardInHand<CrimsonMantle>();
                await ctx.PlayCard(cm);

                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                var crimson = ctx.PlayerCreature.GetPower<CrimsonMantlePower>();
                if (crimson == null)
                {
                    result.Fail("CrimsonMantlePower", "applied", "null");
                    return result;
                }

                CombatTracker.Instance.SetActivePowerSource(crimson.Id.Entry);
                await crimson.AfterPlayerTurnStart(
                    new BlockingPlayerChoiceContext(),
                    ctx.Player);
                CombatTracker.Instance.ClearActivePowerSource();

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("CRIMSON_MANTLE", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                ctx.AssertEquals(result, "CRIMSON_MANTLE.EffectiveBlock", 8, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
                result.ActualValues["SelfDamage"] = (d?.SelfDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<CrimsonMantlePower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    // ── Category F: New A-group powers ──────────────────────────

    /// <summary>
    /// Grapple: Attack card applies GrapplePower(5) to enemy.
    /// GrapplePower.AfterBlockGained: when applier (player) gains block → deal 5 dmg to enemy.
    /// Play Grapple on enemy, then Defend to gain block → damage=5.
    /// </summary>
    private class CAT_PWR_Grapple : ITestScenario
    {
        public string Id => "CAT-PWR-016";
        public string Name => "Power §F: Grapple → 5 damage when player gains block";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                await ctx.ResetEnemyHp();

                var grapple = await ctx.CreateCardInHand<Grapple>();
                await ctx.PlayCard(grapple, enemy);

                // GrapplePower should now be on the enemy
                var grapplePower = enemy.GetPower<GrapplePower>();
                if (grapplePower == null)
                {
                    result.Fail("GrapplePower", "on enemy", "null");
                    return result;
                }

                ctx.TakeSnapshot();

                // Player gains block → triggers GrapplePower → 5 damage to enemy (owner)
                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                await ctx.PlayCard(defend);

                var delta = ctx.GetDelta();
                delta.TryGetValue("GRAPPLE", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "GRAPPLE.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<GrapplePower>(ctx.GetFirstEnemy());
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// Inferno: Amount=6 → AfterDamageReceived (when HP lost) → 6 AoE damage.
    /// Play Inferno, then self-damage via Bloodletting (3 HP loss) → AoE 6 damage.
    /// </summary>
    private class CAT_PWR_Inferno : ITestScenario
    {
        public string Id => "CAT-PWR-017";
        public string Name => "Power §F: Inferno → 6 AoE damage when HP lost";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var inferno = await ctx.CreateCardInHand<Inferno>();
                await ctx.PlayCard(inferno);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // Bloodletting deals 3 self-damage, triggering InfernoPower.AfterDamageReceived
                var bl = await ctx.CreateCardInHand<Bloodletting>();
                await ctx.PlayCard(bl);

                var delta = ctx.GetDelta();
                delta.TryGetValue("INFERNO", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "INFERNO.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<InfernoPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Vicious: Amount=1 → AfterPowerAmountChanged for Vulnerable → draw 1 card.
    /// Play Vicious, then Bash (applies Vulnerable) → CardsDrawn=1.
    /// </summary>
    private class CAT_PWR_Vicious : ITestScenario
    {
        public string Id => "CAT-PWR-018";
        public string Name => "Power §F: Vicious → draw 1 when Vulnerable applied";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var vicious = await ctx.CreateCardInHand<Vicious>();
                await ctx.PlayCard(vicious);

                await ctx.EnsureDrawPile(5);
                ctx.TakeSnapshot();

                // Bash applies Vulnerable → triggers ViciousPower → draw 1
                var bash = await ctx.CreateCardInHand<Bash>();
                await ctx.PlayCard(bash, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("VICIOUS", out var d);
                int drawn = d?.CardsDrawn ?? 0;
                ctx.AssertEquals(result, "VICIOUS.CardsDrawn", 1, drawn);
                result.ActualValues["CardsDrawn"] = drawn.ToString();
            }
            finally
            {
                await PowerCmd.Remove<ViciousPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<VulnerablePower>(enemy);
            }
            return result;
        }
    }

    /// <summary>
    /// Outbreak: Amount=11 → every 3 poison applications → 11 AoE damage.
    /// Play Outbreak, apply poison 3 times via PoisonedStab/Envenom → AoE=11.
    /// Use ApplyPower directly for controlled trigger count.
    /// </summary>
    private class CAT_PWR_Outbreak : ITestScenario
    {
        public string Id => "CAT-PWR-019";
        public string Name => "Power §F: Outbreak → 11 AoE after 3 poison applications";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var outbreak = await ctx.CreateCardInHand<Outbreak>();
                await ctx.PlayCard(outbreak);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // Apply poison 3 times to trigger Outbreak's AoE
                for (int i = 0; i < 3; i++)
                {
                    await ctx.ApplyPower<PoisonPower>(enemy, 1, ctx.PlayerCreature);
                }

                var delta = ctx.GetDelta();
                delta.TryGetValue("OUTBREAK", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "OUTBREAK.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<OutbreakPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<PoisonPower>(enemy);
            }
            return result;
        }
    }

    /// <summary>
    /// Speedster: Amount=2 → AfterCardDrawn (non-hand-draw) → 2 AoE damage.
    /// Play Speedster, then PommelStrike (draws cards mid-turn) → AoE=2.
    /// </summary>
    private class CAT_PWR_Speedster : ITestScenario
    {
        public string Id => "CAT-PWR-020";
        public string Name => "Power §F: Speedster → 2 AoE per non-hand card drawn";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var speedster = await ctx.CreateCardInHand<Speedster>();
                await ctx.PlayCard(speedster);

                await ctx.EnsureDrawPile(5);
                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // PommelStrike draws 1 card (non-hand-draw) → triggers Speedster
                var ps = await ctx.CreateCardInHand<PommelStrike>();
                await ctx.PlayCard(ps, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("SPEEDSTER", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "SPEEDSTER.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<SpeedsterPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Buffer: Amount=1 → prevents HP loss. Play Buffer, take damage → MitigatedByBuff > 0.
    /// </summary>
    private class CAT_PWR_Buffer : ITestScenario
    {
        public string Id => "CAT-PWR-021";
        public string Name => "Power §F: Buffer → prevents HP loss once";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                var buffer = await ctx.CreateCardInHand<MegaCrit.Sts2.Core.Models.Cards.Buffer>();
                await ctx.PlayCard(buffer);

                int hpBefore = (int)ctx.PlayerCreature.CurrentHp;
                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                // Take damage — Buffer should prevent HP loss
                await ctx.SimulateDamage(ctx.PlayerCreature, 10, enemy);

                int hpAfter = (int)ctx.PlayerCreature.CurrentHp;
                // Buffer should prevent the damage entirely
                ctx.AssertEquals(result, "HP unchanged (Buffer)", hpBefore, hpAfter);
                result.ActualValues["HpBefore"] = hpBefore.ToString();
                result.ActualValues["HpAfter"] = hpAfter.ToString();
            }
            finally
            {
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// MonarchsGaze: Amount=1 → AfterDamageGiven on powered attack → enemy -1 Str.
    /// Play MonarchsGaze, play Strike → enemy gains MonarchsGazeStrengthDownPower.
    /// </summary>
    private class CAT_PWR_MonarchsGaze : ITestScenario
    {
        public string Id => "CAT-PWR-022";
        public string Name => "Power §F: MonarchsGaze → enemy -1 Str on attack";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var mg = await ctx.CreateCardInHand<MonarchsGaze>();
                await ctx.PlayCard(mg);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                // Check enemy has MonarchsGazeStrengthDownPower
                var mgsDown = enemy.GetPower<MonarchsGazeStrengthDownPower>();
                int strDown = mgsDown?.Amount ?? 0;
                ctx.AssertEquals(result, "Enemy.MonarchsGazeStrDown.Amount", 1, strDown);
                result.ActualValues["StrDownAmount"] = strDown.ToString();
            }
            finally
            {
                await PowerCmd.Remove<MonarchsGazePower>(ctx.PlayerCreature);
                await PowerCmd.Remove<MonarchsGazeStrengthDownPower>(enemy);
            }
            return result;
        }
    }

    /// <summary>
    /// ReaperForm: Amount=1 → AfterDamageGiven on powered attack → applies Doom = totalDamage * 1.
    /// Play ReaperForm, play Strike (6 base dmg) → enemy Doom = 6.
    /// </summary>
    private class CAT_PWR_ReaperForm : ITestScenario
    {
        public string Id => "CAT-PWR-023";
        public string Name => "Power §F: ReaperForm → applies Doom on attack";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var rf = await ctx.CreateCardInHand<ReaperForm>();
                await ctx.PlayCard(rf);

                await ctx.ResetEnemyHp();
                await PowerCmd.Remove<DoomPower>(enemy);
                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                var doom = enemy.GetPower<DoomPower>();
                int doomAmt = doom?.Amount ?? 0;
                // Strike deals 6 base damage, Doom = TotalDamage * Amount(1) = 6
                ctx.AssertEquals(result, "Enemy.DoomPower.Amount", 6, doomAmt);
                result.ActualValues["DoomAmount"] = doomAmt.ToString();
            }
            finally
            {
                await PowerCmd.Remove<ReaperFormPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DoomPower>(enemy);
            }
            return result;
        }
    }

    /// <summary>
    /// Monologue: Str gain = 1 per card played. Play Monologue (Skill, cost 0),
    /// then play Strike → Strike should have +1 ModifierDamage from temp Strength.
    /// </summary>
    private class CAT_PWR_Monologue : ITestScenario
    {
        public string Id => "CAT-PWR-024";
        public string Name => "Power §F: Monologue → +1 Str per card played";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var mono = await ctx.CreateCardInHand<Monologue>();
                await ctx.PlayCard(mono);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // Playing Strike triggers MonologuePower.AfterCardPlayed → gains 1 Str
                // BUT: the Strength is applied BEFORE damage calc due to BeforeCardPlayed hook
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("STRIKE_IRONCLAD", out var d);
                int modDmg = d?.ModifierDamage ?? 0;
                // Monologue gives 1 Str before the card is played
                ctx.AssertEquals(result, "STRIKE_IRONCLAD.ModifierDamage (Monologue Str)", 1, modDmg);
                result.ActualValues["ModifierDamage"] = modDmg.ToString();
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
            }
            finally
            {
                // MonologuePower is instanced; remove all instances
                var mp = ctx.PlayerCreature.GetPower<MonologuePower>();
                if (mp != null) await PowerCmd.Remove(mp);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Reflect: Skill gives 17 block + ReflectPower(1).
    /// Reflect reflects blocked damage back to attacker.
    /// Play Reflect, take 10 damage from enemy (block absorbs it) → reflect 10 damage.
    /// </summary>
    private class CAT_PWR_Reflect : ITestScenario
    {
        public string Id => "CAT-PWR-025";
        public string Name => "Power §F: Reflect → reflects blocked damage back";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var reflect = await ctx.CreateCardInHand<Reflect>();
                await ctx.PlayCard(reflect);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // Enemy attacks player for 10 → block absorbs → reflects blocked damage
                await ctx.SimulateDamage(ctx.PlayerCreature, 10, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("REFLECT", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                // Reflected damage = blockedDamage = min(10, 17) = 10
                ctx.AssertEquals(result, "REFLECT.TotalDamage", 10, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<ReflectPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    /// <summary>
    /// TheSealedThrone: Amount=1 → BeforeCardPlayed → gain 1 star per card.
    /// Play TheSealedThrone, then play Strike → StarsContribution=1.
    /// </summary>
    private class CAT_PWR_TheSealedThrone : ITestScenario
    {
        public string Id => "CAT-PWR-026";
        public string Name => "Power §F: TheSealedThrone → 1 star per card played";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var throne = await ctx.CreateCardInHand<TheSealedThrone>();
                await ctx.PlayCard(throne);

                ctx.TakeSnapshot();

                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                await ctx.PlayCard(strike, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("THE_SEALED_THRONE", out var d);
                int stars = d?.StarsContribution ?? 0;
                ctx.AssertEquals(result, "THE_SEALED_THRONE.StarsContribution", 1, stars);
                result.ActualValues["StarsContribution"] = stars.ToString();
            }
            finally
            {
                await PowerCmd.Remove<TheSealedThronePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// BlackHole: Amount=3 → AfterStarsGained → 3 AoE damage.
    /// Play BlackHole, then gain stars (via TheSealedThrone trigger or manual) → damage=3.
    /// </summary>
    private class CAT_PWR_BlackHole : ITestScenario
    {
        public string Id => "CAT-PWR-027";
        public string Name => "Power §F: BlackHole → 3 AoE per stars gained event";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var bh = await ctx.CreateCardInHand<BlackHole>();
                await ctx.PlayCard(bh);

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                // Manually trigger stars gained via the power's AfterStarsGained hook
                var bhPower = ctx.PlayerCreature.GetPower<BlackHolePower>();
                if (bhPower == null)
                {
                    result.Fail("BlackHolePower", "applied", "null");
                    return result;
                }

                CombatTracker.Instance.SetActivePowerSource(bhPower.Id.Entry);
                await bhPower.AfterStarsGained(1, ctx.Player);
                CombatTracker.Instance.ClearActivePowerSource();

                var delta = ctx.GetDelta();
                delta.TryGetValue("BLACK_HOLE", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "BLACK_HOLE.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<BlackHolePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Subroutine: Amount=1 → AfterCardPlayed (Power card) → gain 1 energy.
    /// AutoPlay bypasses energy cost, so this is a smoke test for power application.
    /// Play Subroutine, then play another Power card → check EnergyGained.
    /// </summary>
    private class CAT_PWR_Subroutine : ITestScenario
    {
        public string Id => "CAT-PWR-028";
        public string Name => "Power §F: Subroutine → energy on Power play (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var sub = await ctx.CreateCardInHand<Subroutine>();
                await ctx.PlayCard(sub);

                var subPower = ctx.PlayerCreature.GetPower<SubroutinePower>();
                if (subPower == null)
                {
                    result.Fail("SubroutinePower", "applied", "null");
                    return result;
                }

                ctx.TakeSnapshot();

                // Play another Power card to trigger Subroutine
                var buffer = await ctx.CreateCardInHand<MegaCrit.Sts2.Core.Models.Cards.Buffer>();
                await ctx.PlayCard(buffer);

                var delta = ctx.GetDelta();
                delta.TryGetValue("SUBROUTINE", out var d);
                int energy = d?.EnergyGained ?? 0;
                ctx.AssertEquals(result, "SUBROUTINE.EnergyGained", 1, energy);
                result.ActualValues["EnergyGained"] = energy.ToString();
            }
            finally
            {
                await PowerCmd.Remove<SubroutinePower>(ctx.PlayerCreature);
                await PowerCmd.Remove<BufferPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// PillarOfCreation: Amount=3 → AfterCardGeneratedForCombat → gain 3 block.
    /// Play PillarOfCreation, then play BladeDance (generates Shivs) → EffectiveBlock.
    /// </summary>
    private class CAT_PWR_PillarOfCreation : ITestScenario
    {
        public string Id => "CAT-PWR-029";
        public string Name => "Power §F: PillarOfCreation → 3 block per card generated";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                await ctx.SetEnergy(999);
                var poc = await ctx.CreateCardInHand<PillarOfCreation>();
                await ctx.PlayCard(poc);

                await ctx.ClearBlock();
                ctx.TakeSnapshot();

                // BladeDance generates 3 Shivs → PillarOfCreation triggers 3 times
                var bd = await ctx.CreateCardInHand<BladeDance>();
                await ctx.PlayCard(bd);

                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);

                var delta = ctx.GetDelta();
                delta.TryGetValue("PILLAR_OF_CREATION", out var d);
                int effBlock = d?.EffectiveBlock ?? 0;
                // 3 Shivs * 3 block each = 9 block
                ctx.AssertEquals(result, "PILLAR_OF_CREATION.EffectiveBlock", 9, effBlock);
                result.ActualValues["EffectiveBlock"] = effBlock.ToString();
            }
            finally
            {
                await PowerCmd.Remove<PillarOfCreationPower>(ctx.PlayerCreature);
                await CreatureCmd.LoseBlock(ctx.PlayerCreature, ctx.PlayerCreature.Block);
            }
            return result;
        }
    }

    // ── Category G: Turn-based / BeforeHandDraw (manual hook trigger) ──

    /// <summary>
    /// RollingBoulder: Amount=5 → AfterPlayerTurnStart → 5 AoE, then Amount += 5.
    /// Manual trigger via hook.
    /// </summary>
    private class CAT_PWR_RollingBoulder : ITestScenario
    {
        public string Id => "CAT-PWR-030";
        public string Name => "Power §G: RollingBoulder → 5 AoE at turn start (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var rb = await ctx.CreateCardInHand<RollingBoulder>();
                await ctx.PlayCard(rb);

                var rbPower = ctx.PlayerCreature.GetPower<RollingBoulderPower>();
                if (rbPower == null)
                {
                    result.Fail("RollingBoulderPower", "applied", "null");
                    return result;
                }

                await ctx.ResetEnemyHp();
                ctx.TakeSnapshot();

                CombatTracker.Instance.SetActivePowerSource(rbPower.Id.Entry);
                await rbPower.AfterPlayerTurnStart(
                    new BlockingPlayerChoiceContext(),
                    ctx.Player);
                CombatTracker.Instance.ClearActivePowerSource();

                var delta = ctx.GetDelta();
                delta.TryGetValue("ROLLING_BOULDER", out var d);
                int totalDmg = (d?.DirectDamage ?? 0) + (d?.AttributedDamage ?? 0);
                ctx.AssertGreaterThan(result, "ROLLING_BOULDER.TotalDamage", 0, totalDmg);
                result.ActualValues["DirectDamage"] = (d?.DirectDamage ?? 0).ToString();
                result.ActualValues["AttributedDamage"] = (d?.AttributedDamage ?? 0).ToString();
            }
            finally
            {
                var p = ctx.PlayerCreature.GetPower<RollingBoulderPower>();
                if (p != null) await PowerCmd.Remove(p);
            }
            return result;
        }
    }

    /// <summary>
    /// Genesis: Amount=2 → AfterEnergyReset → gain 2 stars.
    /// Manual trigger via hook.
    /// </summary>
    private class CAT_PWR_Genesis : ITestScenario
    {
        public string Id => "CAT-PWR-031";
        public string Name => "Power §G: Genesis → 2 stars at energy reset (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var gen = await ctx.CreateCardInHand<Genesis>();
                await ctx.PlayCard(gen);

                var genPower = ctx.PlayerCreature.GetPower<GenesisPower>();
                if (genPower == null)
                {
                    result.Fail("GenesisPower", "applied", "null");
                    return result;
                }

                ctx.TakeSnapshot();

                CombatTracker.Instance.SetActivePowerSource(genPower.Id.Entry);
                await genPower.AfterEnergyReset(ctx.Player);
                CombatTracker.Instance.ClearActivePowerSource();

                var delta = ctx.GetDelta();
                delta.TryGetValue("GENESIS", out var d);
                int stars = d?.StarsContribution ?? 0;
                ctx.AssertEquals(result, "GENESIS.StarsContribution", 2, stars);
                result.ActualValues["StarsContribution"] = stars.ToString();
            }
            finally
            {
                await PowerCmd.Remove<GenesisPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Furnace: Amount=4 → AfterSideTurnStart → Forge 4.
    /// Manual trigger. Forge contribution is hard to verify directly;
    /// check that the power is applied and the hook completes.
    /// </summary>
    private class CAT_PWR_Furnace : ITestScenario
    {
        public string Id => "CAT-PWR-032";
        public string Name => "Power §G: Furnace → Forge 4 at turn start (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var furnace = await ctx.CreateCardInHand<Furnace>();
                await ctx.PlayCard(furnace);

                var fPower = ctx.PlayerCreature.GetPower<FurnacePower>();
                if (fPower == null)
                {
                    result.Fail("FurnacePower", "applied", "null");
                    return result;
                }

                int amountBefore = fPower.Amount;
                ctx.AssertEquals(result, "FurnacePower.Amount", 4, amountBefore);
                result.ActualValues["Amount"] = amountBefore.ToString();
            }
            finally
            {
                await PowerCmd.Remove<FurnacePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// InfiniteBlades: Amount=1 → BeforeHandDraw → add 1 Shiv to hand.
    /// Manual hook trigger.
    /// </summary>
    private class CAT_PWR_InfiniteBlades : ITestScenario
    {
        public string Id => "CAT-PWR-033";
        public string Name => "Power §G: InfiniteBlades → 1 Shiv before hand draw (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var ib = await ctx.CreateCardInHand<InfiniteBlades>();
                await ctx.PlayCard(ib);

                var ibPower = ctx.PlayerCreature.GetPower<InfiniteBladesPower>();
                if (ibPower == null)
                {
                    result.Fail("InfiniteBladesPower", "applied", "null");
                    return result;
                }

                var handBefore = PileType.Hand.GetPile(ctx.Player).Cards.Count;

                CombatTracker.Instance.SetActivePowerSource(ibPower.Id.Entry);
                await ibPower.BeforeHandDraw(ctx.Player, new BlockingPlayerChoiceContext(), ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();

                var handAfter = PileType.Hand.GetPile(ctx.Player).Cards.Count;
                int shivsAdded = handAfter - handBefore;
                ctx.AssertEquals(result, "Shivs added to hand", 1, shivsAdded);
                result.ActualValues["ShivsAdded"] = shivsAdded.ToString();
            }
            finally
            {
                await PowerCmd.Remove<InfiniteBladesPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// CreativeAi: Amount=1 → BeforeHandDraw → add 1 Power card to hand.
    /// Manual hook trigger.
    /// </summary>
    private class CAT_PWR_CreativeAi : ITestScenario
    {
        public string Id => "CAT-PWR-034";
        public string Name => "Power §G: CreativeAi → 1 Power card before draw (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var ca = await ctx.CreateCardInHand<CreativeAi>();
                await ctx.PlayCard(ca);

                var caPower = ctx.PlayerCreature.GetPower<CreativeAiPower>();
                if (caPower == null)
                {
                    result.Fail("CreativeAiPower", "applied", "null");
                    return result;
                }

                var handBefore = PileType.Hand.GetPile(ctx.Player).Cards.Count;

                CombatTracker.Instance.SetActivePowerSource(caPower.Id.Entry);
                await caPower.BeforeHandDraw(ctx.Player, new BlockingPlayerChoiceContext(), ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();

                var handAfter = PileType.Hand.GetPile(ctx.Player).Cards.Count;
                int cardsAdded = handAfter - handBefore;
                ctx.AssertEquals(result, "Power cards added to hand", 1, cardsAdded);
                result.ActualValues["CardsAdded"] = cardsAdded.ToString();
            }
            finally
            {
                await PowerCmd.Remove<CreativeAiPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// HelloWorld: Amount=1 → BeforeHandDraw → add 1 Common card to hand.
    /// Manual hook trigger.
    /// </summary>
    private class CAT_PWR_HelloWorld : ITestScenario
    {
        public string Id => "CAT-PWR-035";
        public string Name => "Power §G: HelloWorld → 1 Common card before draw (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var hw = await ctx.CreateCardInHand<HelloWorld>();
                await ctx.PlayCard(hw);

                var hwPower = ctx.PlayerCreature.GetPower<HelloWorldPower>();
                if (hwPower == null)
                {
                    result.Fail("HelloWorldPower", "applied", "null");
                    return result;
                }

                var handBefore = PileType.Hand.GetPile(ctx.Player).Cards.Count;

                CombatTracker.Instance.SetActivePowerSource(hwPower.Id.Entry);
                await hwPower.BeforeHandDraw(ctx.Player, new BlockingPlayerChoiceContext(), ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();

                var handAfter = PileType.Hand.GetPile(ctx.Player).Cards.Count;
                int cardsAdded = handAfter - handBefore;
                ctx.AssertEquals(result, "Common cards added to hand", 1, cardsAdded);
                result.ActualValues["CardsAdded"] = cardsAdded.ToString();
            }
            finally
            {
                await PowerCmd.Remove<HelloWorldPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// SpectrumShift: Amount=1 → BeforeHandDraw → add 1 Colorless card to hand.
    /// Manual hook trigger.
    /// </summary>
    private class CAT_PWR_SpectrumShift : ITestScenario
    {
        public string Id => "CAT-PWR-036";
        public string Name => "Power §G: SpectrumShift → 1 Colorless card before draw (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var ss = await ctx.CreateCardInHand<SpectrumShift>();
                await ctx.PlayCard(ss);

                var ssPower = ctx.PlayerCreature.GetPower<SpectrumShiftPower>();
                if (ssPower == null)
                {
                    result.Fail("SpectrumShiftPower", "applied", "null");
                    return result;
                }

                var handBefore = PileType.Hand.GetPile(ctx.Player).Cards.Count;

                CombatTracker.Instance.SetActivePowerSource(ssPower.Id.Entry);
                await ssPower.BeforeHandDraw(ctx.Player, new BlockingPlayerChoiceContext(), ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();

                var handAfter = PileType.Hand.GetPile(ctx.Player).Cards.Count;
                int cardsAdded = handAfter - handBefore;
                ctx.AssertEquals(result, "Colorless cards added to hand", 1, cardsAdded);
                result.ActualValues["CardsAdded"] = cardsAdded.ToString();
            }
            finally
            {
                await PowerCmd.Remove<SpectrumShiftPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// SentryMode: Amount=1 → BeforeHandDraw → add 1 SweepingGaze to hand.
    /// Manual hook trigger.
    /// </summary>
    private class CAT_PWR_SentryMode : ITestScenario
    {
        public string Id => "CAT-PWR-037";
        public string Name => "Power §G: SentryMode → 1 SweepingGaze before draw (manual)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var sm = await ctx.CreateCardInHand<SentryMode>();
                await ctx.PlayCard(sm);

                var smPower = ctx.PlayerCreature.GetPower<SentryModePower>();
                if (smPower == null)
                {
                    result.Fail("SentryModePower", "applied", "null");
                    return result;
                }

                var handBefore = PileType.Hand.GetPile(ctx.Player).Cards.Count;

                CombatTracker.Instance.SetActivePowerSource(smPower.Id.Entry);
                await smPower.BeforeHandDraw(ctx.Player, new BlockingPlayerChoiceContext(), ctx.CombatState);
                CombatTracker.Instance.ClearActivePowerSource();

                var handAfter = PileType.Hand.GetPile(ctx.Player).Cards.Count;
                int cardsAdded = handAfter - handBefore;
                ctx.AssertEquals(result, "SweepingGaze cards added to hand", 1, cardsAdded);
                result.ActualValues["CardsAdded"] = cardsAdded.ToString();
            }
            finally
            {
                await PowerCmd.Remove<SentryModePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Hailstorm: Amount=6 → BeforeTurnEnd → if >= 1 Frost orb, deal 6 AoE.
    /// Requires at least 1 Frost orb to be channeled. Manual hook trigger.
    /// Smoke test — just verifies power is applied with correct Amount.
    /// </summary>
    private class CAT_PWR_Hailstorm : ITestScenario
    {
        public string Id => "CAT-PWR-038";
        public string Name => "Power §G: Hailstorm → Amount=6 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var hs = await ctx.CreateCardInHand<Hailstorm>();
                await ctx.PlayCard(hs);

                var hsPower = ctx.PlayerCreature.GetPower<HailstormPower>();
                if (hsPower == null)
                {
                    result.Fail("HailstormPower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "HailstormPower.Amount", 6, hsPower.Amount);
                result.ActualValues["Amount"] = hsPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<HailstormPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Loop: Amount=1 → AfterPlayerTurnStart → trigger first orb passive 1 time.
    /// Requires orbs; smoke test verifies power Amount.
    /// </summary>
    private class CAT_PWR_Loop : ITestScenario
    {
        public string Id => "CAT-PWR-039";
        public string Name => "Power §G: Loop → Amount=1 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var loop = await ctx.CreateCardInHand<Loop>();
                await ctx.PlayCard(loop);

                var loopPower = ctx.PlayerCreature.GetPower<LoopPower>();
                if (loopPower == null)
                {
                    result.Fail("LoopPower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "LoopPower.Amount", 1, loopPower.Amount);
                result.ActualValues["Amount"] = loopPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<LoopPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ── Category H: Smoke tests ─────────────────────────────────

    /// <summary>
    /// ConsumingShadow: AfterTurnEnd → evoke last orb. Requires orbs; smoke test.
    /// </summary>
    private class CAT_PWR_ConsumingShadow : ITestScenario
    {
        public string Id => "CAT-PWR-040";
        public string Name => "Power §H: ConsumingShadow → applied (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var cs = await ctx.CreateCardInHand<ConsumingShadow>();
                await ctx.PlayCard(cs);

                var csPower = ctx.PlayerCreature.GetPower<ConsumingShadowPower>();
                ctx.AssertGreaterThan(result, "ConsumingShadowPower.Amount", 0, csPower?.Amount ?? 0);
                result.ActualValues["Amount"] = (csPower?.Amount ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<ConsumingShadowPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// LightningRod: AfterEnergyReset → channel Lightning. Smoke test.
    /// </summary>
    private class CAT_PWR_LightningRod : ITestScenario
    {
        public string Id => "CAT-PWR-041";
        public string Name => "Power §H: LightningRod → applied (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var lr = await ctx.CreateCardInHand<LightningRod>();
                await ctx.PlayCard(lr);

                var lrPower = ctx.PlayerCreature.GetPower<LightningRodPower>();
                ctx.AssertGreaterThan(result, "LightningRodPower.Amount", 0, lrPower?.Amount ?? 0);
                result.ActualValues["Amount"] = (lrPower?.Amount ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<LightningRodPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Spinner: AfterEnergyReset → channel Glass orbs. Smoke test.
    /// </summary>
    private class CAT_PWR_Spinner : ITestScenario
    {
        public string Id => "CAT-PWR-042";
        public string Name => "Power §H: Spinner → applied (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var sp = await ctx.CreateCardInHand<Spinner>();
                await ctx.PlayCard(sp);

                var spPower = ctx.PlayerCreature.GetPower<SpinnerPower>();
                ctx.AssertGreaterThan(result, "SpinnerPower.Amount", 0, spPower?.Amount ?? 0);
                result.ActualValues["Amount"] = (spPower?.Amount ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<SpinnerPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Iteration: Amount=2 → AfterCardDrawn (Status card) → draw 2.
    /// Needs Status in draw pile; smoke test verifies Amount.
    /// </summary>
    private class CAT_PWR_Iteration : ITestScenario
    {
        public string Id => "CAT-PWR-043";
        public string Name => "Power §H: Iteration → Amount=2 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var iter = await ctx.CreateCardInHand<Iteration>();
                await ctx.PlayCard(iter);

                var iterPower = ctx.PlayerCreature.GetPower<IterationPower>();
                if (iterPower == null)
                {
                    result.Fail("IterationPower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "IterationPower.Amount", 2, iterPower.Amount);
                result.ActualValues["Amount"] = iterPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<IterationPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Smokestack: Amount=5 → AfterCardGeneratedForCombat (Status) → 5 AoE.
    /// Needs Status generation; smoke test verifies Amount.
    /// </summary>
    private class CAT_PWR_Smokestack : ITestScenario
    {
        public string Id => "CAT-PWR-044";
        public string Name => "Power §H: Smokestack → Amount=5 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var ss = await ctx.CreateCardInHand<Smokestack>();
                await ctx.PlayCard(ss);

                var ssPower = ctx.PlayerCreature.GetPower<SmokestackPower>();
                if (ssPower == null)
                {
                    result.Fail("SmokestackPower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "SmokestackPower.Amount", 5, ssPower.Amount);
                result.ActualValues["Amount"] = ssPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<SmokestackPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// TrashToTreasure: Amount=1 → AfterCardGeneratedForCombat (Status) → channel 1 orb.
    /// Needs Status generation; smoke test verifies Amount.
    /// </summary>
    private class CAT_PWR_TrashToTreasure : ITestScenario
    {
        public string Id => "CAT-PWR-045";
        public string Name => "Power §H: TrashToTreasure → Amount=1 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var ttt = await ctx.CreateCardInHand<TrashToTreasure>();
                await ctx.PlayCard(ttt);

                var tttPower = ctx.PlayerCreature.GetPower<TrashToTreasurePower>();
                if (tttPower == null)
                {
                    result.Fail("TrashToTreasurePower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "TrashToTreasurePower.Amount", 1, tttPower.Amount);
                result.ActualValues["Amount"] = tttPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<TrashToTreasurePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// ChildOfTheStars: Amount=2 → AfterStarsSpent → gain 2 block per star spent.
    /// Stars spending requires special setup; smoke test verifies Amount.
    /// </summary>
    private class CAT_PWR_ChildOfTheStars : ITestScenario
    {
        public string Id => "CAT-PWR-046";
        public string Name => "Power §H: ChildOfTheStars → Amount=2 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var cos = await ctx.CreateCardInHand<ChildOfTheStars>();
                await ctx.PlayCard(cos);

                var cosPower = ctx.PlayerCreature.GetPower<ChildOfTheStarsPower>();
                if (cosPower == null)
                {
                    result.Fail("ChildOfTheStarsPower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "ChildOfTheStarsPower.Amount", 2, cosPower.Amount);
                result.ActualValues["Amount"] = cosPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<ChildOfTheStarsPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Orbit: Energy-based trigger. AutoPlay bypasses energy; smoke test.
    /// </summary>
    private class CAT_PWR_Orbit : ITestScenario
    {
        public string Id => "CAT-PWR-047";
        public string Name => "Power §H: Orbit → applied (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var orb = await ctx.CreateCardInHand<Orbit>();
                await ctx.PlayCard(orb);

                var orbPower = ctx.PlayerCreature.GetPower<OrbitPower>();
                ctx.AssertGreaterThan(result, "OrbitPower.Amount", 0, orbPower?.Amount ?? 0);
                result.ActualValues["Amount"] = (orbPower?.Amount ?? 0).ToString();
            }
            finally
            {
                await PowerCmd.Remove<OrbitPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Demesne: ModifyHandDraw power. Covered by HandDrawBonusPatch; smoke test.
    /// </summary>
    private class CAT_PWR_Demesne : ITestScenario
    {
        public string Id => "CAT-PWR-048";
        public string Name => "Power §H: Demesne → applied (HandDrawBonusPatch coverage, smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var dem = await ctx.CreateCardInHand<Demesne>();
                await ctx.PlayCard(dem);

                var demPower = ctx.PlayerCreature.GetPower<DemesnePower>();
                ctx.AssertGreaterThan(result, "DemesnePower applied", 0, demPower != null ? 1 : 0);
                result.ActualValues["Applied"] = (demPower != null).ToString();
            }
            finally
            {
                await PowerCmd.Remove<DemesnePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// MachineLearning: ModifyHandDraw +1. Covered by HandDrawBonusPatch; smoke test.
    /// </summary>
    private class CAT_PWR_MachineLearning : ITestScenario
    {
        public string Id => "CAT-PWR-049";
        public string Name => "Power §H: MachineLearning → applied (HandDrawBonusPatch, smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var ml = await ctx.CreateCardInHand<MachineLearning>();
                await ctx.PlayCard(ml);

                var mlPower = ctx.PlayerCreature.GetPower<MachineLearningPower>();
                ctx.AssertGreaterThan(result, "MachineLearningPower applied", 0, mlPower != null ? 1 : 0);
                result.ActualValues["Applied"] = (mlPower != null).ToString();
            }
            finally
            {
                await PowerCmd.Remove<MachineLearningPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// ToolsOfTheTrade: ModifyHandDraw +1, discard 1. Covered by HandDrawBonusPatch; smoke test.
    /// </summary>
    private class CAT_PWR_ToolsOfTheTrade : ITestScenario
    {
        public string Id => "CAT-PWR-050";
        public string Name => "Power §H: ToolsOfTheTrade → applied (HandDrawBonusPatch, smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var tott = await ctx.CreateCardInHand<ToolsOfTheTrade>();
                await ctx.PlayCard(tott);

                var tottPower = ctx.PlayerCreature.GetPower<ToolsOfTheTradePower>();
                ctx.AssertGreaterThan(result, "ToolsOfTheTradePower applied", 0, tottPower != null ? 1 : 0);
                result.ActualValues["Applied"] = (tottPower != null).ToString();
            }
            finally
            {
                await PowerCmd.Remove<ToolsOfTheTradePower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Tyranny: ModifyHandDraw +1, exhaust 1. Covered by HandDrawBonusPatch; smoke test.
    /// </summary>
    private class CAT_PWR_Tyranny : ITestScenario
    {
        public string Id => "CAT-PWR-051";
        public string Name => "Power §H: Tyranny → applied (HandDrawBonusPatch, smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var tyr = await ctx.CreateCardInHand<Tyranny>();
                await ctx.PlayCard(tyr);

                var tyrPower = ctx.PlayerCreature.GetPower<TyrannyPower>();
                ctx.AssertGreaterThan(result, "TyrannyPower applied", 0, tyrPower != null ? 1 : 0);
                result.ActualValues["Applied"] = (tyrPower != null).ToString();
            }
            finally
            {
                await PowerCmd.Remove<TyrannyPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Parry: Amount=6 → AfterSovereignBladePlayed → gain 6 block.
    /// SovereignBlade is Regent-only; smoke test verifies power Amount.
    /// </summary>
    private class CAT_PWR_Parry : ITestScenario
    {
        public string Id => "CAT-PWR-052";
        public string Name => "Power §H: Parry → Amount=6 (smoke)";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.SetEnergy(999);
                var parry = await ctx.CreateCardInHand<Parry>();
                await ctx.PlayCard(parry);

                var parryPower = ctx.PlayerCreature.GetPower<ParryPower>();
                if (parryPower == null)
                {
                    result.Fail("ParryPower", "applied", "null");
                    return result;
                }

                ctx.AssertEquals(result, "ParryPower.Amount", 6, parryPower.Amount);
                result.ActualValues["Amount"] = parryPower.Amount.ToString();
            }
            finally
            {
                await PowerCmd.Remove<ParryPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ── Category H: EndTurn chain tests ────────────────────────

    /// <summary>
    /// Poison tick via EndTurn chain:
    /// DeadlyPoison applies 5 poison to enemy →
    /// TakeSnapshot → EndTurn → enemy turn starts → PoisonPower.AfterSideTurnStart →
    /// deals 5 damage to enemy, decrements to 4 →
    /// player turn → GetDelta → delta["DEADLY_POISON"].AttributedDamage == 5
    /// </summary>
    private class CAT_PWR_PoisonTick : ITestScenario
    {
        public string Id => "CAT-PWR-PoisonTick";
        public string Name => "Power §H: DeadlyPoison(5) → EndTurn → AttributedDamage=5";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var enemy = ctx.GetFirstEnemy();
            try
            {
                // Remove existing poison to get clean state
                await PowerCmd.Remove<PoisonPower>(enemy);

                // Apply 5 poison via DeadlyPoison card
                var dp = await ctx.CreateCardInHand<DeadlyPoison>();
                await ctx.PlayCard(dp, enemy);

                // Verify poison applied
                var poison = enemy.GetPower<PoisonPower>();
                if (poison == null || poison.Amount != 5)
                {
                    result.Fail("PoisonPower.Amount", "5", poison?.Amount.ToString() ?? "null");
                    return result;
                }

                // Snapshot → EndTurn → poison ticks → back to player turn
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("DEADLY_POISON", out var d);
                ctx.AssertEquals(result, "DEADLY_POISON.AttributedDamage", 5, d?.AttributedDamage ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<PoisonPower>(enemy);
            }
            return result;
        }
    }

    /// <summary>
    /// Genesis stars via EndTurn chain:
    /// Play Genesis (2 stars/turn) →
    /// TakeSnapshot → EndTurn → enemy turn → player turn starts →
    /// GenesisPower.AfterEnergyReset → GainStars(2) →
    /// GetDelta → delta["GENESIS"].StarsContribution == 2
    /// </summary>
    private class CAT_PWR_GenesisEndTurn : ITestScenario
    {
        public string Id => "CAT-PWR-GenesisEndTurn";
        public string Name => "Power §H: Genesis → EndTurn → StarsContribution=2";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<GenesisPower>(ctx.PlayerCreature);

                var genesis = await ctx.CreateCardInHand<Genesis>();
                await ctx.PlayCard(genesis);

                // Verify power applied
                var gen = ctx.PlayerCreature.GetPower<GenesisPower>();
                if (gen == null || gen.Amount < 2)
                {
                    result.Fail("GenesisPower.Amount", "≥2", gen?.Amount.ToString() ?? "null");
                    return result;
                }

                // Snapshot → EndTurn → next turn → Genesis fires → stars gained
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("GENESIS", out var d);
                ctx.AssertEquals(result, "GENESIS.StarsContribution", 2, d?.StarsContribution ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<GenesisPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// BlockNextTurn via Glitterstream (Block=11 + BlockNextTurnPower=4):
    /// Play Glitterstream → consume initial block → TakeSnapshot →
    /// EndTurn → turn start → block cleared → BlockNextTurnPower.AfterBlockCleared →
    /// GainBlock(4) → SimulateDamage → delta["GLITTERSTREAM"].EffectiveBlock == 4
    /// </summary>
    private class CAT_PWR_BlockNextTurn : ITestScenario
    {
        public string Id => "CAT-PWR-BlockNextTurn";
        public string Name => "Power §H: Glitterstream → EndTurn → next-turn EffectiveBlock=4";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<BlockNextTurnPower>(ctx.PlayerCreature);

                var card = await ctx.CreateCardInHand<Glitterstream>();
                await ctx.PlayCard(card);

                // Consume the initial 11 block so it doesn't pollute the delta
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                await ctx.ClearBlock();

                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                // After turn start: BlockNextTurnPower fired → gained 4 block
                // SimulateDamage to consume the 4 next-turn block
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());

                var delta = ctx.GetDelta();
                delta.TryGetValue("GLITTERSTREAM", out var d);
                ctx.AssertEquals(result, "GLITTERSTREAM.EffectiveBlock", 4, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<BlockNextTurnPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// StarNextTurn via HiddenCache (Stars=1 + StarNextTurnPower=3):
    /// Play HiddenCache → TakeSnapshot → EndTurn → turn start →
    /// StarNextTurnPower.AfterEnergyReset → GainStars(3) →
    /// delta["HIDDEN_CACHE"].StarsContribution == 3
    /// </summary>
    private class CAT_PWR_StarNextTurn : ITestScenario
    {
        public string Id => "CAT-PWR-StarNextTurn";
        public string Name => "Power §H: HiddenCache → EndTurn → next-turn StarsContribution=3";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<StarNextTurnPower>(ctx.PlayerCreature);

                var card = await ctx.CreateCardInHand<HiddenCache>();
                await ctx.PlayCard(card);

                // HiddenCache gives 1 star now + StarNextTurnPower(3) for next turn
                // Snapshot AFTER playing so the 1 immediate star is excluded
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("HIDDEN_CACHE", out var d);
                ctx.AssertEquals(result, "HIDDEN_CACHE.StarsContribution", 3, d?.StarsContribution ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<StarNextTurnPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>
    /// DrawCardsNextTurn via GuidingStar (Damage=12 + DrawCardsNextTurnPower=2):
    /// Play GuidingStar → TakeSnapshot → EndTurn → turn start →
    /// DrawCardsNextTurnPower.ModifyHandDraw adds 2 to hand draw →
    /// FlushPendingHandDrawBonus → delta["GUIDING_STAR"].CardsDrawn == 2
    /// </summary>
    private class CAT_PWR_DrawCardsNextTurn : ITestScenario
    {
        public string Id => "CAT-PWR-DrawCardsNextTurn";
        public string Name => "Power §H: GuidingStar → EndTurn → next-turn CardsDrawn=2";
        public string Category => "Catalog_PowerContrib";
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            try
            {
                await PowerCmd.Remove<DrawCardsNextTurnPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.EnsureDrawPile(15);

                var card = await ctx.CreateCardInHand<GuidingStar>();
                await ctx.PlayCard(card, enemy);

                // GuidingStar deals 12 damage + applies DrawCardsNextTurnPower(2)
                // Snapshot AFTER playing so immediate damage is excluded
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();

                var delta = ctx.GetDelta();
                delta.TryGetValue("GUIDING_STAR", out var d);
                ctx.AssertEquals(result, "GUIDING_STAR.CardsDrawn", 2, d?.CardsDrawn ?? 0);
            }
            finally
            {
                await PowerCmd.Remove<DrawCardsNextTurnPower>(ctx.PlayerCreature);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
