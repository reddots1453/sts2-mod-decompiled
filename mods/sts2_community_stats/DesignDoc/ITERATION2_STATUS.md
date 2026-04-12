# 第二轮迭代 — 进度与上下文

> 最后更新：2026-04-12（**Round 13 贡献归因深度调试 + UI 改进 + 全量回归测试**）
> 此文件供跨设备会话使用，确保新会话能完整理解当前状态

---

## 当前位置

**工作流阶段**：**Round 13 — 测试覆盖扩展 + 贡献归因系统重构**。全量回归 274/388 PASS，55 FAIL 待修（block FIFO + modifier 追踪），UI 面板改进已完成。

**v0.12.0 发布产物**：
- 仓库根 `./stats_the_spire/` — 可直接拷到游戏 `mods/` 目录的完整运行文件夹
- 包含：manifest.json / config.json / sts2_community_stats.dll / README.md / test/test_data.json
- 已 git push 到 origin/master

**下次开机第一件事**：
1. 读本文件 §"Round 13" + "跨设备交接快照" 了解当前状态
2. 继续修复 55 个 FAIL（EffectiveBlock=0 / ModifierDamage=0 / Power hook 归因=0）
3. 核心问题：恢复 ResolveSource 为 card-first 后，power/relic hook 的 SYNC 效果（FeelNoPain block、Juggernaut damage）在 Harmony postfix 清除 context 前应该完成，但全量回归中返回 0——需要调查根因

**v0.12.0 验收完成的功能**（详见 [CHANGELOG.md](../CHANGELOG.md)）：
1. 个人生涯统计（百科大全 → 角色数据）— 8 大区块全部对齐游戏原生 UI 风格
2. 历史记录本局统计弹窗（按钮 + 居中模态）— 与生涯统计同款风格 + 中文化
3. 卡牌图书馆 / 遗物收藏个人样本数 + 选取率 + 升级 / 删除 / 购买率
4. 战斗贡献面板加宽到 760，回看路径无水平滚动条
5. F9 面板进阶 SpinBox 上限 10 + min/max 联动
6. RunHistoryAnalyzer 启动顺序 bug 修复（SaveManager 未 init 时不污染缓存）
7. CareerStatsCache schema 版本检测，旧缓存自动失效

## Round 9 round 50-51 关键修复回顾（本次新增）

1. **历史记录本局统计弹窗（PopupOverlay 重构）** — 三次失败迭代才找到正确路径：
   - 第一版用 Godot `Window` — 自动可见、关闭信号未捕获，输入被困
   - 第二版加 `_ExitTree` Harmony patch — 该方法在 NRunHistory 上不存在，整个 patcher 类未注册（按钮消失）
   - 第三版按钮 parent 到 `_screenContents` (MarginContainer) — 按钮被强制拉伸成全屏，背景成 dim 层
   - **最终版**：按钮 parent 到 `screen` (NRunHistory，普通 Control)，overlay parent 到 `GetTree().Root`
2. **战斗贡献面板水平滚动**：`WrapInScroll` 没有 disable horizontal mode，与 `NewTabScroll` 行为不一致
3. **z-order 冲突**：`OnReplayPressed` 在调用 `ContributionPanel.ShowRunReplay` 前先调用 `RunHistoryPatch.CloseOpenPopup()` 关弹窗
4. **图标加载 .tres 失败统一改为 PNG**：`ui/run_history/{type}.png`、`ui/run_history/{elder_id}.png`
5. **`CallDeferred(nameof(Rebuild))` → `Callable.From(Rebuild).CallDeferred()`**：Godot string-based CallDeferred 找不到 private C# 方法
6. **MarginContainer 是布局容器**：不要往 MarginContainer 里 AddChild 然后期望 anchors 生效，会被强制拉伸到全屏
7. **`[HarmonyPatch(typeof(X), "MethodName")]` 必须方法真实存在**：否则整个 patcher 类全失效

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

### Round 9 会话（2026-04-10）

本轮前半段（context 压缩前）处理了 Round 8 测试后的几项新反馈 + PRD §3.18 新增需求；后半段确认了一个长期误判的"build 环境问题"其实是 csproj HintPath 路径错位，最终对齐了本机仓库布局。

#### 代码/文档变更

**A. §3.18 角色筛选**（用户明确要求 "先改 PRD 再改代码"，PRD 只写需求 + AC、技术细节放 plan）：

PRD 更新：
- `PRD_04_ITERATION2.md` §3.18 新增——F9 面板角色下拉 + CareerStats 独立角色下拉 + 8 条验收标准（AC1-AC8）。compendium 视图（卡牌/遗物/其它）固定为"所有角色"。

代码实现：
- `src/Config/FilterSettings.cs` — 新增 `CharacterFilterMode` 字段（默认 `"auto"`）+ `ResolveCharacter()` 方法：`auto` 模式查 `RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault()?.Character?.Id.Entry`，无活动 run 时返回 `null`（自然满足 compendium 固定"所有角色"）。`ToQueryString` 和 `Equals/GetHashCode` 都改用 `ResolveCharacter()`，语义等价的 filter 走同一份缓存。
- `src/UI/FilterPanel.cs` — F9 面板在 "我的数据" 和 "自动匹配进阶" 之间插入角色下拉（7 选项：自动匹配 / 全部 / 5 个角色），`OnApplyPressed` 写回 `CharacterFilterMode`。
- `src/UI/CareerStatsSection.cs` — 独立角色下拉（6 选项：全部 + 5 角色），`OnCharacterDropdownChanged` 直接调用 `RunHistoryAnalyzer.GetCached(newFilter)` + `LoadAllAsync` 异步加载，与 F9 面板解耦。用户手动选择**持久化**、不因新开 run 重置。
- `src/Patches/RunLifecyclePatch.cs` — `OnRunStartSP/MP` 改为 `preloadChar = filter.ResolveCharacter() ?? runCharacter`，让 F9 手动覆盖优先级高于 run 角色。
- `src/CommunityStatsMod.cs` — `OnFilterApplied` 改为 resolve 后再 `StatsProvider.OnFilterChangedAsync`，dropdown 变更立即生效。
- `src/Config/Localization.cs` — 新增 9 个角色筛选键（EN + CN，各 9 条，含 `settings.character` / `settings.char_auto` / `settings.char_all` / `char.IRONCLAD|SILENT|DEFECT|NECROBINDER|REGENT`）。

**B. Ancient 遗物池拆分 + Darv 幕限定标注**：

- `src/Util/AncientPoolMap.cs`：
  - NEOW 原单池拆成 `positive_pool`（14 条 = 9 基础 + MASSIVE_SCROLL / NUTRITIOUS_OYSTER / STONE_HUMIDIFIER / LAVA_ROCK / SMALL_CAPSULE）和 `curse_pool`（6 条 = 4 基础 + SCROLL_BOXES + SILVER_CRUCIBLE）两池。
  - Pool 新增 `ActGate Dictionary<string,int>?` 字段，DARV 第 7 套标 `ECTOPLASM:2 / SOZU:2`，第 8 套标 `PHILOSOPHERS_STONE:3 / VELVET_CHOKER:3`。
- `src/UI/CareerStatsSection.cs` — `BuildElderDetail` 从线性列表改成 6 列 `GridContainer`（新 `NewElderGrid / AddElderHeaderRow / AppendNumericHeader / AppendNumericCell / AddRelicGridRow` 方法），渲染列标题行；`AddRelicGridRow` 内按 `pool.ActGate.TryGetValue(relicId, out gateAct)` 给遗物名拼接 `" (只在第 X 幕出现)"` 后缀。
- `src/Util/NameLookup.cs` — 新增 `Ancient(string elderId)` 方法：先查 events/encounters loc 表，未命中时中文硬编码回退 `NEOW→涅奥 / PAEL→帕埃尔 / TEZCATARA→泰兹卡塔拉 / OROBAS→奥洛巴斯 / VAKUU→瓦库 / TANX→坦克斯 / NONUPEIPE→诺努皮佩 / DARV→达弗`，英文回退 title case。
- `src/Config/Localization.cs` — 同时补了 `ancient.positive_pool / ancient.curse_pool / ancient.act_only_n` 3 个键。

#### 仓库根重定位（核心环境修复）

> **这修复了一个一直被误判为"build 环境问题"的真实根因，下次在另一台设备启动前务必了解**

**问题**：历史上多次 `dotnet build` 产出 1300+ 个 CS0246 "未能找到类型或命名空间名 MegaCrit/Godot/HarmonyLib"，每次被当成"本机缺游戏 DLL 的环境噪音"过滤掉。实际根因是：

- 本机仓库被 clone 到 `Slay the Spire 2/` 根下，**缺 `Sts2-mod-decompiled/` 中间层**。
- 所有 mod 的 `.csproj` HintPath `..\..\..\data_sts2_windows_x86_64\sts2.dll` 是按"有中间层"的规范布局写的（从 `Sts2-mod-decompiled/mods/<mod>/` 上溯三层正好是 `Slay the Spire 2/`，命中 game DLL 目录）。
- 本机路径上溯三层到 `common/`，game DLL 解析失败，MSBuild 报 `warning MSB3245: 未能解析此引用`，后面的 CS0246 是这个 warning 的连锁症状。
- 我之前按错误码过滤 CS0246 误报 build 成功，才发现规则"只信 `Build succeeded.` + DLL mtime" 被再次踩坑。

**处理**：
1. commit `397c4f0` 做 WIP snapshot（33 个未提交改动全部落盘，包括本轮 §3.18 改动 + Ancient 拆池 + Round 8 残留 + 5 张新截图删除 + 新测试文件）
2. `mkdir Sts2-mod-decompiled/` 并 `mv .git .gitignore CLAUDE.md KnowledgeBase _decompiled mods prompt "Slay the Spire 2.sln"` 进去
3. 过程中 `_decompiled/` 和 `mods/` 被 `VBCSCompiler.exe` + `Microsoft.CodeAnalysis.LanguageServer.exe` 锁住。先 `dotnet build-server shutdown` 释放了 VBCSCompiler/MSBuild server；剩下的 C# DevKit 语言服务锁由用户**手动**完成 mv
4. `dotnet build` from `Sts2-mod-decompiled/mods/sts2_community_stats`：**Build succeeded. 0 errors / 6 pre-existing nullable warnings**，DLL 输出 `sts2_community_stats.dll` 390144 bytes，mtime Apr 10 15:08 = §3.18 代码首次真验证
5. 顺手把未追踪的 `Slay the Spire 2.sln` 也 mv 到新根，`.sln` 里的相对路径 `mods\sts2_community_stats\...csproj` 依然有效

**影响**：
- 规范根变为 `d:\Games\steam\steamapps\common\Slay the Spire 2\Sts2-mod-decompiled\`，与 CLAUDE.md 描述一致、与另一台设备的仓库根一致、与 csproj HintPath 一致。
- `~/.claude/projects/` 下新建 `D--Games-steam-steamapps-common-Slay-the-Spire-2-Sts2-mod-decompiled/` project key，内含 junction `memory/` 指向旧 key 的 `memory/`，两个 project key 共享 memory（双向同步）。本机用户本次选择仍从旧路径继续会话，但将来可自由切换。
- 两条 memory 已更新：`feedback_build_verification.md`（新增规则："永远看 `Build succeeded.` + DLL mtime，遇 MSB3245 立即停下查 HintPath，禁止按 CS0246 过滤"）、`project_repo_layout.md`（从"偏离一层、暂不修"改为"2026-04-10 已对齐、build 正常"）。

#### 待测试（Round 9 人工验收）

§3.18 AC1-AC8 全部 in-game 实测：
1. F9 面板的 `Character:` 下拉默认为"自动匹配当前角色"
2. Run 中选 auto → 社区数据来源 = 当前 run 角色
3. 无 run 时选 auto → 社区数据 = 所有角色
4. F9 手动钉某个角色 → 持久化，新开 run 不重置
5. Compendium（卡牌/遗物）视图固定显示"所有角色"
6. CareerStats 页面独立下拉 = "所有角色"（默认，与 F9 状态无关）
7. CareerStats 下拉切换 → 统计数据按角色过滤重新加载
8. Ancient 页面 Neow 显示两段（positive/curse），Darv Ectoplasm/Sozu 带"(只在第 2 幕出现)"后缀、PhilosophersStone/VelvetChoker 带"(只在第 3 幕出现)"后缀；8 个长老名中文显示

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

### Round 9 修改后等待用户验证（2026-04-10 新增）：
1. **§3.18 角色筛选** — F9 下拉默认 auto / run 中 auto 跟随角色 / 无 run 时 auto = 所有 / 手动钉角色持久化 / compendium 固定全部角色 / CareerStats 独立下拉 / AC1-AC8 见 Round 9 会话小节
2. **Neow 遗物池拆分** — positive_pool (14) + curse_pool (6) 是否正确分两段显示
3. **Darv 幕限定标注** — ECTOPLASM/SOZU "(只在第 2 幕出现)"、PHILOSOPHERS_STONE/VELVET_CHOKER "(只在第 3 幕出现)" 后缀
4. **长老名中文** — NameLookup.Ancient 8 个 elder ID 的中文/英文回退

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

## 跨设备交接快照（2026-04-12 Round 13）

### 仓库根路径（**重要：两台设备必须对齐**）
- 规范路径：`<游戏根>/Slay the Spire 2/Sts2-mod-decompiled/`
- 本机 2026-04-10 已从 `Slay the Spire 2/` 重定位到 `Slay the Spire 2/Sts2-mod-decompiled/`
- 另一台设备如果原本就在规范路径，**无需变更**；如果也在 `Slay the Spire 2/` 下，需按本文件 §"Round 9 会话 → 仓库根重定位" 的步骤迁移
- csproj `HintPath="..\..\..\data_sts2_windows_x86_64\sts2.dll"` 从 `Sts2-mod-decompiled/mods/<mod>/` 上溯三层命中 game DLL 目录——只有规范路径下才能 build 通过

### 当前 git 状态
- **HEAD**：Round 13 commit（测试覆盖扩展 + 贡献归因重构 + UI 改进）
- **working tree**：Round 13 所有改动已提交
- **已 push 到 origin/master**

### 当前 DLL 状态
- `mods/sts2_community_stats/sts2_community_stats.dll`：Round 13 build（归因系统重构 + UI 改进）
- `mods/sts2_contrib_tests/sts2_contrib_tests.dll`：388 个测试，PassedSkipList 为空（全量回归模式）
- 已部署到游戏 mods 目录

### memory / project key 状态
- 旧 project key：`~/.claude/projects/D--Games-steam-steamapps-common-Slay-the-Spire-2/`（对应本机历史 cwd `Slay the Spire 2/`）
- 新 project key：`~/.claude/projects/D--Games-steam-steamapps-common-Slay-the-Spire-2-Sts2-mod-decompiled/`（对应规范 cwd `Slay the Spire 2/Sts2-mod-decompiled/`）
- 新 key 下 `memory/` 是 Windows junction，指向旧 key 的 `memory/`——两个 key 共享同一份 memory（双向同步）
- 用户本次选择仍从旧路径继续会话；切换到规范路径后 memory 无缝衔接，但会话历史（.jsonl）不会自动迁移

### 下次开机第一件事
1. 读本文件 §"Round 13" + "跨设备交接快照"
2. 如另一台设备：确认仓库根在 `Sts2-mod-decompiled/`，`git pull`
3. 继续修复 55 个 FAIL（详见 §Round 13 剩余问题）

### Round 13 — 测试覆盖扩展 + 贡献归因系统深度调试（2026-04-12）

#### 工作范围
1. **测试脚本 spec v3 全面重写**：9 个测试文件，388 个测试实体（从 ~146 扩展）
2. **EndTurn 链测试**：毒、辉星、下回合格挡/抽牌/能量、Thorns、Intangible、Buffer/Plating、orb passive
3. **Mod 源码贡献归因系统重构**：发现并修复 15+ 个 tracking bug
4. **UI 面板改进**：透明度、section 颜色、面板缩小、滚动条

#### 核心技术发现：Harmony async postfix 问题
**Harmony 对 async 方法的 postfix 在第一个 await 时就触发，不是方法完成后。** 这导致：
- Power/Relic hook 的 `ClearActivePowerSource`/`ClearActiveRelic` 在效果（damage/block/draw）完成前就清除了 context
- 同步效果（如 FeelNoPain 的 GainBlock）在第一个 await 前完成，不受影响
- 异步效果（如 DarkEmbrace 的 CardPileCmd.Draw）在第一个 await 后完成，context 已被清除

#### Mod 源码修改清单（CombatHistoryPatch.cs + CombatTracker.cs）
1. **OrbChanneledPatch**：从 `Hook.AfterOrbChanneled`(async) 改为 `CombatHistory.OrbChanneled`(sync)，零参数 postfix，从 OrbQueue 读最后 channeled 的 orb
2. **OrbPassivePatch.PatchOrbTurnEndTriggers**：直接 patch 各 orb 类型的 `BeforeTurnEndOrbTrigger` 实例方法（prefix only），替代不可靠的 `OrbCmd.Passive` static patch
3. **OrbEvokePatch.PatchOrbEvokeMethods**：直接 patch 各 orb 类型的 `Evoke` 实例方法，替代 `OrbCmd.EvokeNext/EvokeLast` static async patch
4. **Dualcast evoke PRD 规则**：第1次 evoke → channeling source（AttributedDamage），第2次+ → evoking card（DirectDamage）。通过 `OrbFirstTriggerUsed` flag 实现
5. **isIndirect 判断**：`ActiveOrbContext != null` 时无条件视为 indirect（orb 伤害永远是间接的）
6. **Pending Draw Source 机制**：解决 async power/relic hook 的 draw 归因。prefix 设置 `_pendingDrawSourceId`，OnCardDrawn 优先消费它
7. **StormPower 加入 PowerHookContextPatcher**
8. **ThornsPower.BeforeDamageReceived** patch 添加
9. **BlockNextTurnPower/StarNextTurnPower/DrawCardsNextTurnPower** 新增 patch
10. **FocusPower ID fallback**：尝试 "FOCUS_POWER" 和 "FOCUS" 两个 key
11. **ForceResetAllContext()**：测试间清除所有 stale context
12. **ResolveSource 优先级**：最终版 `cardSourceId → orbContext → card → potion → relic → power`
13. **OrbChanneledPatch source 查找**：独立于 ResolveSource，使用 `power > relic > card` 顺序

#### 测试基础设施修改
- **ClearHand()**：每个测试间清空手牌（防 10 张手牌上限导致 CardsDrawn=0）
- **ClearOrbs()**：每个测试间清空 orb queue（防 orb 累积导致 EndTurn 卡死）
- **ForceResetAllContext()**：每个测试间清除所有 stale context
- **EndTurnAndWaitForPlayerTurn**：3-phase wait + 15s timeout + 3s post-wait + heal after + poll IsPlayerReadyToEndTurn
- 移除了 MultiplayerOnly 卡（Coordinate、BelieveInYou、Intercept、Lift）
- 移除了 BagOfMarbles/RedMask（RoundNumber<=1 guard）
- 修正了大量错误的 CardsDrawn/EnergyGained 断言（非抽牌卡、下回合效果卡）

#### UI 面板改进（ContributionPanel.cs + ContributionChart.cs）
- 透明度：alpha 0.92 → 0.82
- Section 标题颜色：Damage=红(#FF4444), Defense=绿(#44CC44), Draw=蓝(#4488FF), Energy=橙(#FF8C00), Stars=金(#FFD700)
- 面板尺寸：宽 760→570, 高度锚点 0.1/0.9→0.25/0.75（屏幕 50%）
- Bar 宽度：540→380
- 新增 ScrollContainer 垂直滚动
- 帮助文本位置：`Localization.cs:138`(EN) / `408`(CN)

#### 全量回归结果（最新）：274 PASS / 55 FAIL / 56 skip(combat ended) / 3 skip(EndTurn)
PASS 的包括：所有角色的 DirectDamage 攻击测试、CardsDrawn、EnergyGained、StarsContribution、DarkEmbrace/CharonsAshes/ForgottenSoul（async power/relic draw）、全部 orb passive/evoke、Dualcast PRD 规则、EndTurn 链测试（毒/辉星/格挡/抽牌/能量）

#### 剩余 55 FAIL 待修
1. **EffectiveBlock=0**（~15个）：ALL block 测试。FIFO pool AddBlock 可能受 context 污染
2. **ModifierDamage=0**（~10个）：Str/Dex modifier 通过 DistributeByPowerSources 归因失败
3. **Power/Relic hook 归因=0**（~10个）：Juggernaut, Grapple, Inferno 等。Harmony postfix 清除 context 问题（但理论上 sync 效果应该不受影响）
4. **数值偏差**（~5个）：Conflagration, Stardust, Finisher, ForgeSubBar

**调查方向**：这些测试在之前的增量运行（带 PassedSkipList）中通过过。全量回归时 fail 说明是跨测试状态累积问题，或 Round 13 改动的副作用影响了 block FIFO / modifier 追踪链路。

### 关键技术备注（避免重复踩坑）

1. **构建错误必须看 `Build succeeded.` 行 + DLL mtime**。CS0246/CS0103 几乎永远是**真错**——要么是 mod 源文件缺 using（Round 4 踩坑），要么是 csproj `HintPath` 错位导致 `warning MSB3245: 未能解析此引用` 然后连锁爆出 1300+ CS0246（Round 9 踩坑）。**禁止**按错误码过滤；只有 build 输出末尾出现 `Build succeeded.` / `已成功生成。` 且 DLL mtime 新于 build 启动时间，才算成功。遇到 MSB3245 warning 立刻停下查 HintPath 和 cwd 是否在规范根 `Sts2-mod-decompiled/` 下。
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
