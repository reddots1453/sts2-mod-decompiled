# PLAN: Stats the Spire — 第二轮迭代实施计划

> 版本：3.0 | 日期：2026-04-09（Round 3 最终版）
> PRD：PRD_04_ITERATION2.md v3.0
> 范围：17 个功能需求（§3.1-3.17）+ 6 条 PRD-00 补全（§4.1-4.6）

---

## 1. 架构概览

### 1.1 子系统分解

```
┌─────────────────────────────────────────────────────────┐
│                    CommunityStatsMod                     │
│                   (Entry Point + DI)                     │
├──────────┬──────────┬───────────┬───────────┬───────────┤
│ Config   │ Collect  │ Patches   │ UI        │ Util      │
│ ──────── │ ──────── │ ────────  │ ────────  │ ────────  │
│ ModCfg   │ Combat   │ Compend.  │ Contrib.  │ Safe      │
│ Feature  │ Tracker  │ Tooltip   │ Panel     │ Offline   │
│ Toggles  │ RunAggr. │ Intent    │ InfoMod   │ Queue     │
│ Filter   │ Career   │ Probab.   │ Intent    │ RunHist.  │
│ Settings │ Stats    │ Shop      │ Career    │ Analyzer  │
│ L (i18n) │ Analyzer │ RealTime  │ Filter    │           │
└──────────┴──────────┴───────────┴───────────┴───────────┘
```

### 1.2 依赖关系图

```
FeatureToggles ← (所有 patches, 所有 UI)
ModConfig ← FeatureToggles, FilterSettings
StatsProvider ← (所有统计展示 UI)
CareerStatsAnalyzer ← RunHistoryAnalyzer ← SaveManager API
InfoModPanel (基类) ← UnknownRoomPanel, PotionOddsPanel, CardDropPanel, ShopPricePanel
ContributionPanel ← CombatTracker, RunContributionAggregator
IntentStateMachinePanel ← MonsterMoveStateMachine reflection
```

---

## 2. 新增文件

### 2.1 Config 层

| # | 文件路径 | 类名 | 职责 |
|---|---------|------|------|
| 1 | `src/Config/FeatureToggles.cs` | `FeatureToggles` | 11 个布尔属性（每个子功能一个），持久化到 config.json，静态 `IsEnabled(Feature)` 检查 |

### 2.2 Collection / Analytics 层

| # | 文件路径 | 类名 | 职责 |
|---|---------|------|------|
| 2 | `src/Collection/RunHistoryAnalyzer.cs` | `RunHistoryAnalyzer` | 通过 `SaveManager` 加载所有 RunHistory 文件，聚合为生涯统计（胜率、死因、路径统计、先古遗物选择率、Boss 战损）。后台 Task 运行。 |
| 3 | `src/Collection/CareerStatsData.cs` | `CareerStatsData` | 数据模型：滚动胜率(10/50/全部)、每 Act 死因 Top5、每 Act 路径平均值、先古遗物选择率、Boss 战损平均值。不可变快照。 |
| 4 | `src/Collection/ContributionPersistence.cs` | `ContributionPersistence` | 将贡献数据序列化/反序列化为 JSON，与 .run 文件同目录存放，用于 Run History 回放。 |

### 2.3 Patch 层

| # | 文件路径 | 类名 | 职责 |
|---|---------|------|------|
| 5 | `src/Patches/CardLibraryPatch.cs` | `CardLibraryPatch` | Harmony postfix on `NInspectCardScreen.UpdateCardDisplay()`（private），注入 `NModCardLibraryStats` 统计面板到卡牌大图右侧 |
| 6 | `src/Patches/RelicCollectionPatch.cs` | `RelicCollectionPatch` | Harmony postfix on `NRelicCollectionEntry.OnFocus()`，追加遗物统计到 tooltip |
| 7 | `src/Patches/RelicTooltipPatch.cs` | `RelicTooltipPatch` | Harmony postfix on `IHoverTip.GetHoverTipContents()`，全局遗物胜率浮动 |
| 8 | `src/Patches/IntentHoverPatch.cs` | `IntentHoverPatch` | Harmony postfix on `NIntent.OnHovered()`/`OnUnhovered()`，显示/隐藏意图状态机面板 |
| 9 | `src/Patches/RealTimeCombatPatch.cs` | `RealTimeCombatPatch` | Harmony postfix on 出牌完成 + 药水使用，触发防抖面板刷新 |
| 10 | `src/Patches/RunHistoryPatch.cs` | `RunHistoryPatch` | Harmony postfix on `NRunHistory` 页面加载，注入局统计 + 贡献回看按钮 |
| 11 | `src/Patches/CareerStatsPatch.cs` | `CareerStatsPatch` | Harmony postfix on 统计页面加载，在"总体数据"下方注入生涯统计区块 |
| 12 | `src/Patches/PotionOddsPatch.cs` | `PotionOddsPatch` | Harmony postfix on 战斗 UI 初始化，添加药水概率指示器到右上角 |
| 13 | `src/Patches/CardDropOddsPatch.cs` | `CardDropOddsPatch` | Harmony postfix on 战斗 UI 初始化，添加卡牌掉落概率指示器 |
| 14 | `src/Patches/ShopPricePatch.cs` | `ShopPricePatch` | Harmony postfix on 金币图标悬停/地图商店节点，显示商店价格面板 |

### 2.4 UI 层

| # | 文件路径 | 类名 | 职责 |
|---|---------|------|------|
| 15 | `src/UI/InfoModPanel.cs` | `InfoModPanel` | **基类**：所有 InfoMod 风格悬停面板（深色背景、边框、标题、副标题、分隔线、内容行）。被 4+ 面板复用。 |
| 16 | `src/UI/IntentStateMachinePanel.cs` | `IntentStateMachinePanel` | 渲染怪物 AI 状态机：当前状态高亮、箭头、随机分支(概率/约束)、条件分支、循环指示 |
| 17 | `src/UI/PotionOddsIndicator.cs` | `PotionOddsIndicator` | 紧凑 HUD 元素（图标+百分比），悬停展开为 InfoModPanel 显示累计概率 |
| 18 | `src/UI/CardDropOddsIndicator.cs` | `CardDropOddsIndicator` | 紧凑 HUD 元素（卡牌图标+稀有概率），悬停展开为表格（稀有度×战斗类型） |
| 19 | `src/UI/ShopPricePanel.cs` | `ShopPricePanel` | InfoModPanel 子类，显示删牌费用、各稀有度卡牌/遗物/药水价格范围 |
| 20 | `src/UI/UnknownRoomPanel.cs` | `UnknownRoomPanel` | InfoModPanel 子类，显示各遭遇类型概率（彩色类别名） |
| 21 | `src/UI/CareerStatsSection.cs` | `CareerStatsSection` | 注入统计页面的 VBoxContainer：胜率趋势、死因排行、路径统计、先古遗物下拉、Boss 下拉 |
| 22 | `src/UI/RunHistoryStatsSection.cs` | `RunHistoryStatsSection` | 注入 Run History 的 VBoxContainer：按 Act 路径统计、先古遗物选择、Boss 战损、"查看贡献"按钮 |
| 23 | `src/UI/DraggablePanel.cs` | `DraggablePanel` | 可拖拽面板基类/混入，支持位置持久化 |

---

## 3. 修改文件

### 3.1 入口与配置

| 文件 | 修改内容 | 原因 |
|------|---------|------|
| `CommunityStatsMod.cs` | 加载 FeatureToggles；日志前缀改为 "Stats the Spire"；注册新 UI 面板 | 入口编排 |
| `manifest.json` | `name` 改为 "Stats the Spire"，`id` 保持 "sts2_community_stats" | PRD 3.1 |
| `ModConfig.cs` | 新增 `FeatureToggles Toggles` 属性；新增 `PanelPosition` (Vector2?)；版本号升至 "2.0.0"；离线队列限制(10条/7天) | PRD 3.14, 4.5, 4.6 |
| `FilterSettings.cs` | 新增 `bool UseMyDataOnly` 字段，纳入序列化/hash | PRD 3.13 |
| `Localization.cs` | 新增 ~80 个字符串键（意图面板、生涯统计、概率显示、商店价格、功能开关标签） | 所有新 UI |

### 3.2 Collection 层

| 文件 | 修改内容 | 原因 |
|------|---------|------|
| `CombatTracker.cs` | (1) 新增 `TurnCount`/`TotalDamageDealt` 公开访问器用于 DPS 计算; (2) 新增 `CombatDataUpdated` 事件（出牌/用药后触发）; (3) 实现多修正器比例缩放(PRD 4.1); (4) 新增 Confused/SneckoEye 能量追踪(PRD 4.2); (5) 新增 Enlightenment 部分降费追踪(PRD 4.3) | PRD 3.6, 3.7, 4.1-4.3 |
| `RunContributionAggregator.cs` | 新增 `SerializeToJson()` 用于贡献持久化；新增 `DPS` 计算(总伤害/总回合数) | PRD 3.7, 3.12 |

### 3.3 UI 层

| 文件 | 修改内容 | 原因 |
|------|---------|------|
| `ContributionPanel.cs` | (1) 移到右侧(AnchorRight=1.0); (2) 集成 DraggablePanel; (3) 新增 DPS 行; (4) 新增帮助(?)按钮; (5) 子来源可折叠(▶/▼); (6) 订阅 CombatDataUpdated 实时刷新(500ms 防抖); (7) 避让逻辑(检测奖励面板) | PRD 3.6, 3.7, 4.5 |
| `ContributionChart.cs` | 可折叠子条目（▶/▼ 切换）；处理负能量值显示(Confused 红色) | PRD 4.2, 4.5 |
| `FilterPanel.cs` | (1) 新增"仅显示我的数据"复选框; (2) 新增功能开关区(11 个 NTickbox); (3) 应用时持久化 | PRD 3.13, 3.14 |
| `MapPointOverlay.cs` | 扩展：? 节点悬停时显示 UnknownRoomPanel | PRD 3.8 |
| `StatsLabel.cs` | 确保所有百分比格式为 `F1`（一位小数） | PRD AC-15 |

### 3.4 Patch 层

| 文件 | 修改内容 | 原因 |
|------|---------|------|
| `EventOptionPatch.cs` | 简化：移除胜率显示，仅保留选择率 | PRD 3.5 |
| `MapPointPatch.cs` | 新增 ? 节点悬停检测 → 触发 UnknownRoomPanel | PRD 3.8 |
| `CombatLifecyclePatch.cs` | (1) 显示面板前检查功能开关; (2) 战斗结束时持久化贡献数据 | PRD 3.12, 3.14 |
| `RelicHoverPatch.cs` | 扩展：在所有遗物 tooltip 中注入胜率浮动 | PRD 3.4 |
| `RunLifecyclePatch.cs` | 局结束时触发贡献持久化；应用离线队列限制(10条/7天) | PRD 4.6 |

### 3.5 Util 层

| 文件 | 修改内容 | 原因 |
|------|---------|------|
| `OfflineQueue.cs` | 新增限制：最多 10 条，修剪超过 7 天的条目 | PRD 4.6 |

---

## 4. 共享基础设施

### 4.1 功能开关系统

```csharp
// src/Config/FeatureToggles.cs
public class FeatureToggles
{
    public bool ContributionPanel { get; set; } = true;     // F8 面板 + 实时刷新
    public bool CardLibraryStats { get; set; } = true;      // 百科卡牌统计
    public bool RelicStats { get; set; } = true;            // 遗物胜率浮动
    public bool EventPickRate { get; set; } = true;         // 事件选择率
    public bool MonsterDanger { get; set; } = true;         // 怪物房间危险度
    public bool UnknownRoomOdds { get; set; } = true;       // ? 房间概率
    public bool PotionDropOdds { get; set; } = true;        // 药水掉落概率
    public bool CardDropOdds { get; set; } = true;          // 卡牌掉落概率
    public bool ShopPrices { get; set; } = true;            // 商店价格
    public bool IntentStateMachine { get; set; } = true;    // 敌人意图状态机
    public bool CareerStats { get; set; } = true;           // 个人生涯统计

    // 序列化/反序列化到 config.json "feature_toggles" 键
    // 检查模式：if (!ModConfig.Toggles.CardLibraryStats) return; 在 patch 入口
}
```

**设计决策**：每个 Harmony patch 在 postfix 最顶部检查 `FeatureToggles.IsEnabled`，在 `Safe.Run()` 之前。禁用功能几乎零开销（仅一个布尔检查）。

### 4.2 配置持久化

扩展 `config.json`：
```json
{
    "api_base_url": "...",
    "feature_toggles": { "contribution_panel": true, ... },
    "panel_position": { "x": 850, "y": 100 },
    "use_my_data_only": false
}
```

`ModConfig.LoadOverrides()` 扩展反序列化这些字段；新增 `ModConfig.SaveSettings()` 写回。

### 4.3 InfoModPanel 基类

```csharp
// src/UI/InfoModPanel.cs
public class InfoModPanel : PanelContainer
{
    // 所有 InfoMod 风格面板的共享工厂（PRD 3.8/3.9/3.16/3.17）
    // StyleBoxFlat: bg rgba(0.08,0.08,0.12,0.95), border rgba(0.3,0.4,0.6,0.4), radius 6px
    // 标题: white 14px, 副标题: gray 11px, 分隔线: HSeparator
    // 内容: VBoxContainer + 可配置行
    
    protected VBoxContainer Content;
    
    public static InfoModPanel Create(string title, string? subtitle = null);
    public void AddRow(string label, string value, Color? labelColor = null);
    public void AddSeparator();
    public void PositionNear(Control target); // 智能定位，保持在视口内
}
```

### 4.4 防抖工具

```csharp
// 添加到 Safe 类或新建 Debounce.cs
public class Debounce
{
    private long _lastTick;
    private readonly long _minIntervalTicks;
    
    public Debounce(int minIntervalMs)
    {
        _minIntervalTicks = minIntervalMs * (Stopwatch.Frequency / 1000);
        _lastTick = 0;
    }
    
    public bool CanFire()
    {
        var now = Stopwatch.GetTimestamp();
        if (now - _lastTick < _minIntervalTicks) return false;
        _lastTick = now;
        return true;
    }
}
```

---

## 5. 实施阶段

### Phase 0: 技术验证 Spike（预估 1-2 小时）

**依赖**：无

**目标**：在全面实施前验证 3 个最高风险钩子的可行性

**任务**：
1. **NIntent private 方法 patch** — 写一个最小 Harmony patch 验证能成功 postfix `NIntent.OnHovered()`（private 方法），确认 `AccessTools.Method` 语法和参数名
2. **NCardLibraryGrid 注入与原生 NCardLibraryStats 共存** — 读取完整 `NCardLibraryStats` 类代码，确认大图打开的回调方法名，验证 Mod 节点可与原生节点共存
3. **RunHistoryAnalyzer 性能** — 用 `SaveManager.GetAllRunHistoryNames()` + `LoadRunHistory()` 加载全部历史局，测量耗时，确认后台 Task 方案可行

**产出**：3 个 spike 的结论记录（成功/需调整/不可行），纳入正式实施的具体参数

**Spike 结果（2026-04-09 已完成）**：

**Spike 1: NIntent — 可行 ✅**
- `OnHovered()`/`OnUnhovered()` 是 private void 无参，Harmony 可直接 `[HarmonyPatch(typeof(NIntent), "OnHovered")]`
- 私有字段 Traverse 访问：`_owner`(Creature), `_intent`(AbstractIntent), `_targets`(IEnumerable<Creature>)
- MonsterMoveStateMachine: `_currentState`(private MonsterState) 需 Traverse, `States`(public Dict) 直接访问
- StateWeight 有 stateId/repeatType/maxTimes/weightLambda/cooldown（均 public）
- ⚠️ ConditionalBranchState.States 是 **private List<ConditionalBranch>**，ConditionalBranch 也是 private struct → 需双层 Traverse

**Spike 2: CardLibrary — 注入点确认 ✅**
- **真正注入目标：`NInspectCardScreen.UpdateCardDisplay()`**（private 方法）
- 调用链：NCardLibrary.ShowCardDetail() → NInspectCardScreen.Open() → SetCard() → UpdateCardDisplay()
- `_cards[_index]` 提供当前 CardModel
- 游戏内建 NCardLibraryStats 仅在 grid 中显示 TimesWon，不在大图预览中
- Plan 原定 Phase 3 的 CardLibraryPatch 应改为 patch `NInspectCardScreen.UpdateCardDisplay()`

**Spike 3: RunHistory — 可行（需后台加载） ⚠️**
- SaveManager 仅同步 API，必须用 Task.Run()
- RunHistory.MapPointHistory 类型：`List<List<MapPointHistoryEntry>>`（acts → points）
- PlayerMapPointHistoryEntry 有完整字段含 AncientChoices(List<AncientChoiceHistoryEntry>) + WasChosen 标记
- **注入点：`NGeneralStatsGrid.LoadStats()`**（postfix，在"总体数据"之后注入）
- 1000+ 局可能 5-30s，必须后台加载+缓存+进度指示器

---

### Phase 1: 基础设施（预估 3-4 小时）

**依赖**：Phase 0（确认钩子可行性）

**任务**：
1. **创建 `FeatureToggles.cs`** — 11 个布尔属性，JSON 序列化
2. **扩展 `ModConfig.cs`** — 新增 Toggles 属性、PanelPosition、SaveSettings()、离线队列限制
3. **扩展 `ModConfig.LoadOverrides()`** — 反序列化 feature_toggles 和 panel_position
4. **更新 `manifest.json`** — name = "Stats the Spire"
5. **更新 `CommunityStatsMod.cs`** — 初始化时加载 toggles，日志前缀更名
6. **更新 `Localization.cs`** — 新增所有字符串键（~80 条）
7. **创建 `InfoModPanel.cs`** — 基类：Create/AddRow/AddSeparator/PositionNear
8. **创建 `DraggablePanel.cs`** — InputEventMouseMotion 处理，位置保存/恢复

**涉及文件**: `FeatureToggles.cs` (新), `ModConfig.cs` (改), `CommunityStatsMod.cs` (改), `manifest.json` (改), `Localization.cs` (改), `InfoModPanel.cs` (新), `DraggablePanel.cs` (新)

**测试**：手动 — 验证 mod 加载、开关持久化、InfoModPanel 渲染、面板拖拽

---

### Phase 2: 面板重设计 + 实时刷新 + DPS（预估 3-4 小时）

**依赖**：Phase 1（DraggablePanel, FeatureToggles, Localization）

**任务**：
1. **ContributionPanel 右侧默认** — 修改 anchors: AnchorLeft=1.0, AnchorRight=1.0, OffsetLeft=-520, OffsetRight=-10
2. **集成 DraggablePanel** — 面板响应拖拽，位置持久化到 config
3. **新增 DPS 行** — 标题下方 HBoxContainer: "每回合平均伤害: X.X"，使用 CombatTracker.TotalDamageDealt / CombatTracker.TurnCount
4. **新增帮助(?)按钮** — 标题栏右侧，显示 HoverTip 说明颜色/快捷键
5. **ContributionChart 可折叠子来源** — 每个父行添加 ▶/▼ 切换，默认展开
6. **避让逻辑** — ShowCombatResult() 时检测 NRewardsScreen 是否可见；是则偏移面板
7. **实时刷新** — 创建 `RealTimeCombatPatch.cs`: 出牌完成和药水使用 postfix，触发 `CombatTracker.CombatDataUpdated` 事件；ContributionPanel 订阅并用 500ms 防抖
8. **功能开关检查** — 所有现有 patches 检查 `Toggles.ContributionPanel`

**涉及文件**: `ContributionPanel.cs` (改), `ContributionChart.cs` (改), `RealTimeCombatPatch.cs` (新), `CombatTracker.cs` (改), `CombatLifecyclePatch.cs` (改)

**关键设计决策**：实时刷新通过 CombatTracker 上的 C# 事件 `CombatDataUpdated` 触发。Harmony postfix 在出牌时触发该事件，ContributionPanel 的处理器使用 `Debounce(500)` 门控后调用 `RefreshTabs()`。所有 Godot 节点操作通过 `CallDeferred` 确保主线程安全。

**测试**：手动 — 战斗中验证面板在右侧、拖拽、DPS 更新、实时刷新防抖、子来源折叠/展开、面板避让奖励界面

---

### Phase 3: Tooltip & Compendium（预估 4-5 小时）

**依赖**：Phase 1（FeatureToggles, Localization, InfoModPanel）

**任务**：
1. **CardLibraryPatch.cs** — **注意**：游戏已有内建 `NCardLibraryStats` 节点（显示胜场数 TimesWon），在 `InitGrid()` 中通过 `EnsureCardLibraryStatsExists()` + `UpdateStats()` 加载。Mod 应**创建独立的 `NModCardLibraryStats` 节点**（命名区分），不修改或替换原生统计。注入点应为**卡牌大图打开时的回调**（PRD 要求"点击打开大图后，右侧方显示"），而非 `InitGrid()`（那是 grid 布局阶段）。需探索卡牌预览/大图打开的具体方法名（可能是 `NCardLibraryGrid` 的 `OnCardSelected()` 或类似回调）。注入 VBoxContainer 显示 4 项统计（选取率/胜率/购买率/升级率），定位在卡牌大图右侧。数据来自 `StatsProvider.Instance`。
2. **RelicCollectionPatch.cs** — `NRelicCollectionEntry.OnFocus()` postfix：获取遗物模型，检查 ModelVisibility=Discovered，通过 BBCode 注入追加统计行（分隔线+胜率+浮动+购买率）。
3. **RelicTooltipPatch.cs** — 全局遗物 hover tip 内容 postfix：注入胜率浮动行。格式：`"胜率 52.3% [color=#7FFF00](+2.3%)[/color]"` BBCode。Delta = 遗物胜率 - 全局平均胜率。
4. **EventOptionPatch.cs** — 简化现有：移除胜率行，仅保留选择率。格式：`"选择率: 67.8%"` cream 11px。

**涉及文件**: `CardLibraryPatch.cs` (新), `RelicCollectionPatch.cs` (新), `RelicTooltipPatch.cs` (新), `EventOptionPatch.cs` (改), `RelicHoverPatch.cs` (改)

**关键设计决策（卡牌图书馆）**：游戏已有 `NCardLibraryStats` 节点（显示胜场数），Mod 创建独立的 `NModCardLibraryStats` 节点避免冲突。PRD 要求统计在"卡牌大图打开后右侧方"显示，因此注入点应为卡牌预览/大图打开的回调（非 InitGrid）。方法：遍历节点树找到大图预览区，在其右侧定位统计 VBoxContainer。统计面板是单个可复用节点，每次卡牌聚焦变化时更新内容。实施前需 spike 确认大图打开的具体方法名。

**关键设计决策（遗物全局 Tooltip）**：不是 patch 每个单独的遗物 tooltip 调用点，而是 patch `IHoverTip.GetHoverTipContents()` 或 tooltip 渲染路径。检测 hover tip 是否属于遗物（检查源对象是否实现遗物相关接口），然后追加 delta 行。比 patch 5+ 个独立界面更简洁。

**测试**：手动 — 打开百科大全，验证卡牌统计显示、遗物 tooltip 显示 delta、事件选项仅显示选择率

---

### Phase 4: 概率显示（预估 5-6 小时）

**依赖**：Phase 1（InfoModPanel, FeatureToggles）

**任务**：
1. **UnknownRoomPanel.cs** — InfoModPanel 子类。内容：5 行（事件 green、怪物 red、精英 orange、商店 gold、宝箱 aqua）+ 百分比值，来自 `UnknownMapPointOdds`（通过 `RunManager.Instance.RunState.Odds.UnknownMapPoint` 访问）。注意：UnknownMapPointOdds 在 `RunOddsSet` 上（非 PlayerOddsSet），仅包含 Monster/Elite/Treasure/Shop/Event 五种类型，无"祭坛"类型。
2. **MapPointPatch.cs 扩展** — 检测未访问 ? 节点的悬停；从当前局状态访问 `RunManager.Instance.RunState.Odds.UnknownMapPoint`；在悬停地图点附近显示 `UnknownRoomPanel`；取消悬停时隐藏。
3. **PotionOddsIndicator.cs** — 紧凑 Control：药水图标 TextureRect + Label 当前百分比。定位在战斗界面右上角。悬停时创建 InfoModPanel 显示未来 2/3/4/5 场累计概率。累计 P(N场至少1次) = 1 - ∏(1-p_i)，考虑怜悯变化。
4. **PotionOddsPatch.cs** — 战斗 UI 初始化 postfix，添加 PotionOddsIndicator 作为子节点。读取 `PotionRewardOdds.CurrentValue`（来自 `Player.PlayerOdds.PotionReward`）。
5. **CardDropOddsIndicator.cs** — 类似紧凑 Control：卡牌图标 + 稀有概率 Label。悬停展开为表格：行(稀有/非普通/普通) × 列(普通/精英)。读取 `CardRarityOdds.CurrentValue` 偏移 + 基础概率。
6. **CardDropOddsPatch.cs** — 战斗 UI 初始化 postfix，添加 CardDropOddsIndicator 在 PotionOddsIndicator 旁边。
7. **ShopPricePanel.cs** — InfoModPanel 子类，表格布局。计算价格范围：基础价格 ±5%(卡牌/药水)、±15%(遗物)。显示删牌费用(75+25×次数)。显示打折/无色说明。
8. **ShopPricePatch.cs** — 金币图标悬停或地图商店节点悬停 postfix，显示 ShopPricePanel。

**涉及文件**: `UnknownRoomPanel.cs` (新), `MapPointPatch.cs` (改), `PotionOddsIndicator.cs` (新), `PotionOddsPatch.cs` (新), `CardDropOddsIndicator.cs` (新), `CardDropOddsPatch.cs` (新), `ShopPricePanel.cs` (新), `ShopPricePatch.cs` (新)

**关键设计决策（问号房间）**：访问路径：`RunManager.Instance.RunState.Odds.UnknownMapPoint`（在 RunOddsSet 上，非 PlayerOddsSet）。公开属性 `MonsterOdds`、`EliteOdds`、`TreasureOdds`、`ShopOdds` 和计算得到的 `EventOdds` 提供所有数据。注意：`EliteOdds` 默认 -1（问号房间默认不可能），仅在 >0 时显示。MapPointType 枚举无"祭坛"类型，实际类型为 Monster/Elite/Treasure/Shop + 计算的 Event。

**关键设计决策（药水累计概率）**：累计概率计算需考虑怜悯系统的精确机制：
- 怜悯调整基于 `base.CurrentValue`（不含精英加成）：掉落后 -0.1，未掉落 +0.1
- 实际掉落判定：`random < currentValue + eliteBonus * 0.5`，其中 eliteBonus=0.25（精英房），有效精英加成 = 0.25 * 0.5 = 0.125
- "未来 N 场"计算：模拟前推假设全部未掉落，p_i = min(1, currentValue + i*0.1)（普通房）或 min(1, currentValue + i*0.1 + 0.125)（精英房）
- P(N场至少1次) = 1 - ∏(1-p_i)
- **注意**：怜悯调整和掉落判定使用不同阈值，精英加成仅影响掉落判定不影响怜悯值变化

**关键设计决策（卡牌掉落）**：`CardRarityOdds.CurrentValue` 是应用到稀有概率的偏移。实际稀有概率 = base_rare + offset。同时显示"当前"(含怜悯) 和"基础"值。偏移每次非稀有卡牌增加 `RarityGrowth`(0.01 或苦行 0.005)，稀有后重置 -0.05。访问路径：`Player.PlayerOdds.CardRarity`（在 PlayerOddsSet 上，非 RunOddsSet）。

**测试**：手动 — 悬停地图 ? 节点验证概率总和 ~100%；战斗右上角验证药水概率；验证卡牌掉落概率；验证商店价格

---

### Phase 5: 战斗增强 — 敌人意图状态机（预估 5-6 小时）

**依赖**：Phase 1（InfoModPanel）

**任务**：
1. **IntentStateMachinePanel.cs** — 复杂面板构建器：
   - 通过 `Creature.Monster.MoveStateMachine` 访问怪物状态机
   - 读取 `_currentState`（私有字段，用 Harmony Traverse 或反射）
   - 读取 `States` 字典（公开）枚举所有状态
   - 每个 MoveState：提取 `Intents` (IReadOnlyList<AbstractIntent>) 获取图标，`FollowUpState`/`FollowUpStateId` 获取简单转移
   - RandomBranchState：提取 `States` 列表 (List<StateWeight>)，计算归一化权重为百分比，提取 `maxTimes`/`repeatType` 用于约束标签
   - ConditionalBranchState：显示分支数量但无法显示条件（lambda）
   - 布局：当前状态(32×32 图标+伤害值，aqua 左边框高亮) → 金色箭头 → 分支框(子面板：概率% + 约束 + 意图图标 24×24 + 数值)
   - 循环指示：↩ 金色箭头（当 FollowUpState 指回分支状态时）
2. **IntentHoverPatch.cs** — `NIntent.OnHovered()` postfix：**注意 OnHovered/OnUnhovered 是 private 方法**，需使用 `[HarmonyPatch]` + `AccessTools.Method(typeof(NIntent), "OnHovered")` 显式定位，不能仅用属性标注。通过 Traverse 获取 `_owner` (Creature) 和 `_intent` (AbstractIntent)；构建 IntentStateMachinePanel；定位在意图节点附近；显示。`OnUnhovered()` postfix（同为 private）：隐藏/释放面板。备选方案：如果 private 方法 patch 不稳定，可改为 patch `MouseEntered`/`MouseExited` 信号连接。
3. **意图图标提取** — 使用 `AbstractIntent.GetAnimation()` 获取动画名，然后 `IntentAnimData.GetAnimationFrame()` 获取纹理路径。伤害值：转为 `AttackIntent` 并调用 `GetIntentLabel()`。
4. **降级处理** — 如果 `_currentState` 反射失败，显示用户可见的 "状态机数据不可用" 消息而非静默失败。

**涉及文件**: `IntentStateMachinePanel.cs` (新), `IntentHoverPatch.cs` (新)

**关键设计决策**：`_currentState` 字段是私有的。使用 `Traverse.Create(stateMachine).Field("_currentState").GetValue<MonsterState>()` 读取。安全：仅读不写。

**关键设计决策（权重）**：RandomBranchState.States 包含 `StateWeight` 结构体，有 `weightLambda` (Func<float>)。调用 `GetWeight()` 获取当前动态权重，归一化为百分比。`repeatType` 和 `maxTimes` 提供约束信息（CannotRepeat = "≤1"、CanRepeatXTimes = "≤N"、CanRepeatForever = 无标签、UseOnlyOnce = "1×"）。

**关键设计决策（条件分支）**：ConditionalBranchState 有 lambda 条件，运行时无法反编译。显示分支目标图标但不显示概率标签，用 "条件" 或 "?" 指示器标注。

**风险**：权重 lambda 可能在意外怪物状态下抛异常。所有权重评估包裹在 Safe.Run 中。

**测试**：手动 — 悬停简单怪物(Cultist)、随机分支怪物(Jaw Worm)、条件分支 Boss 的意图。验证当前状态高亮、概率总和正确、约束显示。

---

### Phase 6: 个人数据分析（预估 6-8 小时）

**依赖**：Phase 1（FeatureToggles, Localization, InfoModPanel），Phase 2（ContributionPersistence）

**任务**：
1. **RunHistoryAnalyzer.cs** — 后台加载器：
   - 调用 `SaveManager.GetAllRunHistoryNames()` 获取文件列表
   - 通过 `SaveManager.LoadRunHistory(name)` 批量加载 — 后台 Task 运行
   - 解析 RunHistory 字段：Win, KilledByEncounter, KilledByEvent, Ascension, Players[].Character, MapPointHistory
   - 聚合为 CareerStatsData：滚动胜率、死因排名、每 Act 路径平均值
   - 路径统计：遍历每 Act 的 MapPointHistory，计数 MapPointType 出现次数，累加 CardsGained/Removed/Bought/Upgraded
   - 先古遗物：检测 MapPointType.Ancient 条目，提取遗物选择
   - Boss 战损：检测 Boss 房间条目，提取 DamageTaken
   - 缓存结果；新局完成时失效
   - **不得阻塞主线程**：完全在 Task.Run() 中运行，通过回调在主线程交付结果
2. **CareerStatsData.cs** — 不可变数据模型，包含所有聚合字段
3. **CareerStatsSection.cs** — 注入统计页面的 UI 构建器：
   - 区块标题 "Stats the Spire" gold 14px
   - 胜率趋势：3 列(最近10/50/全部)，绿/红着色
   - 死因排行：VBoxContainer，每 Act 分组 Top 5
   - 路径统计：Grid，Act 列 × 指标行（全称标签："获取卡牌"/"购买卡牌"等）
   - 先古遗物下拉：NDropdownPositioner 或 OptionButton 列出先古之民，选择后显示遗物选择率
   - Boss 下拉：OptionButton 列出 Boss，选择后显示平均战损 + 死亡率
   - 悬停任一统计行：tooltip 显示社区对比数据（来自 StatsProvider，受 F9 筛选）
4. **CareerStatsPatch.cs** — 统计页面节点树注入点 postfix，在现有内容下方添加 CareerStatsSection
5. **RunHistoryStatsSection.cs** — Run History 详情 UI 构建器：
   - 按 Act 路径统计（与生涯相同指标但仅限单局）
   - 先古遗物选择显示
   - 每 Act Boss 战损
   - "查看贡献图表" NButton → 加载持久化贡献 JSON 显示 ContributionPanel
6. **RunHistoryPatch.cs** — `NRunHistory.SelectPlayer()` 或 `OnSubmenuOpened()` postfix：在现有内容下方注入 RunHistoryStatsSection
7. **ContributionPersistence.cs** — 战斗结束时（在 CombatLifecyclePatch 中），序列化当前战斗贡献到 `{DataDir}/contributions/{runId}_{floor}.json`。局结束时写入汇总 `{runId}_summary.json`。RunHistoryStatsSection 加载这些用于回放。
8. **FilterPanel "我的数据"复选框** — 新增 CheckBox "仅显示我的数据"；选中时 StatsProvider 切换到 RunHistoryAnalyzer 聚合数据替代服务器数据

**涉及文件**: `RunHistoryAnalyzer.cs` (新), `CareerStatsData.cs` (新), `CareerStatsSection.cs` (新), `CareerStatsPatch.cs` (新), `RunHistoryStatsSection.cs` (新), `RunHistoryPatch.cs` (新), `ContributionPersistence.cs` (新), `FilterPanel.cs` (改), `FilterSettings.cs` (改), `CombatLifecyclePatch.cs` (改), `RunLifecyclePatch.cs` (改)

**关键设计决策（后台加载）**：`RunHistoryAnalyzer.LoadAllAsync()` 在 `Task.Run()` 中运行避免阻塞。使用信号量防止并发加载。结果通过 `CareerStatsLoaded` 事件交付给 UI 订阅者。加载中显示进度指示器。

**关键设计决策（数据量）**：500+ 局的玩家可能有大量数据。增量加载并缓存。每次新局结束时，增量更新缓存的 CareerStatsData 而非完全重载。

**测试**：手动 — 验证生涯统计区块出现在统计页面且数据正确；验证 Run History 显示单局统计；验证"查看贡献"按钮打开持久化数据；验证"我的数据"复选框切换数据源

---

### Phase 7: PRD-00 补全（预估 3-4 小时）

**依赖**：Phase 2（面板改动已就绪）

**任务**：
1. **多修正器比例缩放(4.1)** — 在 `CombatTracker.OnCombatEnd()` 或伤害归因路径中：计算所有修正器贡献后，检查总和是否 > (total - base)。若是，每个修正器按 `(total - base) / sum` 缩放。实现为 `CombatTracker.ApplyModifierScaling()`。
2. **Confused/SneckoEye 能量追踪(4.2)** — 在费用修改 patch 中：当卡牌实际使用费用因 Confused 效果不同于原始费用时，将能量差归属到 Confused 来源实体（SneckoEye 遗物、SneckoEye??? 遗物、SneckoOil 药水）。可为负值。
3. **Enlightenment 部分降费(4.3)** — 类似上述：检测 Enlightenment 降低卡牌费用时，计算差值，归属到 Enlightenment 卡牌。
4. **离线队列限制(4.6)** — `OfflineQueue.Enqueue()` 中：写入后修剪超过 10 条（删最旧）。`OfflineQueue.DrainAsync()` 中：跳过超过 7 天的文件。永不在上传载荷中包含 Steam ID（验证 `RunDataCollector`）。
5. **多人基础兼容(3.15)** — 审计所有 patches：确保引用本地玩家。添加 `RunManager.Instance?.LocalPlayer` 空检查。多人模式测试不崩溃。
6. **Buffer 防御计算验证(4.4)** — 对照 PRD 4.4 示例验证 `CombatTracker.cs` 中 Buffer 防御计算的正确性：5 点伤害 ×4 次，2 层 Buffer + 7 点格挡 → Buffer 贡献 = 8 点。若现有代码已正确处理，记录为"已验证"；若有偏差则修复。添加对应测试用例到 sts2_contrib_tests。

**涉及文件**: `CombatTracker.cs` (改/验证), `OfflineQueue.cs` (改), `RunDataCollector.cs` (改 — 验证无 Steam ID), 各 patches (审计)

**测试**：自动化单元测试验证修正器缩放（2/3 修正器场景）；手动测试多人不崩溃

---

### Phase 8: 功能开关 UI + 收尾（预估 2-3 小时）

**依赖**：Phase 1（FeatureToggles），所有其他阶段完成

**任务**：
1. **FilterPanel 开关区** — FilterPanel 中新增 ScrollContainer 包含 11 个 CheckBox 行，绑定 FeatureToggles 属性。应用时保存。
2. **集成测试** — 逐个关闭功能，验证对应 UI 消失、patch 短路。
3. **UI 样式检查** — 验证所有颜色匹配 UI_STYLE_GUIDE.md，所有百分比 1 位小数，所有字号匹配规范(16/14/12/11)，所有面板使用正确 StyleBoxFlat 参数。
4. **性能检查** — 全功能启用下 profile；确保无可感知延迟。

---

## 6. 关键设计决策

### 6.1 UI 注入策略

所有游戏 UI 注入使用 **Harmony Postfix** 在目标节点的生命周期方法上（_Ready, OnFocus, OnHovered, InitGrid 等）。永不修改游戏场景树文件。注入的节点通过 `AddChild()` / `CallDeferred()` 添加为子节点，并打上 metadata 标记用于后续识别和清理。

**模式**：
```csharp
[HarmonyPostfix]
public static void AfterSomeMethod(SomeGameNode __instance)
{
    if (!ModConfig.Toggles.SomeFeature) return;
    Safe.Run(() =>
    {
        // 检查是否已注入
        if (__instance.HasMeta("stats_injected")) return;
        __instance.SetMeta("stats_injected", true);
        
        // 创建并添加 UI
        var panel = CreateStatsUI();
        __instance.CallDeferred(Node.MethodName.AddChild, panel);
    });
}
```

### 6.2 读取 MonsterMoveStateMachine 状态

`_currentState` 字段是私有的。通过以下方式访问：
```csharp
var sm = creature.Monster.MoveStateMachine;
var currentState = Traverse.Create(sm).Field("_currentState").GetValue<MonsterState>();
```

RandomBranchState 权重：调用 `stateWeight.GetWeight()` 包裹在 Safe.Run 中（lambda 可能引用已释放的状态）。归一化：`percentage = weight / totalWeight * 100`。

意图图标：从 MoveState.Intents 遍历到 AbstractIntent，然后使用游戏现有的动画/纹理系统。

### 6.3 贡献数据持久化用于 Run History

每场战斗结束时，序列化 `CombatTracker.LastCombatData` (Dictionary<string, ContributionAccum>) 到 JSON：
```
{DataDir}/contributions/{runSeed}_{floor}.json
```

局结束时，序列化 `RunContributionAggregator.RunTotals` 到：
```
{DataDir}/contributions/{runSeed}_summary.json
```

RunHistoryStatsSection 加载匹配显示局的 seed 的汇总文件。"查看贡献"按钮加载每场战斗文件并在 ContributionPanel 中显示。

**清理**：超过 90 天的贡献文件在 mod 初始化时修剪。

### 6.4 RunHistory 聚合不阻塞主线程

```csharp
public async Task<CareerStatsData> LoadAllAsync(string? characterFilter)
{
    return await Task.Run(() =>
    {
        var names = SaveManager.GetAllRunHistoryNames();
        var stats = new CareerStatsBuilder();
        foreach (var name in names)
        {
            var history = SaveManager.LoadRunHistory(name);
            if (characterFilter != null && !history.Players.Any(p => p.Character == characterFilter))
                continue;
            stats.AddRun(history);
        }
        return stats.Build();
    });
}
```

结果缓存。增量更新：新局结束时，将单个新局添加到缓存构建器并重建。

### 6.5 面板避让逻辑

```csharp
// ContributionPanel.ShowCombatResult() 中：
var rewardsScreen = GetTree().Root.FindChild("NRewardsScreen", recursive: true);
if (rewardsScreen?.Visible == true)
{
    // 面板向左偏移避免重叠
    OffsetLeft = -720;
    OffsetRight = -210;
}
else
{
    // 恢复到保存/默认位置
    RestorePosition();
}
```

---

## 7. 风险评估

| 风险 | 严重度 | 可能性 | 缓解措施 |
|------|--------|--------|---------|
| **MonsterMoveStateMachine 私有字段访问在游戏更新后失效** | 高 | 中 | 使用 Traverse + null fallback；反射失败时显示"不可用"消息 |
| **RunHistory 加载 500+ 局时造成卡顿** | 中 | 高 | 后台 Task.Run()；增量缓存；显示加载指示器 |
| **RandomBranchState 权重 lambda 运行时抛异常** | 中 | 中 | 每个 GetWeight() 包裹 Safe.Run，fallback weight=1.0 |
| **Harmony patch 参数名在游戏更新后不匹配** | 高 | 低 | 对照编译后 DLL 验证参数名（非反编译源码）；记录 DLL 版本 |
| **异步 Harmony postfix 在首个 await 处触发而非方法完成** | 高 | 已知 | 所有新 patch 优先使用同步 postfix；需要异步时使用独立 async task |
| **NIntent.OnHovered 是 private 方法** | 中 | 中 | 已确认为 private。需使用 AccessTools.Method 显式定位；Phase 0 Spike 验证；备选方案：patch MouseEntered 信号连接 |
| **功能开关初始化竞态条件** | 低 | 低 | 在 patching 之前同步加载开关 |
| **贡献持久化磁盘空间** | 低 | 中 | 修剪 > 90 天文件；上限 ~1000 文件 |
| **卡牌图书馆 grid 节点结构变化** | 中 | 中 | 防御性节点遍历 + null 检查；优雅降级 |

---

## 8. 测试策略

### 8.1 回归测试

**现有 36 个测试**（sts2_contrib_tests mod）：每个阶段前后运行。覆盖：
- 直接伤害(6)、修正拆分(6)、防御归因(8)、间接伤害(3)、来源优先级(2)、跨角色(4)、NEW 功能(4)、一致性(3)

**4.1 新测试（修正器缩放）**：在 sts2_contrib_tests 中添加 2-3 个测试用例：
- 双修正器：易伤 ×1.5 + 双倍 ×2 → 验证 DirectDmg + ModifierDmg = TotalDmg
- 三修正器：同约束
- 单修正器：无需缩放

**4.4 新测试（Buffer 防御）**：验证 PRD 4.4 示例场景：
- 5 点伤害 ×4 次，2 层 Buffer + 7 点格挡 → Buffer 防御贡献 = 8 点

### 8.2 新增自动化测试

| 功能 | 测试 | 可自动化？ |
|------|------|:---:|
| 修正器缩放(4.1) | 单元测试：输入场景 → 验证比例输出 | 是 |
| FeatureToggles 持久化 | 单元测试：序列化 → 反序列化往返 | 是 |
| RunHistoryAnalyzer | 单元测试：mock RunHistory 数据 → 验证生涯统计 | 是(mock SaveManager) |
| ContributionPersistence | 单元测试：序列化 → 反序列化往返 | 是 |
| 累计药水概率 | 单元测试：数学验证 | 是 |
| 卡牌掉落概率计算 | 单元测试：偏移+基础 → 正确百分比 | 是 |

### 8.3 手动测试清单

| 阶段 | 测试场景 |
|------|---------|
| 1 | Mod 加载；开关持久化；InfoModPanel 渲染 |
| 2 | 面板在右侧；DPS 显示；实时刷新；拖拽可用；避让奖励 |
| 3 | 卡牌图书馆统计；遗物 tooltip delta；事件仅选择率 |
| 4 | ? 房间概率；药水概率指示器；卡牌掉落概率；商店价格 |
| 5 | 意图状态机：Cultist(简单)、Jaw Worm(随机)、Boss(条件) |
| 6 | 生涯统计区块；Run History 统计；贡献回放；"我的数据"开关 |
| 7 | 多修正器缩放；Confused 能量；离线队列限制 |
| 8 | 11 个开关逐个开/关；UI 样式一致性审计 |

### 8.4 多人测试

- 多人模式下加载 mod
- 验证不崩溃
- 验证本地玩家贡献被追踪（非队友）
- 验证 UI 面板仅为本地玩家显示

---

## 附录：文件统计

- **新增文件**：23
- **修改文件**：18（含 RunDataCollector.cs 审计）
- **总涉及文件**：41
- **预估总工时**：29-39 小时，分 9 个阶段（含 Phase 0 Spike）

---

## 附录：Review 历史

### Round 1 Review（v1.0 → v2.0）

**评估**：PASS WITH ISSUES

**Critical 修复**（已应用 v2.0）：
1. ✅ PRD 4.4 Buffer 防御计算遗漏 → 添加到 Phase 7 task 6 + 测试用例
2. ✅ UnknownMapPointOdds 访问路径错误 → 修正为 `RunState.Odds.UnknownMapPoint`（RunOddsSet 而非 PlayerOddsSet）
3. ✅ 药水精英加成数学表述 → 明确 eliteBonus(0.25) * 0.5 = 0.125 有效加成

**Major 修复**（已应用 v2.0）：
4. ✅ NIntent.OnHovered 是 private → 添加 AccessTools 语法说明 + 备选方案
5. ✅ NCardLibraryStats 冲突 → 创建独立 NModCardLibraryStats 节点
6. ✅ 卡牌大图注入点 → 改为卡牌预览回调（非 InitGrid）
7. ✅ 药水累计概率计算 → 精确建模怜悯与精英加成的分离阈值

**Minor 修复**（已应用 v2.0）：
8. ✅ "祭坛"类型不存在 → 修正为 Monster/Elite/Treasure/Shop/Event
9. ✅ Phase 0 Spike 建议 → 新增 Phase 0 技术验证
10. ✅ Debounce 精度 → 改用 Stopwatch.GetTimestamp()
11. ✅ 文件计数修正 → 17 修改文件

### Round 2 Review（v2.0 → v3.0）

**评估**：PASS WITH ISSUES（10/11 Round 1 修复已验证）

**Minor 修复**（已应用 v3.0）：
1. ✅ PRD 3.8 "祭坛"→ 改为"怪物/精英"（PRD 和 Plan 同步修正）
2. ✅ 修改文件计数 17→18（含 RunDataCollector.cs 审计）
3. ✅ CardLibraryPatch 文件表从 InitGrid 改为卡牌大图回调
4. ✅ PRD 3.2 注入位置从 NCardLibraryStats 改为 NModCardLibraryStats
