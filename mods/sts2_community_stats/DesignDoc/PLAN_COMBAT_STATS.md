# PLAN: 战斗贡献统计 — 技术实现方案

> 版本：1.0 | 日期：2026-04-07  
> 对应 PRD：PRD-00 §4.5.1、PRD-02  
> 工作流阶段：Stage 3（Plan + Review）

---

## 1. 核心数据结构

### 1.1 ContributionAccum

每个来源（卡牌/遗物/药水）一个实例，累计以下字段：

| 字段 | 含义 | 上传到服务器 |
|------|------|:---:|
| `DirectDamage` | 卡牌效果直接造成的伤害 | No |
| `AttributedDamage` | 间接伤害（中毒、荆棘、Doom 等，归因到施加来源） | No |
| `ModifierDamage` | 数值修正器贡献的伤害增量（力量、易伤、巨像、双倍伤害等） | No |
| `UpgradeDamage` | 战斗内卡牌升级带来的额外伤害 | No |
| `EffectiveBlock` | 实际吸收了伤害的格挡量（FIFO 消耗） | No |
| `ModifierBlock` | 灵巧等修正器贡献的格挡增量 | No |
| `MitigatedByDebuff` | 敌人身上虚弱（Weak）导致的减伤 | No |
| `MitigatedByBuff` | 玩家身上 Buffer/无实体（Intangible）导致的减伤 | No |
| `MitigatedByStrReduction` | 降低敌人力量后避免的受伤 | No |
| `SelfDamage` | 自伤卡牌（放血、祭祀等）造成的 HP 损失 | No |
| `CardsDrawn` | 额外抽牌数（不含每回合正常抽牌） | No |
| `EnergyGained` | 获得的额外能量 | No |
| `StarsContribution` | 星星贡献（漫游者角色特有） | No |
| `HpHealed` | 治疗量 | No |
| `OriginSourceId` | 父来源 ID（用于子条显示，如无限刀刃→碎片） | No |

**所有字段仅用于本地展示。** 上传 payload（`ContributionUpload`）只包含精简聚合字段：`DirectDamage`、`AttributedDamage`、`EffectiveBlock`、`MitigatedByDebuff`、`MitigatedByBuff`、`MitigatedByStrReduction`、`CardsDrawn`、`EnergyGained`、`HpHealed`、`StarsContribution`、`TimesPlayed`。服务端不区分修正器细分来源。

### 1.2 ContributionMap

全局上下文容器，维护以下状态：

| 数据结构 | 用途 |
|----------|------|
| `_blockPool` (Queue) | FIFO 格挡队列：`(sourceId, sourceType, amount)` |
| `_debuffSources` (Dict) | `(creatureHash, powerId) → (sourceId, sourceType)` — 敌人身上 debuff 的施加来源 |
| `_playerBuffSources` (Dict) | `powerId → (sourceId, sourceType)` — 玩家身上 buff 的施加来源 |
| `_strReductions` (Dict) | `creatureHash → List<(sourceId, amount)>` — 敌人力量削减记录 |
| `_cardOriginMap` (Dict) | `cardHash → (originId, originType)` — 卡牌变形/生成的父来源映射 |
| `_ostyHpStack` (Stack) | LIFO 栈：`(sourceId, sourceType, hpAmount)` — Osty HP 来源 |
| `LastDamageModifiers` (List) | 上一次 `Hook.ModifyDamage` 记录的修正器列表 |
| `SeekingEdgeContext` | 追索之刃非主目标伤害的重定向上下文 |
| `ActiveOrbContext` | 当前触发的充能球的引导来源 |

---

## 2. 来源归因解析链（ResolveSource）

当间接效果发生时，按以下优先级查找根本来源：

```
cardSourceId（明确的卡牌来源，由 CombatHistory 传入）
  → _activeCardId（当前正在打出的卡牌，CardPlayFinished Prefix 设置）
    → _activePotionId（当前正在使用的药水，PotionContextPatch 设置）
      → _activeRelicId（当前正在执行钩子的遗物，RelicHookContextPatcher 设置）
        → _activePowerSourceId（当前触发的力量的施加来源，PowerHookContextPatcher 设置）
          → ContributionMap.ActiveOrbContext（当前触发的充能球，OrbContextPatcher 设置）
            → null（无法归因，记入 "UNKNOWN"）
```

**上下文管理**：各 Harmony Patch 在方法执行前 `Set*()` 设置上下文、执行后 `Clear*()` 清除，形成栈式管理。

---

## 3. 伤害计算流程

### 3.1 对敌人造成伤害

入口：`CombatTracker.OnDamageDealt(totalDamage, blockedDamage, cardSourceId, ...)`

```
1. 从 ContributionMap.LastDamageModifiers 取出修正器列表
   （由 Hook.ModifyDamage 的 Harmony Patch 在伤害计算管线中逐个记录）

2. modifierTotal = Σ modifier.Amount

3. directDamage = max(0, totalDamage - modifierTotal)

4. 各 modifier 的 Amount → 该 modifier 来源的 ModifierDamage
   （modifier 来源通过 ContributionMap 中的 power→source 映射查找）

5. directDamage → ResolveSource(cardSourceId) 的 DirectDamage
```

**特殊处理**：

- **SeekingEdge（追索之刃）**：检查 `ContributionMap.SeekingEdgeContext`，非主目标的伤害重定向到 SeekingEdge 力量的施加来源，而非当前打出的卡牌
- **Focus（聚焦）**：充能球伤害中的聚焦加成部分拆分为 `ModifierDamage`，基础伤害归入引导卡牌的 `DirectDamage`
- **KillingBlowPatcher**：补偿 `IsEnding` 标记导致 `DamageReceived` 回调被跳过的问题。Patch `Creature.Die()` 或等效方法，使用 identity-based dedup（攻击者+目标+伤害值+帧号）避免与正常路径重复计数

### 3.2 玩家受到伤害

```
1. _damageTakenByPlayer += actualDamage（用于遭遇战记录的 DamageTaken）

2. 自伤检测：if attackerHash == targetHash
   → ResolveSource(cardSourceId).SelfDamage += actualDamage

3. 格挡归因：if blockedDamage > 0
   → ContributionMap.ConsumeBlock(blockedDamage)
   → FIFO 消耗 _blockPool 中的条目
   → 每段消耗量归入对应来源的 EffectiveBlock

4. 力量削减减伤：
   → ContributionMap.GetStrReductions(attackerHash)
   → 按各来源的削减量比例分配到 MitigatedByStrReduction
```

### 3.3 间接伤害归因

| 来源类型 | Patch 位置 | 归因方法 |
|----------|-----------|---------|
| 中毒（Poison） | `PoisonPower.AfterSideTurnStart` | `OnPowerDamage()` → 查 ContributionMap 中该目标身上 Poison 的施加来源 → `AttributedDamage` |
| 荆棘 / 反伤 | 遗物/力量钩子 | `PowerHookContextPatcher` 设置 `_activePowerSourceId` → `OnDamageDealt()` 通过 ResolveSource 链解析到来源 |
| Doom 击杀 | `DoomPower.Execute` | Prefix 中预先捕获目标 HP → `OnDoomKillsCompleted()` → 将目标剩余 HP 归入 Doom 施加来源的 `AttributedDamage` |
| 充能球（Orb） | 闪电/暗能触发 | `OrbContextPatcher` Prefix 设置 `ActiveOrbContext` → 伤害归入引导卡牌 |

---

## 4. 防御计算流程

### 4.1 FIFO 格挡池

```
打出格挡卡 → OnBlockGained(amount, cardPlayId)
  → source = ResolveSource(cardPlayId)
  → 如有灵巧/Focus 修正器：
      modifierAmount = 总格挡 - 基础格挡
      source.ModifierBlock += modifierAmount
      修正器来源.ModifierBlock += modifierAmount
  → ContributionMap.AddBlock(sourceId, sourceType, amount) 入队

受到伤害 → ConsumeBlock(blockedDamage)
  → while blockedDamage > 0 && _blockPool 非空:
      entry = _blockPool.Peek()
      consumed = min(entry.amount, blockedDamage)
      entry.source.EffectiveBlock += consumed
      blockedDamage -= consumed
      if entry.amount == consumed: _blockPool.Dequeue()
      else: entry.amount -= consumed
```

### 4.2 虚弱减伤

```
OnWeakMitigation(actualDamage, dealerHash):
  mitigated = actualDamage / 3  （虚弱使攻击力降低 25%）
  source = ContributionMap.GetDebuffSource(dealerHash, "WEAK_POWER")
  source.MitigatedByDebuff += mitigated
```

### 4.3 Buffer / 无实体减伤

```
OnBufferPrevention(preventedDamage):
  source = ContributionMap.GetPlayerBuffSource("BUFFER_POWER")
  source.MitigatedByBuff += preventedDamage

OnIntangibleReduction(originalDamage, reducedTo):
  prevented = originalDamage - reducedTo
  source = ContributionMap.GetPlayerBuffSource("INTANGIBLE_POWER")
  source.MitigatedByBuff += prevented
```

### 4.4 力量削减减伤

```
当敌人攻击玩家时:
  reductions = ContributionMap.GetStrReductions(dealerHash)
  totalReduction = Σ reductions[i].amount
  for each reduction:
    proportion = reduction.amount / totalReduction
    reduction.source.MitigatedByStrReduction += mitigatedTotal * proportion
```

---

## 5. Osty（亡灵契约师召唤物）逻辑

### 5.1 LIFO HP 栈

```
召唤/回复 Osty → OnOstySummoned(sourceId, sourceType, hpAmount)
  → ContributionMap.PushOstyHp(sourceId, sourceType, hpAmount)

Osty 吸收伤害 → OnOstyAbsorbedDamage(damage)
  → while damage > 0 && _ostyHpStack 非空:
      entry = _ostyHpStack.Peek()
      consumed = min(entry.hpAmount, damage)
      entry.source.EffectiveBlock += consumed
      damage -= consumed
      if entry.hpAmount == consumed: _ostyHpStack.Pop()
      else: entry.hpAmount -= consumed

Osty 死亡 → OnOstyKilled()
  → 剩余 HP 从击杀来源的 EffectiveBlock 中扣除（负防御）
```

### 5.2 Osty 攻击

```
Osty 对敌造成伤害 → OnOstyDamageDealt(totalDamage, cardSourceId)
  → source = ResolveSource(cardSourceId)
  → source.DirectDamage += totalDamage
  → source.OriginSourceId = "OSTY"  （在图表中作为 OSTY 的子条目显示）
```

---

## 6. 子来源（Sub-bar）追踪

```
CardCmd.Transform 触发（卡牌变形/生成）:
  → ContributionMap.RecordCardOrigin(newCardHash, originId, originType)
  → 存入 _cardOriginMap

打出生成卡牌时:
  → TagCardOrigin(accum, cardHash)
  → if _cardOriginMap 有该 cardHash:
      accum.OriginSourceId = originId

UI 渲染时:
  → 有 OriginSourceId 的条目显示为父来源下方的缩进子条
  → 子条贡献计入父来源的总量条形宽度
```

---

## 7. 治疗追踪

| 场景 | 来源归因 | 写入位置 |
|------|---------|---------|
| 战斗中治疗（收割者等） | ResolveSource 解析当前上下文 | CombatTracker → _currentCombat |
| 战斗后遗物治疗（灼热之血等） | RelicHookContextPatcher 设置遗物上下文 | 直接写入 RunContributionAggregator |
| 休息点、事件、商店 | 用 fallbackId/fallbackType 标记为 "rest"/"event"/"shop" | 直接写入 RunContributionAggregator |

---

## 8. 数据汇总与上传流程

```
战斗结束 → CombatLifecyclePatch 触发 OnCombatEnd()
  → 快照 _lastCombatData（供 UI "本场战斗" Tab 显示）
  → 调用 RunContributionAggregator.AddCombat(_currentCombat, encounterId)
  → 清空 _currentCombat 和 ContributionMap 所有临时状态

局结束 → OnMetricsUpload 触发 RunDataCollector 打包
  → 从 RunContributionAggregator 读取全局汇总数据
  → 转换为 ContributionUpload[]（只含上传字段，丢弃本地展示字段）
  → ApiClient.UploadRunAsync() 上传
```

---

## 9. 三方字段对齐

上传 payload 的 `ContributionUpload` 字段需要与服务端保持一致：

| C# ContributionUpload | Python models.py | SQL contributions 表 |
|------------------------|-------------------|----------------------|
| SourceId | source_id | source_id |
| SourceType | source_type | source_type |
| EncounterId | encounter_id | encounter_id |
| TimesPlayed | times_played | times_played |
| DirectDamage | direct_damage | direct_damage |
| AttributedDamage | attributed_damage | attributed_damage |
| EffectiveBlock | effective_block | effective_block |
| MitigatedByDebuff | mitigated_by_debuff | mitigated_by_debuff |
| MitigatedByBuff | mitigated_by_buff | mitigated_by_buff |
| CardsDrawn | cards_drawn | cards_drawn |
| EnergyGained | energy_gained | energy_gained |
| HpHealed | hp_healed | hp_healed |
| StarsContribution | stars_contribution | stars_contribution |
| MitigatedByStrReduction | mitigated_by_str | mitigated_by_str |

---

## 10. 关键文件列表

| 文件 | 行数 | 职责 |
|------|------|------|
| `src/Collection/CombatTracker.cs` | ~713 | 核心：ResolveSource、OnDamageDealt、OnBlockGained、OnWeakMitigation、Osty 逻辑 |
| `src/Collection/ContributionMap.cs` | ~461 | 上下文容器：FIFO 格挡池、LIFO Osty 栈、debuff/buff 来源映射、修正器列表 |
| `src/Collection/RunContributionAggregator.cs` | — | 全局汇总：AddCombat()、非战斗治疗、FlushAll() |
| `src/Collection/RunDataCollector.cs` | — | 打包上传：组装 RunUploadPayload |
| `src/Patches/CombatHistoryPatch.cs` | ~1521 | Harmony 补丁：60+ 力量钩子、25+ 遗物钩子、核心战斗回调 |
| `src/Patches/CombatLifecyclePatch.cs` | — | 战斗开始/结束生命周期管理 |
| `src/Api/ApiModels.cs` | — | ContributionUpload 定义（上传字段） |

---

## 11. 实现步骤（按依赖顺序）

已实现（v0.11.0）：
1. ContributionAccum 字段扩展（15+ 字段）
2. ResolveSource 解析链
3. FIFO 格挡池
4. LIFO Osty HP 栈
5. KillingBlowPatcher
6. 60+ 力量钩子上下文补丁
7. 25+ 遗物钩子上下文补丁
8. 药水上下文补丁
9. 子来源追踪（CardOriginMap）
10. 伤害修正器拆分（ModifyDamage Patch）

待修复/验证（PRD-02 定义的测试矩阵）：
- D1-D10: 基础伤害归因场景
- B1-B10: 防御归因场景
- O1-O3: Osty 特殊场景
- S1-S4: 特殊卡牌/力量交互
- 6 角色跨角色基础覆盖
