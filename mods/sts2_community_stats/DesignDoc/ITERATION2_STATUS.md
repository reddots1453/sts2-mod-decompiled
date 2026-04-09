# 第二轮迭代 — 进度与上下文

> 最后更新：2026-04-10（Round 8 人工验收完成，等待新一轮反馈）
> 此文件供跨设备会话使用，确保新会话能完整理解当前状态

---

## 当前位置

**工作流阶段**：Stage 5 人工验收测试循环中（已完成 8 轮反馈）

**下次开机第一件事**：
1. 阅读本文件 §"跨设备交接快照"
2. 阅读 `PRD_04_ITERATION2.md` 末尾的 v3.1 round X 注释，了解每轮变更
3. 等待用户提供下一轮人工测试反馈

**核心文档**：
- `PRD_04_ITERATION2.md` — **第二轮迭代完整需求**（v3.1，含所有 round 4-8 reflowed 注释）
- `PLAN_ITERATION2.md` — 第二轮迭代实施计划（v3.0，已被 round 4-8 修改部分覆盖）
- `CONTRIBUTION_CATALOG.md` — 贡献来源大全（17 章节 ~620 行，由后台 agent 生成）
- `UI_STYLE_GUIDE.md` — STS2 UI 设计规范
- `UI_design_reference/` — 8 张参考截图 + 5 张 round 4-8 反馈截图
- `WORKFLOW.md` — 文档驱动工作流定义

---

## 工作流规则（round 5 起强制）

> **任何需求变更必须先改 PRD，再写代码**。
> 用户在 round 5 反馈中明确：
> "在前面的修改中，有部分修改变更了需求，需要修改 PRD 文档以保持一致性；
>  后续如果有类似情况，请务必先修改 PRD 文档再修改代码。"

每轮人工反馈处理流程：
1. 调研：定位根因 + 寻找需要的游戏 API
2. **PRD 更新先行**（在对应 §X.Y 区块加 v3.1 round N 注释）
3. 代码实现
4. `dotnet build` 必须 "Build succeeded." + DLL mtime 新于源文件
5. 报告变更总览给用户
6. 用户人工测试 → 下一轮反馈

---

## 完整时间线

### 第一轮迭代（已完成）
- 战斗贡献统计核心：36/36 自动化测试通过（sts2_contrib_tests mod）
- NEW 功能：免费能量/辉星归因、最大 HP 治疗、Stars 获得追踪

### Phase 0-5 实施（2026-04-09 之前）
完成顺序：技术 spike → FeatureToggles + Localization + InfoModPanel + DraggablePanel → 战斗面板改造 → Compendium 注入 → 概率显示 → 意图状态机 panel v1。

### Phase 6 — 个人生涯统计 + Run History 增强（2026-04-09）
- `Collection/RunHistoryAnalyzer.cs` — Task.Run 后台聚合
- `Collection/CareerStatsData.cs` — 数据模型
- `UI/CareerStatsSection.cs` — Stats screen 区块
- `Patches/CareerStatsPatch.cs` — `NGeneralStatsGrid.LoadStats` postfix
- `UI/RunHistoryStatsSection.cs` — 单局统计 + 查看贡献按钮
- `Patches/RunHistoryPatch.cs` — `NRunHistory.SelectPlayer` postfix
- `Util/ContributionPersistence.cs` — `_combat.json` / `_summary.json`

### Phase 7 — PRD §4 修复 + Phase 8 — 功能开关 UI（2026-04-09）
- §4.1 多 modifier 超算缩放（CombatTracker.OnDamageDealt 末尾 scale block）
- §4.2 Confused/SneckoEye 能量贡献（ConfusedSourceTagPatch + EnergyCostSetterTagPatch）
- §4.3 Enlightenment 部分降费（自动覆盖 by §4.2）
- §4.4 Buffer 防御按 hit 累加 + DEF5b PRD 示例测试
- §4.5 离线队列 10/7 限制 + 验证不上传 Steam ID
- §4.6 多人 LocalPlayer null 检查
- FilterPanel 11 个 toggle CheckBox

### Round 4 人工反馈（2026-04-09）
> 首次发现 **build 失败**：CS0246/CS0103 之前被误判为环境噪音，实际是 mod 源文件缺 using。
> 教训保存到 memory：[feedback_build_verification.md](C:/Users/wilson/.claude/projects/.../memory/feedback_build_verification.md)
> 规则：**永远看 `Build succeeded.` 行 + DLL mtime**，不要按错误代码过滤

修复：
- RealTimeCombatPatch.cs 缺 `using MegaCrit.Sts2.Core.Combat.History;`
- IntentStateMachinePanel.cs 错用 `MonsterModel.Name`（应为 `Title.GetFormattedText()`）
- CareerStatsSection.cs 错用 `MethodName.Rebuild`（应为 `nameof(Rebuild)`）
- CombatUiOverlayPatch.cs 缺 `using MegaCrit.Sts2.Core.Context;`

之后处理 round 4 真正反馈：
- 图1 CardLibrary stats 被关键词 tooltip 遮挡 → anchor 右下角
- 图2 个人统计加载慢 → CareerStatsCache 磁盘缓存
- 图3 RunHistory 注入位置错 → ScrollContainer 右侧浮动栏
- 图4 顶栏指示器看不到 → LocalContext.GetMe(state) 单人 NetId null fallback
- 图5 死因/Boss/Ancient 显示英文 → NameLookup 走 LocManager
- Q5 Abandon 归到当前战斗 → ResolveAbandonedCause walk MapPointHistory
- Q4 败北局也保存 contribution → LoadRunSummary fallback 拼装 per-combat 文件
- Q3 Ancient 完整 PRD 设计 → AncientPoolMap 静态数据

### Round 5 人工反馈（2026-04-09）
1. 顶栏指示器仍未显示 — 改为 scene tree root 附加（singleton），由 SetUpCombat / OnCombatEnded postfix 控制可见性
2. 卡牌图书馆胜率重复 4 次 — 完全重写 CardLibraryPatch 用 GridContainer 3 列双源（我的 + 社区）
   - 新建 `LocalCardStats.cs` + `RunHistoryAnalyzer.ComputeLocalCardBundle`
3. RunHistory 还遮挡原生 → 缩窄到 420px ScrollContainer
4. 路径统计太丑 → 拆分为 BuildDeckTable + BuildPathTable，行用类别专属颜色
5. 个人生涯位置错（在角色数据下方）→ MoveChild 到 characterContainer 索引前一格
6. Ancient 必须按 3 池分组 + 显示 Act 前缀 + 图标 → 大改 BuildAncientPickRates
7. Boss 同样格式 → BuildBossStats 重写
8. **新工作流规则**：先改 PRD 再改代码（用户明确要求）

### Round 6 人工反馈（2026-04-09）
1. 遗物胜率显示选取率（无意义）+ 缺双源 → RelicLibraryPatch 重写 + LocalRelicStats
2. 商店"无色牌+15%"重复 + 无折扣时显示折扣文本 → subtitle 改为纯"商店价格表"
3. 战斗贡献:
   - 实时不更新 → 移除 RealTimeCombatPatch 重复 hook + 移除 debounce
   - SelfDamage 算入防御 % → 用正贡献和作为分母
   - ? 帮助打不开 → ToggleHelpPanel 自定义 PanelContainer + ✕ 关闭按钮 + 完整指标说明
4. 意图状态机:
   - "no metadata" → MonsterIntentMetadata mod-init pre-bake
   - 增加 fallback 当前 intent 显示
   - hover hook + diagnostic logging

### Round 7 人工反馈（2026-04-09）
1. 顶栏图标位置/大小不对 → reparent 到 NTopBar（`NRun.Instance.GlobalUi.TopBar.Map.GetParent()` 即顶栏 hbox），使用游戏原生纹理：
   - 药水：`atlases/potion_atlas.sprites/distilled_chaos.tres`（混沌药水）
   - 卡牌：`ui/reward_screen/reward_icon_card.png`（"将一张牌添加到你的牌组"奖励行图标）
2. 个人生涯统计与角色数据重叠 → **新建** `Util/LayoutHelper.cs` + 两个工具方法 `FindLayoutAncestor` / `AppendToLayoutAncestor`：
   走父链找第一个 VBox/HBox/GridContainer/ScrollContainer，把 section 加到 layout container 内（让 Godot 自动排版）
3. RunHistory 还遮挡 → 同上，改用 LayoutHelper
4. Boss 图标缺失 → `AncientPoolMap.GetEncounterIcon` 加载 `ui/run_history/{id}.png`

### Round 8 人工反馈（2026-04-10）
1. 中途存档丢失贡献数据 → **新建** `Collection/LiveContributionSnapshot.cs`，扩展 `ContributionPersistence` 加 `SaveLiveState/LoadLiveState/DeleteLiveState`，扩展 `CombatTracker` 加 `BuildLiveSnapshot/HydrateFromLiveSnapshot`，`RunContributionAggregator` 加 `HydrateRunTotals`
   - **写入时机**：每次 NotifyCombatDataUpdated（打牌触发）+ OnCombatEnd
   - **读取时机**：RunLifecyclePatch.TryHydrateLiveState 在 SetUpNewSinglePlayer/MultiPlayer postfix
   - **清理时机**：OnMetricsUpload 删除 `_live.json`
2. 实时刷新仍失效（要关闭再开才更新）→ 根因诊断：上一轮 RefreshTabs 每次 RemoveChild + AddChild ScrollContainer skeleton 导致 TabContainer 状态混乱。**新方案**：
   - `EnsureTabSkeletons` 一次性创建命名 ScrollContainer (CombatTab/RunTab) 永久持有
   - `ReplaceScrollContent` 仅替换 ScrollContainer 内部的 chart 子节点
   - 保存 + 恢复 currentTab index
3. 意图状态机:
   - "no metadata" 根因：mod init 时 RunManager 未启动 → ToMutable+SetUpForCombat 在 GenerateMoveStateMachine 内调 AscensionHelper.GetValueIfAscension 等抛异常
   - **新方案**：完全改为**懒加载 + 双源 fallback**。`Get(monsterId, liveMonster)` 优先用 in-combat 的 `liveMonster.MoveStateMachine`，fallback 到 canonical clone。负面结果也缓存
   - hover trigger 改为 NCreature.OnFocus（敌人本体）而非 NIntent
   - UI 重设计：横向两栏（左当前状态高亮 / 中金色箭头 / 右后继 branch 列表 + intent badge 颜色）

---

## 当前架构总览

### 核心数据流

```
打牌
 ├─ CombatHistoryPatch.AfterCardPlayFinished
 │   └─ CombatTracker.NotifyCombatDataUpdated
 │       ├─ ContributionPersistence.SaveLiveState  ← 持久化（round 8）
 │       └─ CombatDataUpdated event
 │           └─ ContributionPanel.OnCombatDataUpdated
 │               └─ CallDeferred(DeferredRefresh)
 │                   └─ RefreshTabs → ReplaceScrollContent  ← in-place（round 8）

战斗结束 → CombatTracker.OnCombatEnd
        ├─ aggregator.AddCombat
        ├─ ContributionPersistence.SaveCombat (per-combat snapshot)
        └─ ContributionPersistence.SaveLiveState (combatInProgress=false)

新 run / resume → RunLifecyclePatch.OnRunStartSP/MP (postfix)
              ├─ RunDataCollector.OnRunStart
              └─ TryHydrateLiveState  ← round 8

run 结束 → OnMetricsUpload
        ├─ ContributionPersistence.SaveRunSummary
        ├─ ContributionPersistence.DeleteLiveState
        └─ RunHistoryAnalyzer.InvalidateAll
```

### 主要文件清单（截至 round 8）

**新建文件**（27 个）：
```
src/Collection/
  CareerStatsData.cs              v6: ElderEntry/ElderOption/ElderRelicStats
  LocalCardStats.cs               v5: 我的卡牌数据 POCO
  LocalRelicStats.cs              v6: 我的遗物数据 POCO
  LiveContributionSnapshot.cs     v8: 中途存档容器
  RunHistoryAnalyzer.cs           v6+v8: + ComputeLocalCardBundle/ComputeLocalRelicBundle/ComputeAncientByElder
src/Patches/
  CardLibraryPatch.cs             v6: GridContainer 3 列双源
  CareerStatsPatch.cs             v7: LayoutHelper
  CombatUiOverlayPatch.cs         v7: 重写为 NTopBar reparent
  RealTimeCombatPatch.cs          v6: 空文件（合并入 CombatHistoryPatch）
  RelicLibraryPatch.cs            v6: 双源 mine+community
  RunHistoryPatch.cs              v7: LayoutHelper
src/UI/
  CareerStatsSection.cs           v5+v6+v7: split tables + Ancient pools + Boss dropdown + icons
  IntentStateMachinePanel.cs      v8: jaw worm style 两栏布局
  RunHistoryStatsSection.cs       v5: split tables + ancient picks list + view-contrib
src/Util/
  AncientPoolMap.cs               v5: 9 elders 硬编码池子结构 + GetElderIcon/GetRelicIcon/GetEncounterIcon
  CareerStatsCache.cs             v4: 磁盘缓存
  ContributionPersistence.cs      v4+v8: SaveCombat/SaveRunSummary/SaveLiveState
  LayoutHelper.cs                 v7: FindLayoutAncestor/AppendToLayoutAncestor/DescribeAncestry
  MonsterIntentMetadata.cs        v6+v8: lazy + live-first
  NameLookup.cs                   v4: LocManager 包装

mods/sts2_contrib_tests/src/Scenarios/
  Catalog_AttackCardTests.cs      11 categories × ~5-12 tests each = 72 tests total
  Catalog_DefenseBlockTests.cs    （由后台 agent 生成）
  Catalog_DefenseDebuffTests.cs
  Catalog_DefenseStrReductionTests.cs
  Catalog_DrawTests.cs
  Catalog_EnergyTests.cs
  Catalog_HealingTests.cs
  Catalog_InteractionTests.cs
  Catalog_ModifierTests.cs
  Catalog_PowerIndirectTests.cs
  Catalog_SelfDamageTests.cs
```

**重要修改的现有文件**：
```
src/Collection/CombatTracker.cs          v8: BuildLiveSnapshot/HydrateFromLiveSnapshot
src/Collection/RunContributionAggregator.cs  v8: HydrateRunTotals
src/Collection/RunDataCollector.cs       v4+v8: SaveRunSummary 调用 + DeleteLiveState
src/CommunityStatsMod.cs                 v4-v8: pre-warm + Attach + MonsterIntentMetadata.Initialize (no-op)
src/Config/FilterSettings.cs             v4: MyDataOnly
src/Config/Localization.cs               v4-v8: 几十个新 keys (CN+EN)
src/Patches/CombatHistoryPatch.cs        v6+v8: 实时刷新 hook 合并 + LiveState
src/Patches/CombatLifecyclePatch.cs      v4+v8: SaveCombat + SaveLiveState
src/Patches/IntentHoverPatch.cs          v8: NCreature.OnFocus/Unfocus 而非 NIntent
src/Patches/MapPointPatch.cs             v4: ShopPricePanel 折扣联动
src/Patches/RelicCollectionPatch.cs      v6: legacy disabled
src/Patches/RunLifecyclePatch.cs         v8: TryHydrateLiveState
src/UI/ContributionChart.cs              v6: 视觉重设计 + 防御 % 修复
src/UI/ContributionPanel.cs              v4-v8: LEFT 位置 + tab fix + 实时 + help dialog + ShowRunReplay
src/UI/FilterPanel.cs                    v4: 11 toggles
src/UI/PotionOddsIndicator.cs            v7: 原生 Distilled Chaos 图标
src/UI/CardDropOddsIndicator.cs          v7: 原生 reward icon
src/UI/ShopPricePanel.cs                 v4+v6: GridContainer 表格 + relic discount + subtitle simplification
src/UI/StatsLabel.cs                     v4: 1 decimal place
src/UI/UnknownRoomPanel.cs               v4: Hook.ModifyUnknownMapPointRoomTypes
src/Util/OfflineQueue.cs                 v4: 10/7 limits
mods/sts2_contrib_tests/src/Scenarios/DefenseTests.cs  v4: DEF5b 测试
mods/sts2_contrib_tests/src/TestRunner.cs              v5: 注册 11 个 Catalog_* lists
```

---

## 已知遗留 / 待用户验收的项

### Round 8 修改后等待用户验证：
1. **中途存档恢复** — `_live.json` 写入 + hydrate 流程
2. **实时刷新** — in-place skeleton swap 是否真的更新
3. **意图状态机** — 懒加载 + NCreature hover 触发 + 新两栏 UI 是否显示
4. **意图状态机多阶段** — Boss 多状态机渲染（目前只显示当前 SM）

### 长期遗留（暂未触及）：
- **服务端部署**（PRD-01）— 整套社区数据 server 还是 mock
- **MyDataOnly 数据源切换** — FilterPanel checkbox 已加但 StatsProvider 还没 fallback 到 RunHistoryAnalyzer
- **BoughtCards 代理细化** — 当前 RunHistoryAnalyzer 用 BoughtColorless 估算
- **CONTRIBUTION_CATALOG 90 个 ⚠️ 缺口**：
  - Top 5 已修：Confused / Enlightenment / Strength reduction / 食物遗物 +MaxHp / Vajra-Girya-DataDisk
  - 剩余 ~85 个未修

### 技术债务：
- 6 个 nullable warnings（CS8602）— 都在 RunHistoryAnalyzer，不影响功能
- 单元测试 framework `mods/sts2_contrib_tests`：72 个新 Catalog_* tests 已写，但需要 in-game runner 触发执行

---

## 跨设备交接快照（2026-04-10）

### 当前 git 状态
- **未提交**：所有 round 4-8 改动（27 新文件 + 30+ 修改文件 + DLL）
- **未推送**：本地 master 与远端齐平（最后一次 commit "工作流04" = fdfbead）
- **未追踪**：5 张 round 7-8 反馈截图 + CONTRIBUTION_CATALOG.md + 27 新源文件 + 11 新测试文件
- 跨设备前必须 `git add . && git commit -m "..." && git push`

### 当前 DLL 状态
- `mods/sts2_community_stats/sts2_community_stats.dll`：**Apr 10 00:49, 381952 bytes**（round 8 build）
- 用户需手动拷到游戏 dir：`d:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\sts2_community_stats\`

### 下次开机第一件事
1. 读本文件
2. `git pull` 拉同步
3. 等待用户提供 round 9 反馈（如果有）或开始处理遗留项

### 关键技术备注（避免重复踩坑）

1. **构建错误必须看 `Build succeeded.` 行 + DLL mtime**。CS0246/CS0103 在 mod 源文件中是真错（缺 using）；只有在 `_decompiled/sts2/**/*.cs` 中才是环境噪音。
2. **私有方法 Harmony patch**：用字符串形式 `[HarmonyPatch(typeof(X), "MethodName")]`。
3. **私有字段读取**：用 `Traverse.Create(obj).Field("_x").GetValue<T>()`，try/catch + Safe.Warn。
4. **F1 精度**：所有百分比 1 位小数（PRD AC-15）。
5. **toggle 短路**：每个 patch 第一行 `if (!ModConfig.Toggles.X) return;`。
6. **meta 标记**：UI 注入幂等用 `HasMeta/SetMeta/RemoveMeta`。
7. **异步 patch 陷阱**：Harmony postfix 在 async 方法的第一个 await 处触发。
8. **NCombatUi.Activate**：参数 `CombatState state`。用 `LocalContext.GetMe(state)` 拿 Player；单人 NetId 可能为 null，必须 fallback 到 `state.Players.FirstOrDefault()`。
9. **scene 容器注入**：不要直接 `parent.AddChild`，用 `LayoutHelper.AppendToLayoutAncestor` 找真正的 layout container。
10. **NRun.Instance.GlobalUi.TopBar** 在 mod init 时不存在，必须 lazy attach（在 SetUpCombat postfix 时）。
11. **MonsterIntentMetadata** 不要 mod-init eager bake（GenerateMoveStateMachine 内常引用 RunManager）。优先用 live monster 的 MoveStateMachine。

### 工作流约定

- 用户每轮人工测试反馈 → mod 修改循环结构：
  1. 调研 + 找 game API
  2. **PRD 先改**（v3.1 round N 注释）
  3. 代码实现
  4. `dotnet build` 必须 succeeded
  5. 确认 DLL mtime fresh
  6. 报告变更总览
- 用户重启游戏 → 测试 → 下一轮反馈
- 文档跨设备衔接：本文件 + PRD_04_ITERATION2.md + memory/MEMORY.md
