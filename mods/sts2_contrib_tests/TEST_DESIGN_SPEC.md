# 测试设计规范：贡献追踪验证 v3

## 测试目标

**不是测试卡牌/遗物/药水是否生效**，而是验证：
> 当卡牌/遗物/药水产生效果时，该效果的贡献是否被正确归因到正确的 sourceId，写入正确的 ContributionAccum 字段，且数值精确。

## 贡献面板字段（ContributionAccum）

### 伤害类（Damage 面板）
| 字段 | 含义 | 归因计算 |
|------|------|----------|
| DirectDamage | 卡牌基础伤害 | totalDamage - modifierTotal - upgradeDelta |
| ModifierDamage | 加法/乘法修正器贡献 | 力量/活力/遗物加法/Vulnerable 乘区分解 |
| AttributedDamage | 间接伤害 | 中毒 tick、荆棘反射、Power tick（如 Juggernaut） |
| UpgradeDamage | 升级增量 | 战斗内升级带来的额外伤害（从 DirectDamage 拆出） |

### 防御类（Defense 面板）
| 字段 | 含义 | 归因计算 |
|------|------|----------|
| EffectiveBlock | 实际消耗的格挡 | FIFO 队列消耗（需 SimulateDamage 触发） |
| ModifierBlock | 敏捷等修正器贡献的格挡增量 | AfterModifyBlock 中按来源比例分配 |
| MitigatedByDebuff | 敌人虚弱导致的减伤 | 敌人攻击时 Weak 乘区差值 |
| MitigatedByBuff | Buffer/Intangible 减伤 | 玩家受击时 Buffer/Intangible 阻止的伤害 |
| MitigatedByStrReduction | 削力减伤 | DarkShackles/Malaise 降低敌人 Str 避免的伤害 |
| SelfDamage | 自伤 | 卡牌对自身造成的 HP loss |
| UpgradeBlock | 升级格挡增量 | 战斗内升级带来的额外格挡 |

### 资源类
| 字段 | 含义 | 面板 |
|------|------|------|
| CardsDrawn | 非手牌阶段的抽牌（fromHandDraw=false） | Draw |
| EnergyGained | 获得能量 | Energy |
| HpHealed | 治疗量 | Healing |
| StarsContribution | 辉星获取/节省 | Stars |

## 归因规则

| 效果来源 | sourceId（delta key） | 示例 |
|----------|----------------------|------|
| 打出卡牌直接效果 | 卡牌 ID | `STRIKE_IRONCLAD` |
| Power hook 间接效果 | 施加该 Power 的卡牌/遗物/药水 ID | DemonForm 给的力量 → `DEMON_FORM` |
| 遗物 hook 效果 | 遗物 ID | `VAJRA` |
| 药水效果 | 药水 ID | `FIRE_POTION` |
| 修正器加法（Str/Dex/遗物） | 按来源比例分配（多来源支持） | Inflame(2)+Vajra(1) → 2:1 分配 |
| 修正器乘法（Vulnerable） | DecomposeVulnerableContribution 分解 | base/PaperPhrog/Cruelty/Debilitate 各自分配 |
| 子来源（生成卡） | 生成的卡自身 ID，OriginSourceId 指向父 | Shiv → `SHIV`，OriginSourceId=`BLADE_DANCE` |
| Forge 子来源 | `FORGE:来源ID` | `FORGE:BULWARK`，OriginSourceId=`SOVEREIGN_BLADE` |

### 多来源按比例分配（已修复）

当多个来源施加同一种 Power 时（如 Inflame+2 Str + Vajra+1 Str），`RecordPowerSource` 记录所有来源及各自 Amount。`DistributeByPowerSources` 按 Amount 占比分配 ModifierDamage/ModifierBlock：
- Inflame(2) + Vajra(1) → Str=3，Strike ModifierDamage=3 → INFLAME=2, VAJRA=1

### Vulnerable/Weak 乘区分解

`DecomposeVulnerableContribution` 精确逻辑：
```
totalContrib = floor(finalDmg - finalDmg / combinedMult)
vulnBaseDelta = 0.5
phrogDelta = 0.25 (if PaperPhrog equipped)
crueltyDelta = CrueltyPower.Amount / 100 (e.g. 25 → 0.25)
debilitateDelta = (vulnBase + phrog + cruelty) if Debilitate (doubles bonus)
subTotal = vulnBase + phrog + cruelty + debilitate

vulnShare = round(totalContrib × vulnBase / subTotal) → 归因到施加 Vuln 的来源（FIFO）
crueltyShare = round(totalContrib × cruelty / subTotal) → 归因到 CRUELTY 卡
phrogShare = round(totalContrib × phrog / subTotal) → 归因到 PAPER_PHROG 遗物
```

**精确计算示例**（Cruelty 25% + Vulnerable on Strike 6）：
- combinedMult = 1.5 + 0.25 = 1.75
- finalDmg = floor(6 × 1.75) = 10
- totalContrib = 10 - floor(10 / 1.75) = 10 - 5 = 5（注意：10/1.75=5.71→floor=5）
- subTotal = 0.5 + 0.25 = 0.75
- vulnShare = round(5 × 0.5/0.75) = round(3.33) = 3
- crueltyShare = round(5 × 0.25/0.75) = round(1.67) = 2

## OnDamageDealt 关键行为

1. **每一段 hit 单独调用**：SwordBoomerang 3×3 = 三次 OnDamageDealt 各 3，DirectDamage 累加到 9
2. **AoE 对每个敌人分别调用**：Thunderclap 打 2 敌人各 4 = DirectDamage 累加到 8
3. **DirectDamage = max(0, totalDamage - modifierTotal)**
4. **modifier 溢出缩放**：当 sum(modifier) > totalDamage 时，按比例缩小每个 modifier
5. **UpgradeDamage 从 DirectDamage 中拆出**：upgSplit = min(upgradeDelta, directDamage)

## 断言规则

### 必须精确断言（AssertEquals）：
- DirectDamage = base damage × hit count（无修正器干扰时）
- EffectiveBlock = base block（无修正器干扰时）
- CardsDrawn = 卡牌描述的抽牌数
- ModifierDamage = 精确计算值（加法直接 / 乘法按分解公式）
- ModifierBlock = 精确计算值
- AttributedDamage = 精确毒/荆棘/Power tick 值
- SelfDamage = 卡牌描述的 HP loss
- HpHealed = 卡牌/遗物/药水描述的回血量
- StarsContribution = 卡牌描述的辉星数

### 禁止的断言模式：
- ❌ 只检查游戏状态（如"敌人有 PoisonPower"而不检查 AttributedDamage）
- ❌ `result.Passed = true` 无任何断言
- ❌ 可精确时用 `> 0`
- ❌ combo 牌只检查部分字段

---

## 标准测试模板

### A：纯攻击牌 → DirectDamage
```
清理: Remove<StrengthPower>(player), Remove<VulnerablePower>(enemy)
步骤: CreateCardInHand → TakeSnapshot → PlayCard(enemy) → GetDelta
断言: delta["X"].DirectDamage == baseDamage
多段: delta["X"].DirectDamage == baseDamage × hitCount
AoE:  delta["X"].DirectDamage == baseDamage × enemyCount
```

### B：纯格挡牌 → EffectiveBlock
```
清理: ClearBlock(), Remove<DexterityPower>, Remove<FrailPower>
步骤: CreateCardInHand → TakeSnapshot → PlayCard → SimulateDamage(99) → GetDelta
断言: delta["X"].EffectiveBlock == baseBlock
```

### C：抽牌牌 → CardsDrawn
```
清理: EnsureDrawPile(N+5)
步骤: CreateCardInHand → TakeSnapshot → PlayCard → GetDelta
断言: delta["X"].CardsDrawn == N
注意: fromHandDraw=true 的回合开始抽牌不计入
```

### D：combo 牌 → 多字段
```
清理: ClearBlock + Remove Str/Dex/Vuln + EnsureDrawPile
步骤: CreateCardInHand → TakeSnapshot → PlayCard(enemy) → SimulateDamage(99) → GetDelta
断言: 逐字段 AssertEquals（DirectDamage + EffectiveBlock + CardsDrawn + ...）
```

### E：自伤牌 → SelfDamage
```
步骤: CreateCardInHand → TakeSnapshot → PlayCard → GetDelta
断言: delta["X"].SelfDamage == hpLoss
```

### F：Power 间接效果 → 归因到源卡
```
步骤: PlayCard(PowerCard) → 准备触发条件 → ClearBlock/EnsureDrawPile → TakeSnapshot → 触发 → SimulateDamage if block → GetDelta
断言: delta["POWER_CARD_ID"].字段 == 精确值
清理: finally Remove<Power>

示例 Rage(3): 打 Rage → ClearBlock → Snapshot → 打 Strike → SimulateDamage(99) → delta["RAGE"].EffectiveBlock == 3
```

### G：Power 手动 hook（回合类）
```
步骤: PlayCard(PowerCard) → GetPower → ClearBlock/EnsureDrawPile → TakeSnapshot → SetActivePowerSource → hook() → ClearActivePowerSource → SimulateDamage → GetDelta
断言: delta["POWER_CARD_ID"].字段 == 精确值
```

### H：遗物效果 → 归因到遗物 ID
```
步骤: ObtainRelic → TriggerRelicHook / PlayCard → TakeSnapshot → 触发 → GetDelta
断言: delta["RELIC_ID"].字段 == 精确值
清理: finally RemoveRelic
```

### I：药水效果 → 归因到药水 ID
```
步骤: TakeSnapshot → UsePotion(target) → SimulateDamage if block → GetDelta
断言: delta["POTION_ID"].字段 == 精确值
```

### J：伤害修正器 → ModifierDamage 按来源分配
```
加法(Str from Inflame+2):
  Remove<Str> → PlayCard(Inflame) → Snapshot → PlayCard(Strike, enemy) → delta["INFLAME"].ModifierDamage == 2

乘法(Vuln from Bash):
  Remove<Vuln>(enemy) → PlayCard(Bash, enemy) → Snapshot → PlayCard(Strike, enemy)
  Strike base=6, Vuln 1.5x → final=9 → totalContrib=9-floor(9/1.5)=9-6=3
  delta["BASH"].ModifierDamage == 3
```

### K：格挡修正器 → ModifierBlock 按来源分配
```
Footwork(+2 Dex):
  Remove<Dex> → PlayCard(Footwork) → ClearBlock → Snapshot → PlayCard(Defend) → SimulateDamage(99)
  Defend base=5+Dex 2=7 → delta["DEFEND_X"].EffectiveBlock == 5, delta["FOOTWORK"].ModifierBlock == 2
注意: EffectiveBlock 只记录 base block，ModifierBlock 记录敏捷加成
```

### L：卡牌生成 → sub-bar
```
BladeDance → Shiv:
  PlayCard(BladeDance) → 获取手中 Shiv → Snapshot → PlayCard(Shiv, enemy)
  delta["SHIV"].DirectDamage == 4 (Shiv base=4)
  delta["SHIV"].OriginSourceId == "BLADE_DANCE"

AttackPotion → 随机攻击牌:
  Snapshot → UsePotion<AttackPotion>() → 获取手中新牌 → PlayCard(新牌, enemy)
  delta[新牌ID].DirectDamage > 0（随机牌无法精确，唯一允许 > 0 的场景）
```

### M：治疗 → HpHealed
```
步骤: 先让玩家受伤（有空间治疗）→ TakeSnapshot → 触发治疗 → GetDelta
断言: delta["X"].HpHealed == 精确值
```

### N：MitigatedByDebuff（敌人虚弱减伤）
```
需要 EndTurn 让敌人攻击，结果不确定（敌人可能不攻击）。
用 SimulateDamage from 有 Weak 的敌人代替:
  ApplyPower<WeakPower>(enemy, 3) → ClearBlock(player无block) → Snapshot → 
  SimulateDamage(player, 10, enemy) → delta 中 MitigatedByDebuff 归因到 Weak 来源
注意：SimulateDamage 可能不经过 Weak 乘区（Weak 只影响敌人的攻击意图伤害，不影响我们手动 SimulateDamage）
→ 此类测试需要真正的 EndTurn 让敌人攻击，属于 timing-dependent
```

### O：MitigatedByBuff（Buffer/Intangible）
```
Buffer: ApplyPower<BufferPower>(player, 1) → Snapshot → SimulateDamage(player, 10, enemy) → delta 中 MitigatedByBuff
Intangible: ApplyPower<IntangiblePower>(player, 1) → 同上
```

---

## 角色特有机制

### 猎手 Silent

#### 中毒 Poison
- **机制**：PoisonPower 在敌人回合开始时 tick，造成 Amount 不可格挡伤害，然后递减
- **归因链路**：PoisonPower.AfterSideTurnStart → CreatureCmd.Damage(Unblockable|Unpowered) → OnDamageDealt → AttributedDamage。PowerHookContextPatcher 在 tick 时设置 power context → GetPowerSource("POISON_POWER") 映射到施毒来源
- **精确值**：首次 tick = 施加的毒层数 N（tick 后递减为 N-1）
- **Accelerant**（加速剂）：PoisonPower.TriggerCount 增加，一回合 tick 多次

```
测试步骤：
1. Remove<PoisonPower>(enemy)
2. PlayCard(DeadlyPoison, enemy) → 施加 5 毒
3. TakeSnapshot()
4. EndTurnAndWaitForPlayerTurn() → 毒 tick 触发
5. delta["DEADLY_POISON"].AttributedDamage == 5
```

#### 小刀 Shiv
- **基础数值**：Shiv base damage = **4**（STS2，非 STS1 的 1）
- **归因链路**：BladeDance 生成 Shiv → Shiv 打出 → OnDamageDealt → DirectDamage 归到 SHIV，OriginSourceId = BLADE_DANCE（sub-bar）
- **Accuracy**：AccuracyPower 给 Shiv 的 ModifyDamageAdditive = Amount(4)
- **精确值（有 Accuracy）**：Shiv DirectDamage = 4(base), AccuracyPower ModifierDamage = 4 → total hit = 8

```
测试步骤（Accuracy + Shiv）：
1. Remove<StrengthPower>
2. PlayCard(Accuracy) → AccuracyPower(4)
3. PlayCard(BladeDance) → 生成 3 Shiv
4. 获取 Shiv from hand
5. TakeSnapshot()
6. PlayCard(Shiv, enemy)
7. delta["SHIV"].DirectDamage == 4
8. delta["ACCURACY"].ModifierDamage == 4（via GetPowerSource("ACCURACY_POWER")=ACCURACY）
```

#### Sly 灵巧关键词
- 卡牌被弃牌后自动打出
- **贡献追踪无影响**：Sly 不改变伤害/格挡计算，只影响牌堆位置

---

### 机器人 Defect

#### 充能球 Orb
- **球种及精确数值**（无 Focus 时）：
  | 球种 | 被动值 | 弹出值 | 效果类型 |
  |------|--------|--------|----------|
  | Lightning | 3 + Focus | 8 + Focus | 伤害（随机敌人） |
  | Frost | 2 + Focus | 5 + Focus | 格挡（玩家） |
  | Dark | 6 + Focus（蓄力） | 蓄力值 + Focus | 伤害（最低HP敌人） |
  | Plasma | 1（固定） | 2（固定） | 能量（不受 Focus 影响） |
  | Glass | ? | ? | 特殊 |

- **归因链路**：
  1. `OrbChanneledPatch.AfterOrbChanneled` 记录 orbHash → (sourceId, sourceType, orbType)
  2. `OrbPassivePatch.BeforeOrbPassive` 设置 orb context（channeling source 作为 source）
  3. 被动/弹出产生的伤害/格挡 → AttributedDamage/EffectiveBlock 归因到引导该球的卡牌
  4. Focus 加成 → 从 orb 效果中拆出 → ModifierDamage/ModifierBlock 归因到 Focus 来源

- **首次触发逻辑**：在卡牌打出期间，第一次 orb 触发归因到引导卡，后续触发归因到打出的卡（_activeCardId）

```
测试步骤（Lightning 被动 tick）：
1. PlayCard(Zap) → Channel Lightning
2. TakeSnapshot()
3. EndTurnAndWaitForPlayerTurn() → Lightning 被动 tick 3 伤害
4. delta["ZAP"].AttributedDamage == 3（无 Focus 时）
注意：Lightning 打随机敌人，伤害可能被格挡。用 AssertGreaterThan(0) 或确保敌人无 block。
```

```
测试步骤（Focus 加成拆分）：
1. PlayCard(Defragment) → +1 Focus
2. PlayCard(Zap) → Channel Lightning
3. TakeSnapshot()
4. EndTurnAndWaitForPlayerTurn() → Lightning 被动 tick = 3+1=4
5. delta["ZAP"].AttributedDamage == 3（base）
6. delta["DEFRAGMENT"].ModifierDamage == 1（Focus 拆分）
```

#### 引导 Channel / 弹出 Evoke
- Channel 通过 `OrbCmd.Channel` 触发，归因到当前打出的卡牌
- Evoke 通过 `OrbCmd.Evoke` 触发（Dualcast/MultiCast/球满自动弹出）
- **测试弹出**：Channel → Dualcast → 弹出 2 次 → 伤害/格挡归因到引导卡

---

### 储君 Regent

#### 辉星 Stars
- **追踪路径**：`Hook.AfterStarsGained` → `CombatTracker.OnStarsGained(amount)` → `GetOrCreate(sourceId).StarsContribution += amount`
- **归因**：由打出的卡牌或遗物的 active context 决定
- **精确值**：卡牌描述的辉星数

```
测试步骤：
1. CreateCardInHand<GatherLight>() → base Stars=1
2. TakeSnapshot()
3. PlayCard(card)
4. delta["GATHER_LIGHT"].StarsContribution == 1
注意：同时验证 EffectiveBlock == 7（combo 牌多字段断言）
```

#### 锻造 Forge
- **机制**：`ForgeCmd.Forge(amount, player, source)` 增加 SovereignBlade 基础伤害
- **追踪路径**：
  1. `ForgeCmdPatch.AfterForge` postfix → `CombatTracker.OnForge(sourceId, sourceType, amount)` 记录到 `_forgeLog`
  2. `SovereignBladeSeekingEdgePatch.BeforeSovereignBlade` prefix → `FlushForgeSubBars()` 将 log 写入 ContributionAccum sub-bar 条目
- **sub-bar 结构**：
  - `FORGE:X` (OriginSourceId="SOVEREIGN_BLADE") → DirectDamage = forge amount from X
  - `FORGE:BASE` (OriginSourceId="SOVEREIGN_BLADE") → DirectDamage = 10 (SovereignBlade 初始伤害)
- **SovereignBlade 数值**：base damage = 10，每次 Forge N → damage += N

```
测试步骤：
1. ClearBlock() → PlayCard(Bulwark) → Forge 10 + Block 13
2. 获取 SovereignBlade from hand（ForgeCmd 自动创建）
3. TakeSnapshot()
4. PlayCard(SovereignBlade, enemy)
5. delta["SOVEREIGN_BLADE"].DirectDamage == 20 (10 base + 10 forge)
6. delta["FORGE:BULWARK"].DirectDamage == 10（sub-bar）
7. delta["FORGE:BASE"].DirectDamage == 10（sub-bar）
```

```
测试步骤（多来源 Forge）：
1. PlayCard(Bulwark) → Forge 10
2. PlayCard(Furnace power) → 手动触发 FurnacePower.AfterSideTurnStart → Forge 4
3. SovereignBlade damage = 10 + 10 + 4 = 24
4. delta["FORGE:BULWARK"].DirectDamage == 10
5. delta["FORGE:FURNACE"].DirectDamage == 4
6. delta["FORGE:BASE"].DirectDamage == 10
```

#### 君主之刃 SovereignBlade
- **特殊行为**：
  - base damage 10，通过 Forge 累加
  - Retain 关键词（回合结束不弃）
  - 受力量/易伤等修正（IsPoweredAttack = true）
  - SeekingEdge power 使其攻击全体敌人（非主目标伤害归到 SeekingEdge 来源）

---

### 亡灵契约师 Necrobinder

#### 测试范围
- **本次测试**：所有不涉及 Osty/Soul/Summon 的亡灵契约师卡牌（约 58 张）
- **独立脚本（后续）**：涉及 Osty 的卡牌（Afterlife, Bodyguard, BoneShards, CaptureSpirit, DevourLife, Dirge, Fetch, Flatten, GraveWarden, Haunt, HighFive, Invoke, LegionOfBone, NecroMastery, Poke, Protector, PullAggro, Rattle, Reanimate, Reave, RightHandHand, Sacrifice, Seance, Severance, SicEm, Snap, Soul, SoulStorm, Spur, Squeeze, Unleash）

#### 灾厄 Doom
- **机制**：DoomPower 是 debuff，回合结束时若敌人 HP ≤ Doom 层数则 `CreatureCmd.Kill`
- **追踪链路**：`OnDoomTargetCapture(creatureHash, currentHp)` 记录敌人 HP → `DoomKill` 触发 → `OnDoomKillsCompleted()` 将总 HP 作为 AttributedDamage 归因到施加 Doom 的来源（`GetPowerSource("DOOM_POWER")`）
- **精确值**：AttributedDamage = 被杀敌人的剩余 HP 总和

```
测试步骤（Doom 致死归因）：
1. 将敌人 HP 降到极低（如 5）
2. PlayCard(BlightStrike, enemy) → 施加 Doom（base damage=10 + apply Doom）
   - BlightStrike: 10 damage + N Doom
3. 此时敌人 HP ≤ Doom → 回合结束时 DoomKill 触发
4. TakeSnapshot() 在 EndTurn 前
5. EndTurnAndWaitForPlayerTurn()
6. delta["BLIGHT_STRIKE"].AttributedDamage == 敌人被杀时的剩余 HP

注意：Doom 致死只在回合结束时触发，需要 EndTurn。
如果敌人 HP 已被 BlightStrike 的直接伤害杀死，Doom 不触发。
测试需精确控制敌人 HP：先伤害到低于 Doom 值但不致死。
```

```
简化测试（验证 Doom 施加）：
1. Remove<DoomPower>(enemy)
2. PlayCard(BlightStrike, enemy) → 施加 Doom
3. 检查 enemy.GetPower<DoomPower>().Amount > 0
4. 同时验证 delta["BLIGHT_STRIKE"].DirectDamage == 10（直接伤害部分）
```

#### 灾厄相关卡牌列表
| 卡牌 | 效果 | 测试要点 |
|------|------|----------|
| BlightStrike | 10 伤害 + 施加 Doom | DirectDamage=10 + DoomPower 施加 |
| BorrowedTime | 施加 Doom（power） | DoomPower 施加 |
| Deathbringer | 大伤害 + 施加 Doom | DirectDamage + DoomPower |
| EndOfDays | AoE + 施加 Doom | DirectDamage + DoomPower |
| NegativePulse | 伤害 + 施加 Doom | DirectDamage + DoomPower |
| NoEscape | 施加 Doom | DoomPower 施加 |
| Scourge | 伤害 + 施加 Doom | DirectDamage + DoomPower |
| ReaperForm | Power: 攻击时施加伤害×N Doom | DoomPower 施加验证 |
| Countdown | Power: 回合开始施加 Doom | 手动 hook 触发 |
| TimesUp | Doom 相关效果 | 验证 |

#### 其他可测试的亡灵契约师卡牌（非 Osty）
| 类型 | 卡牌 | 效果 | 测试模板 |
|------|------|------|----------|
| 攻击 | SculptingStrike, Eradicate, TheScythe, Dredge | 伤害 + 附加效果 | A/D |
| 格挡 | Defy, Eidolon, Bury | 格挡 + 附加效果 | B/D |
| 抽牌 | Parse, GlimpseBeyond | 抽牌 | C |
| 自伤 | DeathsDoor, Defile | HP loss | E |
| 力量削减 | DrainPower, EnfeeblingTouch, Fear | 敌人 -Str | MitigatedByStrReduction(EndTurn) |
| debuff | Debilitate, Melancholy, Misery, Putrefy | 施加减益 | Doom/debuff 验证 |
| Power | BorrowedTime, Countdown, DanseMacabre, Demesne, Lethality, Oblivion, PagestormPower, ReaperForm, Shroud, SleightOfFlesh, SpiritOfAsh, Veilpiercer | Power 效果 | F/G |
| 能量 | Delay, Friendship | 能量相关 | 特殊 |

#### Osty（独立脚本，后续实现）
- **机制**：Osty 有独立 HP 池，可替主人承受伤害（Die for You）
- **追踪结构**：LIFO 栈追踪每个 Osty 的 HP 来源
- **独立测试脚本**：`Catalog_OstyTests.cs`

---

## 复杂交互场景

### 示例：多修正器叠加（已修复多来源分配）

操作：VulnerablePotion → Inflame(+2 Str) → Cruelty(25%) → SetupStrike(7dmg+2临时Str) → Tremble(+2 Vuln) → Vajra(+1 Str) → MiniatureCannon → StrikeDummy → Armaments升级Strike → 打出 Strike+

打出 Strike+ 时：
- base = 9 (6+3 升级), UpgradeDelta = 3
- Str = 2(Inflame) + 2(SetupStrike) + 1(Vajra) = 5 → 按 2:2:1 分配
- StrikeDummy +3, MiniatureCannon +3
- additive total = 9 + 5 + 3 + 3 = 20
- Vuln multiplier = 1.5 + 0.25(Cruelty) = 1.75
- final = floor(20 × 1.75) = 35

预期 delta:
| sourceId | DirectDamage | ModifierDamage | UpgradeDamage |
|----------|-------------|----------------|---------------|
| STRIKE_IRONCLAD | 6 | 0 | 0 |
| INFLAME | 0 | 2 | 0 |
| SETUP_STRIKE | 0 | 2 | 0 |
| VAJRA | 0 | 1 | 0 |
| STRIKE_DUMMY | 0 | 3 | 0 |
| MINIATURE_CANNON | 0 | 3 | 0 |
| ARMAMENTS | 0 | 0 | 3 |
| VULNERABLE_POTION | 0 | 10 | 0 |
| CRUELTY | 0 | 5 | 0 |

（Vuln 分解: totalContrib=35-floor(35/1.75)=35-20=15, vulnShare=round(15×0.5/0.75)=10, crueltyShare=round(15×0.25/0.75)=5）
（DirectDamage = 35 - (2+2+1+3+3+3+10+5) = 35-29 = 6 = base未升级部分）

### 场景 2：防御综合（EffectiveBlock + ModifierBlock + MitigatedByDebuff + MitigatedByBuff + MitigatedByStrReduction + SelfDamage）

**目标**：在一个回合内验证所有防御分支

**操作序列**：
1. 打出 Footwork(+2 Dex) → ModifierBlock 来源
2. 获得遗物 CloakClasp（手牌数量=N 格挡 at turn end）
3. 打出 Offering(失去 6 HP + 获得 2 能量 + 抽 3) → SelfDamage=6
4. 打出 DarkShackles(敌人 -9 临时 Str) → MitigatedByStrReduction 来源
5. 打出 Neutralize(3 dmg + 施加 2 Weak on enemy) → MitigatedByDebuff 来源（EndTurn 后）
6. 打出 Buffer card(Intangible/Buffer) → MitigatedByBuff 来源
7. ClearBlock → 打出 Defend(5 + 2 Dex = 7 block)
8. TakeSnapshot → EndTurnAndWaitForPlayerTurn（敌人攻击触发全部防御分支）

**预期 delta**：
| sourceId | EffectiveBlock | ModifierBlock | SelfDamage | MitigatedByStrReduction | MitigatedByDebuff |
|----------|---------------|---------------|------------|------------------------|-------------------|
| DEFEND_IRONCLAD | 5 | 0 | 0 | 0 | 0 |
| FOOTWORK | 0 | 2 | 0 | 0 | 0 |
| OFFERING | 0 | 0 | 6 | 0 | 0 |
| DARK_SHACKLES | 0 | 0 | 0 | ≤9 (capped by enemy base) | 0 |
| NEUTRALIZE | 0 | 0 | 0 | 0 | >0 (timing-dependent) |

注意：MitigatedByDebuff 和 MitigatedByStrReduction 依赖敌人攻击，数值取决于敌方行为。

### 场景 3：抽牌综合（CardsDrawn 多来源）

**操作序列**：
1. EnsureDrawPile(20) → 确保有足够牌
2. 获得遗物 GamePiece（打 Power → 抽 1）
3. 打出 DarkEmbrace（消耗牌时抽 1）
4. 打出 Acrobatics（抽 3 弃 1）
5. Snapshot → 打出 Corruption（Power 牌→触发 GamePiece 抽 1）
6. 打出 Defend（Corruption 使其免费+消耗→触发 DarkEmbrace 抽 1）

**预期 delta**：
| sourceId | CardsDrawn |
|----------|------------|
| CORRUPTION | 0（Power 本身不抽牌） |
| GAME_PIECE | 1（打 Power 触发） |
| DARK_EMBRACE | 1（Defend 消耗触发） |

### 场景 4：辉星综合（StarsContribution 多来源）

**操作序列**：
1. 获得遗物 LunarPastry（回合末+1 星）
2. 打出 Genesis power（回合开始+2 星）
3. 打出 GatherLight（+1 星 + 7 格挡）
4. 打出 Venerate（+N 星）
5. TakeSnapshot → EndTurn → 触发 LunarPastry + Genesis

**预期 delta**：
| sourceId | StarsContribution |
|----------|-------------------|
| GATHER_LIGHT | 1 |
| VENERATE | N（查实际值） |
| LUNAR_PASTRY | 1（回合末） |
| GENESIS | 2（回合开始） |

### 场景 5：治疗综合（HpHealed 多来源）

**操作序列**：
1. 让玩家大量受伤（空间充足）
2. 获得遗物 BurningBlood（战斗胜利后回 6 HP）
3. 打出 RegenPotion（Regen 5 → 回合末回 5 HP）
4. 获得遗物 BloodVial（回合开始回 2 HP）
5. TakeSnapshot → EndTurn（Regen tick 回 5 HP + BloodVial 回 2 HP）

**预期 delta**：
| sourceId | HpHealed |
|----------|----------|
| REGEN_POTION | 5（Regen tick） |
| BLOOD_VIAL | 2（回合开始） |

---

## 清洁环境检查清单

- [ ] 伤害测试：`Remove<StrengthPower>(player)`, `Remove<VulnerablePower>(enemy)`, `Remove<WeakPower>(enemy)`
- [ ] 格挡测试：`ClearBlock()`, `Remove<DexterityPower>`, `Remove<FrailPower>`
- [ ] 抽牌测试：`EnsureDrawPile(N+5)`, `Remove<NoDrawPower>`
- [ ] X 费牌：`SetEnergy(3)` 限制，打完恢复 `SetEnergy(999)`
- [ ] 遗物 RoundNumber 守卫：临时设 `CombatState.RoundNumber`
- [ ] 能量测试：AutoPlay EnergySpent=0，无法测试费用节省型能量追踪

## 收尾清理检查清单（finally 块）

- [ ] Remove 施加的 Power
- [ ] RemoveRelic 获得的遗物
- [ ] LoseBlock 残留格挡
- [ ] 恢复 RoundNumber/Energy
