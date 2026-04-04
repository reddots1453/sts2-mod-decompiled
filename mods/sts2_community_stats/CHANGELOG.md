# Changelog

## v0.11.0 (2026-04-04)

### Improvements

- **Panel moved to left side** of screen to avoid overlapping with game UI
- **Auto-close panel** when player clicks the Proceed button after combat
- **Killing blow damage** now correctly counted — fixed `IsEnding` guard skipping `DamageReceived` for the final hit. Uses `KillingBlowPatcher` (manual Harmony patch on `Hook.AfterDamageGiven`) with identity-based dedup
- **Self-damage cards** (Bloodletting, Offering, etc.) show negative defense contribution in red
- **Poison damage attribution** — `PoisonPower.AfterSideTurnStart` added to PowerHookContextPatcher
- **+27 relic hook patches** — CharonsAshes, ForgottenSoul, FestivePopper, MercuryHourglass, MrStruggles, Metronome, Tingsha, ToughBandages, IntimidatingHelmet, HornCleat, CaptainsWheel, GalacticDust, Candelabra, Chandelier, Lantern, HappyFlower, FakeHappyFlower, GremlinHorn, Pendulum, BlessedAntler, Akabeko, BagOfMarbles, Brimstone, RedMask, SlingOfCourage

### Bug Fixes

- Fixed Harmony parameter name mismatch: compiled DLL uses `results`/`target` not `damageResult`/`originalTarget`

## v0.10.0 (2026-04-04)

### New Features — Contribution Attribution Full Coverage

- **Healing stats (run-level)**: New healing category in run summary showing rest site, relic (BurningBlood etc.), event, max HP gain, and per-floor recovery contributions with source attribution
- **~60 Power hook context patches**: PowerHookContextPatcher manually patches all power hook methods with `SetActivePowerSource`/`ClearActivePowerSource` for complete indirect effect attribution
- **~35 Relic hook context patches**: RelicHookContextPatcher wraps relic hook methods with `SetActiveRelic`/`ClearActiveRelic` for relic-triggered effects
- **Generated card sub-bars**: Shivs, GiantRock, and other generated/transformed cards now display as indented sub-bars under their origin card

### Bug Fixes

- **Potion contribution tracking**: Fixed async bug where `OnUseWrapper` Postfix cleared `_activePotionId` before effects executed. Strength potions, draw potions, block potions etc. now correctly attributed
- **Card removal screen label overlap**: CardRemovalPatch now uses shared `DeckViewPatch.StatsLabelMeta` and `RemoveExistingLabel()`, matching the upgrade screen pattern
- **DynamicVars KeyNotFoundException**: Changed `.Damage`/`.Block` property access to `TryGetValue()` for cards without those vars
- **Event healing distinction**: Each event's healing tracked separately with `[事件]`/`[E]` prefix instead of generic "EVENT"
- **Initial HP filtered**: Skip 0→full HP heal at run start (character creation)
- **Post-combat healing timing**: BurningBlood and similar AfterCombatVictory healing now writes to RunContributionAggregator (fires after OnCombatEnded)
- **Unpatchable methods removed**: ArsenalPower.AfterCardPlayed and StampedePower.BeforeTurnEnd gracefully skipped (abstract/interface methods)

### Architecture

- `HealingPatch`: Prefix/Postfix on `CreatureCmd.Heal` with room-type fallback (RestSiteRoom, EventRoom, MerchantRoom, floor regen)
- `PotionUsedPatch`: ClearActivePotion moved here (sync, fires after OnUse completes)
- `RunContributionAggregator.AddHealing()`: Out-of-combat healing direct write
- `ContributionChart`: Healing bars (green), `isRunLevel` parameter, event prefix display
- `Localization.cs`: 6 new healing/source string keys (EN + CN)

## v1.2.0 (2026-04-03)

### New Features — Contribution Attribution Overhaul

- **Module B: Power indirect effect attribution** — 7 powers (Rage, FlameBarrier, FeelNoPain, Plating, Inferno, Juggernaut, Grapple) now correctly attribute their indirect damage/block back to the card that applied the power, via `PowerHookContextPatch` Prefix/Postfix on each power's hook method
- **Module A+F: Damage/Block modifier attribution** — Strength, Dexterity, Vulnerable, Weak, Colossus, DoubleDamage, PenNib and all other additive/multiplicative modifiers now have their bonuses split out and attributed to their power source. Patches `Hook.ModifyDamage` and `Hook.ModifyBlock` to capture per-modifier contributions
- **Module C: Energy gain tracking** — `PlayerCmd.GainEnergy` is now patched; energy gained from cards like Bloodletting is properly attributed in the Energy Gained section
- **Module E: Sub-bar UI for generated/transformed cards** — Cards created by PrimalForce (GiantRock), Juggling, etc. display as indented sub-bars under their origin card in the contribution chart
- **Module D: Upgrade source tracking** — `CardCmd.Upgrade` Prefix/Postfix captures damage/block deltas; upgrade bonuses shown as orange segments in the damage bar

### Architecture

- `ContributionAccum`: Added `ModifierDamage`, `ModifierBlock`, `UpgradeDamage`, `UpgradeBlock`, `OriginSourceId` fields
- `ContributionMap`: Added `LastDamageModifiers`/`LastBlockModifiers` lists, `_cardOriginMap`, `_upgradeDeltaMap`
- `CombatTracker`: Added `_activePowerSourceId`/`_activePowerSourceType` context with `SetActivePowerSource()`/`ClearActivePowerSource()`, unified `ResolveSource()` fallback chain: cardSource → activeCard → activePotion → activeRelic → activePowerSource
- `ContributionChart`: New multi-segment damage bars (direct/attributed/modifier/upgrade), sub-bar rendering for child cards, purple color for modifier segments, orange for upgrade segments
- `PowerHookContextPatch`: Generic Prefix/Postfix pattern for 7 power types
- `DamageModifierPatch` / `BlockModifierPatch`: Patches Hook.ModifyDamage/ModifyBlock
- `EnergyGainPatch`: Patches PlayerCmd.GainEnergy
- `CardOriginPatch`: Patches CardCmd.Transform
- `CardUpgradeTrackerPatch`: Patches CardCmd.Upgrade Prefix/Postfix

## v1.1.0 (2026-04-03)

### New Features
- **Chinese/English localization**: All UI text supports language switching via settings panel
- **Settings panel (F9)**: Centralized mod settings with hotkey
  - Upload toggle: opt-out of data upload
  - Language switch: Chinese / English
  - Filter settings: ascension, win rate, game version
- **Potion contribution tracking**: Fire Potion, Poison Potion etc. damage/block now attributed in combat chart
- **Card draw section**: Per-source card draw contribution displayed as bar chart (e.g., Battle Trance x1 → 3 cards)
- **Energy gain section**: Per-source energy gain contribution displayed as bar chart (e.g., Bloodletting x1 → 2 energy)

### Bug Fixes
- **Neow event options no longer show "Loading"**: Bundled test data loaded synchronously at mod init, available before any async preload
- **Vulnerable damage attribution fixed**: PowerModel.ApplyInternal Postfix replaces CombatHistory.PowerReceived for debuff source tracking — fixes timing issue where power.Owner was null
- **Relic hover stats now work on in-game relic bar**: Patches both NRelicInventoryHolder (game panel) and NRelicBasicHolder (reward screens)
- **DLL reference paths fixed**: csproj HintPaths corrected for project directory depth

### Architecture
- `Localization.cs`: Simple key-value i18n system with `L.Get("key")` pattern
- `PotionContextPatch`: Wraps PotionModel.OnUseWrapper with Prefix/Postfix to set active potion context
- `PowerApplyPatch`: Patches PowerModel.ApplyInternal (post-Owner-set) for reliable debuff/buff source tracking
- Source types expanded: "card" | "relic" | "potion"
- ContributionChart now has 4 sections: Damage, Defense, Card Draw, Energy Gained

## v1.0.0 (2026-04-01)

- Initial release: card pick rates, event stats, encounter danger overlay, combat contributions, filter panel, shop tracking, card removal/upgrade tracking
