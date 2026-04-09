# 第二轮迭代 — 进度与上下文

> 最后更新：2026-04-09（Phase 5 完成，Phase 6 起暂停 — 跨设备交接）
> 此文件供跨设备会话使用，确保新会话能完整理解当前状态

---

## 当前位置

**工作流阶段**：Stage 4 实施进行中 — Phase 0-5 完成，Phase 6 待开始

**下次开机第一件事**：阅读本文件 §"跨设备交接快照"，然后从 Phase 6 任务 1 开始。

**核心文档**：
- `PRD_04_ITERATION2.md` — 第二轮迭代完整需求文档（v3.0，含 UI 设计规范+截图参考）
- `PLAN_ITERATION2.md` — **第二轮迭代实施计划（v3.0，3 轮 Review 通过）**
- `UI_STYLE_GUIDE.md` — STS2 UI 设计规范（v1.0，从反编译代码提取）
- `PRD_00_MASTER.md` — 总体需求文档（含第一轮已实现的 *NEW* 标记）
- `WORKFLOW.md` — 文档驱动工作流定义（六阶段）
- `PLAN_NEW_FEATURES.md` — 第一轮 NEW 功能实施计划（已完成）
- `PLAN_COMBAT_STATS.md` — 第一轮战斗统计实施计划（已完成）

---

## 第一轮迭代成果（已完成）

- 战斗贡献统计核心：36/36 自动化测试通过（sts2_contrib_tests mod）
- 测试覆盖：直接伤害(6)、修正拆分(6)、防御归因(8)、间接伤害(3)、来源优先级(2)、跨角色(4)、NEW功能(4)、一致性(3)
- NEW 功能已实现：免费能量/辉星归因（两层架构 source tag + play-time 比较）、最大 HP 治疗（GainMaxHp patch + heal 抑制 flag）、Stars 获得追踪（PlayerCmd.GainStars patch）
- 关键修复：block pool 回合同步（AfterBlockCleared）、Intangible/Colossus 在 ModifyDamage 阶段追踪、GetPowerSource 全局 fallback、debug 日志已清除

---

## 第二轮迭代需求概要（PRD-04）

### 15 个功能需求

| # | 功能 | 复杂度 |
|---|------|:---:|
| 3.1 | Mod 重命名 → "Stats the Spire" | 低 |
| 3.2 | Compendium 卡牌图书馆：选取率+胜率 | 中 |
| 3.3 | Compendium 遗物收藏：胜率+胜率浮动 | 中 |
| 3.4 | 遗物胜率浮动（全局所有 tooltip） | 低 |
| 3.5 | 事件选项：仅显示选择率 | 低 |
| 3.6 | 实时贡献更新（每次打牌后刷新） | 中 |
| 3.7 | 每回合平均伤害 | 低 |
| 3.8 | 问号房间遭遇概率（地图悬停） | 中 |
| 3.9 | 药水掉落概率（屏幕顶部边角） | 中 |
| 3.10 | 敌人意图状态机（悬停弹出面板） | 高 |
| 3.11 | 个人生涯统计（注入百科大全→角色数据→统计栏） | 高 |
| 3.12 | Run History 增强（本局统计+贡献回看按钮） | 高 |
| 3.13 | 筛选面板"我的数据"（本地 RunHistory 聚合） | 中 |
| 3.14 | 功能开关（每个子功能独立开关） | 中 |
| 3.15 | 多人游戏基础兼容 | 低 |

### §4 PRD-00 补全项（6 条）
- 4.1 多乘法修正器超算：按比例缩放，约束 Direct+Modifier=Total
- 4.2 Confused/SneckoEye：计入能量贡献，可为负（3 个来源实体）
- 4.3 Enlightenment 部分降费也追踪
- 4.4 Buffer 防御计算补充数值示例
- 4.5 贡献面板 UI：左侧、可拖拽、子来源自动展开可收起
- 4.6 离线队列限制 10 条/7 天、不上传 Steam ID

---

## 第一阶段探索成果（Stage 1 发散需求已完成）

### Compendium 钩子
- 卡牌图书馆：`NCardLibraryGrid.InitGrid()` postfix + `AssignCardsToRow()` postfix
- 遗物收藏：`NRelicCollectionEntry.OnFocus()` postfix
- 现有 StatsLabel + meta 标记模式可复用

### 问号房间概率
- `UnknownMapPointOdds` 有公开属性 MonsterOdds/EliteOdds/TreasureOdds/ShopOdds/EventOdds
- 基础：事件 85%、怪物 10%、宝箱 2%、商店 3%，怜悯机制动态调整
- 现有 MapPointOverlay + MapPointPatch 基础设施

### 敌人意图状态机
- MonsterMoveStateMachine：States(public)、_currentState(private)、StateLog(public)
- 状态类型：MoveState(叶子)、RandomBranchState(加权随机)、ConditionalBranchState(条件)
- NIntent.OnHovered() 已有 hover tip 机制，可扩展
- UI 参考：STS1 mod 截图（Cultist 简单循环、Jaw Worm 随机分支+概率+约束）

### 药水掉落概率
- PotionRewardOdds.CurrentValue（公开属性）
- 基础 40%，怜悯 ±10%/次，精英 +12.5%
- UI：屏幕顶部边角药水图标+百分比，悬停展开未来 1/2/3 场累计概率

### RunHistory 数据结构
- SaveManager.GetAllRunHistoryNames() + LoadRunHistory() 公开 API
- RunHistory 字段：Win, KilledByEncounter/Event, Ascension, Character, MapPointHistory
- PlayerMapPointHistoryEntry：CardsGained, CardsRemoved, RelicsBought, DamageTaken 等
- MapPointType 含 Ancient 类型，可统计先古遗物选择

---

## Stage 2 完成：UI 设计 ✅

- `UI_STYLE_GUIDE.md` v1.0 — 从反编译代码提取的 STS2 UI 设计规范（9 章节）
- `UI_design_reference/` — 8 张参考截图 + README 索引
- PRD-04 v3.0 — 所有 17 个功能均含 ASCII mockup + UI 规范引用
- 新增 §3.16 商店价格显示 + §3.17 卡牌掉落概率（基于截图新需求）

---

## Stage 3 完成：Plan + Review 3 轮循环 ✅

### Round 1（v1.0）
- **Plan Agent**：产出完整实施计划（8 阶段、23 新文件、16 修改文件）
- **Review Agent**：评估 PASS WITH ISSUES
  - Critical(3): PRD 4.4 遗漏、UnknownMapPointOdds 路径错误、药水精英加成数学误导
  - Major(4): NIntent private 方法、NCardLibraryStats 冲突、卡牌大图注入点、药水累计概率简化
  - Minor(4): 祭坛类型不存在、缺少 Phase 0 Spike、Debounce 精度、文件计数

### Round 2（v1.0 → v2.0）
- **Plan Agent**：修复全部 3 Critical + 4 Major + 4 Minor
  - 新增 Phase 0 技术验证 Spike
  - 修正 API 访问路径（RunOddsSet vs PlayerOddsSet）
  - 精确建模药水怜悯/精英分离机制
  - 创建独立 NModCardLibraryStats 避免冲突
- **Review Agent**：评估 PASS WITH ISSUES（10/11 修复验证）
  - Minor(4): PRD 3.8 祭坛残留、文件计数 17→18、文件表不一致、PRD 3.2 注入点残留

### Round 3（v2.0 → v3.0 最终版）
- **Plan Agent**：修复全部 4 Minor（PRD + Plan 同步更新）
- **Review Agent**：评估 **PASS**（4/4 修复验证，无新问题）
- **最终结论**：Plan v3.0 可进入实施

---

## 下一步操作

```
1. 阅读 PLAN_ITERATION2.md v3.0（实施计划）
2. 开始 Stage 4 实施：
   a. Phase 0: 技术验证 Spike（NIntent patch、NCardLibraryGrid 注入、RunHistory 性能）
   b. Phase 1: 基础设施（FeatureToggles、InfoModPanel、DraggablePanel）
   c. Phase 2-8: 按依赖顺序实施
3. 每个阶段完成后运行 36 个回归测试
4. Stage 5: 功能测试 → Stage 6: 人工 Review
```

---

## 跨设备交接快照（2026-04-09）

### 已完成 Phase（0-5）

**Phase 0 — 技术验证 Spike**：通过反射确认所有 API 路径可用。

**Phase 1 — 基础设施**：
- `Config/FeatureToggles.cs` — 11 个开关属性 + Get/Set/Map
- `Config/Localization.cs` — EN/CN 字典 + L.Get(key) helper（已加入 §3.8/3.9/3.10/3.16/3.17 全部 keys + stats.event_pick）
- `UI/InfoModPanel.cs` — InfoMod 风格基类 PanelContainer：Create(title, subtitle), AddRow, AddSeparator, AddLabel, AddCustom
- `UI/DraggablePanel.cs` — 可拖拽 + 位置持久化基类
- `Api/StatsProvider.cs` 扩展：GetCardStats / GetRelicStats / GetEventOptionStats / GetEncounterStats / GetGlobalAverageRelicWinRate

**Phase 2 — 战斗面板改造**：贡献面板重设计（位置、避让、实时刷新、每回合伤害行）

**Phase 3 — 全局 Tooltip 注入**：
- `Patches/CardLibraryPatch.cs` — `NInspectCardScreen.UpdateCardDisplay` postfix；通过 Traverse 读 `_cards[_index]` + `_hoverTipRect`，注入 VBoxContainer 显示选取/胜/购买/升级/删除率
- `Patches/RelicCollectionPatch.cs` — `NRelicCollectionEntry.OnFocus/OnUnfocus` postfix；ModelVisibility 来自 `MegaCrit.Sts2.Core.Entities.UI`
- `Patches/RelicHoverPatch.cs` 修改：使用 `StatsLabel.ForRelicStatsWithDelta(stats, globalAvg)`，加 RelicStats toggle short-circuit
- `Patches/EventOptionPatch.cs` 修改：仅显示选择率（移除胜率），加 EventPickRate toggle
- `UI/StatsLabel.cs` 扩展：ForRelicStatsWithDelta + ForEventOption（使用 `stats.event_pick` key, F1 精度）

**Phase 4 — 概率显示**：
- `UI/UnknownRoomPanel.cs` — Create(UnknownMapPointOdds odds)；按 Event/Monster/Elite>0/Treasure/Shop 排序
- `UI/PotionOddsIndicator.cs` — Control 部件，icon+%标签，hover 展开 2-5 场累计概率；CumulativeOdds(base, n, anyElite) miss-path 模拟
- `UI/CardDropOddsIndicator.cs` — Control 部件，hover 展开 Regular×Elite 稀有度表；硬编码 base 0.03/0.37/0.10/0.40，UpdateOffset 应用 CardRarityOdds 怜悯
- `UI/ShopPricePanel.cs` — Cards 50/75/150 ±5%, Relics 200/250/300 ±15%, Potions 50/75/100 ±5%, Removal=75+25×used
- `Patches/CombatUiOverlayPatch.cs` — `NCombatUi.Activate(CombatState state)` postfix；通过 LocalContext.GetMe(state) 拿 Player；右上角注入两指示器；读 `me.PlayerOdds.PotionReward.CurrentValue` 和 `me.PlayerOdds.CardRarity.CurrentValue`
- `Patches/MapPointPatch.cs` 扩展：加 MonsterDanger toggle；新增 `NMapPoint.OnFocus/OnUnfocus` private 方法 postfix；ShowHoverPanel 根据 PointType 派发到 UnknownRoomPanel 或 ShopPricePanel；仅在 State != Traveled 显示；GetShopRemovalsUsed() 通过 Traverse 访问 `ExtraFields.CardShopRemovalsUsed`

**Phase 5 — 意图状态机**：
- `UI/IntentStateMachinePanel.cs` — `Create(Creature owner)` 静态方法；通过 `Traverse.Create(sm).Field("_currentState").GetValue<MonsterState>()` 读私有字段；遍历 `sm.States`；DescribeMoveState（拼接 IntentType.ToString()）+ DescribeRandomBranchState（归一化 GetWeight() + maxTimes 约束）；ConditionalBranchState 跳过（无访问入口）
- `Patches/IntentHoverPatch.cs` — `NIntent.OnHovered/OnUnhovered`（均为 private，用 string form `[HarmonyPatch(typeof(NIntent), "OnHovered")]`）postfix；通过 Traverse 读 `_owner` (Creature)；锚定到 intent 节点上方 -160px

### Phase 6 起未开始（待续）

**已完成的 API 调研**（保存在此供下次使用，避免重复 spike）：

```
SaveManager: MegaCrit.Sts2.Core.Saves.SaveManager
  - GetAllRunHistoryNames(): List<string>
  - LoadRunHistory(string fileName): ReadSaveResult<RunHistory>

RunHistory: MegaCrit.Sts2.Core.Runs.RunHistory（properties，非字段）
  - bool Win, ModelId KilledByEncounter, ModelId KilledByEvent
  - int Ascension, string Seed, long StartTime
  - List<RunHistoryPlayer> Players
  - List<List<MapPointHistoryEntry>> MapPointHistory  ← 外层为 Act
  - List<ModelId> Acts

MapPointHistoryEntry: MegaCrit.Sts2.Core.Runs.History
  - MapPointType MapPointType
  - List<MapPointRoomHistoryEntry> Rooms
  - List<PlayerMapPointHistoryEntry> PlayerStats

PlayerMapPointHistoryEntry: MegaCrit.Sts2.Core.Runs
  - int DamageTaken, int CurrentHp, int MaxHp, int CurrentGold
  - List<SerializableCard> CardsGained / CardsRemoved
  - List<ModelId> RelicsRemoved

NRunHistory: MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NRunHistory
  - private void SelectPlayer(NRunHistoryPlayerIcon)  ← postfix 候选
  - private void LoadGoldHpAndPotionInfo(NRunHistoryPlayerIcon)
  - OnSubmenuOpened() （来自 NSubmenu）

NStatsScreen: MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen.NStatsScreen
  - public void OnSubmenuOpened() → 调用 OpenStatsMenu()
  - 委托至 NGeneralStatsGrid.LoadStats()
NGeneralStatsGrid: 同命名空间
  - void LoadStats() ← 注入候选
  - 字段 Node _gridContainer / Control _characterStatContainer
```

**Phase 6 待办清单（按 PLAN_ITERATION2.md §Phase 6）**：
1. `Collection/RunHistoryAnalyzer.cs` — Task.Run 后台批量加载 RunHistory，聚合为 CareerStatsData；增量缓存
2. `Collection/CareerStatsData.cs` — 不可变数据模型
3. `UI/CareerStatsSection.cs` — Stats 页面注入区块（胜率趋势 / 死因 Top5 / 路径每Act平均 / 先古下拉 / Boss 下拉）
4. `Patches/CareerStatsPatch.cs` — `NGeneralStatsGrid.LoadStats()` postfix
5. `UI/RunHistoryStatsSection.cs` — 单局统计 + "查看贡献"按钮
6. `Patches/RunHistoryPatch.cs` — `NRunHistory.SelectPlayer` 或 `LoadGoldHpAndPotionInfo` postfix
7. `Util/ContributionPersistence.cs` — 战斗结束写入 `{DataDir}/contributions/{seed}_{floor}.json`，局结束写入 `{seed}_summary.json`；90天清理；改 `Patches/CombatLifecyclePatch.cs` 和 `Patches/RunLifecyclePatch.cs` 调用持久化
8. FilterPanel 加 "仅显示我的数据" CheckBox；StatsProvider 在该模式下从 RunHistoryAnalyzer 取数据

### Phase 7-8 概要
- **Phase 7** = PRD-00 §4 修复：4.1 修正器比例缩放、4.2 Confused 能量负贡献、4.3 Enlightenment 部分降费、4.4 Buffer 计算验证（写 sts2_contrib_tests）、4.6 离线队列 10条/7天限制 + 验证不上传 Steam ID、3.15 多人 LocalPlayer null 检查
- **Phase 8** = FilterPanel 11 个 toggle CheckBox、集成测试（逐个关闭验证短路）、UI 样式审计、性能 profile

### 关键技术备注（避免重复踩坑）

1. **环境构建错误**：dotnet build 当前因 SDK 10.0 / 缺少 reference 报 ~975 个 CS0246/CS0103，**全部为环境噪声**，非逻辑错误。验证方法：
   ```bash
   dotnet build 2>&1 | grep -E "error CS(0029|0019|1061|0117|1503|0266|0411)"
   ```
   仅这些类型才算真错。CS0246/CS0103 一律忽略。

2. **私有方法 Harmony patch**：用字符串形式 `[HarmonyPatch(typeof(X), "MethodName")]`，不要用 nameof。

3. **私有字段读取**：统一用 `Traverse.Create(obj).Field("_x").GetValue<T>()`，包 try/catch + Safe.Warn。

4. **F1 精度**：所有百分比小数位 1 位（PRD AC-15）。

5. **toggle 短路**：每个 patch 第一行 `if (!ModConfig.Toggles.X) return;` — 零开销禁用。

6. **meta 标记**：UI 注入幂等用 `HasMeta/SetMeta/RemoveMeta`，每个 patch 独立 meta key。

7. **异步 patch 陷阱**：Harmony postfix 在 async 方法的第一个 await 处触发，不是方法完成。新功能优先同步 postfix。

8. **NCombatUi.Activate**：参数为 `CombatState state`，不是 Player。用 `LocalContext.GetMe(state)` 拿 Player。

9. **未提交状态**：所有 Phase 3/4/5 改动尚未 git commit。下台机器拉代码前需先在本台 commit & push，或反过来。

### 下次上手命令（Phase 6 起步）

```bash
# 1. 阅读本文件 + PLAN_ITERATION2.md §Phase 6
# 2. 阅读 mods/sts2_community_stats/src/Collection/CombatTracker.cs（参考 Collection 文件夹模式）
# 3. 创建 RunHistoryAnalyzer.cs，参考上方 SaveManager API
# 4. 在 _decompiled/sts2/MegaCrit.Sts2.Core.Saves/SaveManager.cs 验证 GetAllRunHistoryNames 签名
# 5. 写 CareerStatsData → CareerStatsSection → CareerStatsPatch
# 6. 增量编译验证（用 grep 过滤逻辑错误）
```
