using CommunityStats.Collection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ContribTests.Scenarios;

/// <summary>
/// Relic batch 3 — contribution tracking verification (spec v3).
/// Every test validates delta fields with exact values from KnowledgeBase.
/// Relics requiring EndTurn, combat start, or room type guards are SKIPPED.
/// </summary>
public static class Catalog_RelicTests3
{
    private const string Cat = "Catalog_Relic3";

    public static IReadOnlyList<ITestScenario> All => new ITestScenario[]
    {
        // A: Damage relics (AfterExhaust/Discard/Play triggers)
        new REL3_CharonsAshes(),       // exhaust → AoE Damage=3×enemies
        new REL3_ForgottenSoul(),      // exhaust → random Damage=1
        new REL3_LostWisp(),           // play Power → AoE Damage=8×enemies
        new REL3_LetterOpener(),       // 3 Skills → AoE Damage=5×enemies
        new REL3_Kusarigama(),         // 3 Attacks → Damage=6 (random)
        new REL3_OrnamentalFan(),      // 3 Attacks → Block=4

        // B: Block relics
        new REL3_DaughterOfTheWind(),  // play Attack → Block=1
        new REL3_Permafrost(),         // first Power → Block=6
        new REL3_IntimidatingHelmet(), // play 2+ cost card → Block=4
        new REL3_FakeOrichalcum(),     // EndTurn with 0 block → 3 block

        // C: Draw relics
        new REL3_GamePiece(),          // play Power → CardsDrawn=1
        new REL3_CentennialPuzzle(),   // first HP loss → draw 3

        // D: Debuff relics — REMOVED: BagOfMarbles/RedMask fire at RoundNumber<=1,
        // cannot be triggered mid-combat via ObtainRelic

        // E: Power contribution chain (3 attacks → Str/Dex)
        new REL3_Kunai(),              // 3 attacks → Dex+1 → play Defend → ModifierBlock=1
        new REL3_Shuriken(),           // 3 attacks → Str+1 → play Strike → ModifierDamage=1

        // F: EndTurn relics
        new REL3_LunarPastry(),        // EndTurn → +1 star
        // GENUINELY SKIPPED: SlingOfCourage (Elite room guard), Girya (rest site),
        //   Lantern (first turn RoundNumber guard), Tingsha (needs discard from other card),
        //   TuningFork (10 skills), Nunchaku (10 attacks), JossPaper (5 exhausts)
    };

    // ═══════════════════════════════════════════════════════════
    // A: Damage relics
    // ═══════════════════════════════════════════════════════════

    /// <summary>CharonsAshes: Exhaust card → AoE 3 damage per enemy.
    /// Play TrueGrit which exhausts → relic hook fires during card play.</summary>
    private class REL3_CharonsAshes : ITestScenario
    {
        public string Id => "CAT-REL3-CharonsAshes";
        public string Name => "CharonsAshes: exhaust → DirectDamage=3×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<CharonsAshes>();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await ctx.CreateCardInHand<StrikeIronclad>(); // card to be exhausted
                var tg = await ctx.CreateCardInHand<TrueGrit>();
                await ctx.ClearBlock();
                ctx.TakeSnapshot();
                await ctx.PlayCard(tg);
                await Task.Delay(300);
                var delta = ctx.GetDelta();
                delta.TryGetValue("CHARONS_ASHES", out var d);
                int enemies = ctx.GetAllEnemies().Count;
                ctx.AssertEquals(result, "CHARONS_ASHES.DirectDamage", 3 * enemies, d?.DirectDamage ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>ForgottenSoul: Exhaust card → 1 damage to random enemy.
    /// Play TrueGrit which exhausts → relic hook fires during card play.</summary>
    private class REL3_ForgottenSoul : ITestScenario
    {
        public string Id => "CAT-REL3-ForgottenSoul";
        public string Name => "ForgottenSoul: exhaust → DirectDamage=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<ForgottenSoul>();
            try
            {
                await ctx.CreateCardInHand<StrikeIronclad>(); // card to be exhausted
                var tg = await ctx.CreateCardInHand<TrueGrit>();
                await ctx.ClearBlock();
                ctx.TakeSnapshot();
                await ctx.PlayCard(tg);
                await Task.Delay(300);
                var delta = ctx.GetDelta();
                delta.TryGetValue("FORGOTTEN_SOUL", out var d);
                ctx.AssertEquals(result, "FORGOTTEN_SOUL.DirectDamage", 1, d?.DirectDamage ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>LostWisp: Play Power → AoE 8 damage per enemy.</summary>
    private class REL3_LostWisp : ITestScenario
    {
        public string Id => "CAT-REL3-LostWisp";
        public string Name => "LostWisp: play Power → DirectDamage=8×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<LostWisp>();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                // Play a Power card to trigger LostWisp
                var power = await ctx.CreateCardInHand<Inflame>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(power);
                var delta = ctx.GetDelta();
                delta.TryGetValue("LOST_WISP", out var d);
                int enemies = ctx.GetAllEnemies().Count;
                ctx.AssertEquals(result, "LOST_WISP.DirectDamage", 8 * enemies, d?.DirectDamage ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // B: Block relics
    // ═══════════════════════════════════════════════════════════

    /// <summary>DaughterOfTheWind: Play Attack → Block=1.</summary>
    private class REL3_DaughterOfTheWind : ITestScenario
    {
        public string Id => "CAT-REL3-DaughterOfTheWind";
        public string Name => "DaughterOfTheWind: play Attack → EffectiveBlock=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<DaughterOfTheWind>();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                var attack = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(attack, enemy);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("DAUGHTER_OF_THE_WIND", out var d);
                ctx.AssertEquals(result, "DAUGHTER_OF_THE_WIND.EffectiveBlock", 1, d?.EffectiveBlock ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>Permafrost: First Power played → Block=6.</summary>
    private class REL3_Permafrost : ITestScenario
    {
        public string Id => "CAT-REL3-Permafrost";
        public string Name => "Permafrost: first Power → EffectiveBlock=6";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<Permafrost>();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                var power = await ctx.CreateCardInHand<Inflame>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(power);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("PERMAFROST", out var d);
                ctx.AssertEquals(result, "PERMAFROST.EffectiveBlock", 6, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>IntimidatingHelmet: Play 2+ cost card → Block=4.</summary>
    private class REL3_IntimidatingHelmet : ITestScenario
    {
        public string Id => "CAT-REL3-IntimidatingHelmet";
        public string Name => "IntimidatingHelmet: play 2+ cost → EffectiveBlock=4";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<IntimidatingHelmet>();
            try
            {
                await ctx.ClearBlock();
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                // Bludgeon costs 3 energy, triggers IntimidatingHelmet
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                var card = await ctx.CreateCardInHand<Bludgeon>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(card, enemy);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("INTIMIDATING_HELMET", out var d);
                ctx.AssertEquals(result, "INTIMIDATING_HELMET.EffectiveBlock", 4, d?.EffectiveBlock ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // C: Draw relics
    // ═══════════════════════════════════════════════════════════

    /// <summary>GamePiece: Play Power → draw 1 card.</summary>
    private class REL3_GamePiece : ITestScenario
    {
        public string Id => "CAT-REL3-GamePiece";
        public string Name => "GamePiece: play Power → CardsDrawn=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<GamePiece>();
            try
            {
                await ctx.EnsureDrawPile(8);
                var power = await ctx.CreateCardInHand<Inflame>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(power);
                var delta = ctx.GetDelta();
                delta.TryGetValue("GAME_PIECE", out var d);
                ctx.AssertEquals(result, "GAME_PIECE.CardsDrawn", 1, d?.CardsDrawn ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D: Debuff relics (combat start — check current state)
    // ═══════════════════════════════════════════════════════════

    /// <summary>BagOfMarbles: Combat start → Vuln=1 on all enemies.</summary>
    private class REL3_BagOfMarbles : ITestScenario
    {
        public string Id => "CAT-REL3-BagOfMarbles";
        public string Name => "BagOfMarbles: Vuln=1 on enemies (combat start)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<BagOfMarbles>();
            try
            {
                // BagOfMarbles fires at combat start. Since we obtain mid-combat,
                // manually trigger via relic hook.
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                await ctx.TriggerRelicHook(() => relic.BeforeCombatStart());
                var vuln = enemy.GetPower<VulnerablePower>();
                ctx.AssertEquals(result, "enemy.VulnerablePower", 1, vuln?.Amount ?? 0);
                await PowerCmd.Remove<VulnerablePower>(enemy);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>RedMask: Combat start → Weak=1 on all enemies.</summary>
    private class REL3_RedMask : ITestScenario
    {
        public string Id => "CAT-REL3-RedMask";
        public string Name => "RedMask: Weak=1 on enemies (combat start)";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<RedMask>();
            try
            {
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<WeakPower>(enemy);
                await ctx.TriggerRelicHook(() => relic.BeforeCombatStart());
                var weak = enemy.GetPower<WeakPower>();
                ctx.AssertEquals(result, "enemy.WeakPower", 1, weak?.Amount ?? 0);
                await PowerCmd.Remove<WeakPower>(enemy);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // E: Power contribution chain (3 attacks → Str/Dex)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Kunai: 3 attacks → +1 Dex → play Defend → ModifierBlock=1 attributed to KUNAI.
    /// </summary>
    private class REL3_Kunai : ITestScenario
    {
        public string Id => "CAT-REL3-Kunai";
        public string Name => "Kunai: 3 attacks → Dex+1 → ModifierBlock=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<Kunai>();
            try
            {
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<FrailPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);

                // Play 3 attacks to trigger Kunai (+1 Dex)
                for (int i = 0; i < 3; i++)
                {
                    var atk = await ctx.CreateCardInHand<StrikeIronclad>();
                    await ctx.PlayCard(atk, enemy);
                }

                // Verify Dex gained
                var dex = ctx.PlayerCreature.GetPower<DexterityPower>();
                if (dex == null || dex.Amount < 1)
                {
                    result.Fail("DexterityPower", "≥1", dex?.Amount.ToString() ?? "null");
                    return result;
                }

                // Now play Defend to check ModifierBlock attribution
                await ctx.ClearBlock();
                var defend = await ctx.CreateCardInHand<DefendIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(defend);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("KUNAI", out var d);
                ctx.AssertEquals(result, "KUNAI.ModifierBlock", 1, d?.ModifierBlock ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    /// <summary>
    /// Shuriken: 3 attacks → +1 Str → play Strike → ModifierDamage=1 attributed to SHURIKEN.
    /// </summary>
    private class REL3_Shuriken : ITestScenario
    {
        public string Id => "CAT-REL3-Shuriken";
        public string Name => "Shuriken: 3 attacks → Str+1 → ModifierDamage=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<Shuriken>();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);

                // Play 3 attacks to trigger Shuriken (+1 Str)
                for (int i = 0; i < 3; i++)
                {
                    var atk = await ctx.CreateCardInHand<StrikeIronclad>();
                    await ctx.PlayCard(atk, enemy);
                }

                // Verify Str gained
                var str = ctx.PlayerCreature.GetPower<StrengthPower>();
                if (str == null || str.Amount < 1)
                {
                    result.Fail("StrengthPower", "≥1", str?.Amount.ToString() ?? "null");
                    return result;
                }

                // Now play another Strike to check ModifierDamage attribution
                var strike = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(strike, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("SHURIKEN", out var d);
                ctx.AssertEquals(result, "SHURIKEN.ModifierDamage", 1, d?.ModifierDamage ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
            }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Counter relics (play N cards to trigger)
    // ═══════════════════════════════════════════════════════════

    /// <summary>LetterOpener: every 3 Skills → AoE 5 damage per enemy.</summary>
    private class REL3_LetterOpener : ITestScenario
    {
        public string Id => "CAT-REL3-LetterOpener";
        public string Name => "LetterOpener: 3 Skills → DirectDamage=5×enemies";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<LetterOpener>();
            try
            {
                // Play 2 Skills to prime the counter
                for (int i = 0; i < 2; i++)
                {
                    var def = await ctx.CreateCardInHand<DefendIronclad>();
                    await ctx.PlayCard(def);
                }
                // 3rd Skill triggers LetterOpener
                await ctx.ClearBlock();
                var third = await ctx.CreateCardInHand<DefendIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(third);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("LETTER_OPENER", out var d);
                int enemies = ctx.GetAllEnemies().Count;
                ctx.AssertEquals(result, "LETTER_OPENER.DirectDamage", 5 * enemies, d?.DirectDamage ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>Kusarigama: every 3 Attacks → 6 damage to random enemy.</summary>
    private class REL3_Kusarigama : ITestScenario
    {
        public string Id => "CAT-REL3-Kusarigama";
        public string Name => "Kusarigama: 3 Attacks → DirectDamage=6";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<Kusarigama>();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                // Play 2 Attacks to prime counter
                for (int i = 0; i < 2; i++)
                {
                    var atk = await ctx.CreateCardInHand<StrikeIronclad>();
                    await ctx.PlayCard(atk, enemy);
                }
                // 3rd Attack triggers Kusarigama
                var third = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(third, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("KUSARIGAMA", out var d);
                ctx.AssertEquals(result, "KUSARIGAMA.DirectDamage", 6, d?.DirectDamage ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>OrnamentalFan: every 3 Attacks → 4 block.</summary>
    private class REL3_OrnamentalFan : ITestScenario
    {
        public string Id => "CAT-REL3-OrnamentalFan";
        public string Name => "OrnamentalFan: 3 Attacks → EffectiveBlock=4";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<OrnamentalFan>();
            try
            {
                await PowerCmd.Remove<StrengthPower>(ctx.PlayerCreature);
                await PowerCmd.Remove<DexterityPower>(ctx.PlayerCreature);
                var enemy = ctx.GetFirstEnemy();
                await PowerCmd.Remove<VulnerablePower>(enemy);
                // Play 2 Attacks to prime
                for (int i = 0; i < 2; i++)
                {
                    var atk = await ctx.CreateCardInHand<StrikeIronclad>();
                    await ctx.PlayCard(atk, enemy);
                }
                // 3rd Attack triggers Fan
                await ctx.ClearBlock();
                var third = await ctx.CreateCardInHand<StrikeIronclad>();
                ctx.TakeSnapshot();
                await ctx.PlayCard(third, enemy);
                await ctx.SimulateDamage(ctx.PlayerCreature, 99, enemy);
                var delta = ctx.GetDelta();
                delta.TryGetValue("ORNAMENTAL_FAN", out var d);
                ctx.AssertEquals(result, "ORNAMENTAL_FAN.EffectiveBlock", 4, d?.EffectiveBlock ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EndTurn relics
    // ═══════════════════════════════════════════════════════════

    /// <summary>FakeOrichalcum: EndTurn with 0 block → gain 3 block (consumed by enemy attack).</summary>
    private class REL3_FakeOrichalcum : ITestScenario
    {
        public string Id => "CAT-REL3-FakeOrichalcum";
        public string Name => "FakeOrichalcum: EndTurn 0 block → EffectiveBlock=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<FakeOrichalcum>();
            try
            {
                await ctx.ClearBlock();
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                // FakeOrichalcum fires at turn end when block=0 → 3 block → enemy consumes
                var delta = ctx.GetDelta();
                delta.TryGetValue("FAKE_ORICHALCUM", out var d);
                ctx.AssertEquals(result, "FAKE_ORICHALCUM.EffectiveBlock", 3, d?.EffectiveBlock ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }

    /// <summary>CentennialPuzzle: first HP loss → draw 3 cards.</summary>
    private class REL3_CentennialPuzzle : ITestScenario
    {
        public string Id => "CAT-REL3-CentennialPuzzle";
        public string Name => "CentennialPuzzle: first HP loss → CardsDrawn=3";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive && ctx.GetAllEnemies().Count > 0;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<CentennialPuzzle>();
            try
            {
                await ctx.ClearBlock();
                await ctx.EnsureDrawPile(10);
                ctx.TakeSnapshot();
                // Take damage to trigger CentennialPuzzle
                await ctx.SimulateDamage(ctx.PlayerCreature, 5, ctx.GetFirstEnemy());
                var delta = ctx.GetDelta();
                delta.TryGetValue("CENTENNIAL_PUZZLE", out var d);
                ctx.AssertEquals(result, "CENTENNIAL_PUZZLE.CardsDrawn", 3, d?.CardsDrawn ?? 0);
            }
            finally { await ctx.RemoveRelic(relic); }
            return result;
        }
    }

    /// <summary>LunarPastry: EndTurn → +1 Star.</summary>
    private class REL3_LunarPastry : ITestScenario
    {
        public string Id => "CAT-REL3-LunarPastry";
        public string Name => "LunarPastry: EndTurn → StarsContribution=1";
        public string Category => Cat;
        public bool CanRun(TestContext ctx) => ctx.IsCombatActive;
        public async Task<TestResult> RunAsync(TestContext ctx, CancellationToken ct)
        {
            var result = new TestResult { ScenarioId = Id, ScenarioName = Name, Category = Category };
            var relic = await ctx.ObtainRelic<LunarPastry>();
            try
            {
                ctx.TakeSnapshot();
                await ctx.EndTurnAndWaitForPlayerTurn();
                var delta = ctx.GetDelta();
                delta.TryGetValue("LUNAR_PASTRY", out var d);
                ctx.AssertEquals(result, "LUNAR_PASTRY.StarsContribution", 1, d?.StarsContribution ?? 0);
            }
            finally
            {
                await ctx.RemoveRelic(relic);
                await ctx.SetEnergy(999);
            }
            return result;
        }
    }
}
