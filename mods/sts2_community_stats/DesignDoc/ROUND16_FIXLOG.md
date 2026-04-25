# Round 16 — 4.18 修复回写 + 客户端 P0/P1 复修闭环

> 起始:2026-04-24(承接 Round 15 ORB-BUG closure)
> 目标:对照 4.18 反编译版本回写丢失修复;复修用户实测发现的 8 项新问题。

---

## A. 已完成 + 实测通过的修复

| # | 项 | commit | 用户实测 |
|---|---|---|---|
| 1 | 最低玩家胜率筛选(客户端发 `min_wr=` + 服务端全链路 + cache key 区分) | `28764b1` | ✅ F9 50% → 样本数明显下降;0% vs 100% 返回不同 bundle |
| 2 | 实验体 hitbox 抖动 → 意图状态机闪烁(`_applyVersion` token + 120ms 延迟 Hide + `_shownOn` 追踪 + `ForceMouseIgnoreRecursive`) | `28764b1` | ✅ 实验体 hover 稳定不闪 |
| 3 | 顶栏图标固定(`_positioned` 锁 + `TryAttachDeferred` 30 次重试 + `ChildOrderChanged` 信号自动 re-pin) | `28764b1` | ✅ 点击地图图标后,药水/卡牌指示器仍在 Map 左侧 |
| 4 | 眼部攻击 Weak 防御贡献重复(`AfterModifyDamage_Enemy` 加 `WEAK_POWER` 跳过 + `OnDebuffPrevention`) | `28764b1` | ✅ GO_FOR_THE_EYES 单条贡献,无 weak_power 重复 |
| 5 | 历史记录路线统计的"购买卡牌数 = 0"(`BuildSingleRunStats` 用 `BuildPerActShopCardBuys` 拉 ShopPurchasePersistence) | 之前轮次 | ✅ 单局复盘购买数正确 |

## B. 本轮新做、待用户实测的修复

| # | 项 | commit | 测试场景 |
|---|---|---|---|
| 6 | 多源 debuff 伤害贡献被首个施加者独占 — `_activePowerId` AsyncLocal + `OnPowerApplied` 入参改为 delta(对齐 4.18:删 `PowerApplyPatch.AfterApplyInternal`,统一在 `AfterSetAmount` `amount > __state` 分支调) | `fa82b06` / `fd433cd` | (与下面 #7 #8 一并测试) |
| 7 | **Vul / Weak 队头独占**(PRD M5 / DEF-2c)`GetDebuffHeadSource` → 当前回合 100% 归 layer[0],下回合 amount-=1 触发 `DecrementDebuffLayers` 队头扣 1,耗尽后下一源接位 | `55625b8` | 见下方 |
| 8 | **Poison FIFO 比例分摊**(PRD I1) `OnDamageDealt` indirect 分支改用 `GetDebuffSourceFractions(targetHash, _activePowerId)`;`AfterSetAmount` 把 `POISON_POWER` 加入 `DecrementDebuffLayers` 触发集 | `55625b8` | 见下方 |

### 测试场景 — Vul/Weak/Poison

#### TS-Vul1:多源易伤队头独占
1. Defect/Silent 装一张能施 2 Vul 的卡(A)+ 一张能施 1 Vul 的卡(B)
2. 同回合先打 A,再打 B 给同一敌人(此时该敌 Vul 总层数 = 3)
3. 同回合再打一张攻击牌
4. **预期**:贡献图 ModifierDamage 列下,该敌人本次受到的 Vul 加成 100% 归到 A;B 在该回合无 Vul 贡献
5. 第 2、3 回合(此时 A 还有剩余 layer):同样 100% 归 A
6. 第 4 回合(A 已耗尽,B 接位):新攻击的 Vul 加成 100% 归 B

判定标准:贡献图中 A 和 B **不应同时**出现 ModifierDamage(同一回合内)。多回合累计后两人都出现,但比例严格按"A 的 layer 在场的回合数 × 该回合 ModifierDamage" 切分。

#### TS-Weak1:多源虚弱队头独占
对偶 TS-Vul1,但目标是 dealer(敌人攻击我们时,我们身上视角的 Weak 减伤归因到施加该 Weak 的卡)。

#### TS-Poison1:多源中毒比例分摊
1. Silent 装两张毒牌 — A 施 3 中毒,B 施 2 中毒
2. 一回合内对同一敌人,A 先打 B 后打,该敌中毒总 = 5
3. 不再施毒,等敌人回合(让 Poison 自然 tick 5 次)
4. **预期 tick 序列**:
   - r1 tick=5 → A=3,B=2(layer (A,3)(B,2) 比例 3:2)
   - r2 tick=4 → A=2,B=2(layer (A,2)(B,2) 比例 1:1)
   - r3 tick=3 → A=1,B=2(layer (A,1)(B,2) 比例 1:2)
   - r4 tick=2 → A=0,B=2(layer (B,2))
   - r5 tick=1 → A=0,B=1(layer (B,1))
   - 总 A=6,B=9,合 15 = `5+4+3+2+1`(等于敌人实际中毒总伤)
5. 判定:贡献图 A 与 B **同时**出现 AttributedDamage,合计 = 敌人实际中毒受伤总量

---

## C. 本批待修复(用户 2026-04-24 提交)

> 共 8 项。每一项含:**现状 / 根因(若已知) / 方案 / 疑问**。逐项澄清后实施。

### C1 静默猎手"紧勒"(STRANGLE)额外伤害未计入伤害贡献

**STS2 卡牌实现**(`_decompiled/sts2/.../Strangle.cs` + `StranglePower.cs`):
- `Strangle.OnPlay` 走 `DamageCmd.Attack` 造成 8 主伤害 + `PowerCmd.Apply<StranglePower>(target, 2, ...)`
- `StranglePower.BeforeCardPlayed`:记录"打这张牌时 Strangle 的 amount"
- `StranglePower.AfterCardPlayed`:**直接**调用 `CreatureCmd.Damage(ctx, base.Owner, amount, Unblockable | Unpowered, null, null)` —— 这是**额外**伤害,与原卡牌的 DamageCmd.Attack 解耦

**根因**:我们的 patch 链没在 `StranglePower.AfterCardPlayed` 期间设置 active power source 上下文。OnDamageDealt 收到 cardSourceId=null + 无 active source → 走 fallback "UNTRACKED"(或归到正在打出的 *别的* 卡牌)。

**方案**:为 `StranglePower.BeforeCardPlayed` 加 prefix(SetActivePowerSource("STRANGLE_POWER" → 从 `_debuffLayers[(target, "STRANGLE_POWER")].head` 解析源卡)、`AfterCardPlayed` 加 postfix(ClearActivePowerSource)。沿用通用 power hook context 框架。

**疑问 Q1**:Strangle 的额外伤害是否需要按"FIFO 队头独占"还是"按 amount 比例分摊"?
- 卡牌设计上每张紧勒+1 power amount,但伤害是**每张牌独立结算**,不是 tick — 与 Vul / Poison 模型不同
- 我倾向用"队头独占"(因为同一个 StranglePower 在 BeforeCardPlayed 锁定了一个 amount 值,这个值是 power 当前 stack 总和而非某源贡献)
- **请确认**

### C2 遗物"风箱"(BELLOWS)战斗内升级贡献未计入

**STS2 实现**(`Bellows.cs`):
- `AfterPlayerTurnStart` 在第 1 回合(`RoundNumber > 1` 直接 return),`Flash()` + `CardCmd.Upgrade(player.Hand)` 升级当回合手牌
- 没有直接产生伤害

**期望**:被 BELLOWS 升级过的卡(STRIKE → STRIKE+ 等)打出时,卡牌本体的 DirectDamage 仍归卡牌自己,但**升级带来的 delta 伤害**(STRIKE+ vs STRIKE 的 +3 = `UpgradeDamage`)应归 BELLOWS。

**当前代码**:`CombatHistoryPatch:412` 已有 `TryPatch(typeof(Bellows), "AfterPlayerTurnStart", prefix, postfix)` —— 这只是设置了 `_activeRelicId`。但 BELLOWS 的 hook 在升级**之前**就结束了,后续打卡时 `_activeRelicId` 已经清空。

**根因**:`_pendingUpgradeSourceId` 字段是"升级过这张卡的源",依靠 ARMAMENTS 等"实时升级"卡牌打出时同步设置。但 BELLOWS 是**回合开始时一次性升级整手牌**,没机制把"这些卡片是被 BELLOWS 升级的"标记到卡牌本身。

**方案**:patch `CardCmd.Upgrade` 或 `CardModel.Upgrade`(具体看哪个是入口),如果当前 `_activeRelicId == "BELLOWS"`,把每张被升级的卡牌的 hash → "BELLOWS" 记到 ContributionMap 一个新表 `_cardUpgraderOrigin`。卡牌打出时 `OnCardPlayStarted` 查这个表设置 `_pendingUpgradeSourceId`。

**疑问 Q2**:本机制是否需要**仅限 BELLOWS**,还是泛化到所有"批量升级手牌"的来源(如某些事件、SHIV+ 之类)?

### C3 历史记录→本局统计的战斗贡献本局汇总不符合实际(疑似只统计最后一场 boss)

**当前路径**(`RunHistoryStatsSection.cs:451 / :462`):
```csharp
var summary = ContributionPersistence.LoadRunSummary(seed);
if (summary == null) ...
ContributionPanel.ShowRunReplay(summary);
```

`LoadRunSummary` 优先读 `_summary.json`,没有则 `AssembleFromCombats(seed)` 扫所有 `{seed}_{floor}.json` merge。

`SaveRunSummary` 在 `RunDataCollector.cs:168` 调用,内容是 `RunContributionAggregator.Instance.RunTotals`。

**根因待查**:
- 如果**已结束的 run**:`_summary.json` 写入了 `RunTotals`,理论应正确(假设 RunTotals 累加无 bug)
- 如果**未结束的 run**(正在打):`_summary.json` 不存在,fallback 到 AssembleFromCombats — 应合并所有战斗
- 用户看到"只有最后一场 boss" → 可能是:
  - (a) `RunTotals` 本身只是最后一场 combat 的 LastCombatData(累加逻辑错)
  - (b) `_summary.json` 被某次 SaveRunSummary 用了 LastCombatData 而非 RunTotals 覆盖
  - (c) `AssembleFromCombats` 只匹配到一个文件名

**疑问 Q3**:用户看到 bug 的具体路径是?
- 用户是看一个 **已结束**的历史 run 还是**进行中**的 run?
- "只显示最后一场 boss" — 是说总伤害量 ≈ 最后一战的伤害,还是只有 boss 战那几个 source 出现在贡献图里?

### C4 药水掉落逻辑是否正确

**当前代码**:`PotionOddsIndicator` + `CombatUiOverlayPatch.RefreshValuesFromPlayer` 用 `me.PlayerOdds.PotionReward.CurrentValue` 显示数值。

**疑问 Q4**:用户具体观察到什么"不对"?
- (a) 数值显示偏差(如游戏显示 40% 但 mod 显示 35%)?
- (b) 触发 / 不触发的时机不对(如打完精英战实际没掉药水但 mod 显示 40%)?
- (c) Tooltip 文字内容错?

请提供一个具体反例(战斗类型/floor/实际值/mod 显示值)。

### C5 地图怪物房间危险度数据刷新不及时

**当前代码**(`MapPointPatch.cs:69`):`MapPointOverlay.AttachTo(__instance, stats)` 在 map point 出现时一次性绑定 stats。

**根因**:F9 改了 filter 后,`StatsProvider.SetFilterAsync` 重建 `_bundle` 并 `FireDataRefreshed`,但 `MapPointPatch` 没订阅 `DataRefreshed` 重新 attach。地图节点 overlay 文字停留在旧 filter 的样本上。

**方案**:在 `MapPointPatch` 加 `StatsProvider.DataRefreshed += OnDataRefreshed`,handler 里遍历当前可见的 map point 重新 `AttachTo`。需维护 live `WeakReference<NMapPoint>` 列表(类比 `CardUpgradePatch._liveScreens`)。

**疑问 Q5**:这一项我可以直接做。**请确认**用户的预期:**仅在打开地图后才刷新**(地图未打开时不必),还是**只要 filter 变了就刷新**(下次打开地图自然就是新的)?

### C6 商店购买率高亮显示生命周期 bug

**现状**:`ShopPatch.cs` 中 line 276/339-347 设置购买率 label `Visible = true` 后,切换到牌组/地图/设置面板时 label 仍 visible。

**根因**:label 直接 attach 到屏幕上(scene root 或某顶层 Control),没跟随 `NMerchantScreen` / 商店 panel 的可见性。

**方案**:把 label parent 改到 `NMerchantScreen` 自身节点(随商店面板被 hide → label 跟随);或在 `NMerchantScreen.OnHide` patch 里手动 `label.Visible = false`。

**疑问 Q6**:可直接做。

### C7 商店折扣计算偏差(尤其删牌费用)

**当前代码**(`ShopPatch.cs` 的 `ComputeDiscountMultiplier` / `ComputeRemovalCost`):
- 删牌基础公式:`(75 + 25 × cardRemovalsUsed) × multiplier` — 但这是 STS1 公式
- STS2 实际删牌费用:游戏内 `MerchantCardRemovalEntry.Cost` 才是真值

我们之前对话提到把 `(75 + 25 × N) × m` 改成 `MerchantCardRemovalEntry.Cost × m`(已部分修?)

**疑问 Q7**:用户能给一个具体反例吗?
- 实际游戏内删牌价格:?
- mod 估算值:?
- 或者只说"删牌费用偏差"是因为基础值不对,还是折扣乘子计算不对?

### C8 卡牌选取/卡牌大图社区数据 vs F9 筛选数据不一致

**用户明确目标**:**所有使用社区数据的 UI 统一为 filter 后的数据**。涉及:
- 怪物房间危险度评估(C5)
- 选卡界面卡牌选取率/胜率
- 顶栏遗物栏遗物胜率/胜率浮动
- 商店界面购买率
- 事件选项选择率
- 升级界面升级率
- 删卡界面删除率
- 卡牌/遗物大图社区数据

**现状**:`StatsProvider.GetCardStats(cardId)` 从 `_bundle.Cards.GetValueOrDefault(cardId)` 读取。`_bundle` 来自 `ApiClient.GetBulkStatsAsync(character, filter)` —— filter 已经包含 `min_wr` 等。所以理论上 `_bundle` 是 filter 后的。

但用户报告"卡牌大图样本数远大于筛选数据总对局数" → **某处 UI 没走 _bundle,而是另外发了一个无 filter 的请求**。

**疑问 Q8**:具体哪个 UI 看到了不一致?(用户原话指"卡牌大图")
- 可能是 `CardLibraryPatch` 中点击卡片放大后,详情面板调用了 `StatsProvider.GetCardStats` 但实际从 `_bundle` 读 — 应该一致
- 或是 `LocalCardStats`(MyDataOnly 模式)走另一条路径
- 或是 fallback 拉了 `/v1/stats/cards/?cards=...` 接口,而那个接口当时**没传 min_wr** —— 但我之前已经修过 main.py 让所有 endpoints 都接受 min_wr

**调研 todo**:
- 看 `CardLibraryPatch` 详情面板的 stats 来源
- 看 `ApiClient.GetCardStatsBatchAsync` 是否传 filter(应该传)
- 看 server 返回时是不是 filter 生效了

---

## D. 实施顺序建议

按确定性 + 影响面排:

| 序 | 项 | 依赖 |
|---|---|---|
| 1 | C5 地图刷新(C5) | 独立、明确 |
| 2 | C6 商店 label 生命周期(C6) | 独立、明确 |
| 3 | C8 卡牌大图样本量调查 → 修复 | 调研后明确 |
| 4 | C1 紧勒额外伤害归因 | 等 Q1 确认 |
| 5 | C2 风箱升级源标记 | 等 Q2 确认 |
| 6 | C7 删牌费用 | 等 Q7 反例 |
| 7 | C4 药水掉落逻辑 | 等 Q4 反例 |
| 8 | C3 本局汇总 bug | 等 Q3 用例 |

---

## E. 疑问汇总

1. **Q1** Strangle 额外伤害归因模型(队头独占 vs 比例分摊)?
2. **Q2** "升级源标记"机制是否泛化到 BELLOWS 之外的批量升级源?
3. **Q3** 本局汇总只看到最后 boss 战 — 已结束 run 还是进行中?具体现象?
4. **Q4** 药水掉落逻辑哪里不对 — 数值/时机/文案?
5. **Q5** 地图刷新 — F9 Apply 立即重绘所有 visible map point,还是仅地图打开时拉新?
6. **Q6** (无疑问,可直接做)
7. **Q7** 删牌费用具体反例?
8. **Q8** 哪个 UI 看到了卡牌大图样本数 > 筛选总对局数?有没有 server 日志?
