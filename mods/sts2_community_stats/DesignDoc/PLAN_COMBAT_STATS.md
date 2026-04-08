# PLAN: 战斗贡献统计模块重整 — Stage 3 Plan + Review

> 版本：3.0 | 日期：2026-04-08
> 对应 PRD：PRD-00 §4.5.1 + PRD-02 测试矩阵
> 工作流阶段：Stage 3（Plan + Review）完成 → Stage 4 实现完成（含 3 轮 Review 追加修复）

---

## Context

战斗贡献统计是 STS2 Community Stats Mod 最复杂的子系统（~2500 行核心代码）。当前代码 v0.11.0 功能框架已搭建，但 Plan Agent GAP 分析 + Review Agent 对抗性审查发现：4 个 CRITICAL bug、4 个 HIGH 缺失特性、3 个 MEDIUM 精度问题。本 Plan 定义从当前代码到 PRD 完全对齐的实现路径。

---

## 1. GAP 分析总表

### 1.1 直接伤害 (D1-D6) — 全部 PASS

| 测试 | 状态 | 说明 |
|------|------|------|
| D1 单体攻击 | ✅ | OnDamageDealt 正确记录 DirectDamage |
| D2 AOE | ✅ | 每个目标独立 DamageReceived |
| D3 多段 | ✅ | 每段独立记录 |
| D4 末杀 HP 封顶 | ✅ | KillingBlowPatcher + identity dedup |
| D5 末杀不重复 | ✅ | _processedDamageResults HashSet |
| D6 遗物被动伤害 | ✅ | RelicHookContextPatcher 56 个遗物 |

### 1.2 修正器拆分 (M1-M8)

| 测试 | 状态 | Gap | 修复 |
|------|------|-----|------|
| M1 加法:力量 | ⚠️ | 多加法修正器时累积值不准 | M-1 |
| M2 加法:灵巧(格挡) | ❌ | BlockModifierPatch 等分而非逐个计算 | M-2 |
| M3 独立乘法:易伤 | ⚠️ | 用 baseDmg 而非 finalDmg 计算 | M-1 |
| M4 非独立乘法:残酷 | ❌ | **Review 发现: 残酷在 VulnerablePower 内部调用，不是独立修正器** | H2-R |
| M5 多来源 FIFO 层 | ❌ | 仅存最新来源，无逐层追踪 | H1 |
| M6 升级增量 | ❌ | GetUpgradeDelta() 从未被调用（死代码） | C2 |
| M7 升级+力量叠加 | ❌ | 同 M6 | C2 |
| M8 多独立乘区叠加 | ⚠️ | 各乘区用 baseDmg 近似 | M-1 |

### 1.3 间接伤害 (I1-I5)

| 测试 | 状态 | Gap | 修复 |
|------|------|-----|------|
| I1 中毒 | ❌ | 进入 DirectDamage 而非 AttributedDamage | C3 |
| I2 荆棘 | ❌ | 同 I1 | C3 |
| I3 充能球 | ✅ | OrbContextPatcher 正确 |
| I4 Doom | ✅ | DoomKillPatch 正确 |
| I5 自伤 | ✅ | SelfDamage 字段正确 |

### 1.4 来源优先级 (P1-P5) — 基本 PASS

| 测试 | 状态 | Gap |
|------|------|-----|
| P1-P4 | ✅ | ResolveSource 链正确 |
| P5 无上下文 | ⚠️ | 返回 null 静默丢弃 → 需加 UNTRACKED 回退 (H5) |

### 1.5 防御归因 (DEF-1a ~ DEF-5c)

| 测试 | 状态 | Gap | 修复 |
|------|------|-----|------|
| DEF-1a 单来源力量削减 | ❌ | **B8 公式 bug**: `Amount/total*total = Amount`，无 cap 无 hitCount | C1 |
| DEF-1b 多来源力量削减 | ❌ | 同 C1，比例分配是恒等式 | C1 |
| DEF-2a 虚弱减伤 | ⚠️ | `actualDmg/3` 近似正确但非精确 | L1 |
| DEF-2b 非独立减伤(纸鹤) | ❌ | **Review 发现: PaperKrane 在 WeakPower 内部调用** | H2-R |
| DEF-2c 多来源虚弱 FIFO | ❌ | 同 M5 | H1 |
| DEF-2d 独立乘区减伤(巨像) | ✅ | Colossus 是玩家 buff，来自易伤敌人的伤害 ×0.5，prevented=actualDamage 归入 MitigatedByBuff | Fix-6 (Colossus) |
| DEF-3a 无实体 | ✅ | PowerMitigationPatch 正确捕获 before/after |
| DEF-4a-4c 格挡 FIFO | ✅ | ConsumeBlock 正确 |
| DEF-4d 格挡修正拆分 | ❌ | 同 M2 | M-2 |
| DEF-4e 遗物被动格挡 | ✅ | RelicHookContextPatcher 覆盖 |
| DEF-5a-5b Buffer | ⚠️ | 单次命中正确；多段命中需验证 |
| DEF-5c 自伤 | ✅ | SelfDamage 正确 |

### 1.6 Osty (O1-O5)

| 测试 | 状态 | Gap | 修复 |
|------|------|-----|------|
| O1-O3, O5 | ✅ | LIFO 栈/攻击子条/Doom 均正确 |
| O4 Osty 死亡延迟负防御 | ❌ | 立即扣减而非延迟到敌人下次攻击 | H3 |

### 1.7 子来源 (SUB1-3) — 全部 PASS

### 1.8 特殊交互 (S1-S4)

| 测试 | 状态 | Gap |
|------|------|-----|
| S1 追索之刃 | ✅ |
| S2 药水修正 | ✅ |
| S3 多修正叠加 | ⚠️ | 乘法近似，同 M3/M8 |
| S4 Focus | ✅ |

### 1.9 一致性 (F1-F6)

| 测试 | 状态 | Gap | 修复 |
|------|------|-----|------|
| F1 未追踪回退 | ⚠️ | 静默丢弃 | H5 |
| F3 全局汇总 | ❌ | SelfDamage 未深拷贝 | C4 |
| F4 伤害总量 | ⚠️ | 中毒/荆棘分类错 + 升级增量=0 | C3+C2 |
| F5 防御总量 | ❌ | B8 过度计算 | C1 |

---

## 2. Review Agent 关键发现（影响 Plan 设计）

### R1: H2 原方案被 REJECT

**原方案**: "非独立乘法修正器桶拆分" — 用静态字典标记共享桶。

**Review 发现**: CrueltyPower、PaperPhrog 在 VulnerablePower.ModifyDamageMultiplicative() **内部**被调用（`power.ModifyVulnerableMultiplier()`），不作为独立修正器出现在 Hook.ModifyDamage 迭代列表中。同理 PaperKrane、DebilitatePower 在 WeakPower 内部调用。

**游戏机制实测**:
- `VulnerablePower` 基础乘数 1.5，内部依次调用 PaperPhrog(+0.25)、CrueltyPower(+Amount/100)、Debilitate(翻倍)
- `WeakPower` 基础乘数 0.75，内部依次调用 PaperKrane(-0.15)、Debilitate(反转)
- `ColossusPower` 独立乘数 0.5，仅在 dealer 有 Vulnerable 时生效

**修正方案 H2-R**: 不做桶系统，改为**显式分解**：在 DamageModifierPatch 中，当检测到 VulnerablePower/WeakPower 时，主动调用其内部修改器查询各自贡献差值。

### R2: 层数追踪是归因策略而非游戏机制

Vulnerable/Weak 使用 `PowerStackType.Counter`（持续时间，非层数叠加）。多来源施加时结果取决于持续时间合并规则（可能是 max 而非 additive）。FIFO 层归因是 PRD 定义的**归因策略**。

### R3: 升级增量 per-play 低估多段攻击

C2 原方案在 OnCardPlayStarted 一次性归因 delta。多段攻击卡（如连击 4 段，升级 6→9）实际贡献 3×4=12，但 per-play 只记 3。需改为 per-hit 分拆。

### R4: 防御不是单一流水线函数

游戏实际伤害管线：`Hook.ModifyDamage → DamageBlockInternal → ModifyHpLostBeforeOsty → ModifyUnblockedDamageTarget → ModifyHpLostAfterOsty → ModifyHpLostAfterOstyLate → LoseHpInternal`。各步已有独立 patch（Buffer、Intangible），改进方向是确保每步用 prefix/postfix 精确捕获 before/after，而非重建流水线。

---

## 3. 优先级分组

### CRITICAL — 产出错误数值

| ID | Bug | 测试用例 | 文件 |
|----|-----|---------|------|
| C1 | 力量削减公式恒等式 | DEF-1a/1b, F5 | CombatTracker.cs:229-246 |
| C2 | 升级增量死代码 | M6, M7, F4 | CombatTracker.cs + ContributionMap.cs |
| C3 | 中毒/荆棘误分类为 DirectDamage | I1, I2, F4 | CombatTracker.cs:250-321 |
| C4 | SelfDamage 深拷贝遗漏 | F3 | RunContributionAggregator.cs:71-93 |

### HIGH — PRD 缺失特性

| ID | 特性 | 测试用例 | 文件 |
|----|------|---------|------|
| H1 | FIFO 层数归因（多来源 Vuln/Weak） | M5, DEF-2c | ContributionMap.cs |
| H2-R | 显式分解非独立修正器（Cruelty/PaperKrane/Debilitate） | M4, DEF-2b/2d | CombatHistoryPatch.cs |
| H3 | Osty 死亡负防御延迟 | O4 | CombatTracker.cs + ContributionMap.cs |
| H5 | 未追踪来源回退 | P5, F1 | CombatTracker.cs:ResolveSource |

### MEDIUM — 精度改进

| ID | 问题 | 测试用例 | 文件 |
|----|------|---------|------|
| M-1 | 乘法修正器用 finalDmg 公式 | M3, M8, S3 | CombatHistoryPatch.cs:DamageModifierPatch |
| M-2 | 格挡修正逐个计算 | M2, DEF-4d | CombatHistoryPatch.cs:BlockModifierPatch |
| M-3 | 遗物修正归因改进 | S3 | CombatHistoryPatch.cs |

---

## 4. 实现详情

### C1: 修复力量削减公式

**文件**: CombatTracker.cs:229-246

**当前 bug**: `share = (int)Math.Round((float)entry.Amount / totalReduction * totalReduction)` = `entry.Amount`

**新逻辑**:
```
需要知道：敌人每段基础伤害 perHitBase、攻击段数 hitCount
effectiveReduction = min(totalStrReduction, perHitBase)
totalMitigated = effectiveReduction * hitCount
each source share = totalMitigated * (source.Amount / totalStrReduction)
```

**获取 perHitBase**: 新增 `EnemyDamageIntentPatch`，Prefix 在 `Hook.ModifyDamage` 上（仅 `!dealer.IsPlayer` 分支），捕获 pre-modifier 基础伤害存入 `ContributionMap.PendingEnemyBaseDamage`。与现有 `DamageModifierPatch`（仅 `dealer.IsPlayer` 分支）互不干扰。

### C2: 激活升级增量

**文件**: CombatTracker.cs, CombatHistoryPatch.cs

**方案 (per-hit，采纳 Review R3 建议)**:
1. `CardUpgradeTrackerPatch.AfterUpgrade`: 修复来源归因，用 ResolveSource 替代硬编码 "upgrade"
2. `OnCardPlayStarted`: 读取 upgradeDelta 并存为 `_pendingUpgradeDelta`
3. `OnDamageDealt` 敌方分支: 每次命中时，从 directDamage 中拆出 `min(damageDelta, directDamage)` 作为 UpgradeDamage
4. `OnBlockGained`: 类似处理 blockDelta

### C3: 修复中毒/荆棘分类

**文件**: CombatTracker.cs:250-321 (OnDamageDealt 敌方分支)

**新逻辑**: ResolveSource 后判断间接伤害：
```csharp
bool isIndirect = cardSourceId == null 
    && _activeCardId == null 
    && _activePotionId == null
    && (_activePowerSourceId != null || ContributionMap.Instance.HasActiveOrbContext);

if (isIndirect)
    accum.AttributedDamage += directDamage;
else
    accum.DirectDamage += directDamage;
```

遗物触发伤害（`_activeRelicId != null`）仍走 DirectDamage，这是正确的。

### C4: 修复 SelfDamage 深拷贝

**文件**: RunContributionAggregator.cs:71-93

**改动**: 在深拷贝块中添加 `SelfDamage = src.SelfDamage,`。一行修复。

### H1: FIFO 层数归因

**文件**: ContributionMap.cs

**游戏机制**: Vulnerable/Weak 使用 `PowerStackType.Counter`，duration-based。`ApplyInternal` 设置 Amount（持续回合数），多次施加时取 max(current, new)。

**归因策略**: 记录每次施加的来源和 duration 贡献，FIFO 消耗。

**新数据结构**:
```csharp
Dictionary<(int creatureHash, string powerId), List<DebuffLayerEntry>> _debuffLayers;
struct DebuffLayerEntry { string SourceId; string SourceType; int Duration; }
```

**当 power 施加时**: `RecordDebuffLayers(hash, powerId, sourceId, sourceType, duration)`
**当需要归因时**: `GetDebuffSourceFractions(hash, powerId)` → FIFO 返回各来源占比

### H2-R: 显式分解非独立修正器（Review 修正方案）

**文件**: CombatHistoryPatch.cs:DamageModifierPatch.AfterModifyDamage

**方案**: 当在修正器列表中遇到 VulnerablePower 或 WeakPower 时：
1. 读取其 `ModifyDamageMultiplicative` 返回的最终乘数 `finalMult`
2. 主动查询 CrueltyPower/PaperPhrog/DebilitatePower 是否存在
3. 如存在，调用其 `ModifyVulnerableMultiplier()`（传入 baseMult=1.5）获取增量
4. Vulnerable 自身贡献 = 总贡献 × (baseMult-1)/(finalMult-1)
5. Cruelty 贡献 = 总贡献 × crueltyDelta/(finalMult-1)
6. PaperPhrog 贡献 = 总贡献 × phrogDelta/(finalMult-1)

WeakPower 同理分解 PaperKrane/Debilitate。

**ColossusPower**: 独立乘区，用 PRD 公式 `total - total/(1+coeff)` 直接计算。

### H3: Osty 死亡负防御延迟

**文件**: CombatTracker.cs:588-599, ContributionMap.cs

**新状态**: `ContributionMap._pendingOstyDeathDefense: (sourceId, sourceType, amount)?`

**OnOstyKilled**: 不立即扣减，存入 pending。
**OnDamageDealt(isPlayerReceiver=true)**: 开头检查 pending，此时才扣减。

### H5: 未追踪来源回退

**文件**: CombatTracker.cs:159-176

**改动**: `ResolveSource` 返回 null 时改返回 `("UNTRACKED", "untracked")`，并 log 一次 warning 含调用栈信息。

### M-1: 乘法修正公式修正

**文件**: CombatHistoryPatch.cs:DamageModifierPatch

**改动**: 独立乘法修正贡献 = `finalDmg - finalDmg / multiplier`（用最终值而非 baseDmg）。

### M-2: 格挡修正逐个计算

**文件**: CombatHistoryPatch.cs:BlockModifierPatch

**改动**: 仿照 DamageModifierPatch，对每个 power 单独调用 `ModifyBlockAdditive()` 获取精确贡献，替代等分。

---

## 5. 实现阶段（按依赖排序）

### Phase 1: 无依赖快速修复（可并行）
- **C4**: SelfDamage 深拷贝 [1 行]
- **H5**: UNTRACKED 回退 [~10 行]
- **C3**: 中毒/荆棘分类修正 [~15 行]
- **M-2**: 格挡修正逐个计算 [~30 行]

### Phase 2: 升级增量激活
- **C2**: 激活 GetUpgradeDelta + per-hit 拆分 [~40 行, CombatTracker + CombatHistoryPatch]

### Phase 3: 防御公式修复
- **C1**: 力量削减公式 + EnemyDamageIntentPatch [~60 行, 新 patch + CombatTracker]
- **M-1**: 乘法修正公式 [~20 行]

### Phase 4: 高级归因特性
- **H1**: FIFO 层数归因 [~80 行, ContributionMap + patches]
- **H2-R**: 显式分解 Vulnerable/Weak 内部修正器 [~60 行, DamageModifierPatch]

### Phase 5: Osty 延迟
- **H3**: Osty 死亡负防御延迟 [~30 行, CombatTracker + ContributionMap]

---

## 6. 关键文件清单

| 文件 | 当前行数 | 修改范围 |
|------|---------|---------|
| `src/Collection/CombatTracker.cs` | 713 | C1, C2, C3, H3, H5 |
| `src/Collection/ContributionMap.cs` | 461 | H1, H3 |
| `src/Patches/CombatHistoryPatch.cs` | 1521 | C1(新patch), C2, H2-R, M-1, M-2 |
| `src/Collection/RunContributionAggregator.cs` | 169 | C4 |

---

## 7. 验证策略

每个修复完成后，对应 PRD-02 测试用例需通过手动验证：

| 修复 | 验证用例 | 方法 |
|------|---------|------|
| C1 | DEF-1a/1b | DevTools：已知敌人基础伤害 + 力量削减，验证 MitigatedByStrReduction |
| C2 | M6/M7 | DevTools：武装升级打击(6→9)，验证 UpgradeDamage=3 |
| C3 | I1/I2 | DevTools：致命毒药施毒，验证 AttributedDamage 而非 DirectDamage |
| C4 | F3 | 两场战斗后检查 Run 汇总的 SelfDamage |
| H1 | M5/DEF-2c | 卡 A 施加 2 层 Vuln + 卡 B 施加 1 层，验证 A 得 2/3 |
| H2-R | M4/DEF-2b | Vulnerable + Cruelty 共存时，验证各自贡献拆分 |
| H3 | O4 | 消灭 Osty 后无敌人攻击→负防御不显示；敌人攻击后才显示 |
| H5 | P5/F1 | 触发无上下文效果，验证 UNTRACKED 出现 |
| M-1 | M3/M8 | 已知 Vulnerable，验证 ModifierDamage = total - total/1.5 |
| M-2 | M2/DEF-4d | 已知灵巧值，验证 ModifierBlock 精确 |

**全局一致性 (DEF-V1/F4/F5)**: 完成所有修复后，进行完整战斗并验证：
- 伤害总量 = DirectDamage + AttributedDamage + ModifierDamage + UpgradeDamage
- 防御总量 ≈ 敌人意图伤害 - 实际受伤

---

## 8. 3轮 Plan+Review 子 Agent 发现 (Round 3 追加修复)

> 完成日期：2026-04-08
> 3轮循环均通过，质量评估 7/10 → 9/10

### 追加修复清单

| ID | 严重度 | 问题 | 修复方案 | 文件 |
|----|--------|------|---------|------|
| Fix-1 | HIGH | DecrementDebuffLayers 从未被调用，FIFO 层数只增不减 | 新增 DebuffDurationPatch: PowerModel.SetAmount Postfix，检测 amount 减少时调用 Decrement | CombatHistoryPatch.cs |
| Fix-2 | HIGH | OnWeakMitigation 用 `actualDamage/3` 假设 0.75x，PaperKrane/Debilitate 存在时不准 | 在调用处计算实际乘数（含 PaperKrane -0.15、Debilitate 双倍），传入 OnWeakMitigation；公式改为 `damage/mult - damage` | CombatHistoryPatch.cs + CombatTracker.cs |
| Fix-3 | MEDIUM | LastDamageModifiers 可能残留上次调用的数据 | 在 BeforeModifyDamage Prefix 中 Clear | CombatHistoryPatch.cs |
| Fix-4 | MEDIUM | DecomposeVulnerableContribution 用单来源 GetPowerSource，忽略 FIFO 多来源 | 替换为 GetDebuffSourceFractions，按比例分配 vulnShare | CombatHistoryPatch.cs |
| Fix-5 | MEDIUM | ContributionUpload 缺少 6 个字段，上传数据丢失 | 添加 ModifierDamage/ModifierBlock/SelfDamage/UpgradeDamage/UpgradeBlock/OriginSourceId 到 ApiModels + ToUpload | ApiModels.cs + RunContributionAggregator.cs |

### Fix-6: ColossusPower 独立乘区减伤修正器（新增）

**机制澄清**: ColossusPower 是**玩家身上的 buff**（由巨像卡施加），`PowerType.Buff`。
- `target == Owner`（Owner=玩家）→ 当**玩家被攻击**时触发
- `dealer.HasPower<VulnerablePower>()`→ 攻击者（敌人）必须有易伤
- 效果：来自易伤敌人的伤害 ×0.5（减半）
- 知识库描述："来自易伤敌人的伤害降低 50%"

这是一个**防御方向的乘法减伤修正器**，类似 Buffer/Intangible，归入 MitigatedByBuff。

**实现**:
1. CombatHistoryPatch.AfterDamageReceived: 当 isPlayerReceiver 且 dealer 有 VulnerablePower 且 receiver 有 ColossusPower 时，调用 OnColossusMitigation
2. CombatTracker.OnColossusMitigation: prevented = actualDamage（0.5x 乘数意味着无 Colossus 时伤害为 2 倍），归入 GetPlayerBuffSource("COLOSSUS_POWER") 的 MitigatedByBuff
3. DamageModifierPatch（player→enemy 攻击路径）不涉及 Colossus，保持不变

### 延迟项

| 问题 | 原因 | 建议 |
|------|------|------|
| 遗物修正器精度 | 当前近似分配，低影响 | 保持现状 |
| 线程安全 | 理论风险，游戏主逻辑单线程 | 不做处理 |

### 实现要点

- **DebuffDurationPatch**: Patch `PowerModel.SetAmount`（同步 void，安全）。Prefix 捕获 oldAmount，Postfix 检测 `amount < oldAmount` 且为非玩家 VULNERABLE_POWER/WEAK_POWER 时调用 `DecrementDebuffLayers`
- **Weak 乘数计算**: 基础 0.75，PaperKrane -0.15，Debilitate 双倍减伤，最低 0.1 floor
- **Vuln FIFO 分配**: `GetDebuffSourceFractions` 返回各来源占比后乘以 vulnShare，fallback 到 GetPowerSource
