# Implementation Plan: NEW-1, NEW-2, NEW-3 (Revision 2)

## Overview

Three new features for the combat contribution system:
- **NEW-1**: Free Energy Attribution — when powers/relics make a card's energy cost 0, attribute saved energy
- **NEW-2**: Free Stars Attribution — same as NEW-1 but for star cost
- **NEW-3**: Max HP Gain as Healing — `CreatureCmd.GainMaxHp` counts as HpHealed

---

## Review Issues Resolved

| # | Severity | Issue | Resolution |
|---|----------|-------|------------|
| 1 | CRITICAL | TryModifyEnergyCostInCombat fires for DISPLAY, not just play | **RESOLVED**: Replaced per-power TryModifyEnergyCostInCombat patches with centralized play-time approach. Cost comparison happens ONCE at `SpendResources` time, not during cost display. See Step 3. |
| 2 | HIGH | Many SetThisTurnOrUntilPlayed(0) callers missed (Crossbow, Discovery, MadScience, LiquidMemories, RocketPunch) | **RESOLVED**: Centralized approach compares `Canonical` vs effective cost at play time. Catches ALL local modifier callers automatically regardless of method used. See Step 3. |
| 3 | HIGH | SetToFreeThisCombat not patched (Metamorphosis, TouchOfInsanity) | **RESOLVED**: Centralized approach detects cost reduction from `SetThisCombat(0)` the same way — `Canonical` vs effective at play time. No separate patch needed. See Step 3. |
| 4 | MEDIUM | GainMaxHp async Postfix timing | **RESOLVED**: Use only Prefix on GainMaxHp (capture maxHp before). Use `_isFromGainMaxHp` context flag. Check flag in HealingPatch to suppress. See Step 7. |
| 5 | MEDIUM | Heal suppression amount mismatch due to ModifyHealAmount | **RESOLVED**: Suppress by context flag (`_isFromGainMaxHp`), not by amount. Flag is set before GainMaxHp calls Heal, cleared after. See Step 7. |
| 6 | LOW | X-cost cards not discussed | **RESOLVED**: Skip X-cost cards from energy savings. X-cost cards intentionally spend all energy; making them free doesn't "save" energy. See Step 3. |

---

## 1. Gap Analysis

### NEW-1: Free Energy Attribution

**What exists:**
- `VoidFormPower.TryModifyEnergyCostInCombat` is patched in `CostSavingsPatch` (CombatHistoryPatch.cs ~line 1222)
- `ContributionMap.RecordPendingCostSavings()` / `ConsumePendingCostSavings()` infrastructure
- `CombatTracker.ConsumePendingCostSavings()` attributes energy to `EnergyGained`
- VoidForm consumption triggered via `VoidFormPower.AfterCardPlayed` postfix

**What's missing (and how centralized approach fixes it):**

All the following cost-reduction sources are missed by the current per-power patch approach. The centralized play-time comparison catches ALL of them:

**Hook-based reductions** (via `TryModifyEnergyCostInCombat`):
- `FreeAttackPower` — makes next Attack free (from Unrelenting card)
- `FreeSkillPower` — makes next Skill free
- `FreePowerPower` — makes next Power free
- `CorruptionPower` — makes ALL Skills free (permanent)
- `VeilpiercerPower` — makes Ethereal cards free
- `CuriousPower` — reduces Power cost by Amount (partial reduction)
- `BrilliantScarf` (relic) — makes every Nth card free

**Local modifier reductions** (via `SetToFreeThisTurn` / `SetThisTurnOrUntilPlayed(0)` / `SetToFreeThisCombat`):
- `BulletTime.OnPlay` — sets all hand cards free this turn
- `MummifiedHand` (relic) — sets random card free after playing Power
- `JeweledMask` (relic) — sets generated cards free
- `Crossbow` (relic) — calls `EnergyCost.SetThisTurnOrUntilPlayed(0)` directly
- `Discovery.OnPlay` — calls `EnergyCost.SetThisTurnOrUntilPlayed(0)` directly
- `MadScience.OnPlay` — calls `EnergyCost.SetThisTurnOrUntilPlayed(0)` directly
- `LiquidMemories.OnUse` — calls `EnergyCost.SetThisTurnOrUntilPlayed(0)` directly
- `RocketPunch.OnPlay` — calls `EnergyCost.SetThisTurnOrUntilPlayed(0)` on self
- `Metamorphosis.OnPlay` — calls `SetToFreeThisCombat()` on transformed cards
- `TouchOfInsanity.OnUse` — calls `SetToFreeThisCombat()` on selected card
- `VexingPuzzlebox` (relic) — calls `EnergyCost.SetThisCombat(0)`
- `AdaptiveStrike` — calls `EnergyCost.SetThisCombat(0)` on self
- `MomentumStrike` — calls `EnergyCost.SetThisCombat(0)` on self

**Generate+Free exception sources** (no energy attribution — tracked via sub-bar):
- `AttackPotion.OnUse`, `SkillPotion.OnUse`, `PowerPotion.OnUse`, `ColorlessPotion.OnUse`, `OrobicAcid.OnUse`
- `InfernalBlade.OnPlay`, `Distraction.OnPlay`, `WhiteNoise.OnPlay`, `Splash.OnPlay`

### NEW-2: Free Stars Attribution

**What exists:**
- `VoidFormPower.TryModifyStarCost` is patched
- `StarsContribution` field exists in `ContributionAccum`

**What's missing:**
- Same centralized approach covers star savings. At play time, compare base star cost vs effective star cost.

### NEW-3: Max HP Gain as Healing

(Unchanged from v1 — see original analysis. The fix addresses review issues 4 and 5.)

---

## 2. Architecture: Centralized Play-Time Cost Comparison

### Why NOT per-power patches

The original plan patched each power's `TryModifyEnergyCostInCombat` postfix. This has fatal flaws:
1. **TryModifyEnergyCostInCombat fires for DISPLAY** — every UI refresh (hovering, hand refresh) triggers it, not just actual plays. Stale pending savings accumulate.
2. **Local modifiers are invisible** — `SetThisTurnOrUntilPlayed(0)`, `SetToFreeThisCombat()` apply BEFORE the hook pipeline. The hook sees `originalCost = 0` and records nothing.
3. **Many callers bypass SetToFreeThisTurn** — Crossbow, Discovery, etc. call `EnergyCost.SetThisTurnOrUntilPlayed(0)` directly.

### The Centralized Approach

**Core idea**: At the moment energy is actually spent (`CardModel.SpendResources`), compare the card's **canonical cost** (printed cost) vs **effective cost** (what was actually paid). The difference is the total energy saved.

**Card play pipeline** (manual play):
```
PlayCardAction.Execute
  → card.SpendResources()
      → EnergyCost.GetAmountToSpend()     ← effective cost (all modifiers applied)
      → SpendEnergy(effectiveCost)
  → card.OnPlayWrapper()
      → Hook.BeforeCardPlayed
      → CombatHistory.CardPlayStarted     ← our existing postfix fires here
      → card.OnPlay()
      → CombatHistory.CardPlayFinished
      → Hook.AfterCardPlayed
```

**Interception point**: Patch `CardModel.SpendResources` with a Prefix to capture `card.EnergyCost.Canonical` (base cost) and a Postfix to capture the actual amounts spent. Record `(Canonical - actualSpent)` as the energy saved.

**For auto-played cards** (`CardCmd.AutoPlay`):
```
CardCmd.AutoPlay
  → ResourceInfo.EnergyValue = card.EnergyCost.GetAmountToSpend()  (but EnergySpent = 0)
  → card.OnPlayWrapper()
```
Auto-played cards don't spend energy, so `EnergySpent = 0` but `EnergyValue` reflects the effective cost. For auto-plays, the "savings" would be `Canonical - 0 = Canonical`, but this is misleading — the card was free because it was auto-played, not because a power made it free. **Skip auto-played cards from energy savings attribution.**

### Per-Source Attribution

The centralized approach tells us the TOTAL savings but not WHICH source caused the reduction. We need per-source attribution for the contribution system.

**Two-layer approach**:

**Layer 1: Source Tagging** — When a source reduces a card's cost, record `(cardHash → sourceId, sourceType)` in a lookup table. This happens at modification time (harmless even if triggered by display):
- For hook-based reductions: Postfix on each power's `TryModifyEnergyCostInCombat` that records `(cardHash, powerId, "power/relic")` to `_lastCostReductionSource`
- For local modifier reductions: Prefix on `CardModel.SetToFreeThisTurn`, `CardModel.SetToFreeThisCombat`, and `CardEnergyCost.SetThisTurnOrUntilPlayed(0)` that records `(cardHash, activeSourceId, sourceType)`

**Layer 2: Play-Time Attribution** — At `SpendResources` time, compute total savings and look up the source tag for this card. Attribute the savings to that source.

Key difference from v1: Layer 1 ONLY records source tags (cheap, overwritten each time). It does NOT record pending energy amounts. Layer 2 computes the actual savings ONCE at play time. This eliminates all stale-entry issues.

### Source Tag Recording Details

For **hook-based reductions**, the tag is recorded every time the UI refreshes, but that's fine — it's just a dictionary write. The last power to reduce cost gets the tag. At play time, we use whatever tag is current.

For **local modifier reductions**, the tag is recorded when `SetToFreeThisTurn` / `SetThisTurnOrUntilPlayed(0)` / `SetToFreeThisCombat` is called. Since local modifiers persist (until turn end / card played / combat end), the tag persists too.

For **multiple simultaneous sources**: If both a local modifier AND a hook-based power reduce cost, the local modifier is applied first (in `GetWithModifiers`). If the local modifier already sets cost to 0, the hook sees `originalCost = 0` and doesn't reduce further — so only the local modifier source gets credit. If the local modifier partially reduces and the hook finishes the job, both tags exist, but we use a priority system: local modifier tag takes priority (since it was applied first).

### X-Cost Card Exclusion

X-cost cards (`EnergyCost.CostsX == true`) are excluded from energy savings attribution. When an X-cost card is made free, `GetAmountToSpend()` returns 0 instead of all available energy. The "savings" would be meaningless — the card was designed to spend all energy, and making it free means 0 energy is relevant, not "all energy was saved." Skip these cards entirely.

---

## 3. Detailed Implementation Steps

### Step 1: Add source tag tracking infrastructure

**File: `ContributionMap.cs`**

Replace the single `_pendingCostSaving` with a source tag dictionary:

```csharp
// ── Cost Reduction Source Tags ──────────────────────────────
// Records which source last reduced a card's cost.
// Key: card instance hash → (sourceId, sourceType)
// Overwritten each time a source modifies cost (safe for display calls).
private readonly Dictionary<int, (string sourceId, string sourceType)> _costReductionSourceTag = new();

public void TagCostReductionSource(int cardHash, string sourceId, string sourceType)
{
    _costReductionSourceTag[cardHash] = (sourceId, sourceType);
}

public (string sourceId, string sourceType)? GetCostReductionSourceTag(int cardHash)
{
    return _costReductionSourceTag.TryGetValue(cardHash, out var tag) ? tag : null;
}

public void ClearCostReductionSourceTag(int cardHash)
{
    _costReductionSourceTag.Remove(cardHash);
}
```

Remove the old `_pendingCostSaving` / `_hasPendingCostSaving` fields and `RecordPendingCostSavings` / `ConsumePendingCostSavings` methods (replaced by centralized approach).

Keep the `_generatedAndFreedCards` HashSet for the exception rule.

Add to `Clear()`: `_costReductionSourceTag.Clear()`.

### Step 2: Add source tagging patches for hook-based reductions

**File: `CombatHistoryPatch.cs`** — replace existing `CostSavingsPatch` class

These patches ONLY record source tags. They do NOT record energy amounts.

```csharp
[HarmonyPatch]
public static class CostReductionSourceTagPatch
{
    // ── Hook-based reductions (TryModifyEnergyCostInCombat) ──

    // Generic helper for power-based cost reduction tagging
    private static void TagPowerCostReduction(bool result, AbstractModel instance, CardModel card,
        decimal originalCost, decimal modifiedCost)
    {
        if (!result || originalCost <= modifiedCost) return;
        var source = ContributionMap.Instance.GetPowerSource(instance.Id.Entry);
        if (source != null)
            ContributionMap.Instance.TagCostReductionSource(
                card.GetHashCode(), source.SourceId, source.SourceType);
    }

    // Generic helper for relic-based cost reduction tagging
    private static void TagRelicCostReduction(bool result, AbstractModel instance, CardModel card,
        decimal originalCost, decimal modifiedCost)
    {
        if (!result || originalCost <= modifiedCost) return;
        ContributionMap.Instance.TagCostReductionSource(
            card.GetHashCode(), instance.Id.Entry, "relic");
    }

    // VoidFormPower (replaces existing patch — now only tags, doesn't record amount)
    [HarmonyPatch(typeof(VoidFormPower), nameof(VoidFormPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterVoidFormEnergy(bool __result, VoidFormPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // FreeAttackPower
    [HarmonyPatch(typeof(FreeAttackPower), nameof(FreeAttackPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterFreeAttackEnergy(bool __result, FreeAttackPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // FreeSkillPower
    [HarmonyPatch(typeof(FreeSkillPower), nameof(FreeSkillPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterFreeSkillEnergy(bool __result, FreeSkillPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // FreePowerPower
    [HarmonyPatch(typeof(FreePowerPower), nameof(FreePowerPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterFreePowerEnergy(bool __result, FreePowerPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // CorruptionPower
    [HarmonyPatch(typeof(CorruptionPower), nameof(CorruptionPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterCorruptionEnergy(bool __result, CorruptionPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // VeilpiercerPower
    [HarmonyPatch(typeof(VeilpiercerPower), nameof(VeilpiercerPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterVeilpiercerEnergy(bool __result, VeilpiercerPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // CuriousPower (partial reduction)
    [HarmonyPatch(typeof(CuriousPower), nameof(CuriousPower.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterCuriousEnergy(bool __result, CuriousPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // BrilliantScarf (relic — energy)
    [HarmonyPatch(typeof(BrilliantScarf), nameof(BrilliantScarf.TryModifyEnergyCostInCombat))]
    [HarmonyPostfix]
    public static void AfterBrilliantScarfEnergy(bool __result, BrilliantScarf __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagRelicCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // BrilliantScarf (relic — stars)
    [HarmonyPatch(typeof(BrilliantScarf), nameof(BrilliantScarf.TryModifyStarCost))]
    [HarmonyPostfix]
    public static void AfterBrilliantScarfStar(bool __result, BrilliantScarf __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagRelicCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }

    // VoidFormPower (stars)
    [HarmonyPatch(typeof(VoidFormPower), nameof(VoidFormPower.TryModifyStarCost))]
    [HarmonyPostfix]
    public static void AfterVoidFormStar(bool __result, VoidFormPower __instance,
        CardModel card, decimal originalCost, decimal modifiedCost)
    {
        Safe.Run(() => TagPowerCostReduction(__result, __instance, card, originalCost, modifiedCost));
    }
}
```

### Step 3: Add source tagging patches for local modifier reductions

**File: `CombatHistoryPatch.cs`** — new patch class for local modifiers

These need to determine the ACTIVE source when the cost is being modified. Unlike hook-based patches (which receive the power instance), local modifier calls happen from card/relic/potion code, so we use the active context from CombatTracker.

```csharp
[HarmonyPatch]
public static class LocalCostModifierSourceTagPatch
{
    // Patch CardModel.SetToFreeThisTurn — called by BulletTime, potions, MummifiedHand, JeweledMask
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.SetToFreeThisTurn))]
    [HarmonyPrefix]
    public static void BeforeSetFreeThisTurn(CardModel __instance)
    {
        Safe.Run(() => TagLocalCostReduction(__instance));
    }

    // Patch CardModel.SetToFreeThisCombat — called by Metamorphosis, TouchOfInsanity
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.SetToFreeThisCombat))]
    [HarmonyPrefix]
    public static void BeforeSetFreeThisCombat(CardModel __instance)
    {
        Safe.Run(() => TagLocalCostReduction(__instance));
    }

    // Patch CardEnergyCost.SetThisTurnOrUntilPlayed — called directly by
    // Crossbow, Discovery, MadScience, LiquidMemories, RocketPunch, SneckoOil, Enlightenment
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.SetThisTurnOrUntilPlayed))]
    [HarmonyPrefix]
    public static void BeforeSetThisTurnOrUntilPlayed(CardEnergyCost __instance, int cost)
    {
        Safe.Run(() =>
        {
            // Only tag when cost is being SET TO 0 (free) — partial reductions are
            // legitimate game mechanics (e.g., Enlightenment sets to 1, not 0)
            // Actually, we should tag ALL reductions. The play-time comparison will
            // compute the actual savings. But we need access to the card...
            // CardEnergyCost has a private _card field. Use Traverse or reflection.
            // For now, only proceed if cost == 0 (the most common free-play case).
            if (cost == 0)
            {
                // Access the card via Traverse
                var card = Traverse.Create(__instance).Field("_card").GetValue<CardModel>();
                if (card != null)
                    TagLocalCostReduction(card);
            }
        });
    }

    // Patch CardEnergyCost.SetThisCombat — called by AdaptiveStrike, MomentumStrike,
    // VexingPuzzlebox, ConfusedPower, Slither enchantment
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.SetThisCombat))]
    [HarmonyPrefix]
    public static void BeforeSetThisCombat(CardEnergyCost __instance, int cost)
    {
        Safe.Run(() =>
        {
            if (cost == 0)
            {
                var card = Traverse.Create(__instance).Field("_card").GetValue<CardModel>();
                if (card != null)
                    TagLocalCostReduction(card);
            }
        });
    }

    private static void TagLocalCostReduction(CardModel card)
    {
        int baseCost = card.EnergyCost.Canonical;
        if (baseCost <= 0) return; // already free by default

        // Determine source from active context
        var tracker = CombatTracker.Instance;
        if (tracker == null) return;

        string sourceId;
        string sourceType;

        if (tracker.ActiveCardId != null)
        {
            sourceId = tracker.ActiveCardId;
            sourceType = "card";
        }
        else if (tracker.ActivePotionId != null)
        {
            sourceId = tracker.ActivePotionId;
            sourceType = "potion";
        }
        else if (tracker.ActiveRelicId != null)
        {
            sourceId = tracker.ActiveRelicId;
            sourceType = "relic";
        }
        else
        {
            return; // no context — can't attribute
        }

        // Check exception rule: if source also generated this card, mark as generated-and-free
        var origin = ContributionMap.Instance.GetCardOrigin(card.GetHashCode());
        if (origin != null && origin.Value.originId == sourceId)
        {
            ContributionMap.Instance.MarkCardAsGeneratedAndFree(card.GetHashCode());
            return; // don't tag — savings will be skipped at play time
        }

        ContributionMap.Instance.TagCostReductionSource(
            card.GetHashCode(), sourceId, sourceType);
    }
}
```

**Note on self-reducing cards** (RocketPunch, AdaptiveStrike, MomentumStrike): These cards call `EnergyCost.SetThisTurnOrUntilPlayed(0)` or `SetThisCombat(0)` on THEMSELVES during their own `OnPlay`. At that point, `tracker.ActiveCardId` equals the card's own ID. The savings would be attributed to the card itself. This is correct — the card's own effect is making it cheaper for future plays.

### Step 4: Centralized play-time cost comparison patch

**File: `CombatHistoryPatch.cs`** — new patch class

This is the core of the new approach. Patch `CardModel.SpendResources` to capture the canonical cost vs actual cost spent.

```csharp
[HarmonyPatch]
public static class PlayTimeCostSavingsPatch
{
    // Prefix: capture canonical costs before spending
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
    [HarmonyPrefix]
    public static void BeforeSpendResources(CardModel __instance,
        out (int canonicalEnergy, int canonicalStar, int cardHash, bool isX) __state)
    {
        __state = (0, 0, 0, false);
        Safe.Run(() =>
        {
            __state = (
                __instance.EnergyCost.Canonical,
                __instance.BaseStarCost,   // canonical star cost
                __instance.GetHashCode(),
                __instance.EnergyCost.CostsX
            );
        });
    }

    // Postfix: compare canonical vs actual, attribute savings
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
    [HarmonyPostfix]
    public static void AfterSpendResources(CardModel __instance,
        Task<(int, int)> __result,
        (int canonicalEnergy, int canonicalStar, int cardHash, bool isX) __state)
    {
        // SpendResources is async — but it completes synchronously before the first
        // actual await (SpendEnergy/SpendStars are synchronous in practice).
        // However, to be safe, use ContinueWith.
        Safe.Run(() =>
        {
            if (__state.isX) return; // Skip X-cost cards (Review Issue #6)

            __result.ContinueWith(task =>
            {
                Safe.Run(() =>
                {
                    if (task.IsFaulted) return;
                    var (energySpent, starsSpent) = task.Result;

                    int energySaved = __state.canonicalEnergy - energySpent;
                    int starsSaved = __state.canonicalStar - starsSpent;

                    if (energySaved <= 0 && starsSaved <= 0) return;

                    // Check exception rule
                    if (ContributionMap.Instance.IsCardGeneratedAndFree(__state.cardHash))
                    {
                        ContributionMap.Instance.ClearCostReductionSourceTag(__state.cardHash);
                        return;
                    }

                    // Look up source tag
                    var tag = ContributionMap.Instance.GetCostReductionSourceTag(__state.cardHash);
                    if (tag == null) return; // no source recorded — can't attribute

                    var (sourceId, sourceType) = tag.Value;

                    // Attribute savings
                    var tracker = CombatTracker.Instance;
                    if (tracker == null) return;

                    if (energySaved > 0)
                        tracker.GetOrCreate(sourceId, sourceType).EnergyGained += energySaved;
                    if (starsSaved > 0)
                        tracker.GetOrCreate(sourceId, sourceType).StarsContribution += starsSaved;

                    // Clear tag after consumption
                    ContributionMap.Instance.ClearCostReductionSourceTag(__state.cardHash);
                });
            });
        });
    }
}
```

**Important**: The `__result` is a `Task<(int,int)>`. Since `SpendResources` is async, the Harmony Postfix fires at the first `await`. However, `SpendEnergy` and `SpendStars` internally do synchronous work before their awaits. The actual energy/star values are computed BEFORE the first await in `SpendResources`:
```csharp
int energyToSpend = EnergyCost.GetAmountToSpend();  // sync
int starsToSpend = ...;                               // sync
await SpendEnergy(energyToSpend);                     // first await
```

So the Postfix fires AFTER `energyToSpend` and `starsToSpend` are computed but potentially before `SpendEnergy` completes. The `__result` task may not be completed yet. Using `ContinueWith` handles this correctly.

**Alternative (simpler)**: Instead of patching `SpendResources`, we can compute savings in the existing `OnCardPlayStarted` method. At that point, the card has already spent resources. We have access to `ResourceInfo.EnergySpent` via the `CardPlay` object in `CombatHistory.CardPlayStarted`.

**Revised approach**: Patch `CombatHistory.CardPlayStarted` postfix. The `CardPlay` parameter contains `Resources.EnergySpent` and `Resources.StarsSpent`. Compare against `card.EnergyCost.Canonical`.

```csharp
// Simpler alternative — inside existing CardPlayStarted postfix:
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayStarted))]
[HarmonyPostfix]
public static void AfterCardPlayStarted(CombatState combatState, CardPlay cardPlay)
{
    Safe.Run(() =>
    {
        var card = cardPlay.Card;
        int cardHash = card.GetHashCode();

        // Skip X-cost cards
        if (card.EnergyCost.CostsX) return;

        // Skip auto-played cards (energy not actually spent)
        if (cardPlay.IsAutoPlay) return;

        int energySaved = card.EnergyCost.Canonical - cardPlay.Resources.EnergySpent;
        int starsSaved = card.BaseStarCost - cardPlay.Resources.StarsSpent;

        if (energySaved <= 0 && starsSaved <= 0) return;

        // Exception rule: generated-and-freed cards
        if (ContributionMap.Instance.IsCardGeneratedAndFree(cardHash)) return;

        // Look up source tag
        var tag = ContributionMap.Instance.GetCostReductionSourceTag(cardHash);
        if (tag == null) return;

        var (sourceId, sourceType) = tag.Value;
        var tracker = CombatTracker.Instance;
        if (tracker == null) return;

        if (energySaved > 0)
            tracker.GetOrCreate(sourceId, sourceType).EnergyGained += energySaved;
        if (starsSaved > 0)
            tracker.GetOrCreate(sourceId, sourceType).StarsContribution += starsSaved;

        ContributionMap.Instance.ClearCostReductionSourceTag(cardHash);
    });
}
```

**USE THIS SIMPLER APPROACH.** It avoids async complications and uses data that's already available.

### Step 5: Remove old VoidForm consumption patches

**File: `CombatHistoryPatch.cs`**

- Remove `AfterVoidFormCardPlayed` patch (consumption via `AfterCardPlayed`)
- Remove old `RecordPendingCostSavings` / `ConsumePendingCostSavings` calls from VoidForm patches
- Replace VoidForm `TryModifyEnergyCostInCombat` patch with the source-tagging-only version (Step 2)

### Step 6: Expose necessary accessors

**File: `CombatTracker.cs`**

Add public read accessors:
```csharp
public string? ActivePotionId => _activePotionId;
public string? ActiveRelicId => _activeRelicId;
```

### Step 7: Implement NEW-3 (Max HP Gain as Healing) with context flag

**File: `CombatHistoryPatch.cs`** — new patch class

Uses a **context flag** instead of amount-based suppression (fixes Review Issues #4 and #5).

```csharp
[HarmonyPatch]
public static class MaxHpGainPatch
{
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp),
        new Type[] { typeof(Creature), typeof(decimal) })]
    [HarmonyPrefix]
    public static void BeforeGainMaxHp(Creature creature, decimal amount,
        out int __state)
    {
        __state = 0;
        try
        {
            if (creature != null && creature.IsPlayer && amount > 0m)
            {
                __state = creature.MaxHp;

                // Set context flag BEFORE GainMaxHp calls Heal internally
                // This flag tells HealingPatch to skip the internal heal
                ContributionMap.Instance.SetGainMaxHpFlag(true);
            }
        }
        catch { /* safe */ }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp),
        new Type[] { typeof(Creature), typeof(decimal) })]
    [HarmonyPostfix]
    public static void AfterGainMaxHp(Creature creature, int __state)
    {
        Safe.Run(() =>
        {
            // Always clear the flag, even if we don't process
            ContributionMap.Instance.SetGainMaxHpFlag(false);

            if (creature == null || !creature.IsPlayer) return;
            if (__state == 0) return; // Prefix didn't run

            int actualGain = creature.MaxHp - __state;
            if (actualGain <= 0) return;

            // Determine source context
            string? fallbackId = null;
            string? fallbackType = null;
            var room = creature.Player?.RunState?.CurrentRoom;
            if (room is EventRoom eventRoom)
            {
                var eventId = eventRoom.Event?.Id.Entry;
                fallbackId = !string.IsNullOrEmpty(eventId) ? eventId : "EVENT_UNKNOWN";
                fallbackType = "event";
            }
            else if (room is RestSiteRoom)
            {
                fallbackId = "REST_SITE_COOK";
                fallbackType = "rest_site";
            }

            CombatTracker.Instance.OnHealingReceived(actualGain, fallbackId, fallbackType);
        });
    }
}
```

**IMPORTANT async timing note** (Review Issue #4): `GainMaxHp` is async. The Harmony Postfix on an async method fires at the first `await` point, which is `await SetMaxHp(...)`. If `SetMaxHp` completes synchronously (it often does — just sets a value and potentially kills a creature), the Postfix fires after `SetMaxHp` but BEFORE `Heal` is called on line 599 of CreatureCmd.cs:

```csharp
public static async Task GainMaxHp(Creature creature, decimal amount)
{
    decimal num = await SetMaxHp(creature, (decimal)creature.MaxHp + amount); // ← first await
    // Postfix fires HERE (after first await resumes)
    ...
    await Heal(creature, num); // ← Heal called AFTER Postfix
}
```

Wait — this is actually a problem. The Postfix fires after the first `await` resumes, which means it fires AFTER `SetMaxHp` returns. Then `Heal` is called AFTER the Postfix. So the Postfix correctly captures `creature.MaxHp` after the gain, and the flag is set during Prefix (before everything) and cleared during Postfix (after SetMaxHp but before Heal).

**Correction**: The flag needs to stay set THROUGH the Heal call. The Postfix clears it too early. 

**Fix**: Don't clear the flag in the Postfix. Instead, clear it in the HealingPatch itself (after it sees the flag and skips). Use a more robust pattern:

```csharp
// Revised Prefix — set flag before GainMaxHp executes
[HarmonyPrefix]
public static void BeforeGainMaxHp(Creature creature, decimal amount, out int __state)
{
    __state = 0;
    try
    {
        if (creature != null && creature.IsPlayer && amount > 0m)
        {
            __state = creature.MaxHp;
            ContributionMap.Instance.SetGainMaxHpFlag(true);
        }
    }
    catch { /* safe */ }
}

// Revised Postfix — capture MaxHp gain, DO NOT clear flag
// The flag will be cleared by HealingPatch when it encounters the internal Heal
[HarmonyPostfix]
public static void AfterGainMaxHp(Creature creature, int __state)
{
    Safe.Run(() =>
    {
        if (creature == null || !creature.IsPlayer || __state == 0) return;

        int actualGain = creature.MaxHp - __state;
        if (actualGain <= 0)
        {
            ContributionMap.Instance.SetGainMaxHpFlag(false); // no gain, clear flag
            return;
        }

        // Store the expected gain amount so HealingPatch knows this is the GainMaxHp heal
        ContributionMap.Instance.SetPendingMaxHpGainAmount(actualGain);

        // Determine source and record healing
        string? fallbackId = null;
        string? fallbackType = null;
        var room = creature.Player?.RunState?.CurrentRoom;
        if (room is EventRoom eventRoom)
        {
            var eventId = eventRoom.Event?.Id.Entry;
            fallbackId = !string.IsNullOrEmpty(eventId) ? eventId : "EVENT_UNKNOWN";
            fallbackType = "event";
        }
        else if (room is RestSiteRoom)
        {
            fallbackId = "REST_SITE_COOK";
            fallbackType = "rest_site";
        }

        CombatTracker.Instance.OnHealingReceived(actualGain, fallbackId, fallbackType);
    });
}
```

**File: `ContributionMap.cs`** — flag infrastructure

```csharp
private bool _isFromGainMaxHp;
private int _pendingMaxHpGainAmount;

public void SetGainMaxHpFlag(bool value)
{
    _isFromGainMaxHp = value;
    if (!value) _pendingMaxHpGainAmount = 0;
}

public void SetPendingMaxHpGainAmount(int amount)
{
    _pendingMaxHpGainAmount = amount;
}

/// <summary>
/// Returns true if the current Heal call is from GainMaxHp (should be suppressed).
/// Clears the flag after checking.
/// </summary>
public bool CheckAndClearGainMaxHpFlag()
{
    if (_isFromGainMaxHp)
    {
        _isFromGainMaxHp = false;
        _pendingMaxHpGainAmount = 0;
        return true;
    }
    return false;
}
```

**File: `CombatHistoryPatch.cs`** — modify `HealingPatch.AfterHeal`

Add at the start of the `Safe.Run` lambda:

```csharp
// Check if this heal is from GainMaxHp — suppress to avoid double-counting
// The MaxHpGainPatch already recorded the healing; this internal Heal call
// is just the game healing to fill the new max HP.
if (ContributionMap.Instance.CheckAndClearGainMaxHpFlag())
    return;
```

**This approach is robust** because:
- The flag is set in Prefix (before GainMaxHp runs)
- GainMaxHp calls SetMaxHp (may await), then calls Heal
- Heal fires our HealingPatch postfix, which sees the flag, clears it, and skips
- Even if `Hook.ModifyHealAmount` changes the actual healed amount, we don't care — we suppress by flag, not amount
- If GainMaxHp somehow doesn't call Heal (e.g., error), the flag stays set but will be cleared on next CheckAndClearGainMaxHpFlag call or at combat end via `Clear()`

### Step 8: Relic AfterObtained patches for Max HP source attribution

**File: `CombatHistoryPatch.cs`**

For relics that call GainMaxHp in `AfterObtained` (outside combat, no active context):

```csharp
[HarmonyPatch]
public static class RelicMaxHpSourcePatch
{
    // List of all fruit/max-HP relics
    private static readonly Type[] MaxHpRelicTypes = new[]
    {
        typeof(Strawberry), typeof(Pear), typeof(Mango), typeof(DragonFruit),
        typeof(BigMushroom), typeof(ChosenCheese), typeof(DarkstonePeriapt),
        typeof(FakeMango), typeof(LeesWaffle), typeof(LoomingFruit),
        typeof(NutritiousOyster), typeof(StoneHumidifier)
    };

    // Use a single Prefix/Postfix pattern for each relic.
    // Since we can't use a generic patch for all at once easily,
    // set the relic context before AfterObtained executes.

    // Helper: call from each relic's AfterObtained Prefix
    private static void SetRelicContext(AbstractModel relic)
    {
        ContributionMap.Instance.SetPendingMaxHpSource(relic.Id.Entry, "relic");
    }

    [HarmonyPatch(typeof(Strawberry), nameof(Strawberry.AfterObtained))]
    [HarmonyPrefix]
    public static void BeforeStrawberry(Strawberry __instance) =>
        Safe.Run(() => SetRelicContext(__instance));

    [HarmonyPatch(typeof(Pear), nameof(Pear.AfterObtained))]
    [HarmonyPrefix]
    public static void BeforePear(Pear __instance) =>
        Safe.Run(() => SetRelicContext(__instance));

    // ... (same pattern for all 12 relics)
    // In implementation, generate all 12 prefix patches.
}
```

Update `MaxHpGainPatch.AfterGainMaxHp` to also check `_pendingMaxHpSource`:
```csharp
// In source resolution, after checking active card/potion/relic context:
var pendingSource = ContributionMap.Instance.ConsumePendingMaxHpSource();
if (pendingSource != null)
{
    fallbackId = pendingSource.Value.sourceId;
    fallbackType = pendingSource.Value.sourceType;
}
```

### Step 9: Clear stale data at turn end / combat end

**File: `ContributionMap.cs`** — update `Clear()` method

```csharp
public void Clear()
{
    // ... existing clears ...
    _costReductionSourceTag.Clear();
    _generatedAndFreedCards.Clear();
    _isFromGainMaxHp = false;
    _pendingMaxHpGainAmount = 0;
    _pendingMaxHpSource = null;
}
```

At turn end, clear turn-scoped data:
```csharp
public void OnTurnEnd()
{
    _generatedAndFreedCards.Clear();
    // Note: _costReductionSourceTag entries persist across turns
    // (for SetToFreeThisCombat cards that remain free). They're
    // consumed when the card is played, or cleared at combat end.
}
```

---

## 4. The Exception Rule (Detailed)

**Rule**: If a source BOTH generated a card AND made it free this turn, do NOT count energy/star savings as EnergyGained/StarsContribution. The contribution is already tracked via the sub-bar system.

### Detection Mechanism

1. When a card is generated (e.g., by AttackPotion), `RecordCardOrigin(cardHash, originId, originType)` is already called by existing patches.

2. When `SetToFreeThisTurn` / `SetToFreeThisCombat` / `SetThisTurnOrUntilPlayed(0)` is called on that card (same source context), the `LocalCostModifierSourceTagPatch` checks:
   - Does `_cardOriginMap` have an entry for this card hash?
   - Is the origin's sourceId the same as the current active source?
   - If YES to both → call `MarkCardAsGeneratedAndFree(cardHash)` and skip tagging.

3. At card play time, `IsCardGeneratedAndFree(cardHash)` prevents attribution.

### Sources that trigger the exception

| Source | Generates Card | Makes Free | Exception Applies? |
|--------|---------------|------------|-------------------|
| AttackPotion | Yes | Yes | **YES** |
| SkillPotion | Yes | Yes | **YES** |
| PowerPotion | Yes | Yes | **YES** |
| ColorlessPotion | Yes | Yes | **YES** |
| OrobicAcid | Yes | Yes | **YES** |
| InfernalBlade | Yes | Yes | **YES** |
| Distraction | Yes | Yes | **YES** |
| WhiteNoise | Yes | Yes | **YES** |
| Splash | Yes | Yes | **YES** |
| BulletTime | No (frees existing) | Yes | **NO** — record savings |
| MummifiedHand | No (frees random) | Yes | **NO** — record savings |
| JeweledMask | No (frees generated from OTHER source) | Yes | **NO** — JeweledMask gets credit |

---

## 5. Edge Cases

### 5a. Multiple free-play powers active simultaneously

Example: Player has both `FreeAttackPower` and `VoidFormPower`, plays an Attack costing 2.

Game iterates hook listeners. Suppose FreeAttackPower fires first:
1. FreeAttackPower: 2 → 0, tags card with FreeAttackPower source
2. VoidFormPower: 0 → 0, `originalCost <= modifiedCost` so no tag override

At play time: canonical = 2, spent = 0, savings = 2. Tagged source = FreeAttackPower. Correct.

If VoidFormPower fires first:
1. VoidFormPower: 2 → 0, tags card with VoidFormPower source
2. FreeAttackPower: 0 → 0, no tag

Savings attributed to VoidFormPower. Also correct (first reducer gets credit).

### 5b. Local modifier + hook-based power on same card

BulletTime makes card free (local modifier), then FreeAttackPower would also make it free (hook).

`GetWithModifiers(CostModifiers.All)` applies local first, then hooks. With local already at 0, hooks see `originalCost = 0`. No hook tags the card. BulletTime's local modifier tag (from Step 3) remains. Play-time comparison: canonical (e.g., 2) - spent (0) = 2 saved, attributed to BulletTime. Correct.

### 5c. X-cost cards made free

Skip entirely. `EnergyCost.CostsX == true` → no savings computation. (Review Issue #6)

### 5d. Card set free but never played

The source tag sits in `_costReductionSourceTag` but is never consumed. It's cleaned up at combat end via `Clear()`. No stale pending amounts accumulate (unlike v1 approach).

### 5e. TryModifyEnergyCostInCombat fires for display

Source tags are overwritten each time — last reducer wins. This is harmless because:
- Tags are just a dictionary write (cheap)
- The same power that would reduce cost for display also reduces for play
- No pending amounts accumulate (fixed from v1)

### 5f. GainMaxHp during combat (Feed card)

Feed calls `CreatureCmd.GainMaxHp` during combat. Prefix sets flag and captures MaxHp. Postfix records healing via `OnHealingReceived` with active card context = "FEED". Internal `Heal` call is suppressed by flag. Correct.

### 5g. GainMaxHp from relic on pickup (Strawberry)

Relic's `AfterObtained` Prefix sets `_pendingMaxHpSource`. GainMaxHp Prefix sets flag. Postfix records healing with relic as fallback source. Internal Heal suppressed. Correct.

### 5h. ModifyHealAmount changes heal amount (Review Issue #5)

Doesn't matter — we suppress by flag, not amount. The flag is set before GainMaxHp runs and cleared when HealingPatch encounters the internal Heal call. The actual healed amount is irrelevant to suppression.

### 5i. GainMaxHp async Postfix timing (Review Issue #4)

The Postfix fires after `await SetMaxHp(...)` resumes, before `Heal` is called. This is CORRECT for our use case:
- We need MaxHp to be updated (to compute `actualGain`) — SetMaxHp is done
- We need the flag to still be set when Heal fires — yes, Postfix records healing but does NOT clear the flag; HealingPatch clears it

### 5j. Self-reducing cards (RocketPunch, AdaptiveStrike, MomentumStrike)

These cards reduce their own cost during play. At the time of the CURRENT play, the cost was whatever it was. The savings apply to the NEXT play of the card. The source tag is set with `sourceId = card's own ID`. On the next play, savings are attributed to the card itself. This is semantically correct — the card's own effect reduced its future cost.

---

## 6. Dependencies and Implementation Order

```
Step 1  → ContributionMap: source tag tracking infrastructure
Step 6  → CombatTracker: expose ActivePotionId, ActiveRelicId
   ↓
Step 2  → CombatHistoryPatch: hook-based source tag patches
Step 3  → CombatHistoryPatch: local modifier source tag patches
Step 4  → CombatHistoryPatch: play-time cost savings in CardPlayStarted postfix
Step 5  → Remove old VoidForm consumption patches
   ↓
Step 7  → ContributionMap + CombatHistoryPatch: GainMaxHp with context flag
Step 8  → CombatHistoryPatch: relic AfterObtained patches
Step 9  → Clear stale data
```

Recommended implementation order:
1. Steps 1, 6 (infrastructure)
2. Steps 2, 3, 4, 5 (NEW-1 + NEW-2 — centralized cost savings)
3. Steps 7, 8, 9 (NEW-3 — max HP as healing)

---

## 7. Affected Files

| File | Changes |
|------|---------|
| `src/Collection/ContributionMap.cs` | Replace `_pendingCostSaving` with `_costReductionSourceTag` dictionary; keep `_generatedAndFreedCards`; add GainMaxHp context flag (`_isFromGainMaxHp`, `CheckAndClearGainMaxHpFlag`); add `_pendingMaxHpSource`; update `Clear()` and `OnTurnEnd()` |
| `src/Collection/CombatTracker.cs` | Expose `ActivePotionId`, `ActiveRelicId`; remove old `ConsumePendingCostSavings`; cost savings now attributed directly in CardPlayStarted postfix |
| `src/Patches/CombatHistoryPatch.cs` | Replace `CostSavingsPatch` with `CostReductionSourceTagPatch` (source-tag-only patches for 8 powers + 2 relics); add `LocalCostModifierSourceTagPatch` (patches for SetToFreeThisTurn, SetToFreeThisCombat, SetThisTurnOrUntilPlayed, SetThisCombat); add play-time savings computation in `CardPlayStarted` postfix; add `MaxHpGainPatch` with context flag; add `RelicMaxHpSourcePatch` for 12 fruit relics; modify `HealingPatch` to check GainMaxHp flag; remove VoidForm AfterCardPlayed consumption |

### New using directives needed in CombatHistoryPatch.cs

```csharp
using MegaCrit.Sts2.Core.Models.Powers;  // FreeAttackPower, FreeSkillPower, FreePowerPower, etc.
using MegaCrit.Sts2.Core.Models.Relics;  // BrilliantScarf, Strawberry, Mango, etc.
using MegaCrit.Sts2.Core.Entities.Cards;  // CardEnergyCost
```

---

## 8. Complete Lists

### Source tag patches for TryModifyEnergyCostInCombat (NEW-1):

| Type | Class | Effect | Tag records |
|------|-------|--------|-------------|
| Power | `VoidFormPower` | Existing (rewritten to tag-only) | source tag |
| Power | `FreeAttackPower` | Next Attack free | source tag |
| Power | `FreeSkillPower` | Next Skill free | source tag |
| Power | `FreePowerPower` | Next Power free | source tag |
| Power | `CorruptionPower` | All Skills free | source tag |
| Power | `VeilpiercerPower` | Ethereal cards free | source tag |
| Power | `CuriousPower` | Power cost reduced | source tag |
| Relic | `BrilliantScarf` | Nth card free | source tag |

### Source tag patches for TryModifyStarCost (NEW-2):

| Type | Class | Effect | Tag records |
|------|-------|--------|-------------|
| Power | `VoidFormPower` | Already patched | source tag |
| Relic | `BrilliantScarf` | Nth card star-free | source tag |

### Local modifier source tag patches:

| Method patched | Callers caught |
|---------------|----------------|
| `CardModel.SetToFreeThisTurn` | BulletTime, AttackPotion, SkillPotion, PowerPotion, ColorlessPotion, OrobicAcid, InfernalBlade, Distraction, WhiteNoise, Splash, MummifiedHand, JeweledMask |
| `CardModel.SetToFreeThisCombat` | Metamorphosis, TouchOfInsanity |
| `CardEnergyCost.SetThisTurnOrUntilPlayed` (cost=0) | Crossbow, Discovery, MadScience, LiquidMemories, RocketPunch, SneckoOil (random cost, may not be 0) |
| `CardEnergyCost.SetThisCombat` (cost=0) | AdaptiveStrike, MomentumStrike, VexingPuzzlebox |

### GainMaxHp patches (NEW-3):

| Type | Class | Context |
|------|-------|---------|
| Card | `Feed` | In combat — active card context |
| Potion | `FruitJuice` | Any time — active potion context |
| Event | (6 events) | Out of combat — event room fallback |
| Relic | (12 relics) | On pickup — needs AfterObtained prefix |
| Rest Site | `CookRestSiteOption` | Rest site — room fallback |

---

## 9. Testing Checklist

- [ ] Play an Attack card with FreeAttackPower active → energy saved attributed to Unrelenting
- [ ] Play a Skill with CorruptionPower active → energy saved attributed to Corruption source
- [ ] Use BulletTime → all hand cards played for free → energy attributed to BulletTime
- [ ] Use AttackPotion → generated card played for free → NO energy attribution (exception rule)
- [ ] Use InfernalBlade → generated Attack played for free → NO energy attribution (exception rule)
- [ ] MummifiedHand triggers on random card → energy attributed to MummifiedHand
- [ ] BrilliantScarf Nth card → energy AND stars attributed to BrilliantScarf
- [ ] VoidForm still works as before (energy + stars)
- [ ] Multiple free-play powers active → only first-in-chain gets credit
- [ ] Crossbow sets card free via SetThisTurnOrUntilPlayed(0) → energy attributed to Crossbow
- [ ] Discovery generates free card via SetThisTurnOrUntilPlayed(0) → exception rule applies (generated + free)
- [ ] Metamorphosis uses SetToFreeThisCombat → energy attributed to Metamorphosis
- [ ] TouchOfInsanity uses SetToFreeThisCombat → energy attributed to TouchOfInsanity
- [ ] X-cost card made free → NO energy attribution (skipped)
- [ ] TryModifyEnergyCostInCombat fires during hover → no stale amounts accumulate
- [ ] Feed kills enemy → max HP gain counted as HpHealed for Feed
- [ ] FruitJuice used → max HP gain counted as HpHealed for FruitJuice
- [ ] Strawberry obtained → max HP gain counted as HpHealed for Strawberry
- [ ] AbyssalBaths event → max HP gain counted as HpHealed for ABYSSAL_BATHS
- [ ] GainMaxHp internal Heal call → NOT double-counted (flag-based suppression)
- [ ] ModifyHealAmount changes heal amount → suppression still works (flag-based)
- [ ] CookRestSiteOption → max HP gain attributed to REST_SITE_COOK

---

## Known Limitations (Post-Implementation)

**实现于 2026-04-08，以下为已知限制，后续迭代修复：**

### 多乘法修正器拆分精度
多个乘法修正器同时生效时（如 Vulnerable 1.5x + DoubleDamage 2x），各修正器的贡献用独立公式 `finalDmg - finalDmg/multiplier` 计算，总和可能超过实际 bonus。当前用 `DirectDamage + ModifierDamage = TotalDamage` 约束保证数据不丢失，但 DirectDamage 可能小于基础伤害。

### 未覆盖的免费能量/辉星场景
- ConfusedPower / SneckoEye 随机改费（可增也可减，设计决策待定）
- Enlightenment 部分降费（降到 1 而非 0，当前 SetThisTurnOrUntilPlayed patch 只在 cost=0 时触发）
- BulletTime 之外的局内永久免费（SetToFreeThisCombat callers 的 source tag 依赖 active context）

### 测试框架线程限制
- 测试在 `Task.Run` 后台线程运行，PlayCard 可能触发 Godot UI 节点操作导致间歇性崩溃
- 使用 `CreateCardInHand` + `skipVisuals: true` 缓解，但非 100% 安全
- 防御测试依赖 `EndTurnAndWaitForPlayerTurn`（1.5s settle），敌人行为不可控导致偶发失败
- 跨角色卡牌（带辉星花费 UI 等）在非对应角色上 PlayCard 可能 NullRef

### Osty 测试
O1-O5（Osty LIFO 栈、攻击归因、死亡延迟负防御）需要亡灵契约师角色，未纳入自动化测试。需手动测试验证。
