# PLAN-04: Stats the Spire — 第二轮迭代实施细节

> 版本：1.0 | 日期：2026-04-10
> 配套文档：`PRD_04_ITERATION2.md`（需求） / `UI_STYLE_GUIDE.md`（视觉规范）
> 作用：把 PRD-04 里的所有技术实现细节集中到这里。**PRD 不写任何 class / file / Godot binding / Harmony patch / 反射 / 算法**，那些全部住这里。

---

## 0. 文档使用约定

1. **PRD 改了 → 看这里改 plan**。Plan 章节号与 PRD §3.x / §4.x 对齐，互为索引。
2. 任何"为什么这样实现"的事故 / postmortem 都记在对应 plan 章节末尾的 **"事故记录"** 段。
3. 圆 4-9 manual acceptance 的所有 round 反馈：每项 fix 都对应 plan 里某个 "round N" 子段。PRD 只看到最终需求，不看到回合迭代。

---

## 1. 工程基础

### 模组身份

- `manifest.json` 字段：`id="sts2_community_stats"`（不变 — 旧 install 兼容）、`name="Stats the Spire"`
- 程序集名：`sts2_community_stats.dll`
- 部署：build 输出 `Sts2-mod-decompiled/mods/sts2_community_stats/sts2_community_stats.dll`，手动 cp 到 `Slay the Spire 2/mods/sts2_community_stats/`

### 公共依赖

- `0Harmony.dll`（游戏自带，version 跟随 .NET runtime）
- 反射工具：`HarmonyLib.Traverse`（私有字段 + 私有方法访问）
- Godot binding：通过 GodotSharp.dll 的 `Godot.*` 命名空间

### 全局约定

| 约定 | 实现 |
|---|---|
| 异常吞掉 | `Safe.Run(() => ...)` 包裹所有 patch body / 信号回调 |
| Toggle 短路 | 每个 patch 第一行 `if (!ModConfig.Toggles.X) return;` — 零开销禁用 |
| 私有方法 patch | 用字符串形式 `[HarmonyPatch(typeof(X), "MethodName")]`，不要用 `nameof` |
| 私有字段读取 | `Traverse.Create(obj).Field("_x").GetValue<T>()`，包 try/catch + Safe.Warn |
| F1 精度 | 所有百分比 `.ToString("F1")` |
| Idempotent UI 注入 | `HasMeta("sts_xxx")` / `SetMeta` / `RemoveMeta` 防重复 |
| Build 验证 | 必须看 `Build succeeded.` 行 + DLL mtime；**禁止**按 CS0246/CS0103 过滤（mod 源文件中的 CS0246 是真错） |

---

## 2. 文件结构（截至 round 9 round 4）

```
src/
  CommunityStatsMod.cs              入口：[ModInitializer] Initialize() — 装 Harmony patches
  Api/
    StatsProvider.cs                社区数据（mock + 服务端）
    ApiClient.cs                    HTTP wrapper
  Collection/
    CombatTracker.cs                贡献追踪核心；live snapshot + hydrate
    ContributionMap.cs              来源归因 (block pool / power source / etc.)
    ContributionAccum.cs            POCO 累计器
    RunContributionAggregator.cs    跨战斗汇总；HydrateRunTotals
    RunDataCollector.cs             OnMetricsUpload + RunStart 钩子
    RunHistoryAnalyzer.cs           个人生涯数据聚合器（Task.Run + 缓存）
    CareerStatsData.cs              POCO + ElderEntry / ElderOption / ElderRelicStats
    LocalCardStats.cs               §3.2 我的数据 POCO
    LocalRelicStats.cs              §3.3 我的数据 POCO
    LiveContributionSnapshot.cs     §3.6.1 中途存档容器
  Config/
    ModConfig.cs                    路径 / toggles / filter
    FeatureToggles.cs               11 个开关 + 元数据
    FilterSettings.cs               F9 筛选条件 + ResolveCharacter()
    Localization.cs                 EN/CN 字典 + L.Get(key)
  Patches/
    CardLibraryPatch.cs             §3.2 NInspectCardScreen.UpdateCardDisplay postfix
    RelicLibraryPatch.cs            §3.3 NInspectRelicScreen.UpdateRelicDisplay postfix
    RelicCollectionPatch.cs         legacy disabled (round 6)
    RelicHoverPatch.cs              §3.4 全局遗物 tooltip postfix
    EventOptionPatch.cs             §3.5 NEventOptionButton._Ready postfix
    CombatHistoryPatch.cs           §3.6 + §4 各种 Hook patches (~80 patches in one file)
    CombatLifecyclePatch.cs         CombatManager.SetUpCombat / CombatRoom.OnCombatEnded postfix
    RealTimeCombatPatch.cs          empty (round 6 merged into CombatHistoryPatch)
    MapPointPatch.cs                §3.8 + §3.16 NMapPoint.OnFocus / Unfocus
    CombatUiOverlayPatch.cs         §3.9 + §3.17 顶栏 indicator 生命周期
    IntentHoverPatch.cs             §3.10 NCreature.OnFocus / Unfocus + CallDeferred
    CareerStatsPatch.cs             §3.11 NGeneralStatsGrid.LoadStats postfix
    RunHistoryPatch.cs              §3.12 NRunHistory.SelectPlayer postfix
    RunLifecyclePatch.cs            RunManager.SetUpNew/Saved Single/MultiPlayer postfixes
    ShopPatch.cs                    §3.16 NMerchantCard / Relic / Potion patches
    ModSettingsPatch.cs             §3.14 NModdingScreen toggle UI
    DeckViewPatch.cs                运行时牌组浏览统计
    CardRewardScreenPatch.cs        卡牌奖励 stats label
    CardRemovalPatch.cs / CardUpgradePatch.cs  事件商店 stats
  UI/
    ContributionPanel.cs            §4.5 主面板 (TabContainer + 持久 skeletons)
    ContributionChart.cs            柱状图渲染
    InfoModPanel.cs                 InfoMod 风格弹出基类
    DraggablePanel.cs               拖拽 + 位置持久化
    StatsLabel.cs                   通用 stats 标签 factory
    PotionOddsIndicator.cs          §3.9 顶栏药水图标
    CardDropOddsIndicator.cs        §3.17 顶栏卡牌图标
    UnknownRoomPanel.cs             §3.8 ? 房间悬停面板
    ShopPricePanel.cs               §3.16 商店悬停面板
    IntentStateMachinePanel.cs      §3.10 BFS 网格 + ArrowOverlay
    CareerStatsSection.cs           §3.11 注入到 NGeneralStatsGrid
    RunHistoryStatsSection.cs       §3.12 注入到 NRunHistory
    FilterPanel.cs                  F9 设置面板 + 角色下拉 + toggles
  Util/
    Safe.cs                         异常吞 + 日志
    NameLookup.cs                   LocManager 包装 (encounter / boss / elder 名)
    LayoutHelper.cs                 §3.11 §3.12 FindLayoutAncestor + AppendToLayoutAncestor
    ContributionPersistence.cs      §3.6.1 + §3.12 SaveCombat / SaveRunSummary / SaveLiveState
    OfflineQueue.cs                 §4.6 离线上传队列 (10 条 / 7 天)
    AncientPoolMap.cs               §3.11 静态先古池映射 + GetEncounterIcon
    MonsterIntentMetadata.cs        §3.10 状态机缓存
    IntentIconCache.cs              §3.10 round 9 round 4 SpritePath 反射 + (Type, tier) 缓存
    CareerStatsCache.cs             §3.11 个人生涯磁盘缓存
```

---

## 3. 各功能实现

### 3.1 Mod 重命名

`mods/sts2_community_stats/manifest.json`:
- `name` → "Stats the Spire"
- `id` → 保留 "sts2_community_stats"（机器标识）

`Localization.cs` 的 `["contrib.title"]` 也是 "Stats the Spire"。

---

### 3.2 Compendium 卡牌图书馆双源统计

#### Patch 入口
- **`CardLibraryPatch.AfterUpdateCardDisplay`** — postfix on `NInspectCardScreen.UpdateCardDisplay`（**private** 方法，必须用字符串形式 `[HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]`）
- 通过 Traverse 读取私有字段：`_cards` (`List<CardModel>`) + `_index`
- 取出当前 card → `card.Id.Entry`

#### UI 注入
- 在 NInspectCardScreen 上 GetNodeOrNull 一个名为 `StatsTheSpireCardStats` 的 PanelContainer；不存在则新建
- Anchor 1.0/1.0 + Offset Left=-260, Top=-160（屏幕右下角）
- 内部：`GridContainer` Columns=3
- Idempotent：同样的 panel 节点名 + 每次 RefreshContent 清空所有子节点重建

#### 数据源
- **左列（我的）**：`RunHistoryAnalyzer.Instance.LocalCards.Get(cardId)` → `LocalCardStats { TimesPicked, TimesWin, ... }`
- **右列（社区）**：`StatsProvider.Instance.GetCardStats(cardId)` (受 F9 filter 影响)
- 顶部样本数：`mine.TotalRuns` / `community?.SampleSize ?? 0`

#### 事故记录

- **round 5**：原实现 ForCardStats / ForUpgradeRate / ForRemovalRate / ForShopBuyRate 四个 factory method 都包含胜率行 → 同一胜率显示 4 次。改为单一 GridContainer 6 行。
- **round 5 PRD 升级**：从单源单列 → 双源双列。

---

### 3.3 Compendium 遗物收藏双源统计

#### Patch 入口
- **`RelicLibraryPatch.AfterUpdateRelicDisplay`** — postfix on `NInspectRelicScreen.UpdateRelicDisplay`（private string-form patch）
- 通过 Traverse 读 `_relics` + `_index` → `relic.Id.Entry`

#### UI 注入
- 同 §3.2：右下角 PanelContainer + GridContainer Columns=3
- panel name `StatsTheSpireRelicStats`

#### 数据源
- **左列**：`RunHistoryAnalyzer.Instance.LocalRelics.Get(relicId)` → `LocalRelicStats { RunsWith, WinsWith }`
- **右列**：`StatsProvider.Instance.GetRelicStats(relicId)`
- 浮动 = `WinRate - AverageWinRate`（按各自数据源算 average）

#### 事故记录
- **round 6**：原实现保留"选取率"行（无意义 — 遗物没有"被玩家选取"语义）。删除该行。从单源 → 双源。
- **round 6**：legacy `RelicCollectionPatch`（hover focus 注入）禁用，类保留但 attribute 注释掉。

---

### 3.4 全局遗物胜率浮动

- **`RelicHoverPatch`** — postfix on `NRelicBasicHolder.OnFocus / OnUnfocus` 和 `NRelicInventoryHolder.OnFocus / OnUnfocus`
- 注入 `StatsLabel.ForRelicStatsWithDelta(stats, globalAvg)` 到 hover tooltip 节点
- toggle short-circuit `RelicStats`

---

### 3.5 事件选项简化

- **`EventOptionPatch`** — postfix on `NEventOptionButton.OnRelease` + `_Ready`
- 只显示 `StatsLabel.ForEventOption(stats.PickRate)` — 移除 `WinRate` 显示
- 用 L key `stats.event_pick`，F1 精度

---

### 3.6 实时贡献更新

#### Patch 链
```
CombatHistory.CardPlayFinished (game)
  └─ CombatHistoryPatch.AfterCardPlayFinished postfix
       ├─ CombatTracker.OnCardPlayFinished()
       └─ if (Toggles.ContributionPanel) CombatTracker.NotifyCombatDataUpdated()
            ├─ ContributionPersistence.SaveLiveState(BuildLiveSnapshot)  ← §3.6.1
            └─ CombatDataUpdated event
                 ├─ ContributionPanel.OnCombatDataUpdated → CallDeferred(DeferredRefresh)
                 │    └─ DeferredRefresh → RefreshLive → RefreshTabs → ReplaceScrollContent
                 └─ CombatUiOverlayPatch.OnCombatDataUpdated → RefreshValuesFromPlayer
```

#### 关键架构（round 8）

`ContributionPanel` 维护**两个永久 ScrollContainer skeleton**（命名为 `CombatTab` 和 `RunTab`），它们在 panel 创建时一次性 add 到 TabContainer，**永不**在 RefreshTabs 里销毁。

`RefreshTabs` 只调用 `ReplaceScrollContent(scroll, newContent)`，把每个 ScrollContainer 内部的旧 chart 子节点 RemoveChild + QueueFree，AddChild 新 chart。

`tabs.SetCurrentTab(tabs.CurrentTab)` 在 refresh 后被显式重设以恢复用户的 tab 选择（防止 RemoveChild 把 currentTab 重置回 0）。

#### 事故记录

- **round 6**：旧版本 RealTimeCombatPatch 用 500ms Debounce — 实测大量 update 被吞。简化为 `CallDeferred` 让 Godot 自然合并同帧多次刷新；同时 RealTimeCombatPatch 的 hook 合并进 CombatHistoryPatch.AfterCardPlayFinished（避免 Harmony 在同一方法上有两个 postfix 的微妙交互）。
- **round 8**：用户仍然报告"open panel → play card → 不更新，关闭再开才更新"。根因：`RefreshTabs` 的 dispose-and-recreate 路径在 panel 已显示时被 Godot 跳过 layout（新建的 child 没立刻进入 visible flow）。修复：永久 skeleton + ReplaceScrollContent。
- 诊断日志关键字：`[ContribPanel] CombatDataUpdated → refresh`、`[ContribPanel] RefreshTabs: combatEntries=N currentTab=I`。

---

### 3.6.1 战斗贡献中途存档持久化

#### 文件位置
`{ApplicationData}/sts2_community_stats/contributions/{seed}_live.json`

#### 写入路径
- `CombatTracker.NotifyCombatDataUpdated`（每次打牌触发）→ `BuildLiveSnapshot(combatInProgress: true)` → `ContributionPersistence.SaveLiveState`
- `CombatTracker.OnCombatEnd` → `BuildLiveSnapshot(combatInProgress: false)` → `SaveLiveState`

`BuildLiveSnapshot` 字段：`EncounterId, EncounterType, Floor, TurnCount, TotalDamageDealt, DamageTakenByPlayer, CombatInProgress, CurrentCombat, RunTotals`。

#### 读取路径
- `RunLifecyclePatch.OnRunStartSP / MP postfix` 和 `OnRunResumeSP / MP postfix`（round 9）→ `TryHydrateLiveState`
- `GetActiveSeed()` 读 `RunManager.Instance.DebugOnlyGetState().Rng.StringSeed`
- 匹配 seed → `LoadLiveState` → `CombatTracker.HydrateFromLiveSnapshot`

#### 清理路径
- `RunDataCollector.OnMetricsUpload` 调 `ContributionPersistence.DeleteLiveState(seed)`

#### 90 天保留
- `ContributionPersistence.PruneOldFiles` 在 mod init 时跑一次，删 `*_combat_*.json` / `*_summary.json` / `*_live.json` 中 mtime 超过 90 天的文件

---

### 3.7 每回合平均伤害

- `ContributionPanel` 标题行下方添加 `Label _dpsLabel`
- `UpdateDps()` = `CombatTracker.Instance.TotalDamageDealt / Math.Max(1, TurnCount)`
- 在 `RefreshLive` 末尾和 `Toggle` 显示后都调用一次

---

### 3.8 问号房间遭遇概率

#### Patch 入口
- **`MapPointPatch.AfterOnFocus`** — postfix on `NMapPoint.OnFocus`（私有 string-form）
- `mapPoint.Point.PointType == MapPointType.Unknown` 且 `mapPoint.State != Traveled` → 显示

#### 数据源
- `RunManager.Instance.DebugOnlyGetState().Odds.UnknownMapPoint` (`UnknownMapPointOdds`)
- 调用 `Hook.ModifyUnknownMapPointRoomTypes(runState, basis)` 拿到真实 eligible set（处理 Juzu Bracelet / Golden Compass 等会拉黑某些 room type 的遗物）
- 将拉黑的 room type 显示为 0%，剩余概率折算到 Event

#### UI
- `UnknownRoomPanel.Create(odds, runState)` 返回 `InfoModPanel`
- 加到 mapPoint 自身作为子节点 + meta key `sts_hover_info_panel`

---

### 3.9 药水掉落概率

#### Indicator 节点
- **`PotionOddsIndicator : Control`** — 自定义 Control，HBox 布局：图标 56×56 TextureRect + Label 18px
- 图标加载：`Godot.ResourceLoader.Load<Texture2D>(ImageHelper.GetImagePath("atlases/potion_atlas.sprites/distilled_chaos.tres"))`，fallback 到 emoji
- Container CustomMinimumSize = 120×64
- `SizeFlagsVertical = ShrinkCenter` 让它在 host HBox 里垂直居中

#### Parent
- **必须 parent 到 NTopBar 内部 hbox**，而**不是** scene tree root
- 通过 `NRun.Instance?.GlobalUi?.TopBar?.Map?.GetParent()` 拿到 host hbox
- 在 hostHbox.MoveChild 到 mapBtn.GetIndex() 之前的位置（让它出现在 Map 按钮**左侧**）

#### 生命周期 — round 9 round 2 严格规则
- **创建时机**：`RunLifecyclePatch.OnRunStartSP / OnRunStartMP / OnRunResumeSP / OnRunResumeMP` postfix → `CombatUiOverlayPatch.OnRunStarted()` → `EnsureAttachedToTopBar()`
- **fallback**：`CombatUiOverlayPatch.AfterSetUpCombat` postfix 也会调一遍 `EnsureAttachedToTopBar()`，因为 SetUpSavedSinglePlayer postfix 时 NTopBar 可能还没构造好（实测：`SetUpSavedSinglePlayer postfix → 'Continuing run' log` 之间 NTopBar 才被 ready）
- **销毁**：**不需要手动 QueueFree** — Godot 的 parent-child 契约会自动清理。NTopBar 被销毁时它的所有 children（包括我们的 indicator）会自动 QueueFree
- **重新创建**：下次 OnRunStarted 调 `IsAlive(ref _potion)` 检查到 wrapper 已 freed → null → lazy create fresh

#### IsAlive 守卫
```csharp
private static bool IsAlive<T>(ref T? slot) where T : Godot.GodotObject
{
    if (slot == null) return false;
    if (!Godot.GodotObject.IsInstanceValid(slot)) { slot = null; return false; }
    return true;
}
```
**所有访问** `_potion` 的代码点都必须先过 `IsAlive(ref _potion)`。`RefreshValuesFromPlayer` 是唯一会被频繁调用的方法，每条都加 IsAlive。

#### 事故记录

- **round 5**：原实现 parent 到 scene tree root + emoji 占位符 + 较小尺寸 → 视觉与原生顶栏不匹配。重写为 reparent 到 NTopBar.Map.GetParent()。
- **round 9 round 1**：用户报告"打牌没实时更新 + 某次崩溃"。dump 解析：crash address = `SlayTheSpire2.exe + 0xebf76b`，read NULL+0x68，主线程 WndProc 派发深处。根因：static `_potion` / `_cardDrop` 字段在战斗 → 地图过渡时被 Godot 连带回收，但 C# wrapper 还活着，下次访问 → AV。修复：IsAlive 守卫 + 把 lifecycle 跟 NTopBar 绑定。
- **round 9 round 2**：实测仍然崩溃（同一 dump 偏移）。bisect 后确认是 IntentHoverPatch，不是这里。但同时还是清理了这里的 lazy create fallback，让逻辑更安全。
- **round 9 round 3**：图标偏小、与原生不对齐。enlarged 到 56×56 / 18px / 120×64，加 SizeFlagsVertical=ShrinkCenter，并在 host hbox 加 spacers (`StatsTheSpireSpacerA / B`，宽度 18px) 拓宽视觉间隔 +50%。

---

### 3.10 敌人意图状态机

#### Hover 触发
- **`IntentHoverPatch.AfterCreatureFocus`** — postfix on `NCreature.OnFocus`（私有 string-form），同样 patch `OnUnfocus`
- NCreature 内部把 `Hitbox.MouseEntered` 信号绑到 `OnFocus` 私有方法

#### CallDeferred 关键约束（round 9 round 4）
postfix 不**直接**操作 scene tree。把 creature 引用存进 static field `_pendingShow`，调度到下一帧执行：

```csharp
public static void AfterCreatureFocus(NCreature __instance)
{
    _pendingShow = __instance;
    if (_showQueued) return;
    _showQueued = true;
    Callable.From(DeferredShow).CallDeferred();
}

private static void DeferredShow()
{
    var target = _pendingShow;
    _pendingShow = null;
    _showQueued = false;
    Safe.Run(() => ShowPanel(target));   // ← 此时已脱离 WndProc 派发链
}
```

`ShowPanel` 内部所有 Godot binding 调用都包 try/catch + IsInstanceValid。Panel 加到 `SceneTree.Root` 而不是 creatureNode（避免 creature 销毁时连带问题）。

#### Metadata 缓存
- **`MonsterIntentMetadata.Get(monsterId, liveMonster)`** — 懒加载 + 双源 fallback
  - 首选 live `creature.Monster.MoveStateMachine`
  - 次选 canonical clone：`canonical.ToMutable().SetUpForCombat()` → 再读 `MoveStateMachine`
  - 失败缓存 `_cache[monsterId] = null` 避免重试
- 解码每个 state 通过模式匹配：`MoveState` → 走 Intents + FollowUpStateId；`RandomBranchState` → 走 `States[]` 拿 `weight + maxTimes`；`ConditionalBranchState` → 反射 `_branches` 拿 stateId
- POCO 结构：`MonsterEntry { MonsterId, DisplayName, InitialStateId, States[] }`，`StateInfo { Id, Kind, Intents[], FollowUpStateId, Branches[] }`，`IntentInfo { IntentType, IntentTypeName, Damage?, Repeats, IntentInstance: AbstractIntent? }`

#### 图标缓存（round 9 round 4 关键设计）

**`IntentIconCache.GetIcon(intent, damageHint)`** — 不依赖 live owner：

1. 对于非 AttackIntent 子类：用 `Traverse.Create(intent).Property("SpritePath").GetValue<string>()` 反射读 `protected SpritePath` 类常量（每个 concrete intent 子类都覆写为常量字符串，例如 `BuffIntent.SpritePath = "atlases/intent_atlas.sprites/intent_buff.tres"`）
2. 对于 AttackIntent / SingleAttackIntent / MultiAttackIntent：复刻引擎 5-tier 选择（damage <5/<10/<20/<40/else → attack_1..5），加载 `attack/intent_attack_N.tres`
3. 缓存 key = `(System.Type, attackTier)`，cache value = `Texture2D?`（含 negative）
4. 用 `Godot.ResourceLoader.Load<Texture2D>(path, null, CacheMode.Reuse)` 加载 — 只需路径，**不需要 owner / targets 参数**

为什么不用 `intent.GetTexture(targets, owner)`：那条路径需要 live combat owner + targets list；我们想在第一次 hover 任何怪物时就能拿到**整个 state machine 包括玩家从未触发过的分支**的图标。state machine 里的每个 MoveState.Intents 已经是 live AbstractIntent 实例（怪物 GenerateMoveStateMachine 时构造），我们 BFS 遍历就能拿到所有；SpritePath 只是常量字段，与 live state 无关。

#### 布局算法

**`IntentStateMachinePanel.Create(owner)`**:
1. 通过 `MonsterIntentMetadata.Get(id, monster)` 拿 entry
2. **BFS** 从 `entry.InitialStateId` 出发，按访问顺序排列所有 reachable state
3. 列数 `cols = clamp(ceil(sqrt(N)), 3, 5)`
4. 每个 cell = 80×100 px：56×56 TextureRect (intent icon) + 24px Label (damage 数字)
5. RandomBranchState / ConditionalBranchState 渲染为蓝色 PanelContainer（border 2px aqua），左上角 prob% / 右上角 ≤N
6. **`ArrowOverlay : Control`** — 自定义 Control 在 `_Draw()` 里用 `DrawLine` + `DrawColoredPolygon` 画黄/红箭头
   - Forward edge = yellow `#EFC851`，width 3px
   - ConditionalBranch 边 = red `#FF4444`
   - Anchor 选择：`AnchorOf(rect, other)` 计算两个 cell 之间最近的边缘中点，避免穿过 cell
   - Arrow head = 三角 polygon at dst，10px

#### 数据语义

**Damage 取值**：base damage（不算当前 strength/vulnerable）。`AttackIntent.DamageCalc()` 通过反射调用拿原始 lambda 返回值。

#### 事故记录

- **round 5**：直接读 `creature.Monster.MoveStateMachine` → null timing race（hover signal 比 SetUpForCombat 早触发）
- **round 6**：mod init 时 eager pre-bake `ModelDb.Monsters` × `ToMutable().SetUpForCombat()` → 大量 bake 失败（"no metadata for ..."）。根因：`GenerateMoveStateMachine()` 内部经常引用 `AscensionHelper`、`Creature` 等 — mod init 时 RunManager 未启动 → 抛异常
- **round 8**：改为懒加载 + 双源 fallback。hover trigger 从 NIntent.OnHovered 改为 NCreature.OnFocus（用户要求 hover 在敌人本体）
- **round 9 round 1/2**：连续三次硬崩 dump，全部 `SlayTheSpire2.exe + 0xebf76b` read NULL+0x68，主线程 WndProc 派发链。bisect（暂时禁用 NCreature.OnFocus postfix）确认是这条 patch。修复：CallDeferred 模式（postfix 只 stash 引用，DeferredShow 在下一帧执行所有 Godot binding 调用），`Godot.GodotObject.IsInstanceValid` 守卫，panel parent 改为 SceneTree.Root
- **round 9 round 4**：用户反馈 PRD 方向错（之前是文字 + 横向两栏）。完全重写为 STS1 Intents mod 风格：纯图标 + 数字、BFS 网格、IntentIconCache 反射 SpritePath、ArrowOverlay 自绘箭头

---

### 3.11 个人生涯统计

#### Patch 入口
- **`CareerStatsPatch.AfterLoadStats`** — postfix on `NGeneralStatsGrid.LoadStats`（public）
- 通过 Traverse 读 `_characterStatContainer` (Control)

#### Layout 注入（round 7）
**关键**：`_characterStatContainer.GetParent()` 不一定是 layout container — 用 `LayoutHelper.FindLayoutAncestor(_characterStatContainer)` 走父链找第一个 `VBoxContainer` / `HBoxContainer` / `GridContainer` / `ScrollContainer` 祖先，然后 `AppendToLayoutAncestor(..., moveBeforeAnchor: true)` 把 section 加到 layout 内并 MoveChild 到正确索引。

如果找不到 layout 祖先，fallback 到 `_characterStatContainer.AddChild(section)` 第一个子节点。

诊断日志：`[CareerStatsPatch] character container ancestry: ...` 打出整个父链类型 + 尺寸。

#### Section 内容
- `RunHistoryAnalyzer.Instance.LoadAllAsync(characterFilter)` 异步加载（Task.Run）
- `CareerStatsData` POCO 字段：`WinRateByWindow / DeathCausesByAct / PathStatsByAct / ElderEntries / BossStats`
- 渲染：胜率趋势 / 死因 Top 5 / 卡组构筑表格 / 路径统计表格 / 先古遗物 OptionButton / Boss OptionButton

#### Ancient 池映射（round 5）

`AncientPoolMap.cs` 静态硬编码 9 个先古之民的池子结构：每个 elder 包含一个或多个 `OptionPool`，每个 pool 含 relic id list + optional `ActGate` 限定。

例如 NEOW 拆 `positive_pool` (14 relics) + `curse_pool` (6 relics)；DARV 第 7/8 套有 `ActGate` 标记 ECTOPLASM/SOZU 第 2 幕、PHILOSOPHERS_STONE/VELVET_CHOKER 第 3 幕。

`RunHistoryAnalyzer.ComputeAncientByElder()` 遍历每个 RunHistory，按 elder + pool + relic 累计选取次数 / 胜率。

`CareerStatsSection.BuildElderDetail` 用 6 列 `GridContainer`（NewElderGrid + AddElderHeaderRow + AppendNumericHeader + AppendNumericCell + AddRelicGridRow），渲染列标题行。`AddRelicGridRow` 内按 `pool.ActGate.TryGetValue(relicId, out gateAct)` 给遗物名拼接 `" (只在第 X 幕出现)"` 后缀。

#### Boss 图标
`AncientPoolMap.GetEncounterIcon(encounterId)` 通过 `ImageHelper.GetImagePath("ui/run_history/{lowercase_id}.png")` 加载游戏原生 run history 房间图标。`OptionButton.SetItemIcon` 设置下拉项图标。

#### NameLookup
`NameLookup.Encounter(id) / Boss(id) / Ancient(elderId)` 包装 `LocString("encounters", id+".title").GetFormattedText()`，未命中时硬编码中文回退（NEOW→涅奥 / PAEL→帕埃尔 / TEZCATARA→泰兹卡塔拉 / OROBAS→奥洛巴斯 / VAKUU→瓦库 / TANX→坦克斯 / NONUPEIPE→诺努皮佩 / DARV→达弗）。

#### 事故记录
- **round 5**：原"路径统计"被解读为单一表格 → 拆分为卡组构筑（获取/购买/删除/升级）+ 路径统计（小怪/精英/?房间/商店）两个表格
- **round 5**：先古遗物原实现展平为单一 "all" 选项池，违反 PRD 严格 3 池结构。新建 AncientPoolMap 静态硬编码池子结构
- **round 5**：Boss 名称必须本地化、显示 Act 前缀、`(N 遭遇次数)` 后缀
- **round 7**：injection 重叠 — fix 用 LayoutHelper

---

### 3.12 Run History 增强

#### Patch 入口
- **`RunHistoryPatch.AfterSelectPlayer`** — postfix on `NRunHistory.SelectPlayer`（**private** string-form）
- 通过 Traverse 读 `_history` (RunHistory) + `_screenContents` (Control) + `_deckHistory` (NDeckHistory) + `_relicHistory` (NRelicHistory)

#### Layout 注入（round 7）
- 用 `LayoutHelper.FindLayoutAncestor(_deckHistory ?? _relicHistory)` 走到第一个 layout 祖先
- `LayoutHelper.AppendToLayoutAncestor(..., moveBeforeAnchor: false)` 加到末尾
- fallback：右侧浮动 `ScrollContainer` 锚到 `_screenContents`（offset Left=-340, Right=-20, Top=160, Bottom=-50）

#### Section 内容
`RunHistoryStatsSection.Create(history)`：
- 卡组构筑表格 + 路径统计表格（结构同 §3.11，但只读单局数据）
- 先古遗物选取列表（不是选率，直接列每次选取）
- Boss 战损列表
- "查看贡献图表" 按钮 → `ContributionPanel.ShowRunReplay(seed)` → 加载 `_summary.json`，只显示"本局汇总" tab

#### 持久化
- 每场战斗结束：`ContributionPersistence.SaveCombat(seed, encounterId, snapshot)` → `_combat_{N}.json`
- 局结束（OnMetricsUpload）：`SaveRunSummary(seed, RunContributionAggregator.RunTotals)` → `_summary.json`
- 加载时：`LoadRunSummary(seed)` 优先；不存在则从 per-combat 文件 fallback 合并

---

### 3.13 筛选面板"我的数据"

- `FilterSettings.MyDataOnly` 字段
- `FilterPanel` UI 加 CheckBox
- F9 Apply 时持久化到 `config.json`
- 注：StatsProvider 当前还没有 fully fallback 到 RunHistoryAnalyzer 当 MyDataOnly = true 时（**长期遗留**）

---

### 3.14 功能开关

- `FeatureToggles.cs` 11 个 bool 属性 + 元数据 `(name, key)` tuple list
- `FilterPanel` ScrollContainer + 11 个 NTickbox-style CheckBox
- `ModConfig.SaveSettings()` 序列化到 `config.json` 的 `feature_toggles` 段
- 每个 patch 第一行 `if (!ModConfig.Toggles.X) return;`

---

### 3.15 多人游戏基础兼容

- `LocalContext.GetMe(state)` 单人模式 NetId 可能为 null → fallback 到 `state.Players?.FirstOrDefault()`
- 注：PRD 要求"不上传 Steam ID"，验证：`RunUploadPayload` 没有 `player_id / steam_id / user_id` 字段；`localPlayerId` 参数接收但被丢弃；加 inline 注释防止后续 regression

---

### 3.16 商店价格显示

#### Patch 入口
`MapPointPatch.AfterOnFocus` 在 `point.PointType == Shop && State != Traveled` 时显示 `ShopPricePanel`

#### 折扣联动（round 5）
`ShopPricePanel.Create(player, cardRemovalsUsed)`:
- 遍历 `player.Relics` 找 `MembershipCard`（×0.5）/ `TheCourier`（×0.8）
- 多个折扣相乘
- 表格里的价格用折扣后的值
- 顶部"当前折扣"行只在持有时显示
- 若没有任何折扣遗物：subtitle 仅"商店价格表"，无折扣描述

#### 表格布局
- `GridContainer` Columns=4
- 行：表头（普通 / 非普通 / 稀有）+ 遗物 + 卡牌 + 药水
- 底部："无色牌价格 +15%" 单行 footer note（**只在底部一次**，不重复）

#### Card removal 费用
- 从 RunState 读 `CardShopRemovalsUsed`
- 公式：`75 + 25 × N`

#### 事故记录
- **round 5**：折扣联动需求新增
- **round 6**：subtitle 写死 "会员卡 50%、送货员 20%、无色牌价格 +15%" 即使玩家两个都没有，会让人误以为商店总是打折。修复：subtitle 仅"商店价格表"；折扣行只在真正持有时出现；"无色牌价格 +15%" 只在面板**底部**出现一次

---

### 3.17 卡牌掉落概率

实现镜像 §3.9 的 PotionOddsIndicator，使用相同的 lifecycle 模型 + IsAlive 守卫 + NTopBar parent + ShrinkCenter 对齐。

#### 图标
游戏原生 `ui/reward_screen/reward_icon_card.png`（"将一张牌添加到你的牌组"奖励行图标），通过 `ResourceLoader.Load<Texture2D>` 加载。

尺寸：64×64 容器 + 56×56 icon（round 9 round 2），后续 round 9 round 3 enlarged 到 72×72 + 62×62 icon（用户反馈+10%）。

#### Hover 表格
`CardDropOddsIndicator.ShowHoverPanel`:
- 表格 4 列：稀有度 × (Regular Combat / Elite Combat)
- 基础概率：Regular = (Rare 3% / Uncommon 37% / Common 60%)；Elite = (Rare 10% / Uncommon 40% / Common 50%)
- pity offset 从 `me.PlayerOdds.CardRarity.CurrentValue` 加到 Rare 概率上

#### 事故记录
同 §3.9 — 共享 NTopBar lifecycle bug 历史。

---

### 3.18 角色筛选

#### Filter 字段
`FilterSettings.cs`：
- `CharacterFilterMode` 字段（默认 `"auto"`）
- `ResolveCharacter()` 方法：
  - `auto` → `RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault()?.Character?.Id.Entry`
  - `all` → null（不过滤）
  - 显式角色 ID → 该 ID
  - 无活动 run 时 auto 返回 null（自然满足 compendium 固定"所有角色"）
- `ToQueryString` 和 `Equals/GetHashCode` 都用 `ResolveCharacter()`，让语义等价的 filter 走同一份缓存

#### F9 面板
`FilterPanel.cs`：在 "我的数据" 和 "自动匹配进阶" 之间插入 OptionButton 角色下拉（7 选项：自动匹配 / 全部 / 5 个角色），`OnApplyPressed` 写回 `CharacterFilterMode`。

#### CareerStatsSection 独立下拉
- 独立 OptionButton（6 选项：全部 + 5 角色）
- `OnCharacterDropdownChanged` 直接调 `RunHistoryAnalyzer.GetCached(newFilter)` + `LoadAllAsync`
- 用户手动选择**持久化**、不因新开 run 重置

#### Run start preload
`RunLifecyclePatch.OnRunStartSP/MP postfix` 改为 `preloadChar = filter.ResolveCharacter() ?? runCharacter`，让 F9 手动覆盖优先级高于 run 角色。

`CommunityStatsMod.OnFilterApplied` 改为 resolve 后再 `StatsProvider.OnFilterChangedAsync`，dropdown 变更立即生效。

#### Localization keys
9 个新角色筛选键（EN + CN，各 9 条，含 `settings.character` / `settings.char_auto` / `settings.char_all` / `char.IRONCLAD|SILENT|DEFECT|NECROBINDER|REGENT`）。

---

## 4. PRD §4 (PRD-00 补全) 实施细节

### 4.1 多乘法修正器拆分

**`CombatTracker.OnDamageDealt`** 末尾的 scale block：
```csharp
if (modifierTotal > totalDamage && modifierTotal > 0 && totalDamage >= 0) {
    float scale = (float)totalDamage / modifierTotal;
    foreach modifier: modifier.Amount = round(modifier.Amount * scale);
}
```

### 4.2 Confused / SneckoEye 能量贡献

`ConfusedSourceTagPatch` (Postfix on `ConfusedPower.AfterCardDrawn`) — 把 active source 设成 `SNECKO_EYE` / `FAKE_SNECKO_EYE` / `SNECKO_OIL`（first-tagger-wins 守卫）。

`EnergyCostSetterTagPatch` (Postfix on `CardEnergyCost.SetThisCombat` + `SetThisTurnOrUntilPlayed`) — 捕获所有 cost setter 调用并归因。

`CombatTracker.AttributeCostSavings` 内部有 `_negativeSavingsAllowedSources = HashSet { SNECKO_EYE, FAKE_SNECKO_EYE, SNECKO_OIL }`，仅这些来源允许负贡献。

### 4.3 Enlightenment 部分降费

自动覆盖 by §4.2 的 `EnergyCostSetterTagPatch`（uses `_activeCardId = "ENLIGHTENMENT"`）。零额外代码。

### 4.4 Buffer 防御按 hit 累加

`BufferPower.ModifyHpLostAfterOstyLate` patch 验证已经按 hit 累加（DEF5b PRD 示例测试在 `mods/sts2_contrib_tests/src/Scenarios/DefenseTests.cs:DEF5b_BufferPrdExample`）。

### 4.5 贡献面板

#### 默认位置
- Anchor LEFT (`AnchorLeft = 0.0, AnchorRight = 0.0, OffsetLeft = 10, OffsetRight = 470`)
- 宽度 460px
- `DraggablePanel` 子类 — InputEventMouseMotion 拖拽，位置写到 `ModConfig.PanelPositionX/Y`

#### Tab 标题
**必须**用 `tabs.SetTabTitle(idx, ...)` 显式设置，**不要**依赖 Node.Name（Godot 会 auto-mangle 中文 / 重复名为 `@ScrollContainer@N`）。

#### 数据源选择
`SelectCombatTabData()`：
```csharp
if (CombatManager.Instance?.IsInProgress == true)
    return CombatTracker.Instance.GetCurrentCombatData();
return CombatTracker.Instance.LastCombatData;
```

#### 防御百分比
分母只用**正贡献之和**：
```csharp
int posSum = 0;
foreach (a in data.Values) {
    int v = a.EffectiveBlock + a.ModifierBlock + a.MitigatedByDebuff
          + a.MitigatedByBuff + a.MitigatedByStrReduction + a.UpgradeBlock;
    if (v > 0) posSum += v;
}
totalVal = posSum;
```
SelfDamage 显示为红条但不进入 % 分母。

#### Help dialog (round 6)
`ToggleHelpPanel()` 自定义 `PanelContainer`（不是 InfoModPanel），带 "✕" 关闭按钮。每次 ? 点击 toggle。`IsInstanceValid(_helpPanel)` 守卫。

#### 事故记录
- **round 4**：原默认右侧 → 挡前进按钮。改为 LEFT。
- **round 4**：tab 显示 `@ScrollContainer@7681` → 改用 SetTabTitle。
- **round 5**：自伤算入防御 % → 出现 125%。改为只用正贡献和。
- **round 6**：? 帮助打不开 → 改为自定义 PanelContainer + 显式关闭按钮。
- **round 8**：实时刷新失效 → 永久 skeleton + ReplaceScrollContent (见 §3.6 事故记录)。

### 4.6 数据上传

`OfflineQueue.cs`:
- `MaxEntries = 10`
- `RetentionDays = 7`
- `TrimToMaxEntries()` LRU evict
- `PruneExpired()` drop 7+ days

`RunDataCollector.BuildPayload`：`RunUploadPayload` 没有任何 player_id / steam_id / user_id 字段；inline 注释防止后续 regression。

---

## 5. 关键技术备注（避免重复踩坑）

1. **构建错误**：必须看 `Build succeeded.` 行 + DLL mtime。CS0246/CS0103 在 mod source 是真错（缺 using）。`warning MSB3245` 是 csproj HintPath 错位 → 检查仓库根是否对齐。
2. **私有方法 Harmony patch**：`[HarmonyPatch(typeof(X), "MethodName")]` 字符串形式
3. **私有字段读取**：`Traverse.Create(obj).Field("_x").GetValue<T>()`，try/catch + Safe.Warn
4. **F1 精度**：所有百分比 1 位小数
5. **toggle 短路**：每个 patch 第一行 `if (!ModConfig.Toggles.X) return;`
6. **meta 标记**：UI 注入幂等用 HasMeta/SetMeta/RemoveMeta
7. **异步 patch 陷阱**：Harmony postfix 在 async 方法的第一个 await 处触发
8. **NCombatUi.Activate**：参数 `CombatState state`。用 `LocalContext.GetMe(state)` 拿 Player；单人 NetId 可能为 null，必须 fallback
9. **scene 容器注入**：不要直接 `parent.AddChild`，用 `LayoutHelper.AppendToLayoutAncestor`
10. **NRun.Instance.GlobalUi.TopBar** 在 mod init 时不存在，必须 lazy attach
11. **MonsterIntentMetadata** 不要 mod-init eager bake (GenerateMoveStateMachine 内常引用 RunManager)。优先用 live monster 的 SM
12. **Win32 message dispatch 上下文**：任何 Harmony postfix on Godot signal callback (e.g. `NCreature.OnFocus`) 都跑在 `DispatchMessageW → WndProc` 链里。**禁止**在该上下文同步访问 / 修改 Godot scene tree → AV at `SlayTheSpire2.exe + 0xebf76b` (read NULL+0x68)。必须用 `Callable.From(callback).CallDeferred()` 调度到下一帧

---

## 6. Round 4-9 时间线（用户验收反馈）

| Round | 日期 | 主要内容 |
|---|---|---|
| 4 | 2026-04-09 | 首次发现 build 一直失败（CS0246 当噪音过滤掉了）；修 4 个真错；处理 5 张反馈截图 |
| 5 | 2026-04-09 | 工作流规则确立（PRD 先行）；CardLibrary 4× 重复 bug、双源双列、路径统计拆分、Ancient 3 池、Boss act 前缀 |
| 6 | 2026-04-09 | RelicLibrary 双源、商店 subtitle、防御 % 修复、Help dialog、MonsterIntentMetadata pre-bake |
| 7 | 2026-04-09 | 顶栏 reparent 到 NTopBar、LayoutHelper 修复重叠、Boss icon |
| 8 | 2026-04-10 | LiveContributionSnapshot、ContributionPanel 实时刷新 in-place skeleton swap、IntentHoverPatch NCreature.OnFocus + jaw worm 两栏 UI |
| 9 round 1 | 2026-04-10 | §3.18 角色筛选、Ancient pool 拆分、Darv ActGate；仓库根重定位修 csproj HintPath |
| 9 round 2 | 2026-04-10 | NTopBar lifecycle 重写、IsAlive 守卫、indicator 全 run 可见 |
| 9 round 3 | 2026-04-10 | 顶栏图标尺寸（图标 56×56 / 文本 18px / 容器 120×64）、垂直对齐 ShrinkCenter、spacer 间隔 +50%、卡牌图标 +10% |
| 9 round 4 | 2026-04-10 | IntentHoverPatch CallDeferred 修复 dump 三连崩；意图状态机完全重写为 BFS 网格 + 原生图标 + IntentIconCache 反射 SpritePath |
