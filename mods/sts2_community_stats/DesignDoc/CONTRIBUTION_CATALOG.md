# 战斗贡献源实体目录 — 测试验收基准

> 第二轮迭代 Phase 7 (§4.1-§4.4) 完成后的预期贡献基准。
> ⚠️ 标记表示当前 mod 代码 (CombatHistoryPatch.cs / CombatTracker.cs) 未追踪该实体或存在 gap。
> 空 Gap 列表示由 `CombatTracker.OnDamageDealt` / `OnBlockGained` 通用追踪覆盖，或已有专用 Harmony patch。
>
> 数值均从 `KnowledgeBase/cards.json` / `relics.json` / `potions.json` 的 `vars` 字段取得 `[base, upgraded]`，测试场景默认：
> - "默认敌人" = 200 HP 无 buff 的训练假人
> - 无力量、无灵巧、无易伤、无虚弱，能量充足，手牌仅含被测卡
> - 升级标注为 +，未标注为基础
>
> 本文档是**测试验收基准 + gap 分析**，不是最终卡牌名录；对于不涉及特殊贡献逻辑的通用攻击/防御/技能卡，以分组汇总行给出。仅对需要专门处理的实体（power 钩子、修正器注入、跨实体归因、免费效果等）逐行展开。

## 0. 概览

- **卡牌总数**: 577 (`KnowledgeBase/cards.json`)
  - 铁甲战士 Ironclad: 87
  - 静默猎手 Silent: 81
  - 机器人 Defect: 85
  - 储君 Regent: 85
  - 亡灵契约师 Necrobinder: 85
  - 无色 Colorless: 80
  - 诅咒 Curse: 19
  - 事件/特殊/派生 Misc: 55
- **遗物总数**: 289 (`KnowledgeBase/relics.json`)
- **药水总数**: 64 (`KnowledgeBase/potions.json`)
- **已 patch 的 Power 钩子**: 60+ (见 CombatHistoryPatch `PatchPowerHooks`)
- **已 patch 的 Relic 钩子**: 65+ (见 CombatHistoryPatch `PatchRelicHooks`)
- **Gap 总数估算**: ~90 个实体需要后续 patch 或补逻辑（详见各章节 ⚠️ 标记与 §16 Gap 汇总）

---

## 1. 直接伤害 (DirectDamage)

说明：卡牌效果直接造成的伤害，从 totalDamage 减去 modifier 后归属到打出卡牌的来源。由 `CombatTracker.OnDamageDealt` 通用追踪，`cardSourceId` 取自 `OnCardPlayStarted`。
**普通攻击牌（单次/多次攻击）在无修正器情况下无需专门 patch**，只列代表性样本和特殊机制。

### 1.1 通用攻击牌（无特殊机制，单行汇总）

| 实体 ID | 名称 | 角色 | 测试场景 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| STRIKE_IRONCLAD | 打击 | Ironclad Basic | 单打 vs 默认敌人 | DirectDamage=6 (+3 upgrade→9) | |
| STRIKE_SILENT | 打击 | Silent Basic | 同上 | DirectDamage=6 | |
| STRIKE_DEFECT | 打击 | Defect Basic | 同上 | DirectDamage=6 | |
| STRIKE_REGENT | 打击 | Regent Basic | 同上 | DirectDamage=6 | |
| STRIKE_NECROBINDER | 打击 | Necrobinder Basic | 同上 | DirectDamage=6 | |
| IRON_WAVE | 铁浪 | Ironclad Common | vs 默认敌人 | DirectDamage=5 + EffectiveBlock=5（见 §6） | |
| CLASH | 抨击 | Ironclad Common | 全攻击手牌 vs 默认敌人 | DirectDamage=14 | |
| ANGER | 愤怒 | Ironclad Common | vs 默认敌人 | DirectDamage=6 + Sub-bar 生成 ANGER 拷贝（见 §15） | |
| POMMEL_STRIKE | 剑柄打击 | Ironclad Common | vs 默认敌人 | DirectDamage=9 + CardsDrawn=1（见 §11） | |
| HEADBUTT | 头锤 | Ironclad Common | vs 默认敌人 | DirectDamage=9 | |
| TWIN_STRIKE | 双重打击 | Ironclad Common | vs 默认敌人 | DirectDamage=5+5=10 | |
| UPPERCUT | 上勾拳 | Ironclad Common | vs 默认敌人 | DirectDamage=13 (+ 弱/易伤由 §7 消费) | |
| SWORD_BOOMERANG | 回旋剑 | Ironclad Common | 单敌 | DirectDamage=3×3=9 | |
| THRASH | 胖揍 | Ironclad Common | vs 默认敌人 | DirectDamage=7 + EffectiveBlock=7 | |
| BLOOD_WALL | 血墙 | Ironclad Common | vs 默认敌人 | DirectDamage=7 + EffectiveBlock=7 | |
| DISMANTLE | 分解 | Ironclad Common | vs 默认敌人 | DirectDamage=16 | |
| WHIRLWIND | 旋风 | Ironclad Uncommon | AoE，每点能量打击全体 | DirectDamage=5×energy 点 × 敌数 | |
| HEMOKINESIS | 血控 | Ironclad Uncommon | vs 默认敌人 | DirectDamage=15 + SelfDamage=2 (见 §10) | |
| IMPERVIOUS | 不屈 | Ironclad Uncommon | 单用 | EffectiveBlock=30（见 §6） | |
| DARK_EMBRACE/RUPTURE | … | Ironclad Power | 见 §2 间接伤害 | — | |
| RAMPAGE | 狂暴 | Ironclad Uncommon | 重复打出 | DirectDamage=8 (第一次)，每次 +5 | ⚠️ 自增量是否归属自身需验证，非通用 |
| PYRE | 柴堆 | Ironclad Uncommon | vs 默认敌人 | DirectDamage=22 | |
| FIEND_FIRE | 恶魔火 | Ironclad Rare | 手牌 5 张 | DirectDamage=7×5=35 | |
| REAPER | 死神（注：KB 无，用 REAPER_FORM power 处理） | — | — | — | |
| BLUDGEON | 重击棒 | Ironclad Rare | vs 默认敌人 | DirectDamage=32 | |
| FEED | 狂宴 | Ironclad Rare | 斩杀 | DirectDamage=12 + HpHealed=+3 MaxHp (见 §14) | ⚠️ 需 patch 追踪 MaxHp 增量归 FEED |
| IMMOLATE | (KB 无) | — | — | — | |

**通用说明**: 铁甲/静默/机器人/储君/亡灵 全部 **单目标攻击牌** (Attack 类型，无 Power 注入、无 Modifier、无 sub-bar 生成) 经由 `OnCardPlayStarted` → `OnDamageDealt` 自动归属。共约 **240 张** 普通攻击牌采用此路径。

### 1.2 Silent 攻击牌（单目标/AoE，特殊机制单列）

| 实体 ID | 名称 | 角色 | 测试场景 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| NEUTRALIZE | 中和 | Silent Basic | vs 默认敌人 | DirectDamage=3 + Weak 施加（见 §8） | |
| SURVIVOR | 幸存者 | Silent Basic | 单用 | EffectiveBlock=8 + 弃 1 张 | |
| DAGGER_THROW | 飞刀 | Silent Common | vs 默认敌人 | DirectDamage=9 | |
| DAGGER_SPRAY | 飞刀横洒 | Silent Common | AoE | DirectDamage=4×2 × 敌数 | |
| POISONED_STAB | 毒刺 | Silent Common | vs 默认敌人 | DirectDamage=6 + Poison 施加（→§2 Attributed） | |
| DEADLY_POISON | 致命毒药 | Silent Common | vs 默认敌人 | Poison 施加（→§2） | |
| BACKSTAB | 背刺 | Silent Uncommon | 从抽牌堆打出 | DirectDamage=11 | |
| BLADE_DANCE | 刀舞 | Silent Common | 单用 | 生成 3 张 SHIV 到手（见 §15 sub-bar） | |
| INFINITE_BLADES | 无限刀刃 | Silent Rare | Power，持续生成 SHIV | Sub-bar 父 = INFINITE_BLADES；子 = SHIV | |
| CLOAK_AND_DAGGER | 披风匕首 | Silent Common | 单用 | EffectiveBlock=6 + 生成 1 SHIV (sub-bar) | |
| STORM_OF_STEEL | 钢铁风暴 | Silent Rare | 手中 5 张牌 | 弃 5 张 + 生成 5 SHIV (sub-bar) | |
| FINISHER | 终结者 | Silent Rare | 本回合打出 3 张攻击 | DirectDamage=6×3=18 | |
| SKEWER | 串烧 | Silent Uncommon | 3 能量 | DirectDamage=7×3=21 | |
| GRAND_FINALE | 大终章 | Silent Rare | 抽牌堆空 | DirectDamage=60 | |
| ALL_OUT_ATTACK | (KB 无) | — | — | — | |
| PHANTOM_BLADES | 幽灵刀 | Silent Uncommon | vs 默认 | DirectDamage=7+7=14 + 消耗 | |
| PREDATOR | 掠食者 | Silent Uncommon | vs 默认 | DirectDamage=15 + 抽 2 (§11) | |
| RICOCHET | 弹射 | Silent Common | AoE | DirectDamage=5×N | |
| SLICE | 削砍 | Silent Common | vs 默认 | DirectDamage=6 | |
| STRANGLE | 扼杀 | Silent Uncommon | vs 默认 | DirectDamage=10 + 给自己 1 张 Pain | |
| FAN_OF_KNIVES | 飞刀扇 | Silent Uncommon | AoE | DirectDamage=4×N | |
| FLECHETTES | 飞镖 | Silent Uncommon | 手中技能数 S | DirectDamage=4×S | |
| LEG_SWEEP | 扫腿 | Silent Uncommon | vs 默认 | EffectiveBlock=11 + Weak 施加 | |
| NOXIOUS_FUMES | 毒烟雾 | Silent Rare | Power | AttributedDamage via Poison（§2） | |
| FLASH_OF_STEEL | (Colorless) | Colorless | vs 默认 | DirectDamage=3 + CardsDrawn=1 | |
| MURDER | 谋杀 | Silent Rare | vs 默认 | DirectDamage=30 (+ 消耗目标非一) | |

### 1.3 Defect 攻击牌（充能球见 §2）

| 实体 ID | 名称 | 测试场景 | 预期贡献 | Gap |
|---|---|---|---|---|
| GO_FOR_THE_EYES | 狙击要害 | vs 默认 | DirectDamage=5 (+ Weak if 敌攻击中) | |
| COLD_SNAP | 凛冬一击 | vs 默认 | DirectDamage=6 + 通道 1 冰球（§2） | |
| COMPILE_DRIVER | 编译驱动 | 每种 orb 多 2 伤 | DirectDamage=7 + per-orb 增量 | ⚠️ 需专门追踪 orb 附加伤害归 COMPILE_DRIVER |
| BALL_LIGHTNING | 雷电球 | vs 默认 | DirectDamage=7 + 通道 1 闪电球 | |
| REBOUND | (Colorless?) | — | — | |
| STREAMLINE | (KB 无) | — | — | — |
| THUNDER_STRIKE | (KB 无) | — | — | — |
| SWEEPING_BEAM | 扫射光束 | AoE | DirectDamage=4×N + 抽 1 (§11) | |
| HYPERBEAM | 超能射线 | AoE | DirectDamage=26×N - 给自己 Focus-3 | |
| METEOR_STRIKE | 流星打击 | vs 默认 | DirectDamage=24 + 通道 3 球 | |
| RIP_AND_TEAR | (Colorless) | 双打 | DirectDamage=7+7=14 (×2 for 2 目标) | |
| SUNDER | 刺穿 | vs 默认 | DirectDamage=24 | |
| THUNDER | 雷电 | Power | AttributedDamage via ThunderPower（§2，已 patch） | |
| ALL_FOR_ONE | (Colorless) | vs 默认 | DirectDamage=10 + 回手所有 0 费牌 | |
| CLAW | 爪击 | 多次打出 | DirectDamage=3, 每次 +2 | ⚠️ 递增量归属需验证 |

### 1.4 Regent/Necrobinder 攻击牌

| 实体 ID | 名称 | 测试场景 | 预期贡献 | Gap |
|---|---|---|---|---|
| KINGLY_KICK | 王者一踢 | vs 默认 | DirectDamage=10 + Weak | |
| KINGLY_PUNCH | 王者一拳 | vs 默认 | DirectDamage=10 | |
| CRESCENT_SPEAR | 新月矛 | vs 默认 | DirectDamage=9 | |
| HEAVENLY_DRILL | 神钻 | vs 默认 | DirectDamage=12 | |
| KNOCKOUT_BLOW | 击倒一击 | vs 默认 | DirectDamage=18 | |
| SEVEN_STARS | 七星 | vs 默认 | DirectDamage=7×N stars | ⚠️ Stars 消费归属 |
| SOLAR_STRIKE | 日辉打击 | vs 默认 | DirectDamage=10 + Stars | |
| SHINING_STRIKE | 辉耀打击 | vs 默认 | DirectDamage= scaling by stars | ⚠️ |
| SEEKING_EDGE | 追索之刃 | 非主目标分裂 | DirectDamage= primary + spill | ⚠️ 非主目标伤害分配已在 PRD-00 §4.5.1 特别指出 |
| SUPERMASSIVE | 超大质量 | AoE | DirectDamage×N | |
| GAMMA_BLAST | 伽马爆破 | vs 默认 | DirectDamage=18 | |
| LUNAR_BLAST | 月辉爆破 | vs 默认 | DirectDamage=20 | |
| QUASAR | 类星体 | vs 默认 | DirectDamage=20 + 星费 | |
| BLIGHT_STRIKE | 枯萎打击 | vs 默认 | DirectDamage=6 + 施加 Weakness/Fear | |
| DEATHBRINGER | 死亡使者 | vs 默认 | DirectDamage=16 | |
| DEFILE | 亵渎 | AoE | DirectDamage=10×N + 生成 Osty 消耗 | ⚠️ Osty 扣血归属 |
| GRAVE_WARDEN/GRAVEBLAST | 墓穴守者/坟爆 | AoE | DirectDamage×N | |
| SOUL_STORM | 灵魂风暴 | AoE | DirectDamage×N | |
| END_OF_DAYS | 末日 | vs 默认 | DirectDamage=40 | |
| REAVE | 掠夺 | vs 默认 | DirectDamage=10 + Fear | |
| INVOKE | 祈唤 | vs 默认 | DirectDamage=10 + 召 Osty | |
| EIDOLON | 魂偶 | vs 默认 | DirectDamage=? + Osty 联动 | ⚠️ Osty 归属 |

### 1.5 特殊：Body-Slam 类（伤害 = 当前格挡）

| BODY_SLAM | 全身撞击 | Ironclad Common | 拥有 10 格挡时 | DirectDamage=10（等于 Block 值）| |

---

## 2. 间接伤害 (AttributedDamage)

说明：通过 Power 间接造成的伤害，归属到施加该 Power 的源实体。Power hook 已 patch 见 CombatHistoryPatch `PatchPowerHooks`。

| 实体 ID | 名称 | 类别 | 触发机制 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| POISON_POWER | 中毒 | Power (施加来源 = DEADLY_POISON/POISONED_STAB/NOXIOUS_FUMES/SYMBIOTIC_VIRUS 等) | 每回合 tick | AttributedDamage=Poison层数 | ✓ PoisonPower patched |
| NOXIOUS_FUMES | 毒烟雾 | Silent Power | 回合末施加 Poison | AttributedDamage 归 NOXIOUS_FUMES（经 Poison 中转） | ✓ |
| ENVENOM | 剧毒 | Silent Power | 攻击时施加 Poison | AttributedDamage → ENVENOM | ✓ EnvenomPower patched |
| CRYSTAL_JAB (KB 无) | — | — | — | — | — |
| BURNING_PACT | 燃烧契约 | Ironclad | 消耗→抽牌，不是伤害 | N/A (→§11) | |
| COMBUST/RUPTURE | (KB 无 COMBUST) | — | — | — | — |
| INFERNO_POWER | 地狱火 | Ironclad | 回合开始对全体 | AttributedDamage 归 INFERNO | ✓ InfernoPower patched |
| FLAME_BARRIER_POWER | 烈焰护壁 | Ironclad | 被打时反伤 | AttributedDamage 归 FLAME_BARRIER | ✓ |
| JUGGERNAUT_POWER | 势不可当 | Ironclad | 获得格挡→伤害 | AttributedDamage 归 JUGGERNAUT | ✓ |
| RAGE_POWER | 暴怒 | Ironclad | 攻击→获得格挡 (块非伤) | EffectiveBlock 归 RAGE（§6） | ✓ |
| THORNS_POWER (KB `POWERS`) | 荆棘 | 玩家 Power | 被攻击反伤 | AttributedDamage 归施加来源（如 PAELS_GROWTH 遗物或某些药水） | ⚠️ 需确认已 patch |
| DOOM_POWER | 灾厄 | Regent/Necrobinder | HP 达阈值即杀 | AttributedDamage = 目标剩余 HP 归施加 DOOM 的卡 | ✓ DoomPower.DoomKill patched |
| BANE_POWER (KB 无) | — | — | — | — | — |
| ORB 引导伤害 (Lightning/Frost/Dark/Plasma) | 充能球 | Defect | evoke/passive | AttributedDamage 归 evoke/通道来源（CaptureOrbEvoke 已 patch） | ✓ OrbCmd.Passive/EvokeNext/EvokeLast patched |
| DARK_ORB | 黑暗球 | Defect | evoke 炸 | AttributedDamage 归 evoke 来源 | ✓ |
| LIGHTNING_ROD_POWER | 雷电棒 | Defect | 能量重置→雷击 | AttributedDamage 归 LIGHTNING_ROD | ✓ |
| THUNDER_POWER | 雷电 | Defect | orb evoke→伤害 | AttributedDamage 归 THUNDER | ✓ |
| STORM_POWER | 风暴 | Defect | 每次 power 通道 1 lightning | Sub-bar to STORM; evoke→§2 | ⚠️ StormPower 未列入 patch 清单 |
| GUNK_UP (KB) | 污泥 | Defect | 施加 Gunk debuff | AttributedDamage 归 GUNK_UP | ⚠️ |
| WHITE_NOISE_POWER (KB 无) | — | — | — | — | — |
| REFLECT_POWER | 反射 | Regent | 被攻击→反弹 | AttributedDamage 归 REFLECT | ✓ |
| SMOKESTACK_POWER | 烟囱 | Defect Power | 每回合生成 Burn，Burn 打出→损血 | 不是伤害 AttributedDamage | ✓ |
| HAILSTORM_POWER | 冰雹 | Defect | 每回合通道多个冰球 | AttributedDamage via orbs | ✓ |
| SPINNER_POWER | 旋转器 | Defect | 能量剩余→channel orb | — | ✓ |
| HAUNT_POWER | 萦绕 | Necrobinder | 每打出一张→伤害 | AttributedDamage 归 HAUNT | ✓ |
| CONSUMING_SHADOW_POWER | 吞噬之影 | Necrobinder | 回合末→伤害 | AttributedDamage 归 CONSUMING_SHADOW | ✓ |
| PAGESTORM_POWER | 纸片风暴 | Necrobinder | 抽牌→伤害 | AttributedDamage 归 PAGESTORM | ✓ |
| SIC_EM_POWER | 袭击 | Necrobinder | 施加时与 DamageGiven 联动 | ✓ |
| REAPER_FORM_POWER | 收割形态 | Necrobinder | 伤害→治疗+攻击 | AttributedDamage + HpHealed 归 REAPER_FORM | ✓ |
| SEANCE (KB) | 招魂 | Necrobinder | Osty 联动 | ⚠️ 需验证 |
| OBLIVION_POWER | 湮灭 | Necrobinder | 打出→伤害 | ✓ |
| MONARCHS_GAZE_POWER | 君主凝视 | Regent | 击杀→增伤 | ✓ |
| BLACK_HOLE_POWER | 黑洞 | Regent | stars→AoE | AttributedDamage | ✓ |
| CHILD_OF_THE_STARS_POWER | 星辰之子 | Regent | stars spent→伤害 | ✓ |
| MONOLOGUE_POWER | 独白 | Regent | 打出→伤害 | ✓ |
| PARRY_POWER | 格挡回击 | Regent | 见 §6 | ✓ |
| PILLAR_OF_CREATION_POWER | 创世之柱 | Regent | 生成→效果 | ✓ |
| CALTROPS | 尖刺陷阱 | Colorless（衍生/事件） | 反伤 | AttributedDamage 归 CALTROPS | ⚠️ 需 patch CaltropsPower |
| OUTBREAK_POWER | 瘟疫 | Silent | 回合 tick | ✓ |
| SERPENT_FORM_POWER | 蛇形 | Silent | 打出→灵巧 | §3 | ✓ |
| SPIRIT_OF_ASH_POWER | 灰烬之魂 | Necrobinder | 消耗→伤害 | ✓ |

---

## 3. 修正伤害 (ModifierDamage) — 加法

说明：力量、灵巧（防御）、活力等加法增量。归属按 FIFO 先施加先计算，贡献 = 加量 a_i。**Hook.ModifyDamage 已 patch 2 处** (CombatHistoryPatch)。

| 实体 ID | 名称 | 类别 | 加法增量来源 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| STRENGTH_POWER | 力量 | 玩家 Power | 卡牌伤害 +力量 | ModifierDamage=力量×攻击段数，归施加来源 | ✓ ModifyDamage patched |
| INFLAME | 燃烧 | Ironclad Power | +2/+3 Strength | ModifierDamage 归 INFLAME | ✓ |
| DEMON_FORM | 恶魔形态 | Ironclad Rare | 每回合+Strength | ✓ |
| VICIOUS_POWER | 恶毒 | Silent Power | 抽牌→+Strength | ✓ |
| LIMIT_BREAK (KB 无) | — | — | — | — | — |
| SPOT_WEAKNESS (KB 无) | — | — | — | — | — |
| FLEX_POTION | 灵活药水 | Common Potion | 临时+Strength | ModifierDamage 归 FLEX_POTION，回合末扣回 | ⚠️ 需 TemporaryStrengthPower.AfterTurnEnd patch 验证 |
| STRENGTH_POTION | 力量药水 | Rare Potion | 永久+Strength | ✓ |
| ACCURACY_POWER | 精准 | Silent Power | SHIV +2 伤害 | ModifierDamage 归 ACCURACY，只对 SHIV 生效 | ⚠️ 需专门 patch (sub-card targeting) |
| PEN_NIB | 笔尖 | Common Relic | 每 9 张牌一次双倍 | ⚠️ 需 patch |
| AKABEKO | 赤牛 | Common Relic | 首张攻击+8 伤害 | ModifierDamage=8 归 AKABEKO | ✓ AfterSideTurnStart patched |
| IRON_CLUB | 铁棒 | Relic | +每战首张卡 2 伤 | ⚠️ 已 patched AfterCardPlayed |
| GHOST_SEED | 幽灵种子 | Relic | — (回血类，§14) | — | |
| STRIKE_DUMMY / FAKE_STRIKE_DUMMY | 击打假人 | Relic | +3 伤害 to Strike 类 | ModifierDamage=3 per strike card | ⚠️ 需 patch StrikeDummy |
| FAKE_STRIKE_DUMMY | 假击打假人 | Fake Relic | 同上 | ⚠️ |
| FENCING_MANUAL | 击剑手册 | Relic | +1 灵巧 每回合? | 影响 Block | §6 |
| PRECARIOUS_SHEARS | 摇摇欲坠的剪刀 | Relic | +伤害 | ⚠️ |
| PUNCH_DAGGER | 冲拳匕首 | Relic | 首张攻击 +2 伤 | ⚠️ |
| VAJRA | 金刚杵 | Regent Starter Relic | +1 Strength 战斗开始 | ModifierDamage 归 VAJRA | ⚠️ 需 patch 战斗开始 Strength |
| GIRYA | 给亚 | Relic | +1 Strength 永久 | ModifierDamage 归 GIRYA | ⚠️ |
| SPOILS_MAP | 战利品图 | Event Relic | +Strength | ⚠️ |
| DATA_DISK | 数据盘 | Defect Starter Relic | +1 Focus | ModifierDamage 影响 orb evoke 伤害 → 归 DATA_DISK | ⚠️ 需 patch Focus sub-calc |
| FOCUS_POTION | 聚焦药水 | Potion | +Focus | → 同上 | ⚠️ |
| SYMBIOTIC_VIRUS | 共生病毒 | Defect Relic | 每回合 1 dark orb | Sub-bar 父，orb 伤害归属 | ⚠️ |
| FROZEN_EGG | 冰冻蛋 | Relic | 升级所有 Power | §5 UpgradeDamage | ⚠️ |
| MOLTEN_EGG | 熔岩蛋 | Relic | 升级所有 Attack | §5 | ⚠️ |
| TOXIC_EGG | 剧毒蛋 | Relic | 升级所有 Skill | §5 | ⚠️ |
| GAMBLERS_BREW (potion KB) | — | — | — | — | — |
| RADIANT_TINCTURE | 辉耀药剂 | Potion | +力量 or +伤害 | ⚠️ |
| THRUMMING_HATCHET | (KB card) | Colorless | 打出增伤 | ⚠️ |

---

## 4. 修正伤害 (ModifierDamage) — 乘法

说明：易伤、双倍伤害、巨像、Cruelty 等乘法。按 §4.1 约束：当总乘法增量超出实际 bonus 时，按比例缩放。

| 实体 ID | 名称 | 类别 | 修正器 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| VULNERABLE_POWER | 易伤 | 敌方 debuff | ×1.5 | ModifierDamage = Total - Total/1.5，归施加来源 (FIFO)| ✓ Hook.ModifyDamage patched |
| BASH | 痛击 | Ironclad | 施加 2 Vuln | 若打 BASH 后接 STRIKE（10 伤害），BASH.ModifierDamage=15-10=5 + BASH.DirectDamage=8 | ✓ via VulnerablePower source tracking |
| UPPERCUT | 上勾拳 | Ironclad | 施加 Vuln | ✓ |
| THUNDERCLAP | 霹雳 | Ironclad | AoE + Vuln | ✓ |
| PIERCING_WAIL | 穿刺悲鸣 | Silent | Vuln AoE | ✓ |
| EXPOSE | 揭露 | Silent | Vuln 施加 | ✓ |
| PALE_BLUE_DOT | 暗淡蓝点 | Regent | Vuln AoE | ✓ |
| SPECTRUM_SHIFT | 光谱转换 | Regent | Vuln 施加 | ✓ |
| WRAITH_FORM | 亡灵形态 | Silent Rare | 玩家 Intangible（减伤 §8）+Vuln 自身 | ⚠️ |
| CRUELTY_POWER | 残酷 | Ironclad Power | 非独立乘区 +25/50% Vuln 基数 | 比例分配：Cruelty.ModifierDamage = (D-D/(1+Σ))·crueltyShare/Σ | ✓ CrueltyPower patched |
| PAPER_PHROG | 纸青蛙 | Relic | Vuln 75% → 100% | ModifierDamage 归 PAPER_PHROG（非独立乘区，归因已实现） | ✓ PaperPhrog patched |
| DEBILITATE_POWER | 摧残 | Necrobinder | Weak 非独立乘区改为 50% | ✓ DebilitatePower patched |
| PAPER_KRANE | 纸鹤 | Relic | Weak 25% → 40% | ✓ PaperKrane patched |
| DOUBLE_DAMAGE / PARRY sp. | (KB?) | — | ×2 伤害 buff | ⚠️ 需 patch 如果存在 |
| DEMON_FORM | 恶魔形态 | — | 实为加法 Strength，归 §3 | — | |

---

## 5. 升级增量 (UpgradeDamage / UpgradeBlock)

说明：战斗内临时升级一张卡后增加的伤害/格挡，归属给执行升级的来源。patch 点：`CardCmd.Upgrade` 双 patch + `CardCmd.Transform`。

| 实体 ID | 名称 | 类别 | 效果 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| ARMAMENTS | 武装 | Ironclad Common | 升级 1 张手牌（升级版：全部） | UpgradeDamage/Block = 被升级卡下次打出时的增量，归 ARMAMENTS | ✓ CardCmd.Upgrade patched |
| APOTHEOSIS | 神化 | Colorless Rare | 升级所有手牌 | UpgradeDamage/Block 归 APOTHEOSIS | ✓ |
| MAD_SCIENCE | 疯狂科学 | Colorless | 升级+随机 | ✓ |
| BLESSING_OF_THE_FORGE | 锻造之祝 | Colorless Potion | 升级本回合手牌 | UpgradeDamage/Block 归 BLESSING_OF_THE_FORGE | ✓ |
| LIQUID_MEMORIES | 液态记忆 | Potion | 用作升级 | ✓ |
| FROZEN_EGG / MOLTEN_EGG / TOXIC_EGG | 三蛋 | Relic | 新加入卡即永久升级（战外） | 不算战斗贡献 | |
| WHETSTONE | 磨刀石 | Relic | 战斗开始升级 2 张攻击 | UpgradeDamage 归 WHETSTONE | ⚠️ 需 patch |
| WAR_PAINT | 战漆 | Relic | 战斗开始升级 2 张技能 | UpgradeBlock 归 WAR_PAINT | ⚠️ |
| ORRERY | 星盘 | Relic | — (生成能力) | | |
| METAMORPHOSIS | 蜕变 | Colorless | 变形攻击牌 | Sub-bar + Upgrade | ⚠️ |
| PREPARED | (SHIV 相关) | — | — | — | |

---

## 6. 有效格挡 (EffectiveBlock) + 修正格挡 (ModifierBlock)

说明：普通 Defend 类卡经由 `OnBlockGained` 通用追踪。Modifier block（灵巧/Block ×2 等）由 `Hook.ModifyBlock` 双 patch 拦截。仅列特殊实体。

### 6.1 基础防御牌（单行汇总）

| 实体 ID | 名称 | 角色 | 测试场景 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| DEFEND_IRONCLAD | 防御 | Ironclad | 单用 | EffectiveBlock=5 (+3→8) | |
| DEFEND_SILENT | 防御 | Silent | | EffectiveBlock=5 | |
| DEFEND_DEFECT | 防御 | Defect | | EffectiveBlock=5 | |
| DEFEND_REGENT | 防御 | Regent | | EffectiveBlock=5 | |
| DEFEND_NECROBINDER | 防御 | Necrobinder | | EffectiveBlock=5 | |
| SHRUG_IT_OFF | 耸肩无视 | Ironclad Common | | EffectiveBlock=8 + CardsDrawn=1 | |
| TRUE_GRIT | 真正的勇气 | Ironclad Common | | EffectiveBlock=9 + 消耗 1 张 | |
| BATTLE_TRANCE | 狂战士状态 | Ironclad Common | | CardsDrawn (§11) | |
| DEMONIC_SHIELD | 恶魔护盾 | Ironclad | | EffectiveBlock=8 | |
| STONE_ARMOR | 石甲 | Ironclad | Power | EffectiveBlock on attack | ⚠️ 需 power patch StoneArmorPower |
| SECOND_WIND | 第二风 | Ironclad Uncommon | 弃牌堆转 Block | EffectiveBlock | |
| PILLAGE | 掠夺 | Ironclad Uncommon | 视 Block 打伤 | DirectDamage=8 | |
| BARRICADE | 街垒 | Ironclad Rare | Block 保留 | Power-类，不直接贡献，但后续格挡归 BARRICADE？ | ⚠️ 保留的 block 归属延续 |

### 6.2 特殊 Block 机制

| 实体 ID | 说明 | 贡献 | Gap |
|---|---|---|---|
| DEXTERITY_POWER | 灵巧 +Block 每次 | ModifierBlock 归施加来源 | ✓ Hook.ModifyBlock patched |
| FOOTWORK | 脚法 | +2/3 Dexterity Power | ModifierBlock 归 FOOTWORK | ✓ |
| AFTERIMAGE_POWER | 残影 | 打出每张牌+Block | EffectiveBlock 归 AFTERIMAGE | ✓ |
| PLATING_POWER | 镀层 | 回合末 +Block | ✓ |
| BUFFER_POWER | 缓冲 | 防一次攻击 (→§8) | — | |
| CRIMSON_MANTLE_POWER | 血披风 | 开场 Block | ✓ |
| GRAPPLE_POWER | 抓钩 | Block+抽 | ✓ |
| ANCHOR | 船锚 | Starter Relic | 开场 10 Block | EffectiveBlock=10 归 ANCHOR | ⚠️ |
| BRONZE_SCALES | 青铜鳞 | Relic | 被打反伤 | AttributedDamage 归 BRONZE_SCALES | ⚠️ |
| CAPTAINS_WHEEL | 船长之轮 | Relic | 第 3 回合 +18 Block | ✓ CaptainsWheel AfterBlockCleared patched |
| HORN_CLEAT | 喇叭扣 | Relic | 第 2 回合 +18 Block | ✓ HornCleat AfterBlockCleared patched |
| TOUGH_BANDAGES | 坚硬绷带 | Relic | 弃牌 +3 Block | ✓ |
| CLOAK_CLASP | 披风扣 | Relic | 回合末手中每张 +1 Block | ✓ |
| ORICHALCUM | 山铜 | Relic | 回合末无 Block → +6 Block | ✓ |
| FAKE_ORICHALCUM | 假山铜 | Fake Relic | 同 | ✓ |
| RIPPLE_BASIN | 涟漪盆 | Relic | 回合末 | ✓ |
| PARRYING_SHIELD | 格挡盾 | Relic | 回合末 | ✓ |
| EMOTION_CHIP | 情感芯片 | Relic | 回合开始 | ✓ |

---

## 7. 减伤 — 削弱 (MitigatedByDebuff)

说明：敌人身上的虚弱（Weak）等使攻击力×0.75 衰减。FIFO 归属施加来源。Hook.ModifyDamage 已 patch。

| 实体 ID | 施加者 | 效果 | 预期贡献 | Gap |
|---|---|---|---|---|
| WEAK_POWER | 各种 | Weak | MitigatedByDebuff = 减少的攻击值 | ✓ |
| NEUTRALIZE | Silent Basic | Weak 1/2 | 归 NEUTRALIZE | ✓ |
| CLASH (NO, 是 Ironclad 攻击) | — | — | — | |
| LEG_SWEEP | Silent Uncommon | Weak 2 | ✓ |
| PIERCING_WAIL | Silent Uncommon | Weak 全敌 | ✓ |
| WEAK_POTION | 虚弱药水 | Weak | ✓ |
| SHACKLING_POTION | 束缚药水 | Weak | ✓ |
| MALAISE | Silent | Weak + StrRed (§9) | ✓ |
| SHOCKWAVE | Colorless | Weak + Vuln AoE | ✓ |
| DARK_SHACKLES | Colorless | StrRed (§9) | ✓ |
| THUNDERCLAP | Ironclad | Vuln (§4) + 伤害 | ✓ |
| COLOSSUS_POWER | 巨像 | 减伤 80% | MitigatedByDebuff 归 COLOSSUS | ✓ |
| COLOSSUS (card) | Ironclad Rare Power | 施加 Colossus | ✓ |
| PAPER_KRANE | (§4) | | | |
| DEBILITATE_POWER | (§4) | | | |

---

## 8. 减伤 — 增益 (MitigatedByBuff)

说明：玩家 Buffer（完全挡下一次攻击）、Intangible（伤害→1）等。Buffer 按 §4.4 计算第一段部分格挡 + 后续段。

| 实体 ID | 名称 | 来源 | 效果 | 预期贡献 | Gap |
|---|---|---|---|---|---|
| BUFFER_POWER | 缓冲 | Defect | 挡 1 次攻击全部伤害 | MitigatedByBuff = §4.4 公式 | ✓ BufferPower.ModifyHpLostAfterOstyLate patched |
| BUFFER (card) | 缓冲 | Defect Rare Power | 给自己 Buffer | 归 BUFFER | ✓ |
| INTANGIBLE_POWER | 无实体 | Silent/其他 | 伤害→1/段 | MitigatedByBuff = (D-1)×段数 | ✓ IntangiblePower.ModifyHpLostAfterOsty patched |
| WRAITH_FORM | 亡灵形态 | Silent Rare | Intangible 1 | 归 WRAITH_FORM | ✓ |
| APPARITION | 幻影 | Colorless | Intangible 1 | 归 APPARITION | ✓ |
| GHOST_IN_A_JAR | 瓶中幽灵 | Potion | Intangible 1 | 归 GHOST_IN_A_JAR | ✓ |
| FAIRY_IN_A_BOTTLE | 瓶中精灵 | Potion | 战败时救命 | HpHealed 归 FAIRY_IN_A_BOTTLE | ⚠️ |
| BLUR | 模糊 | Silent Uncommon | Block 延续至下回合 | Block 归属保留 | ⚠️ |
| CLOAK_OF_STARS | 星辰披风 | Regent | buff | ⚠️ |
| PANIC_BUTTON | 恐慌按钮 | Colorless | 30 Block，下回合不得防 | EffectiveBlock=30 | |

---

## 9. 减伤 — 力量削减 (MitigatedByStrReduction)

说明：削减敌人力量造成的伤害减免，按削减量比例分配。

| 实体 ID | 名称 | 来源 | 效果 | Gap |
|---|---|---|---|---|
| MALAISE | 萎靡 | Silent Uncommon | StrReduction 敌人 | ⚠️ 需专门 patch StrReduction 追踪 |
| DARK_SHACKLES | 黑色枷锁 | Colorless | -Strength 本回合 | ⚠️ |
| POTION_OF_BINDING | 束缚药水 | Potion | -Strength | ⚠️ |
| CRIPPLING_CLOUD (KB 无) | — | — | — | — |
| DRAIN_POWER (Necrobinder) | 吸取之力 | Card | -Strength | ⚠️ |
| TEA_OF_DISCOURTESY | 失礼茶 | Relic | 战斗开始 -Str | ⚠️ |
| WHISPERING_EARRING | 耳语耳坠 | Relic | -Str 全体? | ⚠️ |
| ENFEEBLING_TOUCH | 虚弱之触 | Necrobinder | -Strength | ⚠️ |
| HEIRLOOM_HAMMER | 传家宝锤 | Regent | -Str? | ⚠️ |

---

## 10. 自伤 (SelfDamage)

说明：玩家对自身造成的负向"防御"，归属打出卡的来源。不计入防御百分比。

| 实体 ID | 名称 | 角色 | 自伤量 | Gap |
|---|---|---|---|---|
| BLOODLETTING | 放血 | Ironclad Common | -3 HP / +2 能量 | ⚠️ SelfDamage patch 需验证 |
| HEMOKINESIS | 血控 | Ironclad Uncommon | -2 HP + 15 伤 | ⚠️ |
| OFFERING | 献祭 | Ironclad Rare | -6 HP / +2 能量 + 抽 3 | ⚠️ |
| PACTS_END | 契约终结 | Ironclad Uncommon | -HP | ⚠️ |
| BRAND (KB?) | — | — | — | — |
| RUPTURE | 破裂 | Ironclad Rare Power | +Str 当自伤时 | 伤害追溯 | ⚠️ |
| BURNING_PACT | 燃烧契约 | Ironclad | 消耗→抽 (非自伤) | | |
| SPITE | 怨恨 | Ironclad | -HP | ⚠️ |
| BURNING_BLOOD | 燃烧之血 | Ironclad Starter Relic | -2 Strength (无自伤) | ⚠️ |
| RED_SKULL | 红颅 | Relic | 每受伤+3 Str | ✓ RedSkull AfterCurrentHpChanged patched |
| RUNIC_PYRAMID | 符文金字塔 | Relic | — | | |
| FEED (see §1) | | | | |
| PYRE | 柴堆 | Ironclad | 自消耗 (非自伤) | | |
| REAPER_FORM (Necrobinder) | — | — | | ⚠️ |

---

## 11. 抽牌 (CardsDrawn)

说明：额外抽牌归属触发来源。回合开始的 5 张不计。Hook.AfterModifyingHandDraw + 各 ModifyHandDraw 已 patch。

| 实体 ID | 名称 | 类别 | 效果 | Gap |
|---|---|---|---|---|
| POMMEL_STRIKE | 剑柄打击 | Ironclad Common | 抽 1 | ⚠️ 需按卡 ID 注入 source，通用抽牌 patch 应覆盖 |
| BATTLE_TRANCE | 狂战士状态 | Ironclad Common | 抽 3 + 禁抽本回合 | ⚠️ |
| TRUE_GRIT | (非抽) | | | |
| BURNING_PACT | 燃烧契约 | Ironclad Uncommon | 抽 2 消耗 1 | ⚠️ |
| ACROBATICS | 杂技 | Silent Common | 抽 3 弃 1 | ⚠️ |
| SNEAKY | 潜行 | Silent | 抽 | ⚠️ |
| TACTICIAN | 战术家 | Silent | 抽 1 (弃时) | ⚠️ |
| BACKFLIP | 后空翻 | Silent Common | EffectiveBlock=5 + 抽 2 | ⚠️ |
| ADRENALINE | 肾上腺素 | Silent Uncommon | 抽 2 + 能量 (§12) | ⚠️ |
| CALCULATED_GAMBLE | 算计赌博 | Silent | 弃手抽同数 | ⚠️ |
| ESCAPE_PLAN | 逃生计划 | Silent Common | 抽 1 | ⚠️ |
| INFINITE_BLADES | 无限刀刃 | Silent Rare | 回合开始抽 SHIV (§15) | ✓ InfiniteBladesPower patched |
| EXPERTISE | 专精 | Silent | 抽到手数 | ⚠️ |
| DEEP_BREATH (KB 无) | — | — | — | |
| TOOLS_OF_THE_TRADE_POWER | 工具箱 | Silent Power | 每回合 +1 抽 | ✓ |
| MACHINE_LEARNING_POWER | 机器学习 | Defect Power | +1 抽/回合 | ✓ |
| CREATIVE_AI_POWER | 创造性 AI | Defect | 每回合生成 Power | ✓ |
| HELLO_WORLD_POWER | HelloWorld | Defect | 每回合 1 Common | ✓ |
| AUTOMATION_POWER | 自动化 | Defect | 抽牌→生成 | ✓ |
| ITERATION_POWER | 迭代 | Defect | 抽牌修改 | ✓ |
| SPEEDSTER_POWER | 极速者 | Defect | 抽 | ✓ |
| PALE_BLUE_DOT_POWER | 暗淡蓝点 | Regent | 抽牌 | ✓ PaleBlueDotPower.ModifyHandDraw patched |
| TYRANNY_POWER | 暴政 | Regent | +1 抽 | ✓ |
| DEMESNE_POWER | 领域 | Necrobinder | +1 抽 | ✓ |
| SENTRY_MODE_POWER | 哨兵模式 | Defect | 抽 | ✓ |
| CALL_OF_THE_VOID_POWER | 虚空召唤 | Necrobinder | 抽 | ✓ |
| PAGESTORM_POWER | 纸片风暴 | Necrobinder | 抽牌伤害 | ✓ (§2) |
| SPECTRUM_SHIFT_POWER | 光谱转换 | Regent | 抽 | ✓ |
| BAG_OF_PREPARATION | 准备之袋 | Relic | 开场 +2 抽 | ⚠️ |
| POCKETWATCH | 怀表 | Relic | +3 抽若 ≤3 牌 | ⚠️ |
| UNCEASING_TOP | 不停转陀螺 | Relic | 手空时抽 1 | ⚠️ |
| BELLOWS | 风箱 | Relic | +1 抽 每回合 | ⚠️ |
| TINY_MAILBOX | 迷你邮箱 | Relic | 每张打出抽 1? | ⚠️ |
| TINGSHA | 铜铃 | Relic | 弃牌抽/伤 | ✓ Tingsha AfterCardDiscarded patched |
| MELT_OF_THE_BARD (KB 无) | — | — | — | — |
| JUGGLING (card) | 杂耍 | Ironclad Common? | — | |
| SCRAWL | 草稿 | Colorless | 抽至手满 | ⚠️ |
| FINESSE | 技艺 | Colorless | Block + 抽 1 | ⚠️ |
| IMPATIENCE | 不耐 | Colorless | 无攻击则抽 2 | ⚠️ |
| THINKING_AHEAD | 远见 | Colorless | 抽 2 | ⚠️ |
| SKIM | 略读 | Defect Common | 抽 3 | ⚠️ |
| DUALCAST/MULTI_CAST/QUADCAST | 双/多/四连发 | Defect | evoke | 非抽牌，见 §2 orb | |
| DRUM_OF_BATTLE | 战鼓 | Ironclad Uncommon | 下回合 +能量/抽 | §12 | |

---

## 12. 能量 (EnergyGained)

说明：额外能量归属来源。`PlayerCmd.GainEnergy` 已 patch。**§4.2 Confused（SneckoEye 随机改费可为负）/ §4.3 Enlightenment 部分降费 / §NEW-1 免费打出节省**均计入。

### 12.1 主动回能

| 实体 ID | 名称 | 类别 | +能量 | Gap |
|---|---|---|---|---|
| BLOODLETTING | 放血 | Ironclad Common | +2 | ✓ GainEnergy patched |
| OFFERING | 献祭 | Ironclad Rare | +2 | ✓ |
| SECOND_WIND | 第二风 | — (Block) | | |
| ENERGY_POTION | 能量药水 | Common Potion | +2 | ✓ |
| DOUBLE_ENERGY | 双倍能量 | Defect Uncommon | 本回合 +现能量 | ✓ |
| ADRENALINE | 肾上腺素 | Silent Uncommon | +1 + 抽 2 | ✓ |
| SKIM_POWER (KB 无) | — | — | — | — |
| ENERGY_SURGE | 能量激涌 | Defect | +能量 | ⚠️ |
| TURBO | 涡轮 | Defect | +能量 | ⚠️ |
| CHARGE_BATTERY_POWER | 蓄电池 | Defect | +1 能量下回合 | ⚠️ |
| CONQUEROR | 征服者 | Regent | +能量 if first | ⚠️ |
| LARGESSE | 慷慨 | Regent | +能量 | ⚠️ |
| ROYALTIES | 王室赏 | Regent | +能量 | ⚠️ |
| HIDDEN_GEM | 隐藏宝石 | Colorless | +能量? | ⚠️ |
| LIFT (colorless) | | | | |

### 12.2 免费打出节省 (§NEW-1)

| VOID_FORM_POWER | 虚空形态 | Regent Power | 下 N 张免费 | 节省的 cost 归 VOID_FORM + StarsContribution | ✓ TryModifyEnergyCostInCombat patched |
| FREE_ATTACK_POWER/FREE_SKILL_POWER/FREE_POWER_POWER | 下张免费 | 各种 | 节省 cost 归施加来源 | ✓ patched |
| CORRUPTION_POWER | 腐败 | Ironclad | 技能 0 费 | ✓ patched |
| VEILPIERCER_POWER | 穿纱 | Necrobinder | 技能 0 费 | ✓ patched |
| CURIOUS_POWER | 好奇心 | — | | ✓ patched |
| UNRELENTING | 无情猛攻 | Ironclad | 下张免费 | ✓ (via FreeAttackPower) |
| MAYHEM | 混乱 | Colorless | 免费自动打出 | ⚠️ 需验证 |
| PANACHE (card/power) | 风度 | Colorless | — | | |

### 12.3 生成的免费卡（不算能量，只 sub-bar）

- ATTACK_POTION / SKILL_POTION / POWER_POTION / COLORLESS_POTION — 生成本回合免费打出的牌，贡献计入 sub-bar → §15
- BURST — 重复下张技能
- HAVOC — 随机打出抽牌堆顶
- DRAMATIC_ENTRANCE (colorless) — 战斗开始免费打出 3 张 attack

### 12.4 Confused §4.2

| SNECKO_EYE | 蛇眼 | Ancient Relic | Confused：抽牌随机改费 [0,3] | EnergyGained = Σ(original_cost - random_cost)，可负 | ⚠️ 新需求，未 patch |
| FAKE_SNECKO_EYE | 假蛇眼 | 事件遗物 | 同上 | ⚠️ |
| SNECKO_OIL | 蛇眼油 | Potion | 同上 3 回合 | ⚠️ |

### 12.5 Enlightenment §4.3

| ENLIGHTENMENT | 开悟 | Colorless | 手牌 cost→1 | EnergyGained = Σ max(0, cost-1)，归 ENLIGHTENMENT | ⚠️ 新需求，未 patch |

### 12.6 Relic 能量

| RUNIC_DOME (KB 无) | — | — | — | |
| CURSED_KEY (KB 无) | — | — | — | |
| PHILOSOPHERS_STONE | 哲学家石 | Boss Relic | +1 能量 永久 | ⚠️ |
| COFFEE_DRIPPER (KB 无) | — | — | — | |
| ECTOPLASM | 质体外浆 | Boss Relic | +1 能量 | ⚠️ |
| FUSION_HAMMER (KB 无) | — | — | — | |
| MARK_OF_PAIN (KB 无) | — | — | — | |
| SOZU | 僧都 | Boss Relic | +1 能量 | ⚠️ |
| BOTTLED_POTENTIAL | 瓶装潜能 | Potion (?) | +能量 | ⚠️ |
| VELVET_CHOKER | 天鹅绒项圈 | Relic | +1 能量 | ⚠️ |
| POWER_CELL | 能量电池 | Relic | 战斗开始 +3 能量 | ⚠️ |
| LAVA_LAMP | 熔岩灯 | Relic | +能量? | ⚠️ |
| MERCURY_HOURGLASS | 水银沙漏 | Relic | +能量? | ✓ MercuryHourglass AfterPlayerTurnStart patched |
| CAULDRON | 大锅 | Relic | — | |

---

## 13. 辉星 (StarsContribution)

说明：漫游者（Regent）独有。`PlayerCmd.GainStars` 已 patch。**§NEW: VoidForm 等免费打出节省的星费也计入**。

| 实体 ID | 名称 | 效果 | Gap |
|---|---|---|---|
| GATHER_LIGHT | 聚光 | Regent Common | +Stars | ✓ |
| RADIATE | 辐射 | Regent Common | +Stars | ✓ |
| STARDUST | 星尘 | Regent | +Stars | ✓ |
| SHINING_STRIKE / SOLAR_STRIKE | (§1) | 消耗 Stars 增伤 | ⚠️ 消耗归属 |
| SEVEN_STARS | (§1) | 每颗星打 7 | ⚠️ |
| PROPHESIZE | 预言 | Regent | +Stars | ✓ |
| GLIMMER | 微光 | Regent | +Stars | ✓ |
| GLOW | 炽热 | Regent | +Stars | ✓ |
| HEGEMONY | 霸权 | Regent | +Stars | ✓ |
| CELESTIAL_MIGHT | 天体之力 | Regent Power | +Stars 每 turn | ✓ |
| GUIDING_STAR | 指引之星 | Regent | +Stars | ✓ |
| VOID_FORM_POWER | 虚空形态 | (§12) | 节省的 star cost 归 VOID_FORM | ✓ TryModifyStarCost patched |
| STAR_POTION | 辉星药水 | Potion | +Stars | ✓ |
| TANXS_WHISTLE | 坦克哨 | Relic | +Stars on turn | ⚠️ |
| BLACK_STAR / WHITE_STAR | 黑/白星 | Relic | +Stars | ⚠️ |
| DIVINE_DESTINY | 神圣命运 | Relic | | ⚠️ |
| DIVINE_RIGHT | 神圣权利 | Relic | | ⚠️ |
| RADIANT_PEARL | 辉耀珍珠 | Relic | | ⚠️ |
| GOLDEN_PEARL | 金色珍珠 | Relic | | ⚠️ |

---

## 14. 治疗 (HpHealed)

说明：`CreatureCmd.Heal` 双 patch + `CreatureCmd.GainMaxHp` 双 patch。战斗内/外/最大 HP 增加均计入。

### 14.1 卡牌治疗

| 实体 ID | 名称 | 来源 | 效果 | Gap |
|---|---|---|---|---|
| FEED | 狂宴 | Ironclad Rare | Fatal→+MaxHp | ⚠️ 需验证 FEED 作为 fallbackSource |
| REAPER (KB 无) | — | — | — | — |
| DEVOUR_LIFE | 吞噬生命 | Necrobinder | 伤害→回血 | ✓ DevourLifePower patched |
| REAPER_FORM_POWER | 收割形态 | Necrobinder Power | 伤害→回血 | ✓ |
| NECRO_MASTERY_POWER | 死灵大师 | Necrobinder | 扣血关联 | ✓ NecroMasteryPower.AfterCurrentHpChanged patched |
| BITE (KB 无 general) | — | — | — | — |
| HIGH_FIVE (KB) | 击掌 | Necrobinder | +HP | ⚠️ |

### 14.2 药水治疗

| BLOOD_POTION | 血药 | Common Potion | 回血 | ✓ |
| REGEN_POTION | 回血药水 | Potion | Regen Power | ✓ |
| FRUIT_JUICE | 果汁 | Rare Potion | +MaxHp | ✓ |
| CURE_ALL | 全治 | Potion | 回血 | ✓ |
| HEART_OF_IRON | 铁心 | Potion | — | ⚠️ |

### 14.3 遗物治疗（战斗内/战斗后/回合开始）

| BURNING_BLOOD | 燃烧之血 | Ironclad Starter | 战胜后 +6 HP | ✓ BurningBlood AfterCombatVictory patched |
| BLACK_BLOOD | 黑血 | Evolved Burning Blood | 战胜后 +12 HP | ✓ |
| MEAT_ON_THE_BONE | 肉带骨 | Relic | HP<½ 战胜 +12 HP | ✓ MeatOnTheBone patched |
| DEMON_TONGUE | 恶魔之舌 | Relic | 被击→回血 | ✓ DemonTongue patched |
| BLOOD_VIAL | 血瓶 | Common Relic | 战斗开始 +2 HP | ⚠️ |
| FAKE_BLOOD_VIAL | 假血瓶 | Fake Relic | 同 | ⚠️ |
| BOOK_OF_FIVE_RINGS (KB?) | — | — | — | — |
| OMAMORI (KB 无) | — | — | — | — |
| PEAR | 梨 | Relic | +MaxHp | ⚠️ 需 GainMaxHp fallback |
| STRAWBERRY | 草莓 | Relic | +MaxHp | ⚠️ |
| MANGO | 芒果 | Relic | +MaxHp | ⚠️ |
| FAKE_MANGO | 假芒果 | Fake Relic | +MaxHp | ⚠️ |
| LEES_WAFFLE / FAKE_LEES_WAFFLE | 华夫 | Relic | +MaxHp + 回血 | ⚠️ |
| GHOST_SEED | 幽灵种子 | Relic | 休息时 +HP | ⚠️ |
| LASTING_CANDY | 永恒糖果 | Relic | +MaxHp | ⚠️ |
| POMANDER | 香囊 | Relic | 休息 +HP | ⚠️ |
| BONE_TEA | 骨茶 | Relic | | ⚠️ |
| EMBER_TEA | 余烬茶 | Relic | | ⚠️ |
| VERY_HOT_COCOA | 热可可 | Relic | | ⚠️ |
| NUTRITIOUS_SOUP | 营养汤 | Relic | | ⚠️ |
| NUTRITIOUS_OYSTER | 营养牡蛎 | Relic | | ⚠️ |
| FRAGRANT_MUSHROOM | 芳香蘑菇 | Relic | | ⚠️ |
| DRAGON_FRUIT | 火龙果 | Relic | | ⚠️ |
| YUMMY_COOKIE | 美味饼干 | Relic | | ⚠️ |
| BREAD | 面包 | Relic | | ⚠️ |
| MEAL_TICKET | 餐券 | Relic | Merchant 休息+HP | ⚠️ |
| CHOSEN_CHEESE | 选择之奶酪 | Relic | | ⚠️ |
| ETERNAL_FEATHER | 永恒羽毛 | Relic | 每 5 牌 +3 HP | ⚠️ |
| MAW_BANK | 大嘴银行 | Relic | 金币 (非治疗) | — |
| GOLDEN_COMPASS | 金罗盘 | Relic | — | — |
| LIZARD_TAIL | 蜥蜴尾 | Rare Relic | 死亡救命 +50%HP | ⚠️ |
| DOLLYS_MIRROR | 多莉之镜 | Relic | 复制 | — |

**§NEW 跨层恢复**：Ancients 事件的 +MaxHp 在跨层 80%/100% 恢复时会产生 HpHealed 贡献，需追踪。

---

## 15. 子来源 (Sub-bar 父实体)

说明：生成/变形其他卡的实体，子卡贡献归到父。`CardCmd.Transform` + `Hook.AfterCardGeneratedForCombat` 已 patch，另有多个 power/relic 单独追踪。

| 实体 ID | 名称 | 类别 | 生成/变形内容 | Gap |
|---|---|---|---|---|
| HAVOC | 浩劫 | Ironclad Common | 随机打出抽牌堆顶 | ⚠️ Sub-bar 归属 |
| PYRE | 柴堆 | Ironclad | 消耗→重置 (非生成) | — |
| EXHUME (KB 无) | — | — | — | — |
| FORGE (KB 无) | — | — | — | — |
| METAMORPHOSIS | 蜕变 | Colorless | 变攻击→升级随机 | ⚠️ |
| PRIMAL_FORCE (KB?) | 原始力量 | Ironclad | 攻击→GIANT_ROCK | ⚠️ 特别提及 |
| GIANT_ROCK | 巨石 | 衍生卡 | 由 PRIMAL_FORCE 生成 | sub-bar to PRIMAL_FORCE |
| BLADE_DANCE / CLOAK_AND_DAGGER / STORM_OF_STEEL / INFINITE_BLADES | SHIV 生成器 | Silent | 生成 SHIV | Sub-bar to 父 |
| SHIV | 匕首 | 衍生 | 由多个父生成，DirectDamage=4 | always sub |
| GLASS_KNIFE (KB 无) | — | — | — | — |
| BLADE_OF_INK | 墨之刃 | Silent | | ⚠️ |
| PHANTOM_BLADES | (§1) | 生成? | | |
| UP_MY_SLEEVE | 袖中藏刀 | Silent | 生成 SHIV | ⚠️ |
| BEACON_OF_HOPE (card, colorless) | 希望之光 | Colorless | 生成能力/治疗 | ⚠️ |
| DISCOVERY | 发现 | Colorless | 生成 1 张 | ⚠️ |
| SECRET_TECHNIQUE / SECRET_WEAPON | 秘术/秘武 | Colorless | 抽特定类型 | ⚠️ |
| JACK_OF_ALL_TRADES | 万金油 | Colorless | 生成 colorless | ⚠️ |
| MASTER_OF_STRATEGY | 战略大师 | Colorless | 抽 3 | ⚠️ |
| PROLONG | 延长 | Colorless | 手牌延留 | ⚠️ |
| CALAMITY | 灾厄 | Colorless | — | ⚠️ |
| CATASTROPHE | 大灾难 | Colorless | — | ⚠️ |
| ATTACK_POTION | 攻击药水 | Common Potion | 生成 3 张攻击（免费本回合）| Sub-bar to ATTACK_POTION (不计能量) |
| SKILL_POTION | 技能药水 | 同 | | Sub-bar |
| POWER_POTION | 能力药水 | 同 | | Sub-bar |
| COLORLESS_POTION | 无色药水 | 同 | | Sub-bar |
| ENTROPIC_BREW | 熵酿 | Potion | 生成随机药水 | ⚠️ |
| DUPLICATOR | 复制者 | Potion | 下张打出复制 | ⚠️ |
| MIMIC | 拟态 | Colorless | 复制 | ⚠️ |
| ORRERY | 天文仪 | Boss Relic | 战斗开始生成 | ⚠️ |
| TOOLBOX | 工具箱 | Relic | 战斗开始选 colorless | ⚠️ |
| ASTROLABE | 星盘 | Relic | 战斗开始变换手牌 | ⚠️ |
| PANDORAS_BOX | 潘多拉魔盒 | Relic | 战外变换 | — |
| TOY_BOX (KB) | 玩具盒 | Relic | 生成 | ⚠️ |
| VITRUVIAN_MINION | 维特鲁威小兵 | Relic | 生成小兵 | ⚠️ |
| PAELS_LEGION | 佩尔之军团 | Relic | 生成 | ⚠️ |
| WISH | 愿望 | Colorless Rare | 选 1 | ⚠️ |
| JACKPOT | 头奖 | Colorless | 随机效果 | ⚠️ |
| PRIMAL_FORCE → GIANT_ROCK | (见上) | | | |
| PILLAR_OF_CREATION_POWER | (§2) | | | ✓ AfterCardGeneratedForCombat patched |
| SMOKESTACK_POWER | (§2) | | | ✓ |
| TRASH_TO_TREASURE_POWER | (§2) | | | ✓ |

### 15.1 Osty 相关（Necrobinder）

| OSTY_SUMMON 源 | 各种 Necrobinder 卡/遗物 | 召 Osty HP | HP 消耗→防御贡献 | ✓ OstyCmd.Summon patched |
| EIDOLON / INVOKE / DEFILE / SCOURGE / HAUNT (part) | | | ✓/部分 |
| BOUND_PHYLACTERY | 束缚圣物盒 | Relic | Osty | ⚠️ |
| PHYLACTERY_UNBOUND | 解放圣物盒 | Relic | Osty | ⚠️ |
| UNDYING_SIGIL | 不朽之印 | Relic | Osty | ⚠️ |
| BEATING_REMNANT | 跳动残骸 | Relic | Osty | ⚠️ |
| SPUR (card) | 激励 | Necrobinder | Osty | ⚠️ |
| PROTECTOR (card) | 保护者 | Necrobinder | Osty | ⚠️ |
| HAUNT (card) | 缠扰 | Necrobinder | Osty | ⚠️ |

---

## 16. Gap 汇总（最显著的 5 个 + 完整清单）

### 16.1 最显著 5 个 Gap（按影响面排序）

1. **⚠️ §4.2 SneckoEye / FakeSneckoEye / SneckoOil Confused 能量贡献**（PRD-04 §4.2 新需求）— 全局影响能量贡献，可为负值，当前 mod 完全未追踪 Confused 改费。涉及 `ConfusedPower` hook + 卡牌 cost 变更监听。
2. **⚠️ §4.3 Enlightenment 部分降费**（PRD-04 §4.3）— 开悟把手牌降到 1 费，节省的能量需归属 ENLIGHTENMENT。无现有 patch。
3. **⚠️ Strength Reduction 归因（MALAISE / DARK_SHACKLES / POTION_OF_BINDING / DRAIN_POWER / ENFEEBLING_TOUCH / TEA_OF_DISCOURTESY / WHISPERING_EARRING）** — §4.5.1 明确要求按削减量比例分配，但目前 CombatTracker 未见 `OnStrengthReductionRegistered` 相关 API。
4. **⚠️ 食物类遗物 +MaxHp 追踪缺失**（PEAR / STRAWBERRY / MANGO / LASTING_CANDY / 各种 tea/cocoa 等约 15 个）— 需利用已有 `GainMaxHp` patch + fallback ID 注入。
5. **⚠️ AKABEKO / VAJRA / GIRYA / STRIKE_DUMMY / PEN_NIB / PUNCH_DAGGER 等加法修正遗物** — 一部分仅 patch 了"施加时机"但未保证 ModifyDamage 钩子里的 source 归因，需要验证 PowerSource 注册到对应 StrengthPower。

### 16.2 完整 Gap 清单（按章节）

- §1 直接伤害：RAMPAGE (累加), CLAW (累加), COMPILE_DRIVER (orb 联动 +伤), SEEKING_EDGE (非主目标分裂), SEVEN_STARS/SHINING_STRIKE (stars 消耗), DEFILE/EIDOLON/INVOKE (Osty 联动)
- §2 间接伤害：STORM_POWER, GUNK_UP, CALTROPS, THORNS (来源追踪), SEANCE
- §3 加法修正：FLEX_POTION (临时), ACCURACY_POWER (仅 SHIV), PEN_NIB, STRIKE_DUMMY / FAKE_STRIKE_DUMMY, FENCING_MANUAL, PRECARIOUS_SHEARS, PUNCH_DAGGER, VAJRA, GIRYA, SPOILS_MAP, DATA_DISK (Focus 子计算), FOCUS_POTION, FROZEN_EGG/MOLTEN_EGG/TOXIC_EGG, RADIANT_TINCTURE, THRUMMING_HATCHET
- §4 乘法修正：DOUBLE_DAMAGE (若存在), WRAITH_FORM Vuln 自伤归属
- §5 升级：WHETSTONE, WAR_PAINT, METAMORPHOSIS
- §6 Block：STONE_ARMOR_POWER, BARRICADE 保留归属, ANCHOR, BRONZE_SCALES
- §7 削弱减伤：普通流程覆盖
- §8 Buffer/Intangible：FAIRY_IN_A_BOTTLE (救命回血), BLUR (Block 延续)
- §9 StrRed：MALAISE, DARK_SHACKLES, POTION_OF_BINDING, DRAIN_POWER, TEA_OF_DISCOURTESY, WHISPERING_EARRING, ENFEEBLING_TOUCH, HEIRLOOM_HAMMER
- §10 自伤：BLOODLETTING, HEMOKINESIS, OFFERING, PACTS_END, SPITE, BURNING_BLOOD (Strength 减?), RUPTURE 联动
- §11 抽牌：POMMEL_STRIKE, BATTLE_TRANCE, ACROBATICS, BACKFLIP, CALCULATED_GAMBLE, ESCAPE_PLAN, EXPERTISE, BAG_OF_PREPARATION, POCKETWATCH, UNCEASING_TOP, BELLOWS, TINY_MAILBOX, SCRAWL, FINESSE, IMPATIENCE, THINKING_AHEAD, SKIM, BURNING_PACT — **大部分由 GainEnergy/ModifyHandDraw 通用 patch 覆盖，但需验证 source 注入**
- §12 能量：SNECKO_EYE/FAKE_SNECKO_EYE/SNECKO_OIL (§4.2), ENLIGHTENMENT (§4.3), ENERGY_SURGE, TURBO, CHARGE_BATTERY, CONQUEROR, LARGESSE, ROYALTIES, HIDDEN_GEM, MAYHEM, PHILOSOPHERS_STONE, ECTOPLASM, SOZU, VELVET_CHOKER, POWER_CELL, LAVA_LAMP, BOTTLED_POTENTIAL
- §13 辉星：SHINING_STRIKE/SOLAR_STRIKE/SEVEN_STARS (消耗归属), TANXS_WHISTLE, BLACK_STAR/WHITE_STAR, DIVINE_DESTINY, DIVINE_RIGHT, RADIANT_PEARL, GOLDEN_PEARL
- §14 治疗：FEED (MaxHp), HIGH_FIVE, HEART_OF_IRON, BLOOD_VIAL/FAKE_BLOOD_VIAL, PEAR, STRAWBERRY, MANGO/FAKE_MANGO, LEES_WAFFLE/FAKE_LEES_WAFFLE, GHOST_SEED, LASTING_CANDY, POMANDER, BONE_TEA, EMBER_TEA, VERY_HOT_COCOA, NUTRITIOUS_SOUP, NUTRITIOUS_OYSTER, FRAGRANT_MUSHROOM, DRAGON_FRUIT, YUMMY_COOKIE, BREAD, MEAL_TICKET, CHOSEN_CHEESE, ETERNAL_FEATHER, LIZARD_TAIL, FAIRY_IN_A_BOTTLE, Ancients 事件跨层 (§NEW)
- §15 Sub-bar：HAVOC, METAMORPHOSIS, PRIMAL_FORCE→GIANT_ROCK, BLADE_OF_INK, UP_MY_SLEEVE, BEACON_OF_HOPE, DISCOVERY, SECRET_TECHNIQUE/SECRET_WEAPON, JACK_OF_ALL_TRADES, MASTER_OF_STRATEGY, PROLONG, CALAMITY, CATASTROPHE, ENTROPIC_BREW, DUPLICATOR, MIMIC, ORRERY, TOOLBOX, ASTROLABE, TOY_BOX, VITRUVIAN_MINION, PAELS_LEGION, WISH, JACKPOT, BOUND_PHYLACTERY, PHYLACTERY_UNBOUND, UNDYING_SIGIL, BEATING_REMNANT, SPUR, PROTECTOR, HAUNT (card)

### 16.3 Paels 系列（Ancient Relics，地城后期）

Paels 系列 9 件（PAELS_BLOOD / PAELS_CLAW / PAELS_EYE / PAELS_FLESH / PAELS_GROWTH / PAELS_HORN / PAELS_LEGION / PAELS_TEARS / PAELS_TOOTH / PAELS_WING）仅 PAELS_TEARS 已 patch (AfterSideTurnStart)；其余 8 件需逐一验证是否产生战斗贡献并 patch。

---

## 17. 已 patch 实体清单参考

### 17.1 已 patch Relic（CombatHistoryPatch.PatchRelicHooks）
BurningBlood, BlackBlood, MeatOnTheBone, DemonTongue, ScreamingFlagon, StoneCalendar, ParryingShield, CharonsAshes, ForgottenSoul, FestivePopper, MercuryHourglass, MrStruggles, Metronome, Tingsha, Kusarigama, LetterOpener, LostWisp, CloakClasp, FakeOrichalcum, Orichalcum, RippleBasin, ToughBandages, IntimidatingHelmet, HornCleat, CaptainsWheel, GalacticDust, DaughterOfTheWind, OrnamentalFan, Permafrost, TuningFork, ArtOfWar, IvoryTile, Nunchaku, PaelsTears, Candelabra, Chandelier, Lantern, HappyFlower, FakeHappyFlower, GremlinHorn, CentennialPuzzle, GamePiece, IronClub, JossPaper, Pendulum, BlessedAntler, HandDrill, Kunai, Shuriken, RainbowRing, RedSkull, Akabeko, BagOfMarbles, Brimstone, RedMask, SlingOfCourage, LunarPastry, EmotionChip

### 17.2 已 patch Power（CombatHistoryPatch.PatchPowerHooks）
RagePower, FlameBarrierPower, FeelNoPainPower, PlatingPower, InfernoPower, JuggernautPower, GrapplePower, CrimsonMantlePower, DarkEmbracePower, PoisonPower, OutbreakPower, PanachePower, AfterimagePower, SneakyPower, ViciousPower, EnvenomPower, NoxiousFumesPower, InfiniteBladesPower, HailstormPower, AutomationPower, IterationPower, SubroutinePower, ConsumingShadowPower, LoopPower, LightningRodPower, SpinnerPower, CreativeAiPower, HelloWorldPower, ThunderPower, DanseMacabrePower, CountdownPower, HauntPower, PagestormPower, ShroudPower, SleightOfFleshPower, OblivionPower, CallOfTheVoidPower, NecroMasteryPower, DevourLifePower, SpiritOfAshPower, ReaperFormPower, SicEmPower, BlackHolePower, ChildOfTheStarsPower, ParryPower, PillarOfCreationPower, OrbitPower, MonologuePower, MonarchsGazePower, SpectrumShiftPower, ReflectPower, GenesisPower, TheSealedThronePower, FurnacePower, SentryModePower, SmokestackPower, RollingBoulderPower, SpeedsterPower, SerpentFormPower, TrashToTreasurePower, TemporaryStrengthPower

### 17.3 已 patch Hook/Cmd
Hook.ModifyDamage (×4 — 处理 Vuln/Cruelty/Weak/Paper Krane/Debilitate), Hook.ModifyBlock (×2 — 处理 Dexterity/block multipliers), Hook.AfterBlockCleared, Hook.AfterModifyingHandDraw, Hook.AfterCardGeneratedForCombat, Hook.AfterOrbChanneled
PowerModel.ApplyInternal, PowerModel.SetAmount (×2)
PlayerCmd.GainEnergy, PlayerCmd.GainStars
CardCmd.Transform, CardCmd.Upgrade (×2)
CombatHistory.{CardPlayStarted, CardPlayFinished, DamageReceived, BlockGained, PowerReceived, CardDrawn, PotionUsed}
CreatureCmd.{Heal (×2), GainMaxHp (×2), Kill}
BufferPower.ModifyHpLostAfterOstyLate (×2), IntangiblePower.ModifyHpLostAfterOsty (×2)
PaleBlueDotPower/TyrannyPower/DemesnePower/MachineLearningPower/ToolsOfTheTradePower.ModifyHandDraw
VoidFormPower.{TryModifyEnergyCostInCombat, TryModifyStarCost}
FreeAttack/FreeSkill/FreePowerPower.TryModifyEnergyCostInCombat
CorruptionPower/VeilpiercerPower/CuriousPower.TryModifyEnergyCostInCombat
CardModel.{SetToFreeThisTurn, SetToFreeThisCombat}
DoomPower.DoomKill, SovereignBlade.OnPlay
OstyCmd.Summon, OrbCmd.{Passive, EvokeNext, EvokeLast}, PotionModel.OnUseWrapper

---

## 附录 A：测试回归脚本建议

1. **基础攻防回归**：对每个角色打 STRIKE/DEFEND，预期贡献 = KB vars [base]。
2. **Modifier 组合回归**：BASH→STRIKE（验 Vuln 归因），INFLAME→STRIKE（验 Strength 归因），BASH→CRUELTY→STRIKE（验 §4.1 比例缩放）。
3. **Buffer §4.4 回归**：按 PRD-04 §4.4 示例（5×4 攻击 + 2 Buffer + 7 Block），验 Buffer.MitigatedByBuff=8。
4. **§4.2 回归**：装 SNECKO_EYE，记录多回合 EnergyGained，验 Σ(old-new) 归 SNECKO_EYE 可为负。
5. **§4.3 回归**：手持 3 费牌打出 ENLIGHTENMENT，验 EnergyGained=2 归 ENLIGHTENMENT。
6. **Osty 回归**：Necrobinder 召 Osty，Osty 承伤 5 + 攻击 7，验双向归因。
7. **每张 Gap 卡单测**：按 §16.2 清单逐一打出，对比实际 contribution vs KB 预期数值。
