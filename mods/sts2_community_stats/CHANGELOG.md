# Changelog

## v0.12.1 (2026-04-15) — 贡献归因四连修（Round 14 v6）

> 在 v0.12.0 master 基础上对 contribution test 套件暴露的 4 类归因 bug 做定点修复。
> 设计文档：[DesignDoc/FIX_INTENT_upgrade_origin.md](DesignDoc/FIX_INTENT_upgrade_origin.md)

### Bug 修复

- **Fix A — 脆弱（Frail）产生伪正防御贡献**  
  `BlockModifierPatch.AfterModifyBlock` 之前读 `Math.Abs(power.Amount)` 作为 modifier 贡献值，而 Frail 的 `Amount` 其实是回合计数器（例：2 turns），被错误地当成 +2 block 计入 debuff 的 `ModifierBlock` 桶。现改为完整复刻伤害侧 `AfterModifyDamage` 的三段式：先查 `ModifyBlockAdditive`（正值直接取）→ 否则查 `ModifyBlockMultiplicative`（`< 1m` 直接 skip，这条过滤所有减少玩家格挡的负贡献，例如 Frail 0.75×、NoBlock、Shadowmeld、Unmovable）→ `> 1m` 按 `finalBlock - finalBlock/multiplicative` 折算。文件：[CombatHistoryPatch.cs:1034-1094](src/Patches/CombatHistoryPatch.cs#L1034-L1094)。

- **Fix C — 同名卡 origin 污染（composite bucket key）**  
  `CombatTracker._currentCombat` 之前按扁平 `cardId` 聚合，`TagCardOrigin` 在 bucket 创建**之后**回填 `OriginSourceId`，导致 last-writer-wins：牌库原生 Battle Trance 先打、技能药水生成的 Battle Trance 后打，所有 `BATTLE_TRANCE` 条目被共同的桶吞掉并覆写成 `SKILL_POTION` origin。双生成器 Shiv 场景同理。现新增 `_activeCardOriginAL` AsyncLocal 在 `OnCardPlayStarted` 从 `GetCardOrigin(cardHash)` 一次捕获，`GetOrCreate(sourceId, type, originOverride=null)` 通过 `MakeBucketKey(id, \u0001, origin)` 合成复合 key —— `(BATTLE_TRANCE, null)` 与 `(BATTLE_TRANCE, SKILL_POTION)` 成为两个独立 accum，OriginSourceId 在 bucket 创建时锁死，事后不再回填。`TagCardOrigin` 降为 no-op shim（保留签名以兼容任何外部调用）。文件：[CombatTracker.cs](src/Collection/CombatTracker.cs)。

- **Fix D — Upgrade 增量归因 origin 丢失**  
  `UpgradeDelta` record 之前只记 `SourceId/SourceType`（master Fix 6 已把升级增量归到 upgrader 桶而非被升级卡）。但这步忽略了 upgrader 自己的 origin —— 技能药水生成的 Armaments 升级 Strike 与牌库 Armaments 升级 Strike 会坍缩到同一个 `ARMAMENTS` 桶。现 `UpgradeDelta` 增 `UpgraderOrigin` 字段；`CardUpgradeTrackerPatch.AfterUpgrade` 通过 `tracker.ActiveCardOrigin` 捕获；伤害/块升级分流路径通过 `GetOrCreate(..., originOverride: _pendingUpgradeSourceOrigin)` 路由到 upgrader 的 origin 桶。文件：[ContributionMap.cs:702-707](src/Collection/ContributionMap.cs#L702-L707)、[CombatHistoryPatch.cs:1195-1208](src/Patches/CombatHistoryPatch.cs#L1195-L1208)、[CombatTracker.cs](src/Collection/CombatTracker.cs)。

- **Fix B — Doom 归因错位（per-enemy FIFO 栈 + self-doom 过滤）**  
  `OnDoomKillsCompleted` 之前把**所有**被 Doom 杀掉的敌人 HP 加总，整坨挂到 `GetPowerSource("DOOM_POWER")` 单一源（加 5 级 active-context fallback）。三类问题：(a) 多源 Doom 在同一敌人身上坍缩到一个 source；(b) BorrowedTime / Neurosurge 的 self-doom 经 `_powerSources["DOOM_POWER"]` 覆写，污染后续 enemy-Doom 的 source lookup；(c) 多目标场景下归因完全扁平。现新增 `ContributionMap.DoomLayer(SourceId, SourceType, OriginId, Amount)` record + `_doomStacks : Dictionary<int, List<DoomLayer>>`（per-enemy FIFO 栈）；`OnPowerApplied` 对 `DOOM_POWER` 分支：self-doom `return` 完全忽略，enemy-doom 走 `RecordDoomLayer` 并携带 upgrader origin；`OnDoomKillsCompleted` 按敌人循环调用 `ConsumeDoomLayers(hash, hp)` FIFO-consume 每层 `Math.Min(layer.Amount, remainingHp)` 分配 AttributedDamage 到各自 source 的 origin 桶。层无记录时回退到原 active-context 5 级 fallback。文件：[ContributionMap.cs](src/Collection/ContributionMap.cs)、[CombatTracker.cs](src/Collection/CombatTracker.cs)。

### 数据结构变更

- `ContributionMap.UpgradeDelta` record 加 `UpgraderOrigin` 字段（nullable string，旧存档反序列化到 null 保持兼容）
- `ContributionMap` 新增 `DoomLayer` record + `_doomStacks` dict + `RecordDoomLayer` / `ConsumeDoomLayers` / `ClearDoomStacks` API
- `CombatTracker` 新增 `_activeCardOriginAL` AsyncLocal 字段 + `ActiveCardOrigin` 公共访问器 + `_pendingUpgradeSourceOriginAL` + `MakeBucketKey` / `ResolveOriginFor` helpers
- `CombatTracker.GetOrCreate` 新增 `originOverride` 可选参数
- `TagCardOrigin` 降为 no-op shim

### 兼容性

- 旧存档 plain-key 快照仍可加载：`MakeBucketKey(id, null) == id`，key 形态一致
- `UpgradeDelta` 新字段 nullable，旧序列化数据自动映射到 null origin
- UI `ContributionChart` 遍历 `data.Values` 用 `accum.OriginSourceId` 分类子条，无 API 变更
- 遗物归因路径完全中性：relic sourceId 永不带 origin（`ResolveOriginFor("VAJRA") == null`），桶 key 形态不变

## v0.12.0 (2026-04-12) — 第二轮迭代发布版

### 新功能 — 个人生涯统计

- **百科大全 → 角色数据 新增 tab**：通过 `StatsScreenTabPatch` 在原生统计界面注入"个人生涯统计"标签，包含：
  - **胜率汇总卡片**：4 张 NStatEntry 风格卡片（数据范围 / 进阶 W-L / 进阶胜率 / 最高连胜）+ 4 个滚动窗口（最近 10 / 50 / 100 / 全部）+ 最低进阶 SpinBox 选择器
  - **死因排行**：Top 10 死因（带图标 — 战斗用怪物图标，事件用问号图标，先古之民用对应头像，中途放弃用箭头）
  - **卡组构筑** + **路线统计**：每 Act 平均值表格，含图标行（获取/购买/删除/升级 + 小怪/精英/?房间/商店/火堆）
  - **先古遗物选取率**：按 elder 分组的 dropdown，每个 elder 显示其 3 个池子的遗物选取率/选取次数/胜率/胜率浮动（vs 该角色+进阶下的总胜率）
  - **Boss 战损**：dropdown 列出全部 4 幕的所有 Boss（包括 0 遭遇次数的占位行），含 Underdocks 暗港作为第 1 幕替代图（密林/暗港）
  - 字号、SectionPanel 视觉、行图标全部对齐游戏原生统计界面风格

### 新功能 — 历史记录本局统计弹窗

- **历史记录详情新增"📊 本局统计"按钮**：右上角注入按钮，点击弹出居中模态弹窗
- 弹窗内容（与生涯统计同款 SectionPanel 风格）：
  - 头部：角色中文名 · 进阶 · 楼层 · 胜利/失败
  - **卡组构筑** / **路线统计**（含火堆数）每 Act 表格 + 行图标
  - **先古遗物选取**：行内显示 elder 头像 + 中文名 + 遗物图标 + 遗物名
  - **Boss 战损**：每行 Boss 头像 + 中文名 + HP 损失
  - **查看贡献图表**按钮：从历史记录回看本局战斗贡献（自动关闭弹窗避免 z-order 冲突）
- 弹窗 parent 到 `GetTree().Root`，自适应分辨率（70% × 85% 视口，最小 520×400）
- 关闭路径：✕ 按钮 / 点暗背景 / Esc 键 / SelectPlayer 触发自动清理

### 新功能 — 卡牌图书馆 + 遗物收藏统计注入

- **`CardLibraryPatch` / `RelicLibraryPatch`**：在 Compendium 卡牌/遗物详情页注入个人样本数 + 选取率 + 胜率 + 升级率 + 删除率 + 购买率 + 社区对比
- 数据来源：`RunHistoryAnalyzer` 离线聚合本地 RunHistory 文件（异步加载，磁盘缓存）

### 改进 — 战斗贡献面板

- **宽度 460 → 760**：解决长卡名 + 长进度条横向溢出，柱状图最大宽度 260 → 540
- **`WrapInScroll` 禁用水平滚动**：与 `NewTabScroll` 行为对齐，run 历史回看不再出现底部滚动条
- **打开历史记录的贡献回看时**：先关闭本局统计弹窗，避免 z-order 重叠

### 改进 — 配置 / 筛选

- **F9 面板进阶 SpinBox 上限 10**：游戏最高进阶就是 10，超过会触发后端数据读取异常
- **F9 面板 min/max ascension 联动**：min > max 时自动 clamp 另一边
- **生涯统计 SpinBox 上限同步 10**

### Bug 修复

- **卡组样本数显示 0** — 启动时 `LoadAllAsync` 在 `SaveManager.InitProfileId` 之前触发会拿到空快照，原代码把空快照写进缓存，导致整个 session 卡死。新增 `_lastBuildFailed` 标志，失败的快照不再污染 in-memory 缓存或磁盘
- **`GetCached` 不再回写内存缓存**：之前从磁盘读出的 `CareerStatsData` 会被写进 `_cache`，导致后续 `LoadAllAsync` 命中缓存直接返回，bundles 永不重建
- **CareerStatsCache schema 版本检测**：DTO 新增 `MaxWinStreak` 字段，旧缓存（Wins>0 但 MaxWinStreak==0）会被识别为 stale 并丢弃强制重建
- **OnCareerStatsLoaded 屏幕卡 loading**：`CallDeferred(nameof(Rebuild))` 通过 Godot 反射找不到 private C# 方法，改用 `Callable.From(Rebuild).CallDeferred()`
- **死因图标全部相同** — 原 .tres 路径加载失败，全部 fallback 到 ⚔ Unicode glyph，改为 `ui/run_history/{type}.png` 的已知好路径
- **NEOW 显示怪物图标** — NEOW 实际通过 KilledByEncounter 归类为 Combat，新增 `AncientElderIds` 检测优先用 `ui/run_history/{elder}.png` 的 elder 头像
- **遗物胜率浮动算错** — 原本对比同池平均，应该对比角色+进阶过滤下的总体胜率
- **历史记录注入按钮变成全屏 dim 层** — `_screenContents` 是 MarginContainer，把按钮拉伸到全屏；改为 parent 到 `screen` (NRunHistory 本身)
- **奥洛巴斯池 3 显示英文 ID** — `DISCOVERY_TOTEM_FIRE/WATER/EARTH` 在 relics.json 不存在；按 [Orobas.cs](_decompiled/sts2/MegaCrit.Sts2.Core.Models.Events/Orobas.cs) 实际游戏数据修正为 `TOUCH_OF_OROBAS` + `ARCHAIC_TOOTH`，把 `SEA_GLASS` 合入池 1
- **boss 列表缺 0 遭遇 boss** — 新增 `AllKnownBossEncounterIds()` 枚举 ModelDb 全部 4 幕的 boss，与已有 stats 求并集
- **暗港 boss 错误标为第 4 幕** — Underdocks 在 ActModel 数组里是 index 3 但实际是 Act 1 的替代地图，新增 `BossActFor` 把 Overgrowth/Underdocks 都映射为第 1 幕（密林/暗港 标签区分）
- **路径统计 ? 房间图标不显示** — 路径错为 `unknown_event.png`，正确路径是 `event.png`（详见 ImageHelper.GetRoomIconSuffix）

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
