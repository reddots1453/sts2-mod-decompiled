# DEFECT Cards

## Ancient

### Biased Cognition / 偏差认知
- **ID**: `BIASED_COGNITION` | **Type**: Power | **Rarity**: Ancient | **Cost**: 1 | **Target**: Self
- **Values**: FocusPower: 4 | BiasedCognitionPower: 1
- **EN**: Gain {FocusPower:diff()} [gold]Focus[/gold].
At the start of your turn, lose {BiasedCognitionPower:diff()} [gold]Focus[/gold].
- **CN**: 获得{FocusPower:diff()}点[gold]集中[/gold]。
在你的回合开始时，失去{BiasedCognitionPower:diff()}点[gold]集中[/gold]。

### Quadcast / 四重释放
- **ID**: `QUADCAST` | **Type**: Skill | **Rarity**: Ancient | **Cost**: 1 | **Target**: Self
- **Values**: Repeat: 4
- **EN**: [gold]Evoke[/gold] your rightmost Orb {Repeat:diff()} {Repeat:plural:time|times}.
- **CN**: [gold]激发[/gold]你最右侧的充能球{Repeat:diff()}次。

## Basic

### Defend / 防御
- **ID**: `DEFEND_DEFECT` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Dualcast / 双重释放
- **ID**: `DUALCAST` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **EN**: [gold]Evoke[/gold] your rightmost Orb twice.
- **CN**: [gold]激发[/gold]你最右侧的充能球两次。

### Strike / 打击
- **ID**: `STRIKE_DEFECT` | **Type**: Attack | **Rarity**: Basic | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Zap / 电击
- **ID**: `ZAP` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **EN**: [gold]Channel[/gold] 1 [gold]Lightning[/gold].
- **CN**: [gold]生成[/gold]1个[gold]闪电[/gold]充能球。

## Common

### Ball Lightning / 球状闪电
- **ID**: `BALL_LIGHTNING` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7
- **EN**: Deal {Damage:diff()} damage.
[gold]Channel[/gold] 1 [gold]Lightning[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
[gold]生成[/gold]1个[gold]闪电[/gold]充能球。

### Barrage / 弹幕齐射
- **ID**: `BARRAGE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 5 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: Deal {Damage:diff()} damage for each [gold]Channeled[/gold] Orb.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: 当前每有一个[gold]充能球[/gold]，造成{Damage:diff()}点伤害。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Beam Cell / 光束射线
- **ID**: `BEAM_CELL` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 3 | VulnerablePower: 1
- **EN**: Deal {Damage:diff()} damage.
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Boost Away / 高速脱离
- **ID**: `BOOST_AWAY` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: Self
- **Values**: Block: 6
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Add a [gold]Dazed[/gold] into your [gold]Discard Pile[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将一张[gold]晕眩[/gold]添加到你的[gold]弃牌堆[/gold]中。

### Charge Battery / 充电
- **ID**: `CHARGE_BATTERY` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 7 | Energy: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Next turn, gain {Energy:energyIcons()}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在下个回合获得{Energy:energyIcons()}。

### Claw / 爪击
- **ID**: `CLAW` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 3 | Increase: 2
- **EN**: Deal {Damage:diff()} damage.
Increase the damage of ALL Claw cards by {Increase:diff()} this combat.
- **CN**: 造成{Damage:diff()}点伤害。
本场战斗中所有爪击卡牌的伤害增加{Increase:diff()}点。

### Cold Snap / 寒流
- **ID**: `COLD_SNAP` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
[gold]Channel[/gold] 1 [gold]Frost[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
[gold]生成[/gold]1个[gold]冰霜[/gold]充能球。

### Compile Driver / 编译冲击
- **ID**: `COMPILE_DRIVER` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedCards: 0
- **EN**: Deal {Damage:diff()} damage.
Draw 1 card for each unique Orb you have.{InCombat:
(Draw {CalculatedCards:diff()} {CalculatedCards:plural:card|cards})|}
- **CN**: 造成{Damage:diff()}点伤害。
你每有一种不同的充能球，就抽一张牌。{InCombat:
（抽{CalculatedCards:diff()}张牌）}

### Coolheaded / 冷静头脑
- **ID**: `COOLHEADED` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: [gold]Channel[/gold] 1 [gold]Frost[/gold].
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: [gold]生成[/gold]1个[gold]冰霜[/gold]充能球。
抽{Cards:diff()}张牌。

### Focused Strike / 集中打击
- **ID**: `FOCUSED_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 9 | FocusPower: 1
- **EN**: Deal {Damage:diff()} damage.
Gain {FocusPower:diff()} [gold]Focus[/gold] this turn.
- **CN**: 造成{Damage:diff()}点伤害。
在本回合获得{FocusPower:diff()}点[gold]集中[/gold]。

### Go for the Eyes / 眼部攻击
- **ID**: `GO_FOR_THE_EYES` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 3 | WeakPower: 1
- **EN**: Deal {Damage:diff()} damage.
If the enemy intends to attack, apply {WeakPower:diff()} [gold]Weak[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
如果敌人的意图是攻击，则给予{WeakPower:diff()}层[gold]虚弱[/gold]。

### Gunk Up / 污秽攻击
- **ID**: `GUNK_UP` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 4 | Repeat: 3
- **EN**: Deal {Damage:diff()} damage {Repeat:diff()} {Repeat:plural:time|times}.
Add a [gold]Slimed[/gold] into your [gold]Discard Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害{Repeat:diff()}次。
在你的[gold]弃牌堆[/gold]中加入一张[gold]黏液[/gold]。

### Hologram / 全息影像
- **ID**: `HOLOGRAM` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Block: 3
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Put a card from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将[gold]弃牌堆[/gold]中的一张牌放入你的[gold]手牌[/gold]。

### Hotfix / 热修复
- **ID**: `HOTFIX` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: Self
- **Values**: FocusPower: 2
- **EN**: Gain {FocusPower:diff()} [gold]Focus[/gold] this turn.
- **CN**: 在本回合获得{FocusPower:diff()}点[gold]集中[/gold]。

### Leap / 飞跃
- **ID**: `LEAP` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 9
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Lightning Rod / 引雷针
- **ID**: `LIGHTNING_ROD` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 4 | LightningRodPower: 2
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
At the start of the next {LightningRodPower:plural:turn|{:diff()} turns}, [gold]Channel[/gold] 1 [gold]Lightning[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在下{LightningRodPower:diff()}个回合开始时,[gold]生成[/gold]1个[gold]闪电[/gold]充能球。

### Momentum Strike / 趁势打击
- **ID**: `MOMENTUM_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 10
- **EN**: Deal {Damage:diff()} damage.
Reduce this card's cost to 0 {energyPrefix:energyIcons(1)}.
- **CN**: 造成{Damage:diff()}点伤害。
这张牌的耗能降为0{energyPrefix:energyIcons(1)}。

### Sweeping Beam / 扫荡射线
- **ID**: `SWEEPING_BEAM` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 6 | Cards: 1
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
抽{Cards:diff()}张牌。

### TURBO / 内核加速
- **ID**: `TURBO` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: Self
- **Values**: Energy: 2
- **EN**: Gain {Energy:energyIcons()}.
Add a [gold]Void[/gold] into your [gold]Discard Pile[/gold].
- **CN**: 获得{Energy:energyIcons()}。
将一张[gold]虚空[/gold]加入你的[gold]弃牌堆[/gold]。

### Uproar / 骚动
- **ID**: `UPROAR` | **Type**: Attack | **Rarity**: Common | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 5
- **EN**: Deal {Damage:diff()} damage twice.
Play a random Attack from your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害两次。
随机打出你的[gold]抽牌堆[/gold]中的1张攻击牌。

## Rare

### Adaptive Strike / 适应打击
- **ID**: `ADAPTIVE_STRIKE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 18
- **EN**: Deal {Damage:diff()} damage.
Add a 0{energyPrefix:energyIcons(1)} copy of this card into your [gold]Discard Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将这张牌的一张0{energyPrefix:energyIcons(1)}复制品添加到你的[gold]弃牌堆[/gold]。

### All for One / 万物一心
- **ID**: `ALL_FOR_ONE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 10
- **EN**: Deal {Damage:diff()} damage.
Put ALL 0{energyPrefix:energyIcons(1)} cards from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将你[gold]弃牌堆[/gold]中的所有0{energyPrefix:energyIcons(1)}牌放入你的[gold]手牌[/gold]。

### Buffer / 缓冲
- **ID**: `BUFFER` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: BufferPower: 1
- **EN**: Prevent the next {BufferPower:plural:time|{BufferPower:diff()} times} you would lose HP.
- **CN**: 阻止下{BufferPower:diff()}次你受到的生命值损伤。

### Consuming Shadow / 吞噬暗影
- **ID**: `CONSUMING_SHADOW` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: Repeat: 2 | ConsumingShadowPower: 1
- **EN**: [gold]Channel[/gold] {Repeat:diff()} [gold]Dark[/gold].
At the end of your turn, [gold]Evoke[/gold] your leftmost Orb.
- **CN**: [gold]生成[/gold]{Repeat:diff()}个[gold]黑暗[/gold]充能球。
在你的回合结束时，[gold]激发[/gold]你最左侧的充能球。

### Coolant / 冷却剂
- **ID**: `COOLANT` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: CoolantPower: 2
- **EN**: At the start of your turn, gain {CoolantPower:diff()} [gold]Block[/gold] for each unique Orb you have.
- **CN**: 在你的回合开始时，你每有一种不同的充能球，就获得{CoolantPower:diff()}点[gold]格挡[/gold]。

### Creative AI / 创造性AI
- **ID**: `CREATIVE_AI` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Values**: CreativeAi: 1
- **EN**: At the start of your turn, add a random Power into your [gold]Hand[/gold].
- **CN**: 在你的回合开始时，将一张随机能力牌加入你的[gold]手牌[/gold]。

### Defragment / 碎片整理
- **ID**: `DEFRAGMENT` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: FocusPower: 1
- **EN**: Gain {FocusPower:diff()} [gold]Focus[/gold].
- **CN**: 获得{FocusPower:diff()}点[gold]集中[/gold]。

### Echo Form / 回响形态
- **ID**: `ECHO_FORM` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Keywords**: Ethereal
- **Values**: EchoForm: 1
- **EN**: The first card you play each turn is played an extra time.
- **CN**: 你每回合打出的第一张牌会被打出两次。

### Flak Cannon / 散射炮
- **ID**: `FLAK_CANNON` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: RandomEnemy
- **Values**: Damage: 8 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: [gold]Exhaust[/gold] ALL your [gold]Status[/gold] cards.
Deal {Damage:diff()} damage to a random enemy for each card [gold]Exhausted[/gold].{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: [gold]消耗[/gold]你所有的[gold]状态[/gold]牌。
每有一张被[gold]消耗[/gold]的牌，就随机对敌人造成{Damage:diff()}点伤害。{InCombat:
（攻击{CalculatedHits:diff()}次）|}

### Genetic Algorithm / 遗传算法
- **ID**: `GENETIC_ALGORITHM` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Block: 1 | Increase: 3
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Permanently increase this card's [gold]Block[/gold] by {Increase:diff()}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
每打出一次，这张牌在本局游戏中的[gold]格挡[/gold]值永久增加{Increase:diff()}点。

### Helix Drill / 螺旋钻击
- **ID**: `HELIX_DRILL` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 3 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: Deal {Damage:diff()} damage for each {energyPrefix:energyIcons(1)} previously spent this turn.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: 在本回合中，每个被使用的{energyPrefix:energyIcons(1)}，都会使此牌造成{Damage:diff()}点伤害一次。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Hyperbeam / 超能光束
- **ID**: `HYPERBEAM` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AllEnemies
- **Values**: Damage: 26 | FocusPower: 3
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
Lose {FocusPower:diff()} [gold]Focus[/gold].
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
失去{FocusPower:diff()}点[gold]集中[/gold]。

### Ice Lance / 冰之长枪
- **ID**: `ICE_LANCE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 19 | Repeat: 3
- **EN**: Deal {Damage:diff()} damage.
[gold]Channel[/gold] {Repeat:diff()} [gold]Frost[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
[gold]生成[/gold]{Repeat:diff()}个[gold]冰霜[/gold]充能球。

### Ignition / 引火
- **ID**: `IGNITION` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyAlly
- **Keywords**: Exhaust
- **EN**: Another player [gold]Channels[/gold] [gold]Plasma[/gold].
- **CN**: 使另一名玩家[gold]生成[/gold][gold]等离子[/gold]充能球。

### Machine Learning / 机器学习
- **ID**: `MACHINE_LEARNING` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: At the start of your turn, draw {Cards:diff()} additional {Cards:plural:card|cards}.
- **CN**: 在你的回合开始时，额外抽{Cards:diff()}张牌。

### Meteor Strike / 陨石打击
- **ID**: `METEOR_STRIKE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 5 | **Target**: AnyEnemy
- **Values**: Damage: 24
- **EN**: Deal {Damage:diff()} damage.
[gold]Channel[/gold] 3 [gold]Plasma[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
[gold]生成[/gold]3个[gold]等离子[/gold]充能球。

### Modded / 模组改造
- **ID**: `MODDED` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Values**: Repeat: 1 | Cards: 1
- **EN**: Gain {Repeat:diff()} Orb {Repeat:plural:Slot|Slots}.
Draw {Cards:diff()} {Cards:plural:card|cards}. Increase this card's cost by 1.
- **CN**: 获得{Repeat:diff()}个充能球栏位。
抽{Cards:diff()}张牌。这张牌的耗能加1。

### Multi-Cast / 多重释放
- **ID**: `MULTI_CAST` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **EN**: [gold]Evoke[/gold] your rightmost Orb {IfUpgraded:show:X+1|X} times.
- **CN**: [gold]激发[/gold]你最右侧的充能球{IfUpgraded:show:X+1|X}次。

### Rainbow / 彩虹
- **ID**: `RAINBOW` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: [gold]Channel[/gold] 1 [gold]Lightning[/gold].
[gold]Channel[/gold] 1 [gold]Frost[/gold].
[gold]Channel[/gold] 1 [gold]Dark[/gold].
- **CN**: [gold]生成[/gold]1个[gold]闪电[/gold]充能球。
[gold]生成[/gold]1个[gold]冰霜[/gold]充能球。
[gold]生成[/gold]1个[gold]黑暗[/gold]充能球。

### Reboot / 重启
- **ID**: `REBOOT` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 4
- **EN**: Shuffle ALL your cards into your [gold]Draw Pile[/gold].
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 将你的所有未消耗的卡牌重新洗牌放入[gold]抽牌堆[/gold]。
抽{Cards:diff()}张牌。

### Shatter / 打碎
- **ID**: `SHATTER` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 11
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
[gold]Evoke[/gold] all of your Orbs.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
[gold]激发[/gold]所有充能球。

### Signal Boost / 信号增强
- **ID**: `SIGNAL_BOOST` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: SignalBoostPower: 1
- **EN**: The next Power you play is played an additional time.
- **CN**: 你的下一张能力牌会额外打出一次。

### Spinner / 旋转工艺
- **ID**: `SPINNER` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: SpinnerPower: 1
- **EN**: {IfUpgraded:show:[gold]Channel[/gold] 1 [gold]Glass[/gold].
|}At the start of your turn, [gold]Channel[/gold] 1 [gold]Glass[/gold].
- **CN**: {IfUpgraded:show:[gold]生成[/gold]1个[gold]玻璃[/gold]充能球。
|}在你的回合开始时，[gold]生成[/gold]1个[gold]玻璃[/gold]充能球。

### Supercritical / 超临界态
- **ID**: `SUPERCRITICAL` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Energy: 4
- **EN**: Gain {Energy:energyIcons()}.
- **CN**: 获得{Energy:energyIcons()}。

### Trash to Treasure / 化废为宝
- **ID**: `TRASH_TO_TREASURE` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **EN**: Whenever you create a Status card, [gold]Channel[/gold] 1 random Orb.
- **CN**: 每当你生成状态牌的时候，随机[gold]生成[/gold]一个充能球。

### Voltaic / 电流相生
- **ID**: `VOLTAIC` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: CalculationBase: 0 | CalculationExtra: 1 | CalculatedChannels: 0
- **EN**: [gold]Channel Lightning[/gold] equal to the [gold]Lightning[/gold] already [gold]Channeled[/gold] this combat.{InCombat:
([gold]Channel[/gold] {CalculatedChannels:diff()} [gold]Lightning[/gold])|}
- **CN**: [gold]生成[/gold]等量于你在这场战斗中[gold]生成过[/gold]的[gold]闪电[/gold]充能球数量的[gold]闪电[/gold]充能球。{InCombat:
（[gold]生成[/gold]{CalculatedChannels:diff()}个[gold]闪电[/gold]充能球）|}

## Uncommon

### Boot Sequence / 启动流程
- **ID**: `BOOT_SEQUENCE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Innate, Exhaust
- **Values**: Block: 10
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Bulk Up / 暴涨
- **ID**: `BULK_UP` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: OrbSlots: 1 | StrengthPower: 2 | DexterityPower: 2
- **EN**: Lose {OrbSlots:diff()} Orb {OrbSlots:plural:Slot|Slots}.
Gain {StrengthPower:diff()} [gold]Strength[/gold].
Gain {DexterityPower:diff()} [gold]Dexterity[/gold].
- **CN**: 失去{OrbSlots:diff()}个充能球栏位。
获得{StrengthPower:diff()}点[gold]力量[/gold]。
获得{DexterityPower:diff()}点[gold]敏捷[/gold]。

### Capacitor / 扩容
- **ID**: `CAPACITOR` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Repeat: 2
- **EN**: Gain {Repeat:diff()} Orb Slots.
- **CN**: 获得{Repeat:diff()}个充能球栏位。

### Chaos / 混沌
- **ID**: `CHAOS` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Repeat: 1
- **EN**: [gold]Channel[/gold] {Repeat:diff()} random {Repeat:plural:Orb|Orbs}.
- **CN**: [gold]生成[/gold]{Repeat:diff()}个随机充能球。

### Chill / 冰寒
- **ID**: `CHILL` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: [gold]Channel[/gold] 1 [gold]Frost[/gold] for each enemy.
- **CN**: 当前每有一名敌人，就[gold]生成[/gold]1个[gold]冰霜[/gold]充能球。

### Compact / 压缩
- **ID**: `COMPACT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 6
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Transform[/gold] all Status cards in your [gold]Hand[/gold] into [gold]{IfUpgraded:show:Fuel+|Fuel}[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将你[gold]手牌[/gold]中的全部状态牌[gold]变化[/gold]为[gold]{IfUpgraded:show:燃料+|燃料}[/gold]。

### Darkness / 漆黑
- **ID**: `DARKNESS` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **EN**: [gold]Channel[/gold] 1 [gold]Dark[/gold].
Trigger the passive ability of all [gold]Dark[/gold] Orbs{IfUpgraded:show: twice|}.
- **CN**: [gold]生成[/gold]1[gold]黑暗[/gold]充能球。
触发所有[gold]黑暗[/gold]充能球的被动{IfUpgraded:show:两次|}。

### Double Energy / 双倍能量
- **ID**: `DOUBLE_ENERGY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Double your Energy.
- **CN**: 将你的能量翻倍。

### Energy Surge / 能量涌动
- **ID**: `ENERGY_SURGE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AllAllies
- **Keywords**: Exhaust
- **Values**: Energy: 2
- **EN**: ALL players gain {Energy:energyIcons()}.
- **CN**: 所有玩家获得{Energy:energyIcons()}。

### Feral / 野性
- **ID**: `FERAL` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: FeralPower: 1
- **EN**: The first {FeralPower:plural:time|{FeralPower:diff()} times} you play a
0{energyPrefix:energyIcons(1)} Attack each turn,
return it to your [gold]Hand[/gold].
- **CN**: 你每回合打出的{FeralPower:choose(1):第一张|前{FeralPower:diff()}张}
耗能为0{energyPrefix:energyIcons(1)}的攻击牌，
会放回你的[gold]手牌[/gold]。

### Fight Through / 强撑
- **ID**: `FIGHT_THROUGH` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 13
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Add 2 [gold]Wounds[/gold] into your [gold]Discard Pile[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将2张[gold]伤口[/gold]加入你的[gold]弃牌堆[/gold]。

### FTL / 超越光速
- **ID**: `FTL` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 5 | PlayMax: 3 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
If you have played fewer than {PlayMax:diff()} cards this turn, draw 1 card.
- **CN**: 造成{Damage:diff()}点伤害。
如果你在这回合打出的牌数小于{PlayMax:diff()}张，抽1张牌。

### Fusion / 聚变
- **ID**: `FUSION` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **EN**: [gold]Channel[/gold] 1 [gold]Plasma[/gold].
- **CN**: [gold]生成[/gold]1个[gold]等离子[/gold]充能球。

### Glacier / 冰川
- **ID**: `GLACIER` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Block: 6
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Channel[/gold] 2 [gold]Frost[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
[gold]生成[/gold]2个[gold]冰霜[/gold]充能球。

### Glasswork / 玻璃工艺
- **ID**: `GLASSWORK` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Channel[/gold] 1 [gold]Glass[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
[gold]生成[/gold]1个[gold]玻璃[/gold]充能球。

### Hailstorm / 冰雹风暴
- **ID**: `HAILSTORM` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: HailstormPower: 6
- **EN**: At the end of your turn, if you have [gold]Frost[/gold], deal {HailstormPower:diff()} damage to ALL enemies.
- **CN**: 在你的回合结束时，如果你有[gold]冰霜[/gold]充能球，则对所有敌人造成{HailstormPower:diff()}点伤害。

### Iteration / 迭代
- **ID**: `ITERATION` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: IterationPower: 2
- **EN**: The first time you draw a Status card each turn, draw {IterationPower:diff()} {IterationPower:plural:card|cards}.
- **CN**: 每回合你第一次抽到状态牌时，抽{IterationPower:diff()}张牌。

### Loop / 循环
- **ID**: `LOOP` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Loop: 1
- **EN**: At the start of your turn, trigger the passive ability of your rightmost Orb{IfUpgraded:show: 2 times}.
- **CN**: 在你的回合开始时，触发你最右侧的一个充能球的被动能力{IfUpgraded:show:2次}。

### Null / 空无
- **ID**: `NULL` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 10 | WeakPower: 2
- **EN**: Deal {Damage:diff()} damage.
Apply {WeakPower:diff()} [gold]Weak[/gold].
[gold]Channel[/gold] 1 [gold]Dark[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{WeakPower:diff()}层[gold]虚弱[/gold]。
[gold]生成[/gold]1个[gold]黑暗[/gold]充能球。

### Overclock / 超频
- **ID**: `OVERCLOCK` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Cards: 2
- **EN**: Draw {Cards:diff()} cards.
Add a [gold]Burn[/gold] into your [gold]Discard Pile[/gold].
- **CN**: 抽{Cards:diff()}张牌。
将一张[gold]灼伤[/gold]加入你的[gold]弃牌堆[/gold]。

### Refract / 折射
- **ID**: `REFRACT` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Repeat: 2 | Damage: 9
- **EN**: Deal {Damage:diff()} damage twice.
[gold]Channel[/gold] {Repeat:diff()} [gold]Glass[/gold].
- **CN**: 造成{Damage:diff()}点伤害两次。
[gold]生成[/gold]{Repeat:diff()}个[gold]玻璃[/gold]充能球。

### Rocket Punch / 火箭飞拳
- **ID**: `ROCKET_PUNCH` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 13 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
Draw {Cards:diff()} {Cards:plural:card|cards}.
When a Status card is created, reduce this card's cost to 0 {energyPrefix:energyIcons(1)} this turn.
- **CN**: 造成{Damage:diff()}点伤害。
抽{Cards:diff()}张牌。
当有一张状态被生成时，将此牌的耗能在本回合降为0{energyPrefix:energyIcons(1)}。

### Scavenge / 再利用
- **ID**: `SCAVENGE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Energy: 2
- **EN**: [gold]Exhaust[/gold] a card.
Next turn, gain {Energy:energyIcons()}.
- **CN**: [gold]消耗[/gold]一张牌。
在下个回合获得{Energy:energyIcons()}。

### Scrape / 刮削
- **ID**: `SCRAPE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7 | Cards: 4
- **EN**: Deal {Damage:diff()} damage.
Draw {Cards:diff()} cards.
Discard all cards drawn this way that do not cost 0 {energyPrefix:energyIcons(1)}.
- **CN**: 造成{Damage:diff()}点伤害。
抽{Cards:diff()}张牌。
丢弃抽到的牌中耗能不为0{energyPrefix:energyIcons(1)}的牌。

### Shadow Shield / 暗影之盾
- **ID**: `SHADOW_SHIELD` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Block: 11
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Channel[/gold] 1 [gold]Dark[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
[gold]生成[/gold]1个[gold]黑暗[/gold]充能球。

### Skim / 快速检索
- **ID**: `SKIM` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 3
- **EN**: Draw {Cards:diff()} cards.
- **CN**: 抽{Cards:diff()}张牌。

### Smokestack / 烟囱
- **ID**: `SMOKESTACK` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: SmokestackPower: 5
- **EN**: Whenever you create a Status, deal {SmokestackPower:diff()} damage to ALL enemies.
- **CN**: 每当你生成一张状态牌时，对所有敌人造成{SmokestackPower:diff()}点伤害。

### Storm / 雷暴
- **ID**: `STORM` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: StormPower: 1
- **EN**: Whenever you play a Power, [gold]Channel[/gold] {StormPower:diff()} [gold]Lightning[/gold].
- **CN**: 每当你打出一张能力牌时，[gold]生成[/gold]{StormPower:diff()}个[gold]闪电[/gold]充能球。

### Subroutine / 子程序
- **ID**: `SUBROUTINE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **EN**: Whenever you play a Power, gain {energyPrefix:energyIcons(1)}.
- **CN**: 当你打出一张能力牌时，获得{energyPrefix:energyIcons(1)}。

### Sunder / 分离
- **ID**: `SUNDER` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 24 | Energy: 3
- **EN**: Deal {Damage:diff()} damage.
If this kills an enemy, gain {Energy:energyIcons()}.
- **CN**: 造成{Damage:diff()}点伤害。
如果这张牌击杀了敌人，则获得{Energy:energyIcons()}。

### Synchronize / 同步
- **ID**: `SYNCHRONIZE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: CalculationBase: 0 | CalculationExtra: 2 | CalculatedFocus: 0
- **EN**: Gain {CalculationExtra:diff()} [gold]Focus[/gold] this turn for each unique Orb you have.{InCombat:
(Gain {CalculatedFocus:diff()} [gold]Focus[/gold])|}
- **CN**: 你每有一种不同的充能球，就在本回合获得{CalculationExtra:diff()}点[gold]集中[/gold]。{InCombat:
（获得{CalculatedFocus:diff()}点[gold]集中[/gold]）|}

### Synthesis / 人工合成
- **ID**: `SYNTHESIS` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 12
- **EN**: Deal {Damage:diff()} damage.
The next Power you play costs 0 {energyPrefix:energyIcons(1)}.
- **CN**: 造成{Damage:diff()}点伤害。
你打出的下一张能力牌耗能变为0{energyPrefix:energyIcons(1)}。

### Tempest / 暴风雨
- **ID**: `TEMPEST` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **EN**: [gold]Channel[/gold] {IfUpgraded:show:X+1|X} [gold]Lightning[/gold].
- **CN**: [gold]生成[/gold]{IfUpgraded:show:X+1|X}个[gold]闪电[/gold]充能球。

### Tesla Coil / 特斯拉线圈
- **ID**: `TESLA_COIL` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 3
- **EN**: Deal {Damage:diff()} damage.
Trigger all [gold]Lightning[/gold] against the enemy.
- **CN**: 造成{Damage:diff()}点伤害。
对该敌人触发你的所有[gold]闪电[/gold]充能球的被动。

### Thunder / 雷霆
- **ID**: `THUNDER` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: ThunderPower: 6
- **EN**: Whenever you [gold]Evoke Lightning[/gold], deal {ThunderPower:diff()} damage to each enemy hit.
- **CN**: 每当你[gold]激发闪电[/gold]充能球时，对被命中的敌人造成{ThunderPower:diff()}点伤害。

### White Noise / 白噪声
- **ID**: `WHITE_NOISE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Add a random Power into your [gold]Hand[/gold]. It's free to play this turn.
- **CN**: 将一张随机能力牌加入你的[gold]手牌[/gold]。这张牌在本回合内免费打出。

