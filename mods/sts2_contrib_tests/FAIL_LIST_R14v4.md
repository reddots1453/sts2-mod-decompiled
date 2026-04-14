# Round 14 v4 FAIL 清单（33 条，供人工测试对照）

**基线**：2026-04-13 23:23 第四轮回归运行
**测试结果**：433 total / 325 PASS / 33 FAIL / 75 skip

## 验证方式

在游戏里手动触发每条测试描述的场景，对照"测试期望"和"实际观察"，判断是：
- ✅ **游戏内正常**（测试期望值错，加 passlist 或改断言）
- ❌ **游戏内也没追踪**（真产品 bug，改产品代码）
- 🤔 **游戏内显示不一样**（归因 key 或字段错）

---

## A 组 · PowerContrib / IroncladCards2（Power 触发式，12 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| CAT-PWR-005 / CAT-IC2-Cruelty | 残酷 Cruelty | Cruelty+Bash(Vuln)→Strike(6)，按乘区分解 Vuln/Cruelty | CRUELTY.ModifierDamage=2 | act=1 | ✅ 已证实测试期望错，应为 1 |
| CAT-PWR-007 / CAT-IC2-FeelNoPain | 无惧疼痛 FeelNoPain | 玩 Power→打含 Exhaust 的卡（TrueGrit）→触发 Exhaust 事件 | FEEL_NO_PAIN.EffectiveBlock=3 | act=0 | ? |
| CAT-PWR-009 / CAT-IC2-Juggernaut | 势不可当 Juggernaut | 玩 Power→打 Defend 获得 block→触发 JuggernautPower 攻击 | JUGGERNAUT.AttributedDamage/TotalDamage=5 | act=0 | ? |
| CAT-PWR-011 | 恶魔形态 DemonForm | 玩 Power→EndTurn→新回合 Str+2→打 Strike | STRIKE_IRONCLAD.ModifierDamage=2 | act=0（Str 加了但归因断） | ? |
| CAT-PWR-016 | 擒拿 Grapple | 打 Grapple→每次获得 block 时触发追伤 | GRAPPLE.TotalDamage=5 | act=0 | ? |
| CAT-PWR-017 | 狱火 Inferno | 玩 Power→自伤触发 HP loss→Inferno AoE | INFERNO.TotalDamage>0 | act=0 | ? |
| CAT-PWR-020 | 速行者 Speedster | 玩 Power→抽牌触发 AoE | SPEEDSTER.TotalDamage>0 | act=0 | ? |
| CAT-PWR-022 | 王之凝视 MonarchsGaze | 玩 Power→EndTurn→敌人攻击时 Str-1 | MONARCHS_GAZE.MitigatedByStrReduction>0 | act=0 | ? |
| CAT-PWR-024 | 独白 Monologue | 玩 Power→每打牌 Str+1→打 Strike | STRIKE_IRONCLAD.ModifierDamage=1 | act=0 | ? |
| CAT-PWR-029 | 创世之柱 PillarOfCreation | 玩 Power→生成牌时 block | PILLAR_OF_CREATION.EffectiveBlock=9 | act=0 | ? |
| CAT-PWR-030 | 滚石 RollingBoulder | 玩 Power→EndTurn→回合开始 AoE | ROLLING_BOULDER.TotalDamage>0 | act=0 | ? |
| CAT-IC2-Rupture | 撕裂 Rupture | Rupture+Offering 自伤→Str+1→打 Strike | STRIKE.ModifierDamage>0 | act=0 | ? |

## B 组 · Necrobinder 能力牌（3 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| NB2-Demesne | 领域 Demesne | 玩 Power→EndTurn→新回合开始触发 | DEMESNE.EnergyGained>0 | act=0 | ? |
| NB2-Friendship | 友谊 Friendship | 玩 Power→EndTurn→新回合开始触发 | FRIENDSHIP.EnergyGained>0 | act=0 | ? |
| CAT-NEC-Pagestorm | 书页风暴 Pagestorm | 种 Ethereal 卡到 draw pile→EndTurn→抽到 Ethereal 时额外抽 1 | PAGESTORM.CardsDrawn>0 | act=0 | ? |

## C 组 · 药水/遗物 Modifier/触发（4 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| CAT-POT2-Dexterity | 敏捷药水 | UsePotion→打 Defend→Dex+2 加成归因 | DEXTERITY_POTION.ModifierBlock=2 | act=0 | ? |
| CAT-POT2-Speed | 速度药水 | UsePotion→打 Defend→临时 Dex+5 | SPEED_POTION.ModifierBlock=5 | act=0 | ? |
| CAT-REL3-CharonsAshes | 卡戎之灰 | Exhaust 卡触发遗物 AoE 伤害 | CHARONS_ASHES.DirectDamage=3 | act=0 | ? |
| CAT-REL3-ForgottenSoul | 遗忘之魂 | Exhaust 卡触发遗物伤害 | FORGOTTEN_SOUL.DirectDamage=1 | act=0 | ? |

## D 组 · 其他 Power 触发（4 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| CAT-PWR-032 | 熔炉 Furnace | ApplyPower→EndTurn 驱动 AfterSideTurnStart→SovereignBlade 触发 Forge | FORGE:FURNACE_POWER.DirectDamage=4 | act=0 | ? |
| CAT-PWR-035 | 你好世界 HelloWorld | 玩 Power→EndTurn→扫 hand 找新增 Common 卡→打出→查 sub-bar 贡献 | HELLO_WORLD sub-bar 贡献总和>0 | act=0 | ? |
| CAT-SI-Envenom | 涂毒 Envenom | 玩 Power→打 Strike→敌人中毒→EndTurn→Poison tick | ENVENOM.AttributedDamage=1 | act=0 | ? |
| CAT-CL-Rally | 集结 Rally | 玩 Rally（无色集结）→获得 3 能量或等效 | RALLY.EffectiveBlock=12 | act=0 | ? Rally 效果需核对 KB |

## E 组 · Forge 子条（2 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| CAT-RG-ForgeSubBar | 君王之剑 SovereignBlade / 铸墙 Bulwark | Bulwark(Forge 10)→SovereignBlade→`SOVEREIGN_BLADE.DirectDamage=20` | 20 | act=10（base 未汇入） | ? |
| CAT-RG-ForgeMultiSource | 君王之剑 + 熔炉 | Bulwark(10)+Furnace(4)→SovereignBlade→24 | 24；FORGE:BASE.DirectDamage=10 | act=10；FORGE:BASE=0 | ? |

## F 组 · 复合交互场景（3 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| CAT-INT2-DamageMultiSourceStr | 燃烧 Inflame + 金刚杵 Vajra | Inflame(Str+2) + Vajra(Str+1) → 打 Strike | VAJRA+STRIKE.ModifierDamage>0 | act=0 | ? |
| CIT-01-DamageMultiSource | 多源伤害（燃烧+金刚杵+打击木偶+微型大炮+武装） | 五源组合打 Strike | VAJRA.ModifierDamage=1; ARMAMENTS.UpgradeDamage=3 | 均 0 | ? |
| CIT-02-DefenseOmnibus | 灵动步法+祭品+防御（Footwork+Offering+Defend） | Offering 自伤 6 HP | OFFERING.SelfDamage=6 | act=0 | ? |

## G 组 · 充能球 + Doom（2 条）

| ID | 中文名 | 当前测试逻辑 | 期望 | 实际 | 判定 |
|---|---|---|---|---|---|
| CAT-DE-OrbPlasma-MeteorStrike | 陨石打击 MeteorStrike | Channel 3 Plasma→EndTurn→每个 Plasma 给 1 能量 | METEOR_STRIKE.EnergyGained=3 | act=1 | ? |
| NB-DOOM-Deathbringer | 死亡使者 Deathbringer | 先降敌 HP 到 5→打 Deathbringer 施加 Doom 21→EndTurn→Doom 致死 | DEATHBRINGER.AttributedDamage>0 | act=0 | ? |

---

## 人工验证清单

1. **A 组（12 条 Power 触发）**：玩出 Power，在战斗面板观察对应 Power 的贡献条是否显示伤害/格挡。Cruelty 已确认是测试期望值错。
2. **B 组 Demesne/Friendship/Pagestorm**：玩 Power→结束回合→观察新回合能量条/手牌变化时，战斗面板是否给对应 Power 记了 EnergyGained/CardsDrawn。
3. **C 组 药水/遗物**：用 Dex/Speed 药水后打 Defend，看贡献面板是否把 Dex 加成记到 `DEXTERITY_POTION` 子条。Charons/ForgottenSoul 看 Exhaust 时面板里是否显示对应遗物的伤害条。
4. **D 组 Furnace/HelloWorld/Envenom/Rally**：Furnace 看 Forge 子条名字是 `FORGE:FURNACE` 还是 `FORGE:FURNACE_POWER`；Rally 实际效果是否是 Block；Envenom 看毒伤是否显示在 ENVENOM 条而非 STRIKE。
5. **E 组 Forge 子条 2 条**：打出 SovereignBlade 看贡献面板是否把 Bulwark 的 Forge 10 加进了 SovereignBlade 总伤害；多源场景看 `FORGE:BASE` 是否存在。
6. **F 组 复合场景**：Offering 看是否显示 SelfDamage 子条；Armaments 升级那张牌后看 UpgradeDamage 是否有记录（可能产品代码根本没写 UpgradeDamage 通道）。
7. **G 组 MeteorStrike/Deathbringer**：Channel 3 Plasma 后下回合开始看能量条；Deathbringer 致死时看 Doom 死亡伤害是否归到 DEATHBRINGER。
