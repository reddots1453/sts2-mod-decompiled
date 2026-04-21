# 第二轮迭代 — 进度与上下文

> 最后更新：2026-04-20（**Round 15 — Beta 集成测试闭环 + supervisor 双线验收**）
> 此文件供跨设备会话使用，确保新会话能完整理解当前状态

---

## Round 15（2026-04-20）— Beta 集成测试闭环 + supervisor 双线验收

> 客户端/服务端 supervisor 并行交叉审查完成；服务端 INT-TEST T1-T7 端到端 VPS 回归全部验收通过；Beta 闭环进入"ORB 归因最后一公里"阶段。

### 本轮 roadmap 变化

| 任务 | 变化 | 备注 |
|---|---|---|
| S01 `[监工] supervisor-client` | done | 9/9 审查 + 6/6 修复 全部验收通过 |
| S02 `[监工] supervisor-server` | in-progress → **done** | 部署通过 + 集成测试 T1-T7 验收通过（T5 阈值偏差已知）|
| INT-TEST `T1-T7端到端VPS回归` | in-progress → **done** | M10 执行完毕，T5 阈值偏差已知 / T7 观察窗口证据充分 |
| ORB-BUG `充能球Orb贡献归因错误` | in-progress | 仍待处理；归因链被其他实体效果污染 |

**roadmap objective 改为**：`Stats the Spire Beta — 客户端P0闭环 + 集成测试完成 + Orb归因debug`

### INT-TEST T1-T7 验收摘要

测试环境：`statsthespire.duckdns.org`（64.176.85.164），容器 `sts2stats-api` / `sts2stats-db` / `sts2stats-redis` 均 healthy。

| 测试 | 结论 | 关键证据 |
|---|---|---|
| **T1 Golden Path** | PASS | `POST /v1/runs → 200, run_id=1408`；runs + card_choices(2) + relic_records(1) + event_records(1) + shop_purchases(1) + card_removals(1) + contributions(3) 全部入库正确 |
| **T2a Dedup** | PASS | 相同 `run_hash="test_t2_dup"` 二次 POST 返回 `{"status":"ok","run_id":null,"dedup":true}`，行数无变化（ON CONFLICT DO NOTHING 生效）|
| **T2b win_rate 回填** | PASS | runs 从 0 回填 0.85 后，card_choices / relic_records 同步到 0.85 |
| **T3 id_migration** | PASS | `CARD.TEST_OLD_CARD → TEST_NEW_CARD` 在 card_choices 和 source_type='CARD' 的 contributions 均翻译；`source_type='POWER'` 的 source_id **未**翻译（符合 `_normalize_payload_ids` 白名单）|
| **T4 Bulk Cache** | PASS | 默认键 `bulk:IRONCLAD:0:1.0.0` 64ms→22ms，TTL 796s→780s；`asc_range=10-20` / `min_win_rate=0.5` 生成独立键 |
| **T5 Rate Limit** | **PARTIAL**（功能正常 / 阈值偏差）| Query 60/min 在第 58 次触发首个 429（前期测试占用 2 次配额一致）；Upload VPS 实际 `RATE_LIMIT_UPLOAD=300/minute` 与规范默认 `10/minute` 不一致，**已知偏差**不阻塞生产 |
| **T6 Health 脱敏** | PASS | 正常 200 `{status:healthy, db:ok, redis:ok, version:x.x.x}` 无连接串/密码/IP；stop db 后 503 `{status:unhealthy, db:error, redis:ok}`，错误字段仅 `error` 字面量，无异常堆栈泄漏 |
| **T7 Precompute 观察** | PASS | `350 bundles computed in 4.1s`（5 chars × 7 asc_ranges × 10 versions），Redis 采样 TTL 683-685 落在 `600±20s` 窗口；APScheduler AsyncIOScheduler running, `precompute_all interval=600s` |

**备注**：
- T1 规范期待 `201 Created`，API 返回 `200 OK + JSON body`，业务语义正确不影响对接
- T5 "未在 SSH 侧修改 .env 避免影响生产"，生产压测场景 300/min 功能正常
- T7 的 4.1s 完成时间与 B10-FIX 后 `_count_runs` 单次化结论一致（修复前推测 ~12s）

### 数据清理（测试收尾）

```
DELETE FROM runs WHERE run_hash LIKE 'test_t%';       -- 8 rows (id 1408/1410/1412-1417)
DELETE FROM id_migrations WHERE old_id='TEST_OLD_CARD'; -- 1 row
rm /tmp/t*.json                                        -- 成功
```
级联删除覆盖 card_choices / relic_records / event_records / shop_purchases / card_removals / contributions。

### Round 15 未完成 / 待办清单

| # | 项目 | 优先级 | 备注 |
|---|---|---|---|
| 1 | **ORB-BUG 充能球贡献归因错误** | P0 | 其他实体效果被错误归因到 Orb；下一轮工作焦点。建议 supervisor-client 接手 |
| 2 | **T5 .env RATE_LIMIT_UPLOAD 阈值统一** | P1 | 下次部署时选择：(a) VPS `.env` 改回 `10/minute` 对齐规范；或 (b) 更新规范默认值到 `300/minute`；由 owner 决策 |
| 3 | **T1 HTTP 201 vs 200** | P3 | 规范期待 `201 Created`，实际 `200 OK`；业务语义正确，若追求严格 RESTful 可后续修 |
| 4 | **docker-compose.yml nginx 证书 bind mount**（Round 14 v5 残留）| P2 | 60-90 天 Let's Encrypt 续期前必修，否则证书续期后容器仍用旧证书 |
| 5 | **DuckDNS → 真域名**（Round 14 v5 残留）| P2 | Cloudflare `.xyz` ¥10/年 |

### 下次开机第一件事（跨设备）

1. 读本节 §Round 15 + §Round 14 v6 了解最新状态
2. 核对 roadmap：`& "D:\Tools\Golutra\golutra-cli.exe" run --command-file <payload>` 用 `roadmap.read` 查最新任务
3. ORB-BUG 调试入口：`src/Collection/CombatTracker.cs` 的 Orb context 污染路径（历史记录见 Round 13 §核心技术发现 "Harmony async postfix 问题"）
4. 服务端无需改动，T1-T7 已验收通过，生产稳定运行中
5. 如需验证新打 run 的 power source_type：`ssh root@64.176.85.164 && docker compose exec -T db psql -U sts2stats -d sts2stats -c "SELECT source_type, COUNT(*) FROM contributions GROUP BY source_type;"`

### Round 15 chat 留痕

- 向 owner + 2 位 supervisor 发送 REPORT，messageId=`01KPM6WD6X9B34YS1NZYX24W8B`
- conversationId `01KPHW1ZKY6T685EDAE7A0GMQA`

---

## Round 14 v6（2026-04-15）— 贡献归因四连修

> 在 master Round 14 v5 基础上，针对 contribution test 套件暴露的 4 类归因 bug 做定点修复。Build 通过 0 错误；等待 in-game 回归 + 自动化测试复跑。

### 背景与决策

- 开工前 stash 了 v0.12.0 之前的 origin 重构 WIP（21 文件），但发现 master 已独立落地大部分 v5 修复，合并会产生大量冲突且 master 的 block 模型更优（`OriginalTotal`/`Modifiers`/`ConsumedBlockSlice`）
- 决策：**放弃 stash 硬合并**，改为把原始 diff 和意图归档为 [DesignDoc/FIX_INTENT_upgrade_origin.md](FIX_INTENT_upgrade_origin.md) + `FIX_INTENT_upgrade_origin.patch`（1346 行），作为审计基线
- 在 master HEAD 上重新 review 4 类 bug 的 gap，按最小侵入原则定点补齐

### 四条修复（全部 build 通过）

| # | 问题 | 修法 | 关键文件 |
|---|---|---|---|
| **A** | Frail 把 `power.Amount`（回合计数）当成 +2 block 计入 debuff 的 `ModifierBlock` | `AfterModifyBlock` 照 `AfterModifyDamage` 三段式：`ModifyBlockAdditive` → `ModifyBlockMultiplicative`（`<1m` skip）→ `>1m` 折算 | [CombatHistoryPatch.cs:1034-1094](../src/Patches/CombatHistoryPatch.cs#L1034-L1094) |
| **C** | 同名卡按扁平 `cardId` 聚合导致 origin last-writer-wins 污染（BATTLE_TRANCE 牌库 vs 技能药水、双生成器 Shiv） | `_currentCombat` 改 `MakeBucketKey(sourceId, \u0001, origin)` composite key；`_activeCardOriginAL` 在 `OnCardPlayStarted` 一次捕获；`GetOrCreate` 新 `originOverride` 参数；`TagCardOrigin` 降 no-op | [CombatTracker.cs](../src/Collection/CombatTracker.cs) |
| **D** | `UpgradeDelta` 缺 origin → SKILL_POTION Armaments 与牌库 Armaments 坍缩到同一 ARMAMENTS 桶 | `UpgradeDelta` record 加 `UpgraderOrigin`；`AfterUpgrade` 传 `tracker.ActiveCardOrigin`；伤害/块分流路径用 `_pendingUpgradeSourceOrigin` 作 `originOverride` | [ContributionMap.cs:702-707](../src/Collection/ContributionMap.cs#L702-L707)、[CombatHistoryPatch.cs:1195-1208](../src/Patches/CombatHistoryPatch.cs#L1195-L1208)、[CombatTracker.cs](../src/Collection/CombatTracker.cs) |
| **B** | Doom 多源/多目标归因扁平化 + BorrowedTime/Neurosurge self-doom 污染 `_powerSources["DOOM_POWER"]` | 新增 `DoomLayer` record + `_doomStacks` per-enemy FIFO 栈 + `RecordDoomLayer`/`ConsumeDoomLayers`；`OnPowerApplied` DOOM_POWER 分支 self-doom `return`、enemy-doom 记 layer 带 origin；`OnDoomKillsCompleted` 循环每敌人 FIFO 消费 | [ContributionMap.cs](../src/Collection/ContributionMap.cs)、[CombatTracker.cs](../src/Collection/CombatTracker.cs) |

### 实施顺序与依赖

> A 独立；C → D → B（B 和 D 的 origin 传播依赖 C 的 `_activeCardOrigin` 基础设施）

每步独立 `dotnet build` gate，全部 0 错误。

### 验证状态

- [x] Build 通过（`dotnet build` 0 errors / 7 pre-existing warnings）
- [ ] `sts2_contrib_tests` 套件回归复跑（特别关注 CAT-PWR-* Frail 污染相关 / NB-DOOM-Deathbringer / 同名卡生成场景，参见 [FAIL_LIST_R14v4.md](../../sts2_contrib_tests/FAIL_LIST_R14v4.md)）
- [ ] In-game 手动验收：Armaments+Strike / Armaments+Defend / 两张未升级 Strike / Whetstone 遗物升级 / 旧存档加载 5 个场景

### 下次开机续作

1. 跑 `sts2_contrib_tests` 回归。若有新 fail，先确认是不是原本就 fail 的 33 条（v4 baseline），再看是否被本轮修复引入或顺带解决
2. Self-doom 的 SelfDamage 计入问题：当前是完全忽略，若需要作为 cost 显示到出处卡 SelfDamage 段，需新开一条通道（本轮未做，属 scope 外）
3. 非卡升级路径（Whetstone / 事件）的 upgrader 桶现在落到 `"upgrade" / "unknown"`，UI 显示偏丑 —— 如用户反馈再追加触发点包裹

---

## 当前位置

**工作流阶段**：**Round 14 v5 — 服务端部署完成 + 客户端归因系统对齐**。VPS 已上线（statsthespire.duckdns.org），68 个历史 run 已成功导入 + 203 条贡献条目入库；客户端打包送测；当前正在调试 client 端"主菜单看不到社区数据"的最后一公里 UX 漏洞。

**v0.12.0 测试包**：
- `Sts2-mod-decompiled/dist/stats-the-spire-v0.12.0-test-20260414.zip`（322 KB）
- 包含：manifest.json / config.json（指向生产 API）/ sts2_community_stats.dll / pdb / README.md / CHANGELOG.md
- config.json 默认指向 `https://statsthespire.duckdns.org/v1`
- 已发送给测试小组

**服务端**：
- VPS：Vultr Ubuntu 24.04（IP `64.176.85.164`）
- 域名：`statsthespire.duckdns.org`（DuckDNS 免费）
- HTTPS：Let's Encrypt（standalone）
- 部署目录：`/opt/sts2stats/`
- 5 个 Docker 容器全部 healthy（api / db / redis / nginx / certbot）
- DB schema 已 apply 完整 migration 链（001/002/003_combo/003_defense/004/005）

**下次开机第一件事**：
1. 读本文件 §"Round 14 v5"（本节最末）了解服务端部署 + 客户端 fix 全貌
2. 核对未完成项：
   - 客户端"主菜单 F9 看不到数据"——`OnFilterChangedAsync` 在 `resolvedChar=null` 时回退到 `EnsureTestDataLoaded()` 而不是用 `"all"` 走 API（待修）
   - UI 重叠：`HistoryImporter._progressLabel` 和 `UploadNotice._label` 共占右上角同一坐标（待修）
   - `contributions` 表只有 `card/relic/potion`，无 `power`——历史数据是用旧 DLL 上传的，新打的 run 应当包含 power（未验证）
3. 服务端运维快查：`ssh root@64.176.85.164 → cd /opt/sts2stats → docker compose ps / logs -f api`

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

---

## Round 14 v5 — 贡献归因深度修复 + 服务端部署上线（2026-04-13 ~ 2026-04-14）

本轮跨度极长，覆盖三大块工作：(A) 测试驱动的归因系统全面修复，(B) 服务端 VPS 上线 + Schema 对齐，(C) 客户端测试包发布 + 端到端验证。

### A. 测试驱动的归因系统修复（Round 14 v1-v5）

#### 起点：伪 PASS 审计

第三轮 F10 回归 433/316 PASS / 39 FAIL / 78 skip 后，用户提醒"PASS 数字可能虚高"。
启动**伪 PASS 审计**，扫描 4 类（角色机制 / Power 施加类 / debuff 施加类 / 复杂交互）共 ~230 个 PASS 测试，按以下 3 类标准筛查：
- **A 类硬伪 PASS**：断言完全不读 `delta[X].Field`，只检查 `power.Amount` / `Passed=true` / 游戏状态
- **B 类触发链断裂**：断言 `delta[X].Field` 但下游事件未触发（典型：Doom 致死路径从未被任何测试触发过！）
- **C 类弱断言**：能精确算出却用 `>0`，或 spec-waiver 应升级

**结果**：发现 **66 条伪 PASS**（49 A + 6 B + 11 C），账面 PASS 数应从 330 降到 281（真实覆盖 ~76%）。
审计报告：`mods/sts2_contrib_tests/AUDIT_PSEUDO_PASS_R14.md`

最严重集中区：
1. `Catalog_NecrobinderDoomTests.cs` 9 条中 6 条 A 类（67%）
2. `Catalog_NecrobinderCardTests2.cs` 26 条中 15 条 A 类（58%）
3. `Catalog_PowerContribTests.cs` 22 条 PASS 中 14 条 A 类
4. `Catalog_NecrobinderCardTests.cs` 7 条中 3 条 A 类
5. `Catalog_SilentCardTests.cs` 27 条中 4 条 A 类
6. `Catalog_IroncladCardTests2.cs` 21 条中 2 条 A 类（Rupture / Barricade）

**跨设备关键点**：Doom 致死归因链路（`OnDoomKillsCompleted → AttributedDamage`）从未被任何测试覆盖过，是个真正的盲区。

#### 重写 46 条伪 PASS（3 个并行 agent）

- **Necrobinder 25 条**：Doom 系 9 条全重写（新增 `LowerEnemyHp` helper 用 `CreatureCmd.SetCurrentHp` 直接降 HP），非 Osty 16 条按下游触发链重写
- **PowerContribTests 14 条 smoke 段**：CAT-PWR-032 ~ -052 全部从 `Power.Amount==N` 换成真断言或 SPEC-WAIVER + TimesPlayed
- **Silent/Ironclad 7 条**：GrandFinale / WraithForm / Burst / Rupture 走真断言，InfiniteBlades / WellLaidPlans / Barricade SPEC-WAIVER

**TestRunner 重排**：`Catalog_NecrobinderDoomTests.All` 移到 `BuildScenarioList()` **末尾 tail 组**——Doom 测试故意致死敌人，放最后避免污染其他测试的战斗环境。

#### 6 个产品代码核心 fix

| Fix | 位置 | 内容 |
|---|---|---|
| **Fix 1** | `CombatTracker.ForceResetAllContext` | 测试间状态污染：除 `_active*` 外，新增清 `ContributionMap.Instance.Clear()` + 所有 `_pending*` |
| **Fix 2** | `CombatTracker.OnBlockGained` | Footwork/Dex 双记修复：pool 只加 `amount - modifierTotal`，与伤害路径对称 |
| **Fix 3** | `CombatTracker.ResolveSource` 优先级 | `power > card`（首次）：FeelNoPain/Juggernaut/Grapple 等 7 个 power-hook-during-card-play 立即转绿 |
| **Fix 3.6** | 同上 + 4 处分散调用点 | 扩展为 `power > relic > potion > card`：CharonsAshes/ForgottenSoul/Kusarigama/Nunchaku 等 18+ 遗物受益 |
| **Fix 4** | `ContributionMap.BlockEntry` + `ConsumeBlockDetailed` | Block pool 携带 modifier 列表，按累积比例分配 EffectiveBlock/ModifierBlock，**只在消耗时累计**（修 Footwork/Dex 药水显示时机问题）|
| **Fix 5a** | `OnPowerApplied` | `isPlayerTarget && intAmount > 0` 才写全局 `_powerSources`（防 Friendship -2 Str 污染 Str 分配）|
| **Fix 5b** | 新 patch `Hook.AfterEnergyReset` | Demesne/Friendship 能量归因：不再 patch `ModifyMaxEnergy`（属性 getter，每次读都触发被放大数倍），改为每回合一次性遍历 `player.Powers` |
| **Fix 6** | 新增 `_pendingUpgrade*` AsyncLocal 字段 + 调整 `OnDamageDealt`/`OnBlockGained` 写入 | Armaments 升级贡献归因：原本 `UpgradeDamage` 写到了被升级的卡（STRIKE_IRONCLAD），现在正确写到升级源（ARMAMENTS）|

#### 新增/补缺的 Power hook patch（共 14 个）

之前缺失：DemonForm.AfterSideTurnStart / Rupture.AfterDamageReceived + AfterCardPlayed / RollingBoulder.DoDamage（私有方法绕过 Godot 信号 EC 边界）

P0 §2 review 补齐：Enrage / Galvanic / Juggling / PrepTime / Mayhem / Stratagem / Radiance / EnergyNextTurn / Burst / EchoForm / MasterPlanner

#### Round 14 v5 review 审计后追加修复

| § | 文件 | 改动 |
|---|---|---|
| **§1** | `LocalCostModifierSourceTagPatch` / `EnergyCostSetterTagPatch` | 改为 `power > relic > potion > card` 优先级，对齐 Fix 3.6 |
| **§5** | `Hook.AfterEnergyReset` patch | 加 `PyrePower` 分支（之前只识别 Demesne / Friendship）|
| **§7** | `RecordPowerSource` / `RecordPlayerBuffSource` | 内部 `if (amount < 0) return` 纵深防御 |
| **§8** | `_pendingUpgrade*` 4 字段 → AsyncLocal | 防御 EchoForm/Mayhem 嵌套播放 |
| **§9** | `OnCardPlayFinished` | 显式清 `_pendingUpgrade*`（最小惊讶原则）|
| **§11** | 死代码删除 | `DecomposeWeakContribution`（~60 行）+ `ContributionMap.ConsumeBlock` legacy API |
| **§12** | `MergeFrom` | 补 `OriginSourceId` 传递 |
| **§12** | `HardenedShellPower.ModifyHpLostBeforeOstyLate` | 新 patch（同 Buffer/Intangible 模式），新 helper `OnHardenedShellMitigation` |

### B. 服务端部署上线

#### 部署流程踩坑回顾（按发生顺序）

1. **scp 失败**：`scp -r "D:/game_backup/..."` 被解释为 `host=D, path=...`。Windows 盘符 `:` 与 scp 语法冲突。**正确方式**：Git Bash 用 `/d/` 路径，或直接用 git clone（推荐）
2. **GitHub 不再支持密码登录**：HTTPS clone 必须用 Personal Access Token（PAT）。修复后 `git clone https://github.com/reddots1453/sts2-mod-decompiled.git`
3. **`apt: docker-compose-plugin not found`**：Ubuntu 24.04 默认源没这包，必须先添加 Docker 官方 APT 源。**已修 setup_server.sh**：先装前置依赖 + 清理 legacy docker → 添加官方 GPG key + sources.list.d → install `docker-ce` 全套
4. **UFW 拦 80/443**：默认只开 22。`ufw allow 80/tcp 443/tcp` 解决
5. **DuckDNS CAA 查询 SERVFAIL**：DuckDNS 的 NS 间歇性故障，Let's Encrypt 拒绝签发。重试几次后自愈，证书 OK（90 天有效）
6. **`cannot load certificate`**：宿主机 certbot 写入 `/etc/letsencrypt`，但 Docker nginx 容器挂载的是 named volume `sts2stats_certbot-certs`，两者完全不通。修复：`cp -a /etc/letsencrypt/. $VOL_PATH/`（**长期 fix 待办**：改 docker-compose.yml 的 nginx volume 为 bind mount + renewal hook）
7. **502 Bad Gateway**：nginx `default.conf` 写的是 `server 127.0.0.1:8080`（容器自己 loopback），应该是 `server api:8080`（Compose 服务名 + 容器内部端口）。**已修 nginx/default.conf** 并提交
8. **DB schema 缺失**：docker-compose.yml 只挂载了 `001_init.sql` + `002_run_hash.sql`，缺 `003_combo_events / 003_defense_fields / 004_final_deck_and_shop_offerings / 005_contribution_v5_fields`。`/docker-entrypoint-initdb.d/*` 只在 pgdata 空时跑——已初始化的 DB 必须手动 apply。**已修 docker-compose.yml** 挂载完整 migration 集
9. **客户端/服务端 schema 严重不对齐**：客户端 `ContributionUpload` 有 6 个新字段（`modifier_damage` / `modifier_block` / `self_damage` / `upgrade_damage` / `upgrade_block` / `origin_source_id`），服务端完全没有；服务端 `source_type` 正则 `^(card|relic|potion)$` 拒绝客户端实际产生的 `power` / `untracked` 等。**新建 `005_contribution_v5_fields.sql`**：扩 `source_type → VARCHAR(16)` + 加 6 列；**修 models.py**：放宽正则改用 `clamp_source_type` validator；**修 ingest.py**：INSERT 加 6 列；**修客户端 `IsUploadableSourceType`**：去白名单只拒空字符串

#### Schema migration 应用结果（VPS 上手动 apply）

| Migration | 结果 |
|---|---|
| 003_combo_events.sql | ALTER TABLE + 2× CREATE INDEX ✓ |
| 003_defense_fields.sql | 3 列 already exists（幂等）+ 1 个良性 ERROR（`block_gained does not exist` 是数据回填路径，新 schema 无此列）|
| 004_final_deck_and_shop_offerings.sql | BEGIN / 2× CREATE TABLE / 2× CREATE INDEX / COMMIT ✓ |
| **005_contribution_v5_fields.sql** | 2× ALTER TABLE（source_type 扩宽 + 加 6 列）✓ |

最终 DB 13 张表，contributions 列齐全：source_type(16) + 原 14 列 + 新 6 列。

#### VPS 端到端验证

- `curl https://statsthespire.duckdns.org/health` → `{"status":"healthy","db":"ok","redis":"ok"}` ✅
- 历史导入：68 个 run 全部 `POST /v1/runs HTTP/1.1 200 OK` ingested（IRONCLAD 54 + SILENT 9 + REGENT 3 + NECROBINDER 2）
- contributions 表：179 card + 21 relic + 3 potion = 203 条
- ⚠️ **没有 power source_type 条目**——因为历史 run 是用**旧 DLL** 收集的 contributions，那时 `IsUploadableSourceType` 还在过滤 power。新打的 run 应当包含 power（未验证）

### C. 客户端 UI 改进 + 测试包发布

#### UI 改动

| 文件 | 改动 |
|---|---|
| `ContributionPanel.cs` | BgColor alpha `0.82 → 0.70`（更透明）|
| `ContributionChart.cs:GetLocalizedName` | 新增 `FORGE:*` 前缀识别：`FORGE:BASE` → "基础锻造"，`FORGE:FURNACE` → "熔炉加成"，`FORGE:BULWARK` → "铸墙加成"，未知 variant 兜底用 `<variant>_POWER.title` 走游戏 LocString |
| `Localization.cs` | 新增 `source.forge_*` 4 键 + `settings.section_basic` 1 键 |
| `FilterPanel.cs:123` | F9 第二行 section header 从 `settings.title`（"Stats the Spire — 设置"）改为 `settings.section_basic`（"基础设置"）|
| **`FilterPanel.cs:PopulateCharacterDropdown`** | 加 `dropdown.Clear()` 使方法可重复调用 |
| **`FilterPanel.cs:TryGetCharIcon`** | 双层加载：PreloadManager 缓存 → ResourceLoader.Load 兜底（直接从 `res://images/ui/top_panel/character_icon_*.png`），解决 F9 在主菜单打开时角色图标不显示的问题 |
| **`FilterPanel.cs:Toggle`** | 每次显示面板时调用 `PopulateCharacterDropdown` 重建 dropdown，让 PreloadManager 后续缓存的图标也能显示 |

#### 测试包构建

- 路径：`Sts2-mod-decompiled/dist/stats-the-spire-v0.12.0-test-20260414.zip`（322 KB）
- 内含：`sts2_community_stats/` 目录，包含 manifest.json / config.json（指向生产 API）/ DLL / pdb / README.md / CHANGELOG.md
- `config.json` 关键预设：`api_base_url=https://statsthespire.duckdns.org/v1`、`enable_upload=true`、`history_import_completed=false`（让测试者首次启动看到导入对话框）、所有 8 个 feature_toggles=true
- `.gitignore` 加 `dist/`
- 已发送测试小组

### D. 客户端数据加载诊断（未完待续）

**症状**：测试者反馈"客户端看不到任何服务端发送的数据"——卡牌库悬浮显示"加载中..."、F9 设置面板显示"未加载数据"，但服务端日志显示 `GET /v1/stats/bulk` 200 OK。

**Game log 诊断**（`%APPDATA%\SlayTheSpire2\logs\godot*.log`）：

```
[INFO] [DIAG:StatsProvider] Loaded bundled test data at init: 37 cards, 16 relics, 7 events
[INFO] [DIAG:Preload] Starting preload for character=IRONCLAD, ApiBaseUrl=https://statsthespire.duckdns.org/v1
[INFO] [DIAG:Preload] GetBulkStatsAsync returned: non-null
[INFO] Preloaded bulk stats for IRONCLAD (0 cards, 0 relics, 0 events, 0 encounters)  ← 空 bundle!
[WARN] Asset not cached: res://images/ui/top_panel/character_icon_ironclad.png  ← 图标缓存确实不存在
[INFO] [DIAG:OnFilterApplied] resolvedChar=, GameVersion=, effectiveVer=v0.99.1  ← 主菜单 resolvedChar 为空!
```

**两个并行根因**：

1. **历史 preload 返回空 bundle**：5 次都是 `(0 cards, 0 relics, 0 events, 0 encounters)`。这些 preload 都发生在 migrations apply 之前，服务端 `aggregation.py:132 FROM final_deck` 表不存在 → SQL UndefinedTable 错 → 聚合函数返回空 dict → bundle 空。**已修服务端 schema**，待客户端开新 run 触发新 preload 验证。

2. **主菜单 `resolvedChar=null` 走 fallback 分支**：`OnFilterChangedAsync(character, ...)` 当 `character==null` 时调 `EnsureTestDataLoaded()`（只读本地 test_data.json 37 cards）而非走 API。这是个 **UX 漏洞**——用户在主菜单 F9 选了"所有角色"应该立即调 API，不应该退化到测试数据。**待修**：

   ```csharp
   var charForApi = character ?? (newFilter.CharacterFilterMode == "all" ? "all" : null);
   if (charForApi != null) await PreloadForRunAsync(charForApi, newFilter);
   else EnsureTestDataLoaded();
   ```

#### 服务端 API 日志直接验证

```bash
ssh root@64.176.85.164
cd /opt/sts2stats
docker compose logs -f api          # 实时观察上传/查询
docker compose exec -T db psql -U sts2stats -d sts2stats -c "\dt"
docker compose exec -T db psql -U sts2stats -d sts2stats -c "SELECT character, COUNT(*) FROM runs GROUP BY character;"
docker compose exec -T db psql -U sts2stats -d sts2stats -c "SELECT source_type, COUNT(*) FROM contributions GROUP BY source_type;"
```

### E. SSH 工作流踩坑

- **TUN 模式拦截 SSH**：用户用 Clash Verge 开 TUN，所有 TCP 经过 TUN 用户态栈，DIRECT 规则也无效（Clash TUN 对 SSH 协议有 bug）。Bash tool 发起的 SSH 也被同样拦截
- **临时方案**：用户在 PowerShell 关 TUN 后执行 `ssh-copy-id` 等价命令安装 SSH key，然后 TUN 关闭情况下做诊断；TUN 开启时**已建立**的 SSH 会话仍可用
- **Bash tool 通过 SSH 直连**：通过 `ssh-copy-id` 后 Bash tool 可以无密码 SSH，但 TUN 开启时仍会被拦——所以对话期间用户开 TUN 时仍走"复制粘贴"模式

### F. Round 14 v5 已提交 commit 列表（按时间倒序）

```
4fbadff  Round 14 v5 修复: 贡献上传 schema 客户端/服务端对齐 + F9 设置重命名 + 生产 API URL
633fe07  nginx upstream 修复: 127.0.0.1:8080 → api:8080
ee047b8  服务端部署修复: Docker 官方 APT 源 + VPS 工作流澄清
b25f075  Merge remote-tracking branch 'origin/服务端部署'
cf61230  Round 14 v5: 贡献归因系统全面修复 + 测试扩展 + 伪PASS审计重写
```

### Round 14 v5 未完成 / 待办清单

| # | 项目 | 优先级 | 备注 |
|---|---|---|---|
| 1 | **客户端"主菜单 F9 看不到数据"UX 漏洞** | P0 | 见 §D.2 修复方案 |
| 2 | **UI 重叠**：HistoryImporter `_progressLabel` 和 UploadNotice `_label` 占同一坐标 | P0 | 推荐方案：历史导入期间静默 UploadNotice，且把 UploadNotice 错开 25px |
| 3 | **新打 run 是否包含 power source_type 验证** | P0 | 进一局新 run 后 `SELECT source_type, COUNT(*) FROM contributions` |
| 4 | **图标修复实测验证** | P1 | 重启游戏 → F9 → dropdown 应有头像 |
| 5 | **重新打包测试 dll** | P1 | 等 §1 / §2 修完后 |
| 6 | **VPS 长期 fix**：docker-compose.yml nginx 卷挂载改为 bind mount + 加 certbot deploy hook 自动 reload nginx | P2 | 60-90 天证书续期前修，否则证书续期后容器还在用旧证书 |
| 7 | **DuckDNS → 真域名**（Cloudflare `.xyz` ¥10/年） | P2 | 长期稳定性 |
| 8 | **fail2ban 把本机 IP 加白名单** | P3 | 防止 Bash tool 多次 SSH 探测被误封 |

### 服务端运维快查表

| 操作 | 命令 |
|---|---|
| SSH 进 VPS | `ssh root@64.176.85.164` |
| 切到部署目录 | `cd /opt/sts2stats` |
| 查所有容器状态 | `docker compose ps` |
| 看 API 实时日志 | `docker compose logs -f api` |
| 看 nginx 日志 | `docker compose logs --tail 50 nginx` |
| 进数据库 shell | `docker compose exec -T db psql -U sts2stats -d sts2stats` |
| Flush Redis 缓存 | `docker compose exec -T redis redis-cli FLUSHALL` |
| 重启 API 容器 | `docker compose restart api` |
| 拉最新代码 + 同步 server/ | `cd ~/sts2-mod-decompiled && git pull && cp -r mods/sts2_community_stats/server/. /opt/sts2stats/` |
| Apply 新 SQL migration | `docker compose exec -T db psql -U sts2stats -d sts2stats < /opt/sts2stats/sql/<file>.sql` |
| 续证书（手动） | `docker compose stop nginx && certbot certonly --standalone -d statsthespire.duckdns.org --non-interactive --agree-tos -m 你@qq.com && docker compose up -d nginx` |
