# FIX INTENT: Upgrade / Origin 归因（未直接应用）

> 日期：2026-04-15
> 状态：**意图存档**。原始实现基于 master 的旧版本（pre–Round 14 v5），在合并 master 时发现 master 已独立实现大部分功能，因此放弃原始 stash，改为在 master 基础上重新审视 gap。
> 原始 diff：同目录 `FIX_INTENT_upgrade_origin.patch`（`git stash show -p stash@{1}` 导出）
> 原始 plan：`C:\Users\reddots\.claude\plans\scalable-juggling-deer.md`

---

## 本次工作要解决的三类 Bug

这些问题是通过 master 分支的 contribution test 套件发现的，修复意图必须在 master 最新代码上重新验证落地。

### 1. 同名卡按 origin 污染

**症状**：
- 临时生成卡与牌库原生同名卡混淆。例：牌库原生 Battle Trance（origin=null）先打，之后技能药水生成的 Battle Trance（origin=SKILL_POTION）再打，所有 `BATTLE_TRANCE` entry 按 last-writer-wins 被挂到 SKILL_POTION sub-bar。
- 双生成器 Shiv：CARD_A 和 CARD_B 都生成 Shiv；两张 Shiv 分别打出时 origin 在同一个 `SHIV` entry 上反复覆写。

**根因**：`_currentCombat` 按 `cardId` 做 key 聚合，origin 事后通过 `TagCardOrigin` 回填，粒度不匹配。

**原始方案（stashed）**：
- `_currentCombat` key 从 `cardId` 改为复合 `MakeBucketKey(sourceId, origin)`（分隔符 `\u0001`）
- `ContributionAccum.OriginSourceId` 在桶创建时固定，之后不再写
- `OnCardPlayStarted` 一次解析 `_activeCardOrigin`，后续所有 `GetOrCreate(sourceId, "card")` 自动取用
- 所有 FIFO / layer 结构（BlockEntry / DebuffLayer / DoomLayer / OstyHp / StrReduction / ModifierContribution / OrbSource / PowerSourceEntry）扩展 `OriginId` 字段
- 消费路径统一通过 `GetOrCreate(id, type, originOverride)` 写入

### 2. Upgrade 增量归因错位

**症状**：Armaments 升级 Strike 后，Strike 打出产生的 3 点升级伤害归到 `STRIKE.UpgradeDamage`，但该增量是 Armaments 带来的，语义上应归 Armaments。

**连带问题**：
- `UpgradeDelta.SourceId/SourceType` 是死字段（未消费）
- `UpgradeBlock` 在 Defense TotalVal 分母中双计（AddBlock 按完整 block 计入 EffectiveBlock，UpgradeBlock 又加一份）

**原始方案（stashed）**：
- `UpgradeDelta` 改为 `(DamageDelta, BlockDelta, UpgraderSourceId, UpgraderSourceType, UpgraderOrigin)`
- `RecordUpgradeDelta` 在 upgrade 发生时捕获 `tracker.ActiveCardOrigin`
- `_pendingUpgradeSourceId/Type/Origin` 三字段 pending 到被升级卡打出时
- 伤害分流：`GetOrCreate(_pendingUpgradeSourceId, type, originOverride: origin).DirectDamage += upgSplit`
- 块分流：同上，写入 `EffectiveBlock`
- 分母移除 `+UpgradeBlock`

### 3. Vulnerable / Weak 产生正防御贡献

**症状**：脆弱frail 作为 减小玩家格挡 的状态，在防御贡献统计中被错误计入正贡献；所有负贡献都不应计入贡献，可参考伤害贡献逻辑中的实现

**原始方案**：
- Defense 分母只取每个 source 的**正向贡献之和**（`if (v > 0) posSum += v;`）
- 所有 debuff 相关的增伤不进入 `MitigatedByDebuff`，只进入伤害取负部分

**注意**：这条是否 master 已修需重点核实 —— master 的 Round 14 v5 review §11 已经重写了 weak 链路（`DecomposeWeakContribution removed`），但 **Vulnerable + 正防御贡献**的具体路径需单独验证。

### 4. Doom 机制归因

**症状**：Necrobinder 的 Doom power 杀敌时，击杀贡献归到了错误的来源（施加 Doom 的卡而非 Doom 最终触发的语境）。且

**原始方案**：
- `DoomLayerEntry` 扩展 `OriginId` 字段
- `OnDoomKillsCompleted` 消费时返回 4-tuple `(sourceId, sourceType, originId, share)`
- Doom 杀敌贡献按 layer origin 分配到各自桶

**注意**：master 有 `Catalog_NecrobinderDoomTests.cs`，说明 master 已经针对 Doom 建立测试。master 的具体修法需核实。

---

## 原始 stash diff 摘要（供 review 用）

完整 diff 见 `FIX_INTENT_upgrade_origin.patch`（1346 行）。涉及文件：

| 文件 | stash 改动要点 |
|---|---|
| `src/Collection/ContributionMap.cs` | `PowerSourceEntry`/`BlockEntry`/`DebuffLayerEntry`/`ModifierContribution`/`OrbSource` 加 OriginId；`AddBlock`/`RecordPowerSource`/`RecordDebuffSource` 签名加 originId；`ConsumeBlock` 返回 4-tuple 含 origin；`UpgradeDelta` 加 UpgraderOrigin |
| `src/Collection/CombatTracker.cs` | 新增 `_activeCardOrigin`/`_activePowerSourceOrigin`/`_pendingDrawSourceOrigin`/`_pendingUpgradeSourceOrigin` 字段；`ResolveActiveOrigin(sourceId)` helper；`GetOrCreate` composite-key + origin 优先级（card > power-source > orb-context > override）；`OnCardPlayStarted` 解析 origin；`OnCardPlayFinished`/`ForceResetAllContext` 清理；伤害/块分流路由 upgrader 桶 |
| `src/Patches/CombatHistoryPatch.cs` | 所有 `ModifierContribution` 构造传 origin；`DistributeByPowerSources` 消费 4-tuple；`SetPendingDrawSource`/`SetActiveOrbContext`/`SetPendingOrbFocusContrib`/`RecordStrengthReduction` 等传 origin；`CardUpgradeTrackerPatch.AfterUpgrade` 传 `tracker.ActiveCardOrigin` |
| `src/UI/ContributionChart.cs` | Defense 分母移除 `+UpgradeBlock` |

---

## 对 master 的 Review Checklist

在 master 基础上逐项核实（已有 → 跳过；未有 → 补上）：

### A. Origin 传播

- [ ] `_currentCombat` 是否按 `(cardId, origin)` 复合 key 聚合？master 用的是什么机制？
  - master 有 `PowerSourceEntry.OriginId` 等，说明**数据结构层面**已支持 origin
  - 但**bucket key** 是否 composite 还是扁平 `cardId`？需查 master 的 `GetOrCreate`
- [ ] master 的 `RecordPowerSource` merge 条件是 `(sourceId, originId)` 双匹配（已确认 master 有此行为，见 `ContributionMap.cs:134-142`）
- [ ] 各 FIFO/layer 结构 origin 字段是否齐全（BlockEntry/DebuffLayer/DoomLayer/OrbSource/StrReduction/OstyHp）

### B. Upgrade 归因

- [x] master 已实现 "attribute upgrade bonus to UPGRADE SOURCE" ← **已确认**（master `CombatTracker.cs:621-628`，`Fix 6`）
- [ ] master 的 `UpgradeDelta` **不带 OriginId**（已确认），意味着 potion-生成 Armaments 升级 Strike 与牌库 Armaments 升级 Strike 会坍缩到同一桶
  - **这是 gap** → 需要补上 `UpgradeDelta.UpgraderOrigin` + `CardUpgradeTrackerPatch.AfterUpgrade` 传 `tracker.ActiveCardOrigin`
- [ ] master 使用 `.UpgradeDamage` / `.UpgradeBlock` 字段记 upgrader 桶（非 `.DirectDamage`/`.EffectiveBlock`）—— 这是**设计选择差异**，master 的做法保留"升级增量"作为独立段更清晰，**不改**

### C. Defense 分母

- [ ] master 保留 `+ a.UpgradeBlock` 在分母中。结合 master 把 UpgradeBlock 写在 upgrader 桶（而非双写），这是**一致的**，无需改动
- [ ] Vulnerable/Weak 是否产生正防御贡献污染分母？需跑 master 测试确认
  - master 已删 `DecomposeWeakContribution`（review §11），weak 改走 `MitigatedByDebuff` 独占路径
  - Vulnerable 路径需单独查

### D. Doom 机制

- [ ] master 的 `Catalog_NecrobinderDoomTests.cs` 测试覆盖哪些场景？是否已测 multi-source Doom layer？
- [ ] master 的 DoomLayer 是否带 origin？
- [ ] Doom 杀敌贡献的归因路径是否按 layer 细分？

---

## 重现优先级

1. **最高**：`UpgradeDelta` 补 `UpgraderOrigin` 字段（确认 gap，改动小，2~3 处代码）
2. **中**：核实 master 的 composite bucket key —— 如果 master 的 `_currentCombat` 仍按纯 `cardId` 聚合，则同名卡 origin 细分这个**核心意图**未落地，需要重点补
3. **中**：核实 Vulnerable 正防御污染路径
4. **低**：核实 Doom 测试覆盖与 DoomLayer origin（如果 master 测试已 PASS 则不碰）
