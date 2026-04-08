using CommunityStats.Collection;
using CommunityStats.Util;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CommunityStats.Patches;

// ═══════════════════════════════════════════════════════════
// Core combat history hooks
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class CombatHistoryPatch
{
    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
    [HarmonyPostfix]
    public static void AfterCardPlayStarted(CombatState combatState, CardPlay cardPlay)
    {
        Safe.Run(() =>
        {
            var card = cardPlay.Card;
            var cardId = card?.Id.Entry;
            if (cardId != null)
                CombatTracker.Instance.OnCardPlayStarted(cardId, card!.GetHashCode());
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
    [HarmonyPostfix]
    public static void AfterCardPlayFinished(CombatState combatState, CardPlay cardPlay)
    {
        Safe.Run(() => CombatTracker.Instance.OnCardPlayFinished());
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.DamageReceived))]
    [HarmonyPostfix]
    public static void AfterDamageReceived(CombatState combatState,
        Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
    {
        Safe.Run(() =>
        {
            // Mark this result as processed so KillingBlowPatcher won't double-count
            CombatTracker.Instance.MarkDamageResultProcessed(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(result));

            var isPlayerReceiver = receiver.IsPlayer;
            var cardSourceId = cardSource?.Id.Entry;

            // Detect Osty as dealer or receiver
            bool isOstyDealer = dealer != null && dealer.Monster is Osty;
            bool isOstyReceiver = receiver.Monster is Osty;

            CombatTracker.Instance.OnDamageDealt(
                result.TotalDamage,
                result.BlockedDamage,
                cardSourceId,
                isPlayerReceiver,
                receiver.GetHashCode(),
                dealer?.GetHashCode() ?? 0,
                isOstyDealer: isOstyDealer,
                isOstyReceiver: isOstyReceiver);

            if (isPlayerReceiver && result.WasTargetKilled)
                CombatTracker.Instance.OnPlayerDied();

            // Defense attribution: Weak on enemy attacker reduces damage to player
            if (isPlayerReceiver && dealer != null && result.TotalDamage > 0)
            {
                var weakPower = dealer.GetPower<WeakPower>();
                if (weakPower != null)
                {
                    // Fix-2: Compute actual weak multiplier including PaperKrane/Debilitate
                    decimal weakMult = 0.75m;
                    if (receiver?.Player != null)
                    {
                        var krane = receiver.Player.GetRelic<PaperKrane>();
                        if (krane != null) weakMult -= 0.15m;
                    }
                    var debilitate = dealer.GetPower<DebilitatePower>();
                    if (debilitate != null)
                        weakMult = 1m - (1m - weakMult) * 2m; // doubles the reduction
                    // Floor at 0.1 to avoid division issues
                    if (weakMult < 0.1m) weakMult = 0.1m;

                    CombatTracker.Instance.OnWeakMitigation(
                        result.TotalDamage, dealer.GetHashCode(), (float)weakMult);
                }

                // Defense attribution: ColossusPower on player halves damage from Vulnerable enemies
                var colossusPower = receiver.GetPower<ColossusPower>();
                if (colossusPower != null && dealer.HasPower<VulnerablePower>())
                {
                    // Colossus multiplier is 0.5 (DamageDecrease dynamic var)
                    // prevented = actualDamage / 0.5 - actualDamage = actualDamage
                    CombatTracker.Instance.OnColossusMitigation(
                        result.TotalDamage, receiver.GetHashCode());
                }
            }
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.BlockGained))]
    [HarmonyPostfix]
    public static void AfterBlockGained(CombatState combatState,
        Creature receiver, int amount, ValueProp props, CardPlay? cardPlay)
    {
        Safe.Run(() =>
        {
            if (!receiver.IsPlayer) return;
            var cardPlayId = cardPlay?.Card?.Id.Entry;
            CombatTracker.Instance.OnBlockGained(amount, cardPlayId);
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PowerReceived))]
    [HarmonyPostfix]
    public static void AfterPowerReceived(CombatState combatState,
        PowerModel power, decimal amount, Creature? applier)
    {
        Safe.Run(() =>
        {
            var powerId = power?.Id.Entry;
            if (powerId == null) return;
            CombatTracker.Instance.OnPowerSourceRecorded(powerId);
        });
    }

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDrawn))]
    [HarmonyPostfix]
    public static void AfterCardDrawn(CombatState combatState, CardModel card, bool fromHandDraw)
    {
        Safe.Run(() => CombatTracker.Instance.OnCardDrawn(fromHandDraw));
    }
}

// ═══════════════════════════════════════════════════════════
// Module B: Power indirect effects — set/clear power source context
// Patches PowerModel hook methods so indirect damage/block from powers
// (RagePower, FlameBarrier, FeelNoPain, Plating, Inferno, Juggernaut, Grapple)
// is attributed back to the card that applied the power.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class PowerApplyPatch
{
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ApplyInternal))]
    [HarmonyPostfix]
    public static void AfterApplyInternal(PowerModel __instance, Creature owner)
    {
        Safe.Run(() =>
        {
            var powerId = __instance.Id.Entry;
            if (string.IsNullOrEmpty(powerId)) return;

            int creatureHash = owner.GetHashCode();
            bool isPlayerTarget = owner.IsPlayer;

            CombatTracker.Instance.OnPowerApplied(powerId, __instance.Amount, creatureHash, isPlayerTarget);
        });
    }
}

/// <summary>
/// Manual RelicHookContext patcher. Sets/clears _activeRelicId around relic hook methods
/// so indirect effects (healing, damage, block, energy, draw) are attributed to the relic.
/// For hooks that fire during card play (AfterCardPlayed), _activeCardId takes priority
/// in ResolveSource, so relic context only matters for hooks outside card play.
/// </summary>
public static class RelicHookContextPatcher
{
    public static void SetRelicContext(RelicModel __instance)
    {
        Safe.Run(() => CombatTracker.Instance.SetActiveRelic(__instance.Id.Entry));
    }

    public static void ClearRelicContext()
    {
        Safe.Run(() => CombatTracker.Instance.ClearActiveRelic());
    }

    public static void PatchAll(Harmony harmony)
    {
        var prefix = new HarmonyMethod(AccessTools.Method(typeof(RelicHookContextPatcher), nameof(SetRelicContext)));
        var postfix = new HarmonyMethod(AccessTools.Method(typeof(RelicHookContextPatcher), nameof(ClearRelicContext)));

        // ── Healing relics ──
        TryPatch(harmony, typeof(BurningBlood), "AfterCombatVictory", prefix, postfix);
        TryPatch(harmony, typeof(BlackBlood), "AfterCombatVictory", prefix, postfix);
        TryPatch(harmony, typeof(MeatOnTheBone), "AfterCombatVictoryEarly", prefix, postfix);
        TryPatch(harmony, typeof(DemonTongue), "AfterDamageReceived", prefix, postfix);

        // ── Damage relics (outside card play) ──
        TryPatch(harmony, typeof(ScreamingFlagon), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(StoneCalendar), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(ParryingShield), "AfterTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(CharonsAshes), "AfterCardExhausted", prefix, postfix);
        TryPatch(harmony, typeof(ForgottenSoul), "AfterCardExhausted", prefix, postfix);
        TryPatch(harmony, typeof(FestivePopper), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(MercuryHourglass), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(MrStruggles), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(Metronome), "AfterOrbChanneled", prefix, postfix);
        TryPatch(harmony, typeof(Tingsha), "AfterCardDiscarded", prefix, postfix);

        // ── Damage relics (during card play — relic context used for power source recording) ──
        TryPatch(harmony, typeof(Kusarigama), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(LetterOpener), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(LostWisp), "AfterCardPlayed", prefix, postfix);

        // ── Block relics (outside card play) ──
        TryPatch(harmony, typeof(CloakClasp), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(FakeOrichalcum), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(Orichalcum), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(RippleBasin), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(ToughBandages), "AfterCardDiscarded", prefix, postfix);
        TryPatch(harmony, typeof(IntimidatingHelmet), "BeforeCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(HornCleat), "AfterBlockCleared", prefix, postfix);
        TryPatch(harmony, typeof(CaptainsWheel), "AfterBlockCleared", prefix, postfix);
        TryPatch(harmony, typeof(GalacticDust), "AfterStarsSpent", prefix, postfix);

        // ── Block relics (during card play) ──
        TryPatch(harmony, typeof(DaughterOfTheWind), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(OrnamentalFan), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(Permafrost), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(TuningFork), "AfterCardPlayed", prefix, postfix);

        // ── Energy relics ──
        TryPatch(harmony, typeof(ArtOfWar), "AfterEnergyReset", prefix, postfix);
        TryPatch(harmony, typeof(IvoryTile), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(Nunchaku), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(PaelsTears), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(Candelabra), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(Chandelier), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(Lantern), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(HappyFlower), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(FakeHappyFlower), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(GremlinHorn), "AfterDeath", prefix, postfix);

        // ── Draw relics ──
        TryPatch(harmony, typeof(CentennialPuzzle), "AfterDamageReceived", prefix, postfix);
        TryPatch(harmony, typeof(GamePiece), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(IronClub), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(JossPaper), "AfterCardExhausted", prefix, postfix);
        TryPatch(harmony, typeof(Pendulum), "AfterShuffle", prefix, postfix);
        TryPatch(harmony, typeof(BlessedAntler), "BeforeHandDraw", prefix, postfix);

        // ── Power application relics (records relic as power source) ──
        TryPatch(harmony, typeof(HandDrill), "AfterDamageGiven", prefix, postfix);
        TryPatch(harmony, typeof(Kunai), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(Shuriken), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(RainbowRing), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(RedSkull), "AfterCurrentHpChanged", prefix, postfix);
        TryPatch(harmony, typeof(Akabeko), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(BagOfMarbles), "BeforeSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(Brimstone), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(RedMask), "BeforeSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(SlingOfCourage), "AfterRoomEntered", prefix, postfix);

        // ── Stars relic ──
        TryPatch(harmony, typeof(LunarPastry), "AfterTurnEnd", prefix, postfix);

        // ── Orb relic ──
        TryPatch(harmony, typeof(EmotionChip), "AfterPlayerTurnStart", prefix, postfix);
    }

    private static void TryPatch(Harmony harmony, Type type, string methodName,
        HarmonyMethod prefix, HarmonyMethod postfix)
    {
        try
        {
            var method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                Safe.Warn($"[RelicPatch] Method not found: {type.Name}.{methodName}");
                return;
            }
            harmony.Patch(method, prefix, postfix);
        }
        catch (Exception ex)
        {
            Safe.Warn($"[RelicPatch] Failed {type.Name}.{methodName}: {ex.Message}");
        }
    }
}

/// <summary>
/// Manual PowerHookContext patcher. Patches each power's hook method individually
/// with try/catch so one failure doesn't prevent other powers from being patched.
/// Prefix: sets the active power source on CombatTracker.
/// Postfix: clears the active power source.
/// </summary>
public static class PowerHookContextPatcher
{
    public static void SetPowerContext(PowerModel __instance)
    {
        Safe.Run(() => CombatTracker.Instance.SetActivePowerSource(__instance.Id.Entry));
    }

    public static void ClearPowerContext()
    {
        Safe.Run(() => CombatTracker.Instance.ClearActivePowerSource());
    }

    public static void PatchAll(Harmony harmony)
    {
        var prefix = new HarmonyMethod(AccessTools.Method(typeof(PowerHookContextPatcher), nameof(SetPowerContext)));
        var postfix = new HarmonyMethod(AccessTools.Method(typeof(PowerHookContextPatcher), nameof(ClearPowerContext)));

        // ── Ironclad ──
        TryPatch(harmony, typeof(RagePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(FlameBarrierPower), "AfterDamageReceived", prefix, postfix);
        TryPatch(harmony, typeof(FeelNoPainPower), "AfterCardExhausted", prefix, postfix);
        TryPatch(harmony, typeof(PlatingPower), "BeforeTurnEndEarly", prefix, postfix);
        TryPatch(harmony, typeof(InfernoPower), "AfterDamageReceived", prefix, postfix);
        TryPatch(harmony, typeof(InfernoPower), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(JuggernautPower), "AfterBlockGained", prefix, postfix);
        TryPatch(harmony, typeof(GrapplePower), "AfterBlockGained", prefix, postfix);
        TryPatch(harmony, typeof(CrimsonMantlePower), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(DarkEmbracePower), "AfterCardExhausted", prefix, postfix);
        TryPatch(harmony, typeof(DarkEmbracePower), "AfterTurnEnd", prefix, postfix);

        // ── Silent ──
        TryPatch(harmony, typeof(PoisonPower), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(OutbreakPower), "AfterPowerAmountChanged", prefix, postfix);
        TryPatch(harmony, typeof(PanachePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(AfterimagePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(SneakyPower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(ViciousPower), "AfterPowerAmountChanged", prefix, postfix);
        TryPatch(harmony, typeof(EnvenomPower), "AfterDamageGiven", prefix, postfix);
        TryPatch(harmony, typeof(NoxiousFumesPower), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(InfiniteBladesPower), "BeforeHandDraw", prefix, postfix);

        // ── Defect ──
        TryPatch(harmony, typeof(HailstormPower), "BeforeTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(AutomationPower), "AfterCardDrawn", prefix, postfix);
        TryPatch(harmony, typeof(IterationPower), "AfterCardDrawn", prefix, postfix);
        TryPatch(harmony, typeof(SubroutinePower), "BeforeCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(SubroutinePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(ConsumingShadowPower), "AfterTurnEnd", prefix, postfix);
        TryPatch(harmony, typeof(LoopPower), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(LightningRodPower), "AfterEnergyReset", prefix, postfix);
        TryPatch(harmony, typeof(SpinnerPower), "AfterEnergyReset", prefix, postfix);
        TryPatch(harmony, typeof(CreativeAiPower), "BeforeHandDraw", prefix, postfix);
        TryPatch(harmony, typeof(HelloWorldPower), "BeforeHandDraw", prefix, postfix);
        TryPatch(harmony, typeof(ThunderPower), "AfterOrbEvoked", prefix, postfix);

        // ── Necrobinder ──
        TryPatch(harmony, typeof(DanseMacabrePower), "BeforeCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(CountdownPower), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(HauntPower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(PagestormPower), "AfterCardDrawn", prefix, postfix);
        TryPatch(harmony, typeof(ShroudPower), "AfterPowerAmountChanged", prefix, postfix);
        TryPatch(harmony, typeof(SleightOfFleshPower), "AfterPowerAmountChanged", prefix, postfix);
        TryPatch(harmony, typeof(OblivionPower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(CallOfTheVoidPower), "BeforeHandDraw", prefix, postfix);
        TryPatch(harmony, typeof(NecroMasteryPower), "AfterCurrentHpChanged", prefix, postfix);
        TryPatch(harmony, typeof(DevourLifePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(SpiritOfAshPower), "BeforeCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(ReaperFormPower), "AfterDamageGiven", prefix, postfix);
        TryPatch(harmony, typeof(SicEmPower), "AfterDamageGiven", prefix, postfix);

        // ── Regent ──
        TryPatch(harmony, typeof(BlackHolePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(BlackHolePower), "AfterStarsGained", prefix, postfix);
        TryPatch(harmony, typeof(ChildOfTheStarsPower), "AfterStarsSpent", prefix, postfix);
        TryPatch(harmony, typeof(ParryPower), "AfterSovereignBladePlayed", prefix, postfix);
        TryPatch(harmony, typeof(PillarOfCreationPower), "AfterCardGeneratedForCombat", prefix, postfix);
        TryPatch(harmony, typeof(OrbitPower), "AfterEnergySpent", prefix, postfix);
        // ArsenalPower.AfterCardPlayed not patchable at runtime (not implemented, only inherited)
        // Arsenal's Strength application is tracked via PowerApplyPatch instead.
        TryPatch(harmony, typeof(MonologuePower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(MonarchsGazePower), "AfterDamageGiven", prefix, postfix);
        TryPatch(harmony, typeof(SpectrumShiftPower), "BeforeHandDraw", prefix, postfix);
        TryPatch(harmony, typeof(ReflectPower), "AfterDamageReceived", prefix, postfix);
        TryPatch(harmony, typeof(GenesisPower), "AfterEnergyReset", prefix, postfix);
        TryPatch(harmony, typeof(TheSealedThronePower), "BeforeCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(FurnacePower), "AfterSideTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(SentryModePower), "BeforeHandDraw", prefix, postfix);

        // ── Shared / Colorless ──
        TryPatch(harmony, typeof(SmokestackPower), "AfterCardGeneratedForCombat", prefix, postfix);
        TryPatch(harmony, typeof(RollingBoulderPower), "AfterPlayerTurnStart", prefix, postfix);
        TryPatch(harmony, typeof(SpeedsterPower), "AfterCardDrawn", prefix, postfix);
        TryPatch(harmony, typeof(SerpentFormPower), "AfterCardPlayed", prefix, postfix);
        TryPatch(harmony, typeof(TrashToTreasurePower), "AfterCardGeneratedForCombat", prefix, postfix);
        // StampedePower.BeforeTurnEnd not patchable at runtime (not implemented, only inherited)
        // Stampede's auto-played cards generate their own CardPlay events.

        // NOTE: TemporaryStrengthPower.AfterTurnEnd is handled by TempStrengthRevertPatch
        // with specific revert logic, not the generic Set/Clear power context.
    }

    private static void TryPatch(Harmony harmony, Type type, string methodName,
        HarmonyMethod prefix, HarmonyMethod postfix)
    {
        try
        {
            var method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                Safe.Warn($"[PowerPatch] Method not found: {type.Name}.{methodName}");
                return;
            }
            harmony.Patch(method, prefix, postfix);
        }
        catch (Exception ex)
        {
            Safe.Warn($"[PowerPatch] Failed {type.Name}.{methodName}: {ex.Message}");
        }
    }
}

// ═══════════════════════════════════════════════════════════
// Module A+F: Damage/Block modifier attribution
// Patches Hook.ModifyDamageInternal and Hook.ModifyBlock to capture
// per-modifier contributions (Strength, Vulnerable, Dexterity, Frail, etc.)
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class DamageModifierPatch
{
    /// <summary>
    /// Patch Hook.ModifyDamage (the public entry point) to capture base damage before
    /// modifiers and the final result after, then record per-modifier contributions.
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    [HarmonyPrefix]
    public static void BeforeModifyDamage(decimal damage, out decimal __state)
    {
        __state = damage;
        // Fix-3: Clear stale modifiers from previous call to prevent leaking
        ContributionMap.Instance.LastDamageModifiers.Clear();
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    [HarmonyPostfix]
    public static void AfterModifyDamage(decimal __result, decimal __state,
        Creature? target, Creature? dealer, ValueProp props, CardModel? cardSource,
        IEnumerable<AbstractModel> modifiers)
    {
        Safe.Run(() =>
        {
            // Only track damage dealt by player to enemies
            if (dealer == null || !dealer.IsPlayer || target == null || target.IsPlayer)
                return;
            // IsPoweredAttack = has Move flag and not Unpowered
            if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
                return;

            decimal baseDmg = __state;
            decimal finalDmg = __result;
            decimal totalBonus = finalDmg - baseDmg;

            if (totalBonus <= 0 || modifiers == null)
                return;

            var modList = ContributionMap.Instance.LastDamageModifiers;
            modList.Clear();

            // Attribute modifier bonuses to their power/relic sources
            foreach (var mod in modifiers)
            {
                if (mod is PowerModel power)
                {
                    var powerId = power.Id.Entry;
                    if (string.IsNullOrEmpty(powerId)) continue;

                    // For additive modifiers (Strength, Vigor, etc.), get the additive contribution
                    decimal additive = power.ModifyDamageAdditive(target, baseDmg, props, dealer, cardSource);

                    if (additive != 0)
                    {
                        var source = ContributionMap.Instance.GetPowerSource(powerId);
                        if (source != null)
                            modList.Add(new ContributionMap.ModifierContribution(
                                source.SourceId, source.SourceType, Math.Abs((int)additive)));
                        continue;
                    }

                    // For multiplicative modifiers
                    decimal multiplicative = power.ModifyDamageMultiplicative(target, baseDmg + additive, props, dealer, cardSource);
                    if (multiplicative == 1m || multiplicative <= 0) continue;

                    // H2-R: Decompose VulnerablePower/WeakPower internal modifiers
                    if (powerId == "VULNERABLE_POWER")
                    {
                        DecomposeVulnerableContribution(power, target, dealer, props, cardSource,
                            finalDmg, multiplicative, modList);
                        continue;
                    }
                    if (powerId == "WEAK_POWER")
                    {
                        DecomposeWeakContribution(power, target, dealer, props, cardSource,
                            finalDmg, multiplicative, modList);
                        continue;
                    }

                    // Standard multiplicative: contribution = finalDmg - finalDmg/multiplier
                    int contribution = (int)(finalDmg - finalDmg / multiplicative);
                    if (contribution == 0) continue;

                    var powerSource = ContributionMap.Instance.GetPowerSource(powerId);
                    if (powerSource != null)
                    {
                        modList.Add(new ContributionMap.ModifierContribution(
                            powerSource.SourceId, powerSource.SourceType, Math.Abs(contribution)));
                    }
                }
                else if (mod is RelicModel relic)
                {
                    var relicId = relic.Id.Entry;
                    if (!string.IsNullOrEmpty(relicId))
                    {
                        // Relic modifier contribution — approximate
                        int contribution = modList.Count == 0 ? (int)totalBonus :
                            (int)(totalBonus / (modList.Count + 1));
                        if (contribution > 0)
                        {
                            modList.Add(new ContributionMap.ModifierContribution(
                                relicId, "relic", contribution));
                        }
                    }
                }
            }
        });
    }

    /// <summary>
    /// H2-R: Decompose VulnerablePower's combined multiplier into individual contributions:
    /// Vulnerable base (1.5) + PaperPhrog (+0.25) + Cruelty (+Amount/100) + Debilitate (doubles bonus).
    /// Each sub-contributor gets a proportional share of the total Vulnerable damage bonus.
    /// </summary>
    private static void DecomposeVulnerableContribution(
        PowerModel vulnPower, Creature? target, Creature? dealer, ValueProp props, CardModel? cardSource,
        decimal finalDmg, decimal combinedMult,
        List<ContributionMap.ModifierContribution> modList)
    {
        // Total damage bonus from Vulnerable = finalDmg - finalDmg / combinedMult
        int totalContrib = (int)(finalDmg - finalDmg / combinedMult);
        if (totalContrib <= 0) return;

        // Base Vulnerable multiplier bonus (typically 0.5 from 1.5)
        decimal vulnBaseDelta = 0.5m;
        decimal phrogDelta = 0;
        decimal crueltyDelta = 0;
        decimal debilitateDelta = 0;

        // Check PaperPhrog relic on dealer
        if (dealer?.Player != null)
        {
            var phrog = dealer.Player.GetRelic<PaperPhrog>();
            if (phrog != null)
                phrogDelta = 0.25m;
        }

        // Check CrueltyPower on dealer
        if (dealer != null)
        {
            var cruelty = dealer.GetPower<CrueltyPower>();
            if (cruelty != null)
                crueltyDelta = (decimal)cruelty.Amount / 100m;
        }

        // Debilitate on target doubles the bonus
        if (target != null)
        {
            var debilitate = target.GetPower<DebilitatePower>();
            if (debilitate != null)
            {
                decimal preDebilMult = 1m + vulnBaseDelta + phrogDelta + crueltyDelta;
                debilitateDelta = preDebilMult - 1m; // doubles the bonus
            }
        }

        // Proportional share of total contribution
        decimal subTotal = vulnBaseDelta + phrogDelta + crueltyDelta + debilitateDelta;
        if (subTotal <= 0) return;

        // Vulnerable base — split among FIFO sources (Fix-4)
        int vulnShare = (int)Math.Round(totalContrib * (vulnBaseDelta / subTotal));
        if (vulnShare > 0 && target != null)
        {
            var fractions = ContributionMap.Instance.GetDebuffSourceFractions(target.GetHashCode(), "VULNERABLE_POWER");
            if (fractions.Count > 0)
            {
                foreach (var (srcId, srcType, frac) in fractions)
                {
                    int srcShare = (int)Math.Round(vulnShare * frac);
                    if (srcShare > 0)
                        modList.Add(new ContributionMap.ModifierContribution(srcId, srcType, srcShare));
                }
            }
            else
            {
                var vulnSource = ContributionMap.Instance.GetPowerSource("VULNERABLE_POWER");
                if (vulnSource != null)
                    modList.Add(new ContributionMap.ModifierContribution(vulnSource.SourceId, vulnSource.SourceType, vulnShare));
            }
        }

        // PaperPhrog relic
        if (phrogDelta > 0)
        {
            int phrogShare = (int)Math.Round(totalContrib * (phrogDelta / subTotal));
            if (phrogShare > 0)
                modList.Add(new ContributionMap.ModifierContribution("PAPER_PHROG", "relic", phrogShare));
        }

        // CrueltyPower
        if (crueltyDelta > 0)
        {
            var crueltySource = ContributionMap.Instance.GetPowerSource("CRUELTY_POWER");
            int crueltyShare = (int)Math.Round(totalContrib * (crueltyDelta / subTotal));
            if (crueltyShare > 0 && crueltySource != null)
                modList.Add(new ContributionMap.ModifierContribution(crueltySource.SourceId, crueltySource.SourceType, crueltyShare));
        }

        // DebilitatePower
        if (debilitateDelta > 0)
        {
            var debilSource = ContributionMap.Instance.GetPowerSource("DEBILITATE_POWER");
            int debilShare = (int)Math.Round(totalContrib * (debilitateDelta / subTotal));
            if (debilShare > 0 && debilSource != null)
                modList.Add(new ContributionMap.ModifierContribution(debilSource.SourceId, debilSource.SourceType, debilShare));
        }
    }

    /// <summary>
    /// H2-R: Decompose WeakPower's combined multiplier into individual contributions:
    /// Weak base (0.75) + PaperKrane (-0.15) + Debilitate (doubles reduction).
    /// </summary>
    private static void DecomposeWeakContribution(
        PowerModel weakPower, Creature? target, Creature? dealer, ValueProp props, CardModel? cardSource,
        decimal finalDmg, decimal combinedMult,
        List<ContributionMap.ModifierContribution> modList)
    {
        // Weak reduces damage: the "bonus" is negative (damage was reduced)
        // We track this as a positive number representing damage mitigated
        int totalContrib = (int)(finalDmg / combinedMult - finalDmg);
        if (totalContrib <= 0) return;

        decimal baseReduction = 0.25m; // from 0.75 multiplier
        decimal kraneDelta = 0;
        decimal debilitateDelta = 0;

        // Check PaperKrane relic on target (player)
        if (target?.Player != null)
        {
            var krane = target.Player.GetRelic<PaperKrane>();
            if (krane != null)
                kraneDelta = 0.15m;
        }

        // Debilitate on dealer doubles the reduction
        if (dealer != null)
        {
            var debilitate = dealer.GetPower<DebilitatePower>();
            if (debilitate != null)
                debilitateDelta = baseReduction + kraneDelta; // doubles
        }

        decimal subTotal = baseReduction + kraneDelta + debilitateDelta;
        if (subTotal <= 0) return;

        // Weak base share
        var weakSource = ContributionMap.Instance.GetPowerSource("WEAK_POWER");
        int weakShare = (int)Math.Round(totalContrib * (baseReduction / subTotal));
        if (weakShare > 0 && weakSource != null)
            modList.Add(new ContributionMap.ModifierContribution(weakSource.SourceId, weakSource.SourceType, weakShare));

        // PaperKrane relic
        if (kraneDelta > 0)
        {
            int kraneShare = (int)Math.Round(totalContrib * (kraneDelta / subTotal));
            if (kraneShare > 0)
                modList.Add(new ContributionMap.ModifierContribution("PAPER_KRANE", "relic", kraneShare));
        }

        // DebilitatePower
        if (debilitateDelta > 0)
        {
            var debilSource = ContributionMap.Instance.GetPowerSource("DEBILITATE_POWER");
            int debilShare = (int)Math.Round(totalContrib * (debilitateDelta / subTotal));
            if (debilShare > 0 && debilSource != null)
                modList.Add(new ContributionMap.ModifierContribution(debilSource.SourceId, debilSource.SourceType, debilShare));
        }
    }
}

// ═══════════════════════════════════════════════════════════
// C1: Enemy base damage capture (for str reduction formula)
// Captures enemy's base damage BEFORE modifiers so we can compute
// how much strength reduction actually mitigated.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class EnemyDamageIntentPatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    [HarmonyPrefix]
    public static void BeforeModifyDamage_Enemy(decimal damage, Creature? dealer)
    {
        Safe.Run(() =>
        {
            // Only track damage dealt BY enemies TO player
            if (dealer == null || dealer.IsPlayer) return;
            ContributionMap.Instance.PendingEnemyBaseDamage = (int)damage;
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Fix-1: Decrement FIFO debuff layers when duration ticks down
// PowerModel.SetAmount is synchronous void — safe for Harmony.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class DebuffDurationPatch
{
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
    [HarmonyPrefix]
    public static void BeforeSetAmount(PowerModel __instance, out int __state)
    {
        __state = __instance.Amount;
    }

    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
    [HarmonyPostfix]
    public static void AfterSetAmount(PowerModel __instance, int amount, int __state)
    {
        Safe.Run(() =>
        {
            // Only care about duration decrements (amount < old) on non-player creatures
            if (amount >= __state) return;
            var owner = __instance.Owner;
            if (owner == null || owner.IsPlayer) return;

            var powerId = __instance.Id.Entry;
            if (string.IsNullOrEmpty(powerId)) return;

            // Only decrement for duration-based debuffs we track (Vulnerable, Weak)
            if (powerId != "VULNERABLE_POWER" && powerId != "WEAK_POWER") return;

            int decremented = __state - amount;
            for (int i = 0; i < decremented; i++)
                ContributionMap.Instance.DecrementDebuffLayers(owner.GetHashCode(), powerId);
        });
    }
}

[HarmonyPatch]
public static class BlockModifierPatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
    [HarmonyPrefix]
    public static void BeforeModifyBlock(decimal block, out decimal __state)
    {
        __state = block;
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
    [HarmonyPostfix]
    public static void AfterModifyBlock(decimal __result, decimal __state,
        Creature target, CardModel? cardSource, CardPlay? cardPlay,
        IEnumerable<AbstractModel> modifiers)
    {
        Safe.Run(() =>
        {
            if (target == null || !target.IsPlayer) return;

            decimal baseBlock = __state;
            decimal finalBlock = __result;
            decimal totalBonus = finalBlock - baseBlock;

            if (totalBonus <= 0 || modifiers == null)
                return;

            var modList = ContributionMap.Instance.LastBlockModifiers;
            modList.Clear();

            // Each modifier's actual contribution is its ModifyBlockAdditive return value
            // The game calls each power with a running block total, and each returns its additive delta
            foreach (var mod in modifiers)
            {
                if (mod is PowerModel power)
                {
                    var powerId = power.Id.Entry;
                    if (string.IsNullOrEmpty(powerId)) continue;

                    var source = ContributionMap.Instance.GetPowerSource(powerId);
                    if (source == null) continue;

                    // Use the power's actual additive contribution (Amount for most powers like Dexterity)
                    int contribution = Math.Abs(power.Amount);
                    if (contribution > 0)
                    {
                        modList.Add(new ContributionMap.ModifierContribution(
                            source.SourceId, source.SourceType, contribution));
                    }
                }
            }
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Module C: Energy gain tracking
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class EnergyGainPatch
{
    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))]
    [HarmonyPostfix]
    public static void AfterGainEnergy(decimal amount)
    {
        Safe.Run(() =>
        {
            if (amount > 0)
                CombatTracker.Instance.OnEnergyGained((int)amount);
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Module E: Card origin tracking (generated/transformed cards)
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class CardOriginPatch
{
    /// <summary>
    /// Tracks CardCmd.Transform to record that the new card originated from
    /// the card that was being played (PrimalForce transforms attacks to GiantRock).
    /// </summary>
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Transform), new Type[] { typeof(CardModel), typeof(CardModel), typeof(CardPreviewStyle) })]
    [HarmonyPostfix]
    public static void AfterTransform(CardModel original, CardModel replacement)
    {
        Safe.Run(() =>
        {
            if (replacement == null) return;
            var replacementId = replacement.Id.Entry;
            if (replacementId == null) return;

            // Use the currently-playing card (e.g. BEGONE!, CHARGE!!) as origin,
            // falling back to the original card ID if no active context.
            var activeCard = CombatTracker.Instance.ActiveCardId;
            var originId = activeCard ?? original?.Id.Entry ?? "unknown";
            CombatTracker.Instance.RecordCardOrigin(
                replacement.GetHashCode(), originId, "card");
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Module D: Upgrade source tracking
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class CardUpgradeTrackerPatch
{
    private static decimal _preDamage;
    private static decimal _preBlock;
    private static int _cardHash;

    /// <summary>
    /// Before a card is upgraded, capture its current damage/block values.
    /// </summary>
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade),
        new Type[] { typeof(CardModel), typeof(CardPreviewStyle) })]
    [HarmonyPrefix]
    public static void BeforeUpgrade(CardModel card)
    {
        Safe.Run(() =>
        {
            _cardHash = card.GetHashCode();
            var dv = card.DynamicVars;
            _preDamage = dv != null && dv.TryGetValue("Damage", out var dmg) ? dmg.BaseValue : 0;
            _preBlock = dv != null && dv.TryGetValue("Block", out var blk) ? blk.BaseValue : 0;
        });
    }

    /// <summary>
    /// After upgrade, compute delta and record upgrade source.
    /// </summary>
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade),
        new Type[] { typeof(CardModel), typeof(CardPreviewStyle) })]
    [HarmonyPostfix]
    public static void AfterUpgrade(CardModel card)
    {
        Safe.Run(() =>
        {
            if (card.GetHashCode() != _cardHash) return;

            var dv = card.DynamicVars;
            decimal postDamage = dv != null && dv.TryGetValue("Damage", out var dmg) ? dmg.BaseValue : 0;
            decimal postBlock = dv != null && dv.TryGetValue("Block", out var blk) ? blk.BaseValue : 0;

            int damageDelta = (int)(postDamage - _preDamage);
            int blockDelta = (int)(postBlock - _preBlock);

            if (damageDelta <= 0 && blockDelta <= 0) return;

            // Store the delta for later attribution when the upgraded card is played.
            // Source = the card that triggered the upgrade (e.g., Armaments), or "upgrade" fallback
            var tracker = CombatTracker.Instance;
            string sourceId = tracker.ActiveCardId ?? "upgrade";
            string sourceType = tracker.ActiveCardId != null ? "card" : "upgrade";
            ContributionMap.Instance.RecordUpgradeDelta(card.GetHashCode(),
                damageDelta, blockDelta, sourceId, sourceType);
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Existing patches: Buffer, Intangible, Potion
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class PowerMitigationPatch
{
    [HarmonyPatch(typeof(BufferPower), nameof(BufferPower.ModifyHpLostAfterOstyLate))]
    [HarmonyPrefix]
    public static void BeforeBufferModify(Creature target, decimal amount, out decimal __state)
    {
        __state = amount;
    }

    [HarmonyPatch(typeof(BufferPower), nameof(BufferPower.ModifyHpLostAfterOstyLate))]
    [HarmonyPostfix]
    public static void AfterBufferModify(decimal __result, decimal __state, Creature target)
    {
        Safe.Run(() =>
        {
            if (__state > 0 && __result == 0 && target.IsPlayer)
            {
                CombatTracker.Instance.OnBufferPrevention((int)__state);
            }
        });
    }

    [HarmonyPatch(typeof(IntangiblePower), nameof(IntangiblePower.ModifyHpLostAfterOsty))]
    [HarmonyPrefix]
    public static void BeforeIntangibleModify(Creature target, decimal amount, out decimal __state)
    {
        __state = amount;
    }

    [HarmonyPatch(typeof(IntangiblePower), nameof(IntangiblePower.ModifyHpLostAfterOsty))]
    [HarmonyPostfix]
    public static void AfterIntangibleModify(decimal __result, decimal __state, Creature target)
    {
        Safe.Run(() =>
        {
            if (__state > __result && __result >= 0 && target.IsPlayer)
            {
                CombatTracker.Instance.OnIntangibleReduction((int)__state, (int)__result);
            }
        });
    }
}

[HarmonyPatch]
public static class PotionUsedPatch
{
    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PotionUsed))]
    [HarmonyPostfix]
    public static void AfterPotionUsed(CombatState combatState, PotionModel potion, Creature? target)
    {
        Safe.Run(() =>
        {
            var potionId = potion?.Id.Entry;
            if (!string.IsNullOrEmpty(potionId))
            {
                Safe.Info($"[DIAG:Potion] Used: {potionId}");
            }
            // Clear potion context here — this sync method fires AFTER OnUse() completes
            // inside the async OnUseWrapper chain (line 258 of PotionModel.cs).
            CombatTracker.Instance.ClearActivePotion();
        });
    }
}

[HarmonyPatch]
public static class PotionContextPatch
{
    // Only Prefix — do NOT add a Postfix here.
    // OnUseWrapper is async: a Postfix would fire at the first await (before OnUse()),
    // clearing _activePotionId before effects execute.
    // Clearing happens in PotionUsedPatch.AfterPotionUsed (sync, fires after OnUse).
    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
    [HarmonyPrefix]
    public static void BeforePotionUse(PotionModel __instance)
    {
        Safe.Run(() =>
        {
            var potionId = __instance.Id.Entry;
            if (!string.IsNullOrEmpty(potionId))
            {
                CombatTracker.Instance.SetActivePotion(potionId);
            }
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Healing tracking
// Patches CreatureCmd.Heal to record HP healed with source attribution.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class HealingPatch
{
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal),
        new Type[] { typeof(Creature), typeof(decimal), typeof(bool) })]
    [HarmonyPrefix]
    public static void BeforeHeal(Creature creature, decimal amount, out (int hash, decimal hpBefore) __state)
    {
        __state = (0, 0);
        try
        {
            if (creature != null && creature.IsPlayer)
            {
                __state = (creature.GetHashCode(), creature.CurrentHp);
            }
        }
        catch { /* safe */ }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal),
        new Type[] { typeof(Creature), typeof(decimal), typeof(bool) })]
    [HarmonyPostfix]
    public static void AfterHeal(Creature creature, (int hash, decimal hpBefore) __state)
    {
        Safe.Run(() =>
        {
            if (creature == null || !creature.IsPlayer) return;
            if (__state.hash != creature.GetHashCode()) return;

            // Skip initial HP setup (character created with 0 HP then healed to full)
            if (__state.hpBefore <= 0) return;

            int actualHealed = (int)(creature.CurrentHp - __state.hpBefore);
            if (actualHealed <= 0) return;

            // Determine fallback source from room type when no active context exists
            string? fallbackId = null;
            string? fallbackType = null;
            var runState = creature.Player?.RunState;
            var room = runState?.CurrentRoom;
            int floor = runState?.TotalFloor ?? 0;

            if (room is RestSiteRoom)
            {
                fallbackId = "REST_SITE";
                fallbackType = "rest";
            }
            else if (room is EventRoom eventRoom)
            {
                // Use the specific event ID for per-event distinction
                var eventId = eventRoom.CanonicalEvent?.Id.Entry;
                fallbackId = !string.IsNullOrEmpty(eventId) ? eventId : "EVENT_UNKNOWN";
                fallbackType = "event";
            }
            else if (room is MerchantRoom)
            {
                fallbackId = "MERCHANT";
                fallbackType = "merchant";
            }
            else
            {
                // Between-floor recovery or other non-room healing
                fallbackId = $"FLOOR_{floor}_REGEN";
                fallbackType = "floor_regen";
            }

            CombatTracker.Instance.OnHealingReceived(actualHealed, fallbackId, fallbackType);
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Killing blow damage capture
// CreatureCmd.Damage skips CombatHistory.DamageReceived when IsEnding is true
// (last primary enemy killed). Hook.AfterDamageGiven (line 265) is still called
// regardless. We manually patch it to capture the missed damage.
// Uses identity hash dedup to avoid double-counting results that DamageReceived
// already processed (for multi-target attacks where only the last target's hit
// was skipped).
// ═══════════════════════════════════════════════════════════

public static class KillingBlowPatcher
{
    // NOTE: Parameter names MUST match the compiled DLL, not the decompiled source.
    // Compiled: results (not damageResult), target (not originalTarget)
    public static void CaptureKillingBlow(
        CombatState combatState,
        Creature? dealer,
        DamageResult results,
        Creature target,
        CardModel? cardSource)
    {
        Safe.Run(() =>
        {
            // Skip if DamageReceived already handled this result
            int resultId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(results);
            if (CombatTracker.Instance.IsDamageResultProcessed(resultId)) return;

            var receiver = results.Receiver;
            bool isPlayerReceiver = receiver.IsPlayer;

            // Only care about damage TO enemies
            if (isPlayerReceiver) return;
            if (results.TotalDamage <= 0) return;

            var cardSourceId = cardSource?.Id.Entry;

            Safe.Info($"[KillingBlow] Capturing missed damage: {cardSourceId ?? "?"} → {results.TotalDamage} to {target}");

            CombatTracker.Instance.OnDamageDealt(
                results.TotalDamage,
                results.BlockedDamage,
                cardSourceId,
                isPlayerReceiver,
                receiver.GetHashCode(),
                dealer?.GetHashCode() ?? 0);
        });
    }

    public static void PatchAll(Harmony harmony)
    {
        try
        {
            var targetMethod = AccessTools.Method(typeof(Hook), nameof(Hook.AfterDamageGiven));
            if (targetMethod == null)
            {
                Safe.Warn("[KillingBlow] Hook.AfterDamageGiven method not found!");
                return;
            }
            var prefix = new HarmonyMethod(AccessTools.Method(typeof(KillingBlowPatcher), nameof(CaptureKillingBlow)));
            harmony.Patch(targetMethod, prefix: prefix);
            Safe.Info("[KillingBlow] Successfully patched Hook.AfterDamageGiven");
        }
        catch (Exception ex)
        {
            Safe.Warn($"[KillingBlow] Failed to patch: {ex}");
        }
    }
}

// NOTE: RegentPowerHookContextPatch removed — consolidated into PowerHookContextPatcher.

// ═══════════════════════════════════════════════════════════
// SovereignBlade + SeekingEdge damage split
// When SeekingEdge is active, records the primary target (highest HP enemy)
// so that non-primary damage is attributed to SeekingEdge's source card.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class SovereignBladeSeekingEdgePatch
{
    [HarmonyPatch(typeof(SovereignBlade), "OnPlay")]
    [HarmonyPrefix]
    public static void BeforeSovereignBlade(SovereignBlade __instance)
    {
        Safe.Run(() =>
        {
            if (!__instance.Owner.Creature.HasPower<SeekingEdgePower>()) return;
            var enemies = __instance.CombatState?.HittableEnemies;
            if (enemies == null || enemies.Count == 0) return;

            Creature? primary = null;
            decimal maxHp = -1;
            foreach (var e in enemies)
            {
                if (e.CurrentHp > maxHp)
                {
                    maxHp = e.CurrentHp;
                    primary = e;
                }
            }
            if (primary != null)
                CombatTracker.Instance.SetSeekingEdgeContext(primary.GetHashCode());
        });
    }

    [HarmonyPatch(typeof(SovereignBlade), "OnPlay")]
    [HarmonyPostfix]
    public static void AfterSovereignBlade()
    {
        Safe.Run(() => CombatTracker.Instance.ClearSeekingEdgeContext());
    }
}

// ═══════════════════════════════════════════════════════════
// VoidForm / BulletTime cost savings tracking
// Records energy and star cost reductions as contributions.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class CostSavingsPatch
{
    // ── VoidFormPower: TryModifyEnergyCostInCombat → energy savings ──
    [HarmonyPatch(typeof(VoidFormPower), nameof(VoidFormPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterVoidFormEnergy(bool __result, VoidFormPower __instance,
        decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() =>
        {
            if (!__result || originalCost <= modifiedCost) return;
            int saved = (int)(originalCost - modifiedCost);
            if (saved <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordPendingCostSavings(
                    source.SourceId, source.SourceType, saved, 0);
        });
    }

    // ── VoidFormPower: TryModifyStarCost → star savings ──
    [HarmonyPatch(typeof(VoidFormPower), nameof(VoidFormPower.TryModifyStarCost))]
    [HarmonyPostfix]
    public static void AfterVoidFormStar(bool __result, VoidFormPower __instance,
        decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() =>
        {
            if (!__result || originalCost <= modifiedCost) return;
            int saved = (int)(originalCost - modifiedCost);
            if (saved <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordPendingCostSavings(
                    source.SourceId, source.SourceType, 0, saved);
        });
    }

    // ── Consume pending cost savings when a card actually plays ──
    // VoidFormPower.AfterCardPlayed fires after the card is played,
    // confirming the cost reduction was actually used.
    [HarmonyPatch(typeof(VoidFormPower), nameof(VoidFormPower.AfterCardPlayed))]
    [HarmonyPostfix]
    public static void AfterVoidFormCardPlayed()
    {
        Safe.Run(() => CombatTracker.Instance.ConsumePendingCostSavings());
    }
}

// ═══════════════════════════════════════════════════════════
// ModifyHandDraw extra draw tracking (PaleBlueDot, Tyranny)
// Records extra draws from powers that modify hand draw count.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class HandDrawBonusPatch
{
    [HarmonyPatch(typeof(PaleBlueDotPower), nameof(PaleBlueDotPower.ModifyHandDraw))]
    [HarmonyPostfix]
    public static void AfterPaleBlueDotDraw(decimal __result, PaleBlueDotPower __instance,
        Player player, decimal count)
    {
        Safe.Run(() =>
        {
            if (player != __instance.Owner.Player) return;
            int extra = (int)(__result - count);
            if (extra <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordHandDrawBonus(source.SourceId, source.SourceType, extra);
        });
    }

    [HarmonyPatch(typeof(TyrannyPower), nameof(TyrannyPower.ModifyHandDraw))]
    [HarmonyPostfix]
    public static void AfterTyrannyDraw(decimal __result, TyrannyPower __instance,
        Player player, decimal count)
    {
        Safe.Run(() =>
        {
            if (player != __instance.Owner.Player) return;
            int extra = (int)(__result - count);
            if (extra <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordHandDrawBonus(source.SourceId, source.SourceType, extra);
        });
    }

    [HarmonyPatch(typeof(DemesnePower), nameof(DemesnePower.ModifyHandDraw))]
    [HarmonyPostfix]
    public static void AfterDemesneDraw(decimal __result, DemesnePower __instance,
        Player player, decimal count)
    {
        Safe.Run(() =>
        {
            if (player != __instance.Owner.Player) return;
            int extra = (int)(__result - count);
            if (extra <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordHandDrawBonus(source.SourceId, source.SourceType, extra);
        });
    }

    [HarmonyPatch(typeof(MachineLearningPower), nameof(MachineLearningPower.ModifyHandDraw))]
    [HarmonyPostfix]
    public static void AfterMachineLearningDraw(decimal __result, MachineLearningPower __instance,
        Player player, decimal count)
    {
        Safe.Run(() =>
        {
            if (player != __instance.Owner.Player) return;
            int extra = (int)(__result - count);
            if (extra <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordHandDrawBonus(source.SourceId, source.SourceType, extra);
        });
    }

    [HarmonyPatch(typeof(ToolsOfTheTradePower), nameof(ToolsOfTheTradePower.ModifyHandDraw))]
    [HarmonyPostfix]
    public static void AfterToolsOfTheTradeDraw(decimal __result, ToolsOfTheTradePower __instance,
        Player player, decimal count)
    {
        Safe.Run(() =>
        {
            if (player != __instance.Owner.Player) return;
            int extra = (int)(__result - count);
            if (extra <= 0) return;
            var source = ContributionMap.Instance.GetPowerSource(__instance.Id.Entry);
            if (source != null)
                ContributionMap.Instance.RecordHandDrawBonus(source.SourceId, source.SourceType, extra);
        });
    }

    // Flush pending hand draw bonuses after hand draw modifiers complete
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterModifyingHandDraw))]
    [HarmonyPostfix]
    public static void AfterModifyingHandDraw()
    {
        Safe.Run(() => CombatTracker.Instance.FlushPendingHandDrawBonus());
    }
}

// ═══════════════════════════════════════════════════════════
// Enemy strength reduction tracking for defense attribution
// Detects when negative Strength is applied to enemies by player sources.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class EnemyStrReductionPatch
{
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ApplyInternal))]
    [HarmonyPostfix]
    public static void AfterApplyInternal_StrTracking(PowerModel __instance, Creature owner)
    {
        Safe.Run(() =>
        {
            // Only track StrengthPower with negative amount applied to non-player creatures
            if (__instance is not StrengthPower) return;
            if (owner.IsPlayer) return;
            if (__instance.Amount >= 0) return; // Only negative (reduction)

            // Determine the source: use active power source first, then active card/potion/relic
            string? sourceId = CombatTracker.Instance.ActivePowerSourceId;
            string sourceType = CombatTracker.Instance.ActivePowerSourceType ?? "card";

            if (sourceId == null)
            {
                sourceId = CombatTracker.Instance.ActiveCardId;
                sourceType = "card";
            }
            if (sourceId == null) return;

            bool isTemporary = __instance is TemporaryStrengthPower;
            int reductionAmount = (int)Math.Abs(__instance.Amount);

            ContributionMap.Instance.RecordStrengthReduction(
                owner.GetHashCode(), sourceId, sourceType, reductionAmount, isTemporary);
        });
    }
}

// ═══════════════════════════════════════════════════════════
// TemporaryStrengthPower revert tracking
// When temporary strength reverts at turn end, remove from tracking.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class TempStrengthRevertPatch
{
    [HarmonyPatch(typeof(TemporaryStrengthPower), nameof(TemporaryStrengthPower.AfterTurnEnd))]
    [HarmonyPostfix]
    public static void AfterTempStrRevert(TemporaryStrengthPower __instance)
    {
        Safe.Run(() =>
        {
            if (__instance.Owner != null && !__instance.Owner.IsPlayer)
            {
                ContributionMap.Instance.RevertTemporaryStrReduction(__instance.Owner.GetHashCode());
            }
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Card generation origin tracking (SpectrumShift + general)
// Records origin for cards generated via CardPileCmd.AddGeneratedCardsToCombat.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class CardGenerationOriginPatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat))]
    [HarmonyPostfix]
    public static void AfterCardGenerated(CardModel card, bool addedByPlayer)
    {
        Safe.Run(() =>
        {
            if (!addedByPlayer) return;

            // If there's an active card context (e.g., CHARGE!! generating cards)
            var activeCard = CombatTracker.Instance.ActiveCardId;
            if (activeCard != null)
            {
                CombatTracker.Instance.RecordCardOrigin(
                    card.GetHashCode(), activeCard, "card");
                return;
            }

            // If there's an active power source (e.g., SpectrumShift generating cards)
            var powerSourceId = CombatTracker.Instance.ActivePowerSourceId;
            if (powerSourceId != null)
            {
                // Resolve to the card that created the power
                var source = ContributionMap.Instance.GetPowerSource(powerSourceId);
                if (source != null)
                {
                    CombatTracker.Instance.RecordCardOrigin(
                        card.GetHashCode(), source.SourceId, source.SourceType);
                }
            }
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Osty Summon HP tracking
// Records which card/relic summoned Osty HP for LIFO defense attribution.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class OstySummonPatch
{
    /// <summary>
    /// After OstyCmd.Summon completes, record the HP added to Osty's LIFO stack.
    /// We use a Prefix to capture the source and amount, and Postfix to record.
    /// </summary>
    [HarmonyPatch(typeof(OstyCmd), nameof(OstyCmd.Summon))]
    [HarmonyPrefix]
    public static void BeforeSummon(Player summoner, decimal amount, AbstractModel? source,
        out (string sourceId, string sourceType, int amount) __state)
    {
        __state = ("", "", 0);
        try
        {
            int summonAmount = (int)amount;
            if (summonAmount <= 0) return;

            string sourceId;
            string sourceType;

            if (source is CardModel card)
            {
                sourceId = card.Id.Entry ?? "unknown";
                sourceType = "card";
            }
            else if (source is RelicModel relic)
            {
                sourceId = relic.Id.Entry ?? "unknown";
                sourceType = "relic";
            }
            else
            {
                // Fallback: check power source context (e.g., DevourLifePower, SicEmPower)
                var powerSrcId = CombatTracker.Instance.ActivePowerSourceId;
                if (powerSrcId != null)
                {
                    sourceId = powerSrcId;
                    sourceType = CombatTracker.Instance.ActivePowerSourceType ?? "card";
                }
                else
                {
                    sourceId = CombatTracker.Instance.ActiveCardId ?? "unknown";
                    sourceType = "card";
                }
            }

            __state = (sourceId, sourceType, summonAmount);
        }
        catch (Exception ex)
        {
            Safe.Warn($"[OstySummonPatch.Prefix] {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(OstyCmd), nameof(OstyCmd.Summon))]
    [HarmonyPostfix]
    public static void AfterSummon((string sourceId, string sourceType, int amount) __state)
    {
        Safe.Run(() =>
        {
            if (__state.amount <= 0 || string.IsNullOrEmpty(__state.sourceId)) return;
            CombatTracker.Instance.OnOstySummoned(__state.sourceId, __state.sourceType, __state.amount);
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Doom kill damage attribution
// Captures enemy HP before DoomKill, attributes as damage after.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class DoomKillPatch
{
    /// <summary>
    /// Before DoomPower.DoomKill executes, capture each creature's current HP.
    /// After kill, attribute total HP as damage to Doom source.
    /// </summary>
    [HarmonyPatch(typeof(DoomPower), nameof(DoomPower.DoomKill))]
    [HarmonyPrefix]
    public static void BeforeDoomKill(IReadOnlyList<Creature> creatures)
    {
        Safe.Run(() =>
        {
            foreach (var creature in creatures)
            {
                if (creature == null || !creature.IsAlive) continue;
                int hp = (int)creature.CurrentHp;
                if (hp > 0)
                {
                    CombatTracker.Instance.OnDoomTargetCapture(creature.GetHashCode(), hp);
                }
            }
        });
    }

    [HarmonyPatch(typeof(DoomPower), nameof(DoomPower.DoomKill))]
    [HarmonyPostfix]
    public static void AfterDoomKill()
    {
        Safe.Run(() =>
        {
            CombatTracker.Instance.OnDoomKillsCompleted();
        });
    }
}

// ═══════════════════════════════════════════════════════════
// Osty death tracking
// When Osty is killed (BoneShards sacrifice, Sacrifice card, combat death),
// clears LIFO HP stack and subtracts remaining HP from killer card's defense.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class OstyDeathPatch
{
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Kill),
        new Type[] { typeof(Creature), typeof(bool) })]
    [HarmonyPrefix]
    public static void BeforeKill(Creature creature)
    {
        Safe.Run(() =>
        {
            if (creature.Monster is Osty)
            {
                CombatTracker.Instance.OnOstyKilled();
            }
        });
    }
}

// NOTE: NecrobinderPowerHookContextPatch removed — consolidated into PowerHookContextPatcher.

// ═══════════════════════════════════════════════════════════
// Defect Orb contribution tracking
// Records which card channeled each orb, sets orb context during
// passive/evoke so damage/block/energy is attributed to the channeling card.
// Focus bonus is split out as ModifierDamage/ModifierBlock.
// ═══════════════════════════════════════════════════════════

[HarmonyPatch]
public static class OrbChanneledPatch
{
    /// <summary>
    /// After an orb is channeled, record the source card/relic that channeled it.
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterOrbChanneled))]
    [HarmonyPostfix]
    public static void AfterOrbChanneled(CombatState combatState, Player player, OrbModel orb)
    {
        Safe.Run(() =>
        {
            string? sourceId = CombatTracker.Instance.ActiveCardId;
            string sourceType = "card";

            if (sourceId == null)
            {
                sourceId = CombatTracker.Instance.ActivePowerSourceId;
                sourceType = CombatTracker.Instance.ActivePowerSourceType ?? "card";
            }
            if (sourceId == null) return;

            string orbType = orb switch
            {
                LightningOrb => "lightning",
                FrostOrb => "frost",
                DarkOrb => "dark",
                PlasmaOrb => "plasma",
                GlassOrb => "glass",
                _ => "unknown"
            };

            ContributionMap.Instance.RecordOrbSource(
                orb.GetHashCode(), sourceId, sourceType, orbType);
        });
    }
}

[HarmonyPatch]
public static class OrbPassivePatch
{
    /// <summary>
    /// Before orb passive triggers, set orb context and compute Focus split.
    /// First-trigger logic: during a card play, first trigger → channeling source,
    /// subsequent triggers → let _activeCardId (the trigger card) take priority.
    /// Outside card play (turn-end), orb context always applies.
    /// </summary>
    [HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.Passive))]
    [HarmonyPrefix]
    public static void BeforeOrbPassive(OrbModel orb)
    {
        Safe.Run(() =>
        {
            var source = ContributionMap.Instance.GetOrbSource(orb.GetHashCode());
            if (source == null) return;

            // First-trigger logic: if a card is playing and first trigger already used,
            // skip setting orb context so _activeCardId takes priority.
            bool duringCardPlay = CombatTracker.Instance.ActiveCardId != null;
            if (duringCardPlay && ContributionMap.Instance.OrbFirstTriggerUsed)
            {
                // Don't set orb context — _activeCardId (evoke/trigger card) will be used
                SetOrbFocusContrib(orb);
                return;
            }

            ContributionMap.Instance.SetActiveOrbContext(
                source.SourceId, source.SourceType, source.OrbType);

            if (duringCardPlay)
                ContributionMap.Instance.MarkOrbFirstTriggerUsed();

            SetOrbFocusContrib(orb);
        });
    }

    [HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.Passive))]
    [HarmonyPostfix]
    public static void AfterOrbPassive()
    {
        Safe.Run(() => ContributionMap.Instance.ClearActiveOrbContext());
    }

    /// <summary>Shared Focus computation used by both passive and evoke patches.</summary>
    internal static void SetOrbFocusContrib(OrbModel orb)
    {
        if (orb is PlasmaOrb) return; // Plasma doesn't use ModifyOrbValue
        var focusSource = ContributionMap.Instance.GetPowerSource("FOCUS_POWER");
        if (focusSource == null) return;
        var player = orb.Owner;
        if (player?.Creature == null) return;
        var focusPower = player.Creature.GetPower<FocusPower>();
        if (focusPower != null && focusPower.Amount > 0)
        {
            ContributionMap.Instance.SetPendingOrbFocusContrib(
                focusSource.SourceId, focusSource.SourceType,
                (int)focusPower.Amount);
        }
    }
}

[HarmonyPatch]
public static class OrbEvokePatch
{
    /// <summary>
    /// Before orb evoke, set orb context and compute Focus split.
    /// We patch EvokeNext and EvokeLast since the private Evoke method isn't directly patchable.
    /// The orb to be evoked is the front (EvokeNext) or last (EvokeLast) of the queue.
    /// </summary>
    [HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.EvokeNext))]
    [HarmonyPrefix]
    public static void BeforeEvokeNext(Player player)
    {
        Safe.Run(() =>
        {
            var orbQueue = player.PlayerCombatState?.OrbQueue;
            if (orbQueue == null || orbQueue.Orbs.Count == 0) return;
            var orb = orbQueue.Orbs[0]; // front orb
            SetOrbEvokeContext(orb);
        });
    }

    [HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.EvokeNext))]
    [HarmonyPostfix]
    public static void AfterEvokeNext()
    {
        Safe.Run(() => ContributionMap.Instance.ClearActiveOrbContext());
    }

    [HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.EvokeLast))]
    [HarmonyPrefix]
    public static void BeforeEvokeLast(Player player)
    {
        Safe.Run(() =>
        {
            var orbQueue = player.PlayerCombatState?.OrbQueue;
            if (orbQueue == null || orbQueue.Orbs.Count == 0) return;
            var orb = orbQueue.Orbs[orbQueue.Orbs.Count - 1]; // last orb
            SetOrbEvokeContext(orb);
        });
    }

    [HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.EvokeLast))]
    [HarmonyPostfix]
    public static void AfterEvokeLast()
    {
        Safe.Run(() => ContributionMap.Instance.ClearActiveOrbContext());
    }

    private static void SetOrbEvokeContext(OrbModel orb)
    {
        var source = ContributionMap.Instance.GetOrbSource(orb.GetHashCode());
        if (source == null) return;

        // First-trigger logic: same as OrbPassivePatch
        bool duringCardPlay = CombatTracker.Instance.ActiveCardId != null;
        if (duringCardPlay && ContributionMap.Instance.OrbFirstTriggerUsed)
        {
            // Don't set orb context — _activeCardId (evoke card) will be used
            OrbPassivePatch.SetOrbFocusContrib(orb);
            return;
        }

        ContributionMap.Instance.SetActiveOrbContext(
            source.SourceId, source.SourceType, source.OrbType);

        if (duringCardPlay)
            ContributionMap.Instance.MarkOrbFirstTriggerUsed();

        OrbPassivePatch.SetOrbFocusContrib(orb);
    }
}

// NOTE: All PowerHookContext patches (Ironclad, Silent, Defect, Necrobinder,
// Regent, Shared) are applied via PowerHookContextPatcher.PatchAll()
// in CommunityStatsMod.Initialize(), NOT via [HarmonyPatch] attributes.

