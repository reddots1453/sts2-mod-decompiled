# NECROBINDER Cards

## Ancient

### Forbidden Grimoire / 禁忌魔典
- **ID**: `FORBIDDEN_GRIMOIRE` | **Type**: Power | **Rarity**: Ancient | **Cost**: 2 | **Target**: Self
- **Keywords**: Eternal
- **EN**: At the end of combat, you may remove a card from your [gold]Deck[/gold].
- **CN**: 在战斗结束时，你可以从你的[gold]牌组[/gold]中选一张牌移除。

### Protector / 守护者
- **ID**: `PROTECTOR` | **Type**: Attack | **Rarity**: Ancient | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 10 | ExtraDamage: 1 | CalculatedDamage: 10
- **EN**: [gold]Osty[/gold] deals {CalculatedDamage:diff()} damage.
Deals additional damage equal to [gold]Osty's[/gold] [gold]Max HP[/gold].
- **CN**: [gold]奥斯提[/gold]造成{CalculatedDamage:diff()}点伤害。
额外造成等量于[gold]奥斯提[/gold][gold]最大生命值[/gold]的伤害。

## Basic

### Bodyguard / 护卫
- **ID**: `BODYGUARD` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **Values**: Summon: 5
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
- **CN**: [gold]召唤[/gold]{Summon:diff()}。

### Defend / 防御
- **ID**: `DEFEND_NECROBINDER` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Strike / 打击
- **ID**: `STRIKE_NECROBINDER` | **Type**: Attack | **Rarity**: Basic | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Unleash / 出击
- **ID**: `UNLEASH` | **Type**: Attack | **Rarity**: Basic | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 6 | ExtraDamage: 1 | CalculatedDamage: 6
- **EN**: [gold]Osty[/gold] deals {CalculatedDamage:diff()} damage.
Deals additional damage equal to [gold]Osty's[/gold] current HP.
- **CN**: [gold]奥斯提[/gold]造成{CalculatedDamage:diff()}点伤害。
这张牌额外造成等量于[gold]奥斯提[/gold]当前生命值的伤害。

## Common

### Afterlife / 来生
- **ID**: `AFTERLIFE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Summon: 6
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
- **CN**: [gold]召唤[/gold]{Summon:diff()}。

### Blight Strike / 荒疫打击
- **ID**: `BLIGHT_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 8
- **EN**: Deal {Damage:diff()} damage.
Apply [gold]Doom[/gold] equal to damage dealt.
- **CN**: 造成{Damage:diff()}点伤害。
给予等量于所造成伤害的[gold]灾厄[/gold]。

### Defile / 玷污
- **ID**: `DEFILE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Ethereal
- **Values**: Damage: 13
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Defy / 违逆
- **ID**: `DEFY` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Ethereal
- **Values**: Block: 6 | WeakPower: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Apply {WeakPower:diff()} [gold]Weak[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
给予{WeakPower:diff()}层[gold]虚弱[/gold]。

### Drain Power / 能量汲取
- **ID**: `DRAIN_POWER` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 10 | Cards: 2
- **EN**: Deal {Damage:diff()} damage.
[gold]Upgrade[/gold] {Cards:diff()} random {Cards:plural:card|cards} in your [gold]Discard Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
随机[gold]升级[/gold]你[gold]弃牌堆[/gold]中的{Cards:diff()}张牌。

### Fear / 恐惧
- **ID**: `FEAR` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Ethereal
- **Values**: Damage: 7 | VulnerablePower: 1
- **EN**: Deal {Damage:diff()} damage.
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Flatten / 重压
- **ID**: `FLATTEN` | **Type**: Attack | **Rarity**: Common | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: OstyDamage: 12
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
This card costs 0 {energyPrefix:energyIcons(1)} if [gold]Osty[/gold] has attacked this turn.
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。
如果[gold]奥斯提[/gold]本回合攻击过，则这张牌的费用变为0{energyPrefix:energyIcons(1)}。

### Grave Warden / 守墓人
- **ID**: `GRAVE_WARDEN` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 8 | Cards: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Add a [gold]{IfUpgraded:show:Soul+|Soul}[/gold] into your [gold]Draw Pile[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将一张[gold]{IfUpgraded:show:灵魂+|灵魂}[/gold]放入你的[gold]抽牌堆[/gold]中。

### Graveblast / 坟冢爆射
- **ID**: `GRAVEBLAST` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 4
- **EN**: Deal {Damage:diff()} damage.
Put a card from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将[gold]弃牌堆[/gold]中的一张牌放入你的[gold]手牌[/gold]。

### Invoke / 唤起
- **ID**: `INVOKE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Summon: 2 | Energy: 2
- **EN**: Next turn, [gold]Summon[/gold] {Summon:diff()} and gain {Energy:energyIcons()}.
- **CN**: 在下个回合[gold]召唤[/gold]{Summon:diff()}并获得{Energy:energyIcons()}。

### Negative Pulse / 负能量脉冲
- **ID**: `NEGATIVE_PULSE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Block: 5 | DoomPower: 7
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Apply {DoomPower:diff()} [gold]Doom[/gold] to ALL enemies.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
给予所有敌人{DoomPower:diff()}层[gold]灾厄[/gold]。

### Poke / 戳击
- **ID**: `POKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: OstyDamage: 6
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。

### Pull Aggro / 吸引仇恨
- **ID**: `PULL_AGGRO` | **Type**: Skill | **Rarity**: Common | **Cost**: 2 | **Target**: Self
- **Values**: Summon: 4 | Block: 7
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
Gain {Block:diff()} [gold]Block[/gold].
- **CN**: [gold]召唤[/gold]{Summon:diff()}。
获得{Block:diff()}点[gold]格挡[/gold]。

### Reap / 收割
- **ID**: `REAP` | **Type**: Attack | **Rarity**: Common | **Cost**: 3 | **Target**: AnyEnemy
- **Keywords**: Retain
- **Values**: Damage: 27
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Reave / 剥夺
- **ID**: `REAVE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 9 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
Add a [gold]{IfUpgraded:show:Soul+|Soul}[/gold] into your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将一张[gold]{IfUpgraded:show:灵魂+|灵魂}[/gold]加入你的[gold]抽牌堆[/gold]。

### Scourge / 鞭打
- **ID**: `SCOURGE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: DoomPower: 13 | Cards: 1
- **EN**: Apply {DoomPower:diff()} [gold]Doom[/gold].
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 给予{DoomPower:diff()}层[gold]灾厄[/gold]。
抽{Cards:diff()}张牌。

### Sculpting Strike / 雕琢打击
- **ID**: `SCULPTING_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 8
- **EN**: Deal {Damage:diff()} damage.
Add [gold]Ethereal[/gold] to a card in your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
为一张[gold]手牌[/gold]添加[gold]虚无[/gold]。

### Snap / 响指
- **ID**: `SNAP` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: OstyDamage: 7
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
Add [gold]Retain[/gold] to a card in your [gold]Hand[/gold].
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。
给[gold]手牌[/gold]中的一张牌添加[gold]保留[/gold]。

### Sow / 播种
- **ID**: `SOW` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AllEnemies
- **Keywords**: Retain
- **Values**: Damage: 8
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。

### Wisp / 鬼火
- **ID**: `WISP` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Energy: 1
- **EN**: Gain {Energy:energyIcons()}.
- **CN**: 获得{Energy:energyIcons()}。

## Rare

### Banshee's Cry / 女妖之嚎
- **ID**: `BANSHEES_CRY` | **Type**: Attack | **Rarity**: Rare | **Cost**: 6 | **Target**: AllEnemies
- **Values**: Damage: 33 | Energy: 2
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
Costs {Energy:energyIcons()} less for each [gold]Ethereal[/gold] card played this combat.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
本场战斗中每打出过一张[gold]虚无[/gold]牌，此牌费用就减少{Energy:energyIcons()}。

### Call of the Void / 虚空之唤
- **ID**: `CALL_OF_THE_VOID` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: At the start of your turn, add {Cards:diff()} random {Cards:plural:card|cards} into your [gold]Hand[/gold]. {Cards:plural:It gains|They gain} [gold]Ethereal[/gold].
- **CN**: 在你的回合开始时，将{Cards:diff()}张随机牌添加到你的[gold]手牌[/gold]中。添加的牌会获得[gold]虚无[/gold]。

### Demesne / 领域
- **ID**: `DEMESNE` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Keywords**: Ethereal
- **Values**: Energy: 1 | Cards: 1
- **EN**: At the start of your turn, gain {Energy:energyIcons()} and draw {Cards:diff()} additional {Cards:plural:card|cards}.
- **CN**: 在你的回合开始时，获得{Energy:energyIcons()}并额外多抽{Cards:diff()}张牌。

### Devour Life / 吞噬生命
- **ID**: `DEVOUR_LIFE` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: DevourLifePower: 1
- **EN**: Whenever you play a [gold]Soul[/gold], [gold]Summon[/gold] {DevourLifePower:diff()}.
- **CN**: 每当你打出一张[gold]灵魂[/gold]时，[gold]召唤[/gold]{DevourLifePower:diff()}。

### Eidolon / 幻景
- **ID**: `EIDOLON` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **EN**: [gold]Exhaust[/gold] your [gold]Hand[/gold].
If 9 cards were [gold]Exhausted[/gold] this way, gain 1 [gold]Intangible[/gold].
- **CN**: [gold]消耗[/gold]所有[gold]手牌[/gold]。
如果有至少9张牌被通过这个方法[gold]消耗[/gold]，则获得1层[gold]无实体[/gold]。

### End of Days / 末日降临
- **ID**: `END_OF_DAYS` | **Type**: Skill | **Rarity**: Rare | **Cost**: 3 | **Target**: AllEnemies
- **Values**: DoomPower: 29
- **EN**: Apply {DoomPower:diff()} [gold]Doom[/gold] to ALL enemies.
Kill enemies with at least as much [gold]Doom[/gold] as HP.
- **CN**: 给予所有敌人{DoomPower:diff()}层[gold]灾厄[/gold]。
杀死所有[gold]灾厄[/gold]大于等于当前生命值的敌人。

### Eradicate / 根除
- **ID**: `ERADICATE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Keywords**: Retain
- **Values**: Damage: 11
- **EN**: Deal {Damage:diff()} damage X times.
- **CN**: 造成{Damage:diff()}点伤害X次。

### Glimpse Beyond / 彼岸一瞥
- **ID**: `GLIMPSE_BEYOND` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: AllAllies
- **Keywords**: Exhaust
- **Values**: Cards: 3
- **EN**: ALL players add {Cards:diff()} [gold]{Cards:plural:Soul|Souls}[/gold] into their [gold]Draw Pile[/gold].
- **CN**: 在所有玩家的[gold]抽牌堆[/gold]中加入{Cards:diff()}张[gold]灵魂[/gold]。

### Hang / 吊杀
- **ID**: `HANG` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 10
- **EN**: Deal {Damage:diff()} damage.
Double the damage ALL Hang cards deal to this enemy.
- **CN**: 造成{Damage:diff()}点伤害。
让所有“吊杀”牌对这名敌人造成的伤害翻倍。

### Misery / 苦难
- **ID**: `MISERY` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 7
- **EN**: Deal {Damage:diff()} damage.
Apply any debuffs on the enemy to ALL other enemies.
- **CN**: 造成{Damage:diff()}点伤害。
给予其他敌人该名敌人身上的所有负面效果。

### Necro Mastery / 亡灵精通
- **ID**: `NECRO_MASTERY` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: Summon: 5
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
Whenever [gold]Osty[/gold] loses HP,
ALL enemies lose that much HP as well.
- **CN**: [gold]召唤[/gold]{Summon:diff()}。
每当[gold]奥斯提[/gold]失去生命值时，
所有敌人失去等量生命值。

### Neurosurge / 精神过载
- **ID**: `NEUROSURGE` | **Type**: Power | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Values**: NeurosurgePower: 3 | Energy: 3 | Cards: 2
- **EN**: Gain {Energy:energyIcons()}.
Draw {Cards:diff()} {Cards:plural:card|cards}.
At the start of your turn, apply {NeurosurgePower:diff()} [gold]Doom[/gold] to yourself.
- **CN**: 获得{Energy:energyIcons()}。
抽{Cards:diff()}张牌。
在你的回合开始时，给予自身{NeurosurgePower:diff()}层[gold]灾厄[/gold]。

### Oblivion / 湮灭
- **ID**: `OBLIVION` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: DoomPower: 3
- **EN**: Whenever you play a card this turn, apply {DoomPower:diff()} [gold]Doom[/gold] to the enemy.
- **CN**: 你在本回合内每打出一张牌，就给予该敌人{DoomPower:diff()}层[gold]灾厄[/gold]。

### Reanimate / 死者苏生
- **ID**: `REANIMATE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Summon: 20
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
- **CN**: [gold]召唤[/gold]{Summon:diff()}。

### Reaper Form / 死神形态
- **ID**: `REAPER_FORM` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **EN**: Whenever Attacks deal damage, they also apply that much [gold]Doom[/gold].
- **CN**: 每当你的攻击造成伤害时，同时给予等量的[gold]灾厄[/gold]。

### Sacrifice / 牺牲
- **ID**: `SACRIFICE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Retain
- **EN**: If [gold]Osty[/gold] is alive, he dies and you gain [gold]Block[/gold] equal to double his Max HP.
- **CN**: 如果[gold]奥斯提[/gold]存活，则他死去，然后你获得等量于其双倍最大生命值的[gold]格挡[/gold]。

### Seance / 降灵
- **ID**: `SEANCE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Ethereal
- **Values**: Cards: 1
- **EN**: Transform a card in your [gold]Draw Pile[/gold] into [gold]{IfUpgraded:show:Soul+|Soul}[/gold].
- **CN**: 将你[gold]抽牌堆[/gold]中的一张牌变化为[gold]{IfUpgraded:show:灵魂+|灵魂}[/gold]。

### Sentry Mode / 哨卫模式
- **ID**: `SENTRY_MODE` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: SentryModePower: 1
- **EN**: At the start of your turn, add {SentryModePower:diff()} [gold]Sweeping Gaze[/gold] into your [gold]Hand[/gold].
- **CN**: 在你的回合开始时，将{SentryModePower:diff()}张[gold]扫荡凝视[/gold]加入你的[gold]手牌[/gold]。

### Shared Fate / 命运同担
- **ID**: `SHARED_FATE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: EnemyStrengthLoss: 2 | PlayerStrengthLoss: 2
- **EN**: Lose {PlayerStrengthLoss:diff()} [gold]Strength[/gold].
Enemy loses {EnemyStrengthLoss:diff()} [gold]Strength[/gold].
- **CN**: 失去{PlayerStrengthLoss:diff()}点[gold]力量[/gold]。
敌人失去{EnemyStrengthLoss:diff()}点[gold]力量[/gold]。

### Soul Storm / 灵魂风暴
- **ID**: `SOUL_STORM` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 9 | ExtraDamage: 2 | CalculatedDamage: 9
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each [gold]Soul[/gold] in your [gold]Exhaust Pile[/gold].
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
你的[gold]消耗牌堆[/gold]中每有一张[gold]灵魂[/gold]，伤害增加{ExtraDamage:diff()}。

### Spirit of Ash / 灰烬之灵
- **ID**: `SPIRIT_OF_ASH` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: BlockOnExhaust: 4
- **EN**: Whenever you play an [gold]Ethereal[/gold] card, gain {BlockOnExhaust:diff()} [gold]Block[/gold].
- **CN**: 每当你打出一张[gold]虚无[/gold]牌时，获得{BlockOnExhaust:diff()}点[gold]格挡[/gold]。

### Squeeze / 榨取
- **ID**: `SQUEEZE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: CalculationBase: 25 | ExtraDamage: 5 | CalculatedDamage: 25
- **EN**: [gold]Osty[/gold] deals {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for ALL your other [gold]Osty[/gold] Attacks.
- **CN**: [gold]奥斯提[/gold]造成{CalculatedDamage:diff()}点伤害。
你每有一张[gold]奥斯提[/gold]的攻击牌，这张牌就额外造成{ExtraDamage:diff()}点伤害。

### The Scythe / 巨镰
- **ID**: `THE_SCYTHE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 13 | Increase: 3
- **EN**: Deal {Damage:diff()} damage.
Permanently increase this card's damage by {Increase:diff()}.
- **CN**: 造成{Damage:diff()}点伤害。
这张牌在本局游戏中的伤害永久性增加{Increase:diff()}。

### Time's Up / 大限已至
- **ID**: `TIMES_UP` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: CalculationBase: 0 | ExtraDamage: 1 | CalculatedDamage: 0
- **EN**: Deal damage equal to the enemy's [gold]Doom[/gold].{IsTargeting:
(Deals {CalculatedDamage:diff()} damage)|}
- **CN**: 造成等量于该敌人身上的[gold]灾厄[/gold]层数的伤害。{IsTargeting:
（造成{CalculatedDamage:diff()}点伤害）|}

### Transfigure / 重构
- **ID**: `TRANSFIGURE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Energy: 1
- **EN**: Add [gold]Replay[/gold] to a card in your [gold]Hand[/gold].
It costs an extra {Energy:energyIcons()}.
- **CN**: 给一张[gold]手牌[/gold]添加[gold]重放[/gold]。
其费用增加1{Energy:energyIcons()}。

### Undeath / 不死
- **ID**: `UNDEATH` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Values**: Block: 7
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Add a copy of this card into your [gold]Discard Pile[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在[gold]弃牌堆[/gold]放入一张此牌的复制品。

## Uncommon

### Bone Shards / 碎骨
- **ID**: `BONE_SHARDS` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AllEnemies
- **Values**: OstyDamage: 9 | Block: 9
- **EN**: If [gold]Osty[/gold] is alive, he deals {OstyDamage:diff()} damage to ALL enemies and you gain {Block:diff()} [gold]Block[/gold].
[gold]Osty[/gold] dies.
- **CN**: 如果[gold]奥斯提[/gold]存活，则他对所有敌人造成{OstyDamage:diff()}点伤害并且你获得{Block:diff()}点[gold]格挡[/gold]。
然后[gold]奥斯提[/gold]死去。

### Borrowed Time / 预借时间
- **ID**: `BORROWED_TIME` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: DoomPower: 3 | Energy: 1
- **EN**: Apply {DoomPower:diff()} [gold]Doom[/gold] to yourself.
Gain {Energy:energyIcons()}.
- **CN**: 给予自身{DoomPower:diff()}层[gold]灾厄[/gold]。
获得{Energy:energyIcons()}。

### Bury / 埋葬
- **ID**: `BURY` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 4 | **Target**: AnyEnemy
- **Values**: Damage: 52
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Calcify / 钙化
- **ID**: `CALCIFY` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: CalcifyPower: 4
- **EN**: [gold]Osty's[/gold] attacks deal {CalcifyPower:diff()} additional damage.
- **CN**: [gold]奥斯提[/gold]的攻击额外造成{CalcifyPower:diff()}点伤害。

### Capture Spirit / 捕捉灵魂
- **ID**: `CAPTURE_SPIRIT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 3 | Cards: 3
- **EN**: Enemy loses {Damage:diff()} HP.
Add {Cards:diff()} [gold]{Cards:plural:Soul|Souls}[/gold] into your [gold]Draw Pile[/gold].
- **CN**: 敌人失去{Damage:diff()}点生命。
将{Cards:diff()}张[gold]灵魂[/gold]加入你的[gold]抽牌堆[/gold]。

### Cleanse / 洁净
- **ID**: `CLEANSE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Summon: 3
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
[gold]Exhaust[/gold] 1 card from your [gold]Draw Pile[/gold].
- **CN**: [gold]召唤[/gold]{Summon:diff()}。
从你的[gold]抽牌堆[/gold]中选择一张牌将其[gold]消耗[/gold]。

### Countdown / 倒数计时
- **ID**: `COUNTDOWN` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: CountdownPower: 6
- **EN**: At the start of your turn, apply {CountdownPower:diff()} [gold]Doom[/gold] to a random enemy.
- **CN**: 在你的回合开始时，给予随机敌人{CountdownPower:diff()}点[gold]灾厄[/gold]。

### Danse Macabre / 死亡之舞
- **ID**: `DANSE_MACABRE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: DanseMacabrePower: 3 | Energy: 2
- **EN**: Whenever you play a card that costs {Energy:energyIcons()} or more, gain {DanseMacabrePower:diff()} [gold]Block[/gold].
- **CN**: 每当你打出一张耗能大于等于{Energy:energyIcons()}的牌时，获得{DanseMacabrePower:diff()}点[gold]格挡[/gold]。

### Death March / 死亡行军
- **ID**: `DEATH_MARCH` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 8 | ExtraDamage: 3 | CalculatedDamage: 8
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each card drawn during your turn.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
你在回合进行中每抽到一张牌，都会使其额外造成{ExtraDamage:diff()}点伤害。

### Death's Door / 死亡之门
- **ID**: `DEATHS_DOOR` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 6 | Repeat: 2
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
If you applied [gold]Doom[/gold] this turn, gain [gold]Block[/gold] {Repeat:diff()} additional {Repeat:plural:time|times}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
如果你在本回合中曾给予过[gold]灾厄[/gold]，则额外获得{Repeat:diff()}次[gold]格挡[/gold]。

### Deathbringer / 死亡使者
- **ID**: `DEATHBRINGER` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AllEnemies
- **Values**: DoomPower: 21 | WeakPower: 1
- **EN**: Apply {DoomPower:diff()} [gold]Doom[/gold] and {WeakPower:diff()} [gold]Weak[/gold] to ALL enemies.
- **CN**: 给予所有敌人{DoomPower:diff()}层[gold]灾厄[/gold]和{WeakPower:diff()}层[gold]虚弱[/gold]。

### Debilitate / 摧残
- **ID**: `DEBILITATE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7 | DebilitatePower: 3
- **EN**: Deal {Damage:diff()} damage.
[gold]Vulnerable[/gold] and [gold]Weak[/gold] are twice as effective against the enemy for the next {DebilitatePower:diff()} {DebilitatePower:plural:turn|turns}.
- **CN**: 造成{Damage:diff()}点伤害。
在接下来的{DebilitatePower:diff()}回合内，该敌人身上的[gold]易伤[/gold]与[gold]虚弱[/gold]效率翻倍。

### Delay / 拖延
- **ID**: `DELAY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Block: 11 | Energy: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Next turn,
gain {Energy:energyIcons()}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在下个回合，
获得{Energy:energyIcons()}。

### Dirge / 挽歌
- **ID**: `DIRGE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Summon: 3
- **EN**: [gold]Summon[/gold] {Summon:diff()} X times.
Add X [gold]{IfUpgraded:show:Souls+|Souls}[/gold] into your [gold]Draw Pile[/gold].
- **CN**: [gold]召唤[/gold]{Summon:diff()}X次。
将X张[gold]{IfUpgraded:show:灵魂+|灵魂}[/gold]添加到你的[gold]抽牌堆[/gold]中。

### Dredge / 清淤
- **ID**: `DREDGE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 3
- **EN**: Put {Cards:diff()} {Cards:plural:card|cards} from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 将{Cards:diff()}张牌从[gold]弃牌堆[/gold]加入你的[gold]手牌[/gold]。

### Enfeebling Touch / 弱化之触
- **ID**: `ENFEEBLING_TOUCH` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Ethereal
- **Values**: StrengthLoss: 8
- **EN**: Enemy loses {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
- **CN**: 让一名敌人在本回合内失去{StrengthLoss:diff()}点[gold]力量[/gold]。

### Fetch / 取回
- **ID**: `FETCH` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: OstyDamage: 3 | Cards: 1
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
If this is the first time this card has been played this turn, draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。
如果这是这张牌第一次在本回合被打出，则抽{Cards:diff()}张牌。

### Friendship / 友谊
- **ID**: `FRIENDSHIP` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: StrengthPower: 2 | Energy: 1
- **EN**: Lose {StrengthPower:inverseDiff()} [gold]Strength[/gold].
Gain {Energy:energyIcons()} at the start of each turn.
- **CN**: 失去{StrengthPower:inverseDiff()}点[gold]力量[/gold]。
在每个回合开始时获得{Energy:energyIcons()}。

### Haunt / 纠缠
- **ID**: `HAUNT` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: HpLoss: 6
- **EN**: Whenever you play a [gold]Soul[/gold], a random enemy loses {HpLoss:diff()} HP.
- **CN**: 每当你打出一张[gold]灵魂[/gold]时，随机一名敌人失去{HpLoss:diff()}点生命。

### High Five / 击掌
- **ID**: `HIGH_FIVE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AllEnemies
- **Values**: OstyDamage: 11 | VulnerablePower: 2
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage
and applies {VulnerablePower:diff()} [gold]Vulnerable[/gold]
to ALL enemies.
- **CN**: [gold]奥斯提[/gold]对所有敌人
造成{OstyDamage:diff()}点伤害并给予它们
{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Legion of Bone / 骸骨军团
- **ID**: `LEGION_OF_BONE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AllAllies
- **Keywords**: Exhaust
- **Values**: Summon: 6
- **EN**: ALL players [gold]Summon[/gold] {Summon:diff()}.
- **CN**: 所有玩家[gold]召唤[/gold]{Summon:diff()}。

### Lethality / 致死性
- **ID**: `LETHALITY` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Ethereal
- **Values**: LethalityPower: 50
- **EN**: The first Attack each turn deals {LethalityPower:diff()}% additional damage.
- **CN**: 每回合的第一张攻击牌会造成{LethalityPower:diff()}%额外伤害。

### Melancholy / 忧郁
- **ID**: `MELANCHOLY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 3 | **Target**: Self
- **Values**: Block: 13 | Energy: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Reduce this card's cost by {Energy:energyIcons()} whenever ANYONE dies.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
每当有任何生物死亡时，这张牌的耗能减少{Energy:energyIcons()}。

### No Escape / 无处可逃
- **ID**: `NO_ESCAPE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: DoomThreshold: 10 | CalculationBase: 10 | CalculationExtra: 5 | CalculatedDoom: 10
- **EN**: Apply {CalculationBase:diff()} [gold]Doom[/gold], plus an additional {CalculationExtra:diff()} [gold]Doom[/gold] for every {DoomThreshold:diff()} [gold]Doom[/gold] already on this enemy.{IsTargeting:
(Apply {CalculatedDoom:diff()} [gold]Doom[/gold])|}
- **CN**: 给予{CalculationBase:diff()}层[gold]灾厄[/gold]，敌人身上每有{DoomThreshold:diff()}层[gold]灾厄[/gold]，则额外给予这名敌人{CalculationExtra:diff()}层[gold]灾厄[/gold]。{IsTargeting:
（给予{CalculatedDoom:diff()}层[gold]灾厄[/gold]）|}

### Pagestorm / 书页风暴
- **ID**: `PAGESTORM` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: Whenever you draw an [gold]Ethereal[/gold] card, draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 每当你抽到一张[gold]虚无[/gold]牌时, 抽{Cards:diff()}张牌。

### Parse / 领会
- **ID**: `PARSE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Ethereal
- **Values**: Cards: 3
- **EN**: Draw {Cards:diff()} cards.
- **CN**: 抽{Cards:diff()}张牌。

### Pull from Below / 亡魂牵引
- **ID**: `PULL_FROM_BELOW` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 5 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: Deal {Damage:diff()} damage for each [gold]Ethereal[/gold] card played this combat.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: 本场战斗中每打出过一张[gold]虚无[/gold]牌，此牌就造成{Damage:diff()}点伤害一次。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Putrefy / 腐败
- **ID**: `PUTREFY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Power: 2
- **EN**: Apply {Power:diff()} [gold]Weak[/gold].
Apply {Power:diff()} [gold]Vulnerable[/gold].
- **CN**: 给予{Power:diff()}层[gold]虚弱[/gold]。
给予{Power:diff()}层[gold]易伤[/gold]。

### Rattle / 猛晃
- **ID**: `RATTLE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: OstyDamage: 7 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
Hits an additional time for each other time he has attacked this turn.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。
他在本回合每攻击过一次，此牌就额外造成一次伤害。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Right Hand Hand / 得力助手
- **ID**: `RIGHT_HAND_HAND` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: OstyDamage: 4 | Energy: 2
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
Whenever you play a card that costs {Energy:energyIcons()} or more, return this to your [gold]Hand[/gold] from the [gold]Discard Pile[/gold].
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。
每当你打出耗能为{Energy:energyIcons()}或以上的牌，将此牌从[gold]弃牌堆[/gold]放回你的[gold]手牌[/gold]。

### Severance / 切断
- **ID**: `SEVERANCE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 13
- **EN**: Deal {Damage:diff()} damage.
Add a [gold]Soul[/gold] into your [gold]Draw Pile[/gold], [gold]Hand[/gold], and [gold]Discard Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将一张[gold]灵魂[/gold]分别加入你的[gold]抽牌堆[/gold]，[gold]手牌[/gold]和[gold]弃牌堆[/gold]中。

### Shroud / 厄运之衣
- **ID**: `SHROUD` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 2
- **EN**: Whenever you apply [gold]Doom[/gold], gain {Block:diff()} [gold]Block[/gold].
- **CN**: 每当你给予[gold]灾厄[/gold]时，获得{Block:diff()}点[gold]格挡[/gold]。

### Sic 'Em / 紧追不放
- **ID**: `SIC_EM` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: OstyDamage: 5 | SicEmPower: 2
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage.
Whenever [gold]Osty[/gold] hits this enemy this turn, [gold]Summon[/gold] {SicEmPower:diff()}.
- **CN**: [gold]奥斯提[/gold]造成{OstyDamage:diff()}点伤害。
在本回合内，每当[gold]奥斯提[/gold]攻击这名敌人时，[gold]召唤[/gold]{SicEmPower:diff()}。

### Sleight of Flesh / 血肉戏法
- **ID**: `SLEIGHT_OF_FLESH` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: SleightOfFleshPower: 9
- **EN**: Whenever you apply a debuff to an enemy, they take {SleightOfFleshPower:diff()} damage.
- **CN**: 每当你给予一个敌人负面状态时，使其受到{SleightOfFleshPower:diff()}点伤害。

### Spur / 增生
- **ID**: `SPUR` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Retain
- **Values**: Summon: 3 | Heal: 5
- **EN**: [gold]Summon[/gold] {Summon:diff()}.
[gold]Osty[/gold] heals {Heal:diff()} HP.
- **CN**: [gold]召唤[/gold]{Summon:diff()}。
[gold]奥斯提[/gold]回复{Heal:diff()}点生命。

### Veilpiercer / 刺破帷幕
- **ID**: `VEILPIERCER` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 10
- **EN**: Deal {Damage:diff()} damage.
The next [gold]Ethereal[/gold] card you play costs 0 {energyPrefix:energyIcons(1)}.
- **CN**: 造成{Damage:diff()}点伤害。
你打出的下一张[gold]虚无[/gold]牌耗能变为0{energyPrefix:energyIcons(1)}。

