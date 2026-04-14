# 伪 PASS 审计报告 — sts2_contrib_tests Round 14

**日期**：2026-04-13
**基线**：Round 14 第二轮回归运行，433 total / 330 PASS / 39 FAIL / 64 skip

## 审计标准

### 标准 A — 硬伪 PASS
断言完全不涉及贡献字段 delta（只检查 `power.Amount`、`Passed=true`、`CurrentHp` 差值、游戏状态）。

### 标准 B — 触发链断裂
断言 `delta[X].Field` 形式正确，但 X 的贡献字段需要下游事件触发（致死/回合结束/受击/抽 N 张），而测试未触发该事件。

### 标准 C — 弱断言
用 `AssertGreaterThan(..., 0)` 但可精确计算；或断言某字段却漏掉同卡牌的另一贡献字段（如 Break 只查 DirectDamage 未查 Vuln 施加的副作用）。

## 发现汇总

| 级别 | 数量 |
|---|---|
| A 类硬伪 PASS | 49 |
| B 类触发链断裂 | 6 |
| C 类弱断言 | 11 |
| 可疑项 | 3 |
| **总计** | **69** |

**真实 PASS 数估算**：330 − 49 = 281（真实覆盖率 76.2% vs 账面 89%）

**核心发现**：Doom 致死归因链路（`OnDoomKillsCompleted → AttributedDamage`）在整个 PASS 套件中**从未被任何测试验证过**。所有 Doom PASS 测试都只断言 `DoomPower.Amount`。

---

## A 类：硬伪 PASS（49 条）

### Catalog_NecrobinderDoomTests.cs（5 条 A）

#### [NB-DOOM-Deathbringer] L74
- 当前：`AssertEquals("Enemy.DoomPower.Amount", 21, amt)`
- 缺失：未致死，`delta["DEATHBRINGER"].AttributedDamage` 未运行
- 正确：低 HP + EndTurn → `delta["DEATHBRINGER"].AttributedDamage == 敌人剩余 HP`

#### [NB-DOOM-EndOfDays] L106
- 当前：`AssertEquals("Enemy.DoomPower.Amount", 29, amt)`
- 正确：低 HP + EndTurn → `delta["END_OF_DAYS"].AttributedDamage`

#### [NB-DOOM-NoEscape] L166
- 当前：只断言 `DoomPower.Amount == 10`
- 正确：低 HP + EndTurn → `delta["NO_ESCAPE"].AttributedDamage`

#### [NB-DOOM-ReaperForm] L229
- 当前：`AssertEquals("Enemy.DoomPower.Amount", 6, amt)`
- 正确：低 HP + EndTurn → `delta["REAPER_FORM"].AttributedDamage` 或 `delta["STRIKE_NECROBINDER"].DirectDamage == 6`

#### [NB-DOOM-Countdown] L277
- 当前：`AssertEquals("CountdownPower.Amount", 6, amt)`
- 缺失：未手动驱动 `AfterSideTurnStart`，Countdown 施加 Doom 逻辑未运行
- 正确：驱动 hook → 低 HP → `delta["COUNTDOWN"].AttributedDamage`

### Catalog_NecrobinderCardTests.cs（3 条 A）

#### [CAT-NEC-DevourLife] L138
- 当前：`result.Pass("DevourLifePower.Amount", ...)` 绕过 delta
- 缺失：未打攻击触发 DevourLife 吸血
- 正确：玩家受伤 → 玩 Strike → `delta["DEVOUR_LIFE"].HpHealed == 1`

#### [CAT-NEC-Pagestorm] L172
- 当前：`result.Pass("PagestormPower.Amount", ...)`
- 正确：触发下游 hook + 断言 delta 字段

#### [CAT-NEC-Haunt] L203
- 当前：同上，只查 `HauntPower.Amount`
- 正确：至少 `delta["HAUNT"].TimesPlayed == 1`，理想情况触发下游并验证贡献

### Catalog_NecrobinderCardTests2.cs（15 条 A）

#### [NB2-Eidolon] L325
- 当前：`delta["EIDOLON"].TimesPlayed == 1`（TimesPlayed 非贡献字段）
- 正确：若 ≥9 Exhaust 触发 Intangible → SimulateDamage → `delta["EIDOLON"].MitigatedByBuff`

#### [NB2-Dredge] L351
- 当前：只断言 TimesPlayed=1
- 正确：验证从弃牌堆取 3 张，`delta["DREDGE"].CardsDrawn == 3`

#### [NB2-GlimpseBeyond] L394
- 当前：TimesPlayed=1
- 正确：DrawPile Soul 增量或贡献字段

#### [NB2-Putrefy] L468
- 当前：只断言 `enemy.WeakPower.Amount` + `enemy.VulnerablePower.Amount`（游戏状态）
- 缺失：未让敌人攻击 / 未后续打攻击
- 正确：打 Putrefy → Snapshot → 打 Strike → `delta["PUTREFY"].ModifierDamage == Vuln 贡献`

#### [NB2-DanseMacabre] L531
- 当前：`DanseMacabrePower.Amount == 3`
- 正确：下游触发 + delta 断言

#### [NB2-Demesne] L553
- 当前：`result.Pass` + `DemesnePower.Amount > 0`
- 正确：delta 贡献字段

#### [NB2-Lethality] L583
- 当前：`LethalityPower.Amount == 50`
- 正确：玩 Lethality → 打 Strike → `delta["LETHALITY"].ModifierDamage`

#### [NB2-Oblivion] L607
- 当前：`delta["OBLIVION"].TimesPlayed == 1`
- 正确：下游贡献字段

#### [NB2-Shroud] L627
- 当前：`ShroudPower.Amount == 2`

#### [NB2-SleightOfFlesh] L649
- 当前：`SleightOfFleshPower.Amount == 9`

#### [NB2-SpiritOfAsh] L676
- 当前：`SpiritOfAshPower.Amount > 0`

#### [NB2-Friendship] L702
- 当前：`FriendshipPower.Amount > 0`
- 缺失：未 EndTurn，未验证每回合 -2 Str + 1 能量的贡献
- 正确：EndTurn → `delta["FRIENDSHIP"].EnergyGained` + MitigatedByStrReduction 验证

### Catalog_PowerContribTests.cs（14 条 A，smoke 段）

#### [CAT-PWR-004] Envenom L223
- 当前：`enemy.PoisonPower.Amount == 1`
- 正确：Envenom → 打 Strike → EndTurn → `delta["ENVENOM"].AttributedDamage == 1`

#### [CAT-PWR-022] MonarchsGaze L1047
- 当前：`enemy.MonarchsGazeStrDown.Amount == 1`
- 正确：敌人攻击 → `delta["MONARCHS_GAZE"].MitigatedByStrReduction > 0`

#### [CAT-PWR-023] ReaperForm L1088
- 当前：`enemy.DoomPower.Amount == 6`
- 正确：与 NB-DOOM-ReaperForm 相同重写方案

#### [CAT-PWR-032] Furnace L1504
- 当前：`AssertEquals("FurnacePower.Amount", 4, ...)`
- 正确：驱动 `AfterSideTurnStart` → SovereignBlade → `delta["FORGE:FURNACE"].DirectDamage == 4`

#### [CAT-PWR-038] Hailstorm L1768
- 当前：`AssertEquals("HailstormPower.Amount", 6, ...)`
- 正确：EndTurn → `delta["HAILSTORM"].AttributedDamage`

#### [CAT-PWR-039] Loop L1805
#### [CAT-PWR-040] ConsumingShadow L1843
#### [CAT-PWR-041] LightningRod L1876
#### [CAT-PWR-042] Spinner L1908
#### [CAT-PWR-043] Iteration L1941
#### [CAT-PWR-044] Smokestack L1978
#### [CAT-PWR-045] TrashToTreasure L2015
#### [CAT-PWR-046] ChildOfTheStars L2052
#### [CAT-PWR-047] Orbit L2088

以上 9 条模式相同：仅 `AssertEquals("XxxPower.Amount", N, ...)`，属批量重写候选。
若下游 hook 无法驱动，应改为 Skipped 而非伪 PASS。

### Catalog_IroncladCardTests2.cs（2 条 A）

#### [CAT-IC2-Rupture] L570
- 当前：`StrengthPower.Amount after-before == 1`（游戏状态）
- 正确：Rupture → Offering 自伤 → 打 Strike → `delta["RUPTURE"].ModifierDamage == 1`

#### [CAT-IC2-Barricade] L635
- 当前：`result.Passed = true` + `BarricadePower != null`
- 正确：Barricade → 积累 block → EndTurn → 下回合 `delta["BARRICADE"].EffectiveBlock > 0`

### Catalog_SilentCardTests.cs（5 条 A）

#### [CAT-SI-GrandFinale] L666
- 当前：draw pile 非空时分支走 `result.Passed=true` 直接返回（条件分支伪 PASS）
- 正确：清空 DrawPile → `delta["GRAND_FINALE"].DirectDamage == 50 * enemyCount`

#### [CAT-SI-InfiniteBlades] L812
- 当前：`InfiniteBladesPower != null → Passed=true`
- 正确：EndTurn → 下回合 Shiv 生成验证

#### [CAT-SI-WellLaidPlans] L834
- 当前：`WellLaidPlansPower != null → Passed=true`

#### [CAT-SI-WraithForm] L856
- 当前：`IntangiblePower != null && Amount > 0 → Passed=true`
- 正确：SimulateDamage → `delta["WRAITH_FORM"].MitigatedByBuff`

#### [CAT-SI-Burst] L882
- 当前：`BurstPower != null → Passed=true`

---

## B 类：触发链断裂（6 条）

### [NB-DOOM-Scourge] Catalog_NecrobinderDoomTests.cs:196
- 当前：`delta["SCOURGE"].CardsDrawn == 1`（抽牌字段正确但 Doom 致死未验证）
- 补充：低 HP 场景 + EndTurn + `delta["SCOURGE"].AttributedDamage`

### [NB-DOOM-BorrowedTime] Catalog_NecrobinderDoomTests.cs:42
- 当前：`delta["BORROWED_TIME"].EnergyGained == 1`（能量正确，自身 Doom 层数未验证）
- 补充：`delta["BORROWED_TIME"].SelfDamage` 或 Doom 自身叠加验证

### [NB2-DeathsDoor] Catalog_NecrobinderCardTests2.cs:416
- 当前：`delta["DEATHS_DOOR"].EffectiveBlock == 6`（缺失 Doom 已施加分支验证）
- 补充：预先施加 Doom → `EffectiveBlock == 18`（6×3 重复分支）

### [CIT-05-HealingMultiSource] Catalog_ComplexInteractionTests.cs
- 当前：只断言 `BLOOD_VIAL.HpHealed == 2`（场景名为多源但实际单源）
- Regen tick 全被注释为 EndTurn-bound 未覆盖
- 补充：完整多源 HpHealed 断言，或改名去除 MultiSource

### [CAT-IC2-DemonForm] Catalog_IroncladCardTests2.cs:471（可疑）
- 当前通过但归因到 DEMON_FORM 或 STRENGTH_POWER 未确认
- 需要核实实际值

### [CAT-INT2-DamageMultiSourceStr]
- 已在 Round 14 FAIL 列表，不在此审计

---

## C 类：弱断言/漏断言（11 条）

### Catalog_IroncladCardTests2.cs
- [CAT-IC2-Break] L80：只断言 DirectDamage=20，未检查 Vuln 施加副作用
- [CAT-IC2-Mangle] L126：只 DirectDamage=15，未检查敌 -10 Str 的 MitigatedByStrReduction
- [CAT-IC2-Taunt] L303：只 EffectiveBlock=7，未验证 Vuln 施加副产物

### Catalog_DefectCardTests.cs
- [CAT-DE-BeamCell] L148：DirectDamage=3 + Vuln spec-waiver
- [CAT-DE-Null] L571：DirectDamage=10 + Weak spec-waiver

### Catalog_RegentCardTests.cs
- [CAT-RG-FallingStar] L309
- [CAT-RG-GammaBlast] L364
- 两者 DirectDamage 精确但附加 Weak/Vuln 仅 spec-waiver

### Catalog_ColorlessCardTests.cs
- [CAT-CL-Shockwave] L637：无 DirectDamage 基础（Shockwave 不打伤害），全部游戏状态断言 → 实际应升 A 类
- [CAT-CL-MindBlast] L818：`AssertGreaterThan(0)` non-det 豁免（低优）
- [CAT-CL-RipAndTear] L843：同上

### Catalog_PowerContribTests.cs
- [CAT-PWR-006] SerpentForm、[CAT-PWR-009] Juggernaut、[CAT-PWR-027] BlackHole、[CAT-PWR-030] RollingBoulder：non-det 豁免合理

---

## 可疑项

### [CAT-PWR-005] Cruelty L267
- 当前：`AssertGreaterThan("STRIKE_IRONCLAD.ModifierDamage (Cruelty-boosted)", 3, modDmg)`
- 按 spec 可精确计算 Vuln 乘区 split (vuln=3, cruelty=2)
- 判定：C+ 可疑

### [CAT-INT-04/05/07/10] Catalog_InteractionTests.cs
- 使用 `foreach (_, d) in delta` 聚合求和（spec-waiver 豁免但与 v3 精神相违）

### [CAT-INT-09/11/12] Catalog_InteractionTests.cs:293,350,371
- 直接操作 `ContributionMap`/`ContributionAccum` 数据结构
- 单元测试性质，保留可

---

## 修复优先级

| 优先级 | 范围 | 数量 |
|---|---|---|
| **P0** | Necrobinder Doom 系全重写（引入低 HP + EndTurn + AttributedDamage 模板） | 9 |
| **P0** | Necrobinder 非 Osty Power 分组 | 15 |
| **P1** | PowerContribTests smoke 段 CAT-PWR-032 ~ -052 + -004/-022/-023 | 17 |
| **P1** | Catalog_NecrobinderCardTests 3 Power smoke | 3 |
| **P2** | Silent Power smoke + GrandFinale 分支 | 5 |
| **P2** | Ironclad Rupture/Barricade | 2 |
| **P3** | DefenseDebuff Neutralize/PiercingWail | 2 |

## 测试顺序注意

**Necrobinder Doom 系测试会直接杀死敌人**，必须放在所有其他测试**之后**执行，避免污染后续测试的战斗环境。建议在 TestRunner.cs 的 `BuildScenarioList` 末尾单独追加 Doom 组。
