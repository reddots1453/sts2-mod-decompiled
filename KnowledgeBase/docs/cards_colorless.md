# Colorless Cards

## Ancient

### Apotheosis / 神化
- **ID**: `APOTHEOSIS` | **Type**: Skill | **Rarity**: Ancient | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust, Innate
- **EN**: [gold]Upgrade[/gold] ALL your cards.
- **CN**: [gold]升级[/gold]你的全部卡牌。

### Apparition / 灵体
- **ID**: `APPARITION` | **Type**: Skill | **Rarity**: Ancient | **Cost**: 1 | **Target**: Self
- **Keywords**: Ethereal, Exhaust
- **Values**: IntangiblePower: 1
- **EN**: Gain {IntangiblePower:diff()} [gold]Intangible[/gold].
- **CN**: 获得{IntangiblePower:diff()}层[gold]无实体[/gold]。

### Brightest Flame / 至亮之焰
- **ID**: `BRIGHTEST_FLAME` | **Type**: Skill | **Rarity**: Ancient | **Cost**: 0 | **Target**: Self
- **Values**: MaxHp: 1 | Energy: 2 | Cards: 2
- **EN**: Gain {Energy:energyIcons()}.
Draw {Cards:diff()} {Cards:plural:card|cards}.
Lose {MaxHp:diff()} Max HP.
- **CN**: 获得{Energy:energyIcons()}。
抽{Cards:diff()}张牌。
失去{MaxHp:diff()}点最大生命。

### Maul / 撕咬
- **ID**: `MAUL` | **Type**: Attack | **Rarity**: Ancient | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 5 | Increase: 1
- **EN**: Deal {Damage:diff()} damage twice.
Increase the damage of ALL Maul cards by {Increase:diff()} this combat.
- **CN**: 造成{Damage:diff()}点伤害两次。
在这场战斗中，将所有“撕咬”牌的伤害增加{Increase:diff()}。

### Neow's Fury / 涅奥之怒
- **ID**: `NEOWS_FURY` | **Type**: Attack | **Rarity**: Ancient | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 10 | Cards: 2
- **EN**: Deal {Damage:diff()} damage.
Put {Cards:plural:a random card|{:diff()} random cards} from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将你[gold]弃牌堆[/gold]中的{Cards:diff()}张随机牌放入你的[gold]手牌[/gold]。

### Relax / 放松
- **ID**: `RELAX` | **Type**: Skill | **Rarity**: Ancient | **Cost**: 3 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Block: 15 | Cards: 2 | Energy: 2
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Next turn, draw {Cards:diff()} {Cards:plural:card|cards} and gain {Energy:energyIcons()}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
下个回合，抽{Cards:diff()}张牌并获得{Energy:energyIcons()}。

### Whistle / 吹哨
- **ID**: `WHISTLE` | **Type**: Attack | **Rarity**: Ancient | **Cost**: 3 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 33
- **EN**: Deal {Damage:diff()} damage.
[gold]Stun[/gold] the enemy.
- **CN**: 造成{Damage:diff()}点伤害。
[gold]击晕[/gold]该敌人。

### Wish / 许愿
- **ID**: `WISH` | **Type**: Skill | **Rarity**: Ancient | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Put a card from your [gold]Draw Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 将你[gold]抽牌堆[/gold]中的一张牌放入你的[gold]手牌[/gold]。

## Curse

### Ascender's Bane / 进阶之灾
- **ID**: `ASCENDERS_BANE` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Eternal, Unplayable, Ethereal

### Bad Luck / 霉运
- **ID**: `BAD_LUCK` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Eternal, Unplayable
- **Values**: HpLoss: 13
- **EN**: At the end of your turn, if this is in your Hand, lose {HpLoss:diff()} HP.
- **CN**: 在你的回合结束时，如果这张牌在你的手牌中，则失去{HpLoss:diff()}点生命。

### Clumsy / 笨拙
- **ID**: `CLUMSY` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable, Ethereal

### Curse of the Bell / 铃铛的诅咒
- **ID**: `CURSE_OF_THE_BELL` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Eternal, Unplayable

### Debt / 债务
- **ID**: `DEBT` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: Gold: 10
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], lose {Gold:diff()} [gold]Gold[/gold].
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，则失去{Gold:diff()}[gold]金币[/gold]。

### Decay / 腐朽
- **ID**: `DECAY` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: Damage: 2
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], take {Damage:diff()} damage.
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中, 你受到{Damage:diff()}点伤害。

### Deprecated Card / 弃用卡牌
- **ID**: `DEPRECATED_CARD` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **EN**: This card has been removed from the game.
- **CN**: 这张卡牌已经从游戏中移除。

### Doubt / 疑虑
- **ID**: `DOUBT` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: WeakPower: 1
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], gain {WeakPower:diff()} [gold]Weak[/gold].
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，获得{WeakPower:diff()}层[gold]虚弱[/gold]。

### Enthralled / 执迷
- **ID**: `ENTHRALLED` | **Type**: Curse | **Rarity**: Curse | **Cost**: 2 | **Target**: None
- **Keywords**: Eternal
- **EN**: If this is in your [gold]Hand[/gold], it must be played before other cards.
- **CN**: 如果这张牌在你的[gold]手牌[/gold]中，你必须优先打出这张牌。

### Folly / 愚行
- **ID**: `FOLLY` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable, Eternal, Innate

### Greed / 贪婪
- **ID**: `GREED` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Eternal, Unplayable

### Guilty / 愧疚
- **ID**: `GUILTY` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: Combats: 5
- **EN**: Removed from your [gold]Deck[/gold] after {Combats:diff()} {Combats:plural:combat|combats}.
- **CN**: 在{Combats:diff()}场战斗后从你的[gold]牌组[/gold]中移除。

### Injury / 受伤
- **ID**: `INJURY` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable

### Normality / 凡庸
- **ID**: `NORMALITY` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: CalculationBase: 3 | CalculationExtra: -1 | CalculatedCards: 3
- **EN**: You cannot play more than 3 cards this turn.{InCombat:
({CalculatedCards:plural:{} card|{} cards} left)|}
- **CN**: 你在本回合不能打出超过3张牌。{InCombat:
（还剩{CalculatedCards}张牌）|}

### Poor Sleep / 睡眠不佳
- **ID**: `POOR_SLEEP` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable, Retain

### Regret / 悔恨
- **ID**: `REGRET` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], lose 1 HP for each card in your [gold]Hand[/gold].
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，失去相当于[gold]手牌[/gold]数量的生命。

### Shame / 羞耻
- **ID**: `SHAME` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: Frail: 1
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], gain {Frail:diff()} [gold]Frail[/gold].
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，则获得{Frail:diff()}层[gold]脆弱[/gold]。

### Spore Mind / 孢子心灵
- **ID**: `SPORE_MIND` | **Type**: Curse | **Rarity**: Curse | **Cost**: 1 | **Target**: None
- **Keywords**: Exhaust

### Writhe / 苦恼
- **ID**: `WRITHE` | **Type**: Curse | **Rarity**: Curse | **Cost**: -1 | **Target**: None
- **Keywords**: Innate, Unplayable

## Event

### Byrd Swoop / 异鸟扑击
- **ID**: `BYRD_SWOOP` | **Type**: Attack | **Rarity**: Event | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 14
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Caltrops / 铁蒺藜
- **ID**: `CALTROPS` | **Type**: Power | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **Values**: ThornsPower: 3
- **EN**: Whenever you are attacked, deal {ThornsPower:diff()} damage back.
- **CN**: 每当你被攻击时，对攻击者造成{ThornsPower:diff()}点伤害。

### Clash / 交锋
- **ID**: `CLASH` | **Type**: Attack | **Rarity**: Event | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 14
- **EN**: Can only be played if every card in your [gold]Hand[/gold] is an Attack.
Deal {Damage:diff()} damage.
- **CN**: 只有在[gold]手牌[/gold]中每一张牌都是攻击牌时才能被打出。
造成{Damage:diff()}点伤害。

### Distraction / 声东击西
- **ID**: `DISTRACTION` | **Type**: Skill | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Add a random Skill into your [gold]Hand[/gold]. It's free to play this turn.
- **CN**: 将一张随机技能牌添加到你的[gold]手牌[/gold]中。这张牌在本回合内可以免费打出。

### Dual Wield / 双持
- **ID**: `DUAL_WIELD` | **Type**: Skill | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: Choose an Attack or Power card. Add {IfUpgraded:show:{Cards} copies|a copy} of that card into your [gold]Hand[/gold].
- **CN**: 选择一张攻击牌或能力牌。将{IfUpgraded:show:{Cards}张此牌的复制品|一张此牌的复制品}加入你的[gold]手牌[/gold]。

### Enlightenment / 开悟
- **ID**: `ENLIGHTENMENT` | **Type**: Skill | **Rarity**: Event | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Reduce the cost of ALL cards in your [gold]Hand[/gold] to 1 this {IfUpgraded:show:combat|turn}.
- **CN**: 在这{IfUpgraded:show:场战斗|个回合}，你当前[gold]手牌[/gold]中所有牌的耗能降低至1。

### Entrench / 巩固
- **ID**: `ENTRENCH` | **Type**: Skill | **Rarity**: Event | **Cost**: 2 | **Target**: Self
- **EN**: Double your [gold]Block[/gold].
- **CN**: 将你当前的[gold]格挡[/gold]翻倍。

### Exterminate / 杀灭
- **ID**: `EXTERMINATE` | **Type**: Attack | **Rarity**: Event | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 3 | Repeat: 4
- **EN**: Deal {Damage:diff()} damage {Repeat:diff()} times to ALL enemies.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害{Repeat:diff()}次。

### Feeding Frenzy / 疯狂进食
- **ID**: `FEEDING_FRENZY` | **Type**: Skill | **Rarity**: Event | **Cost**: 0 | **Target**: Self
- **Values**: StrengthPower: 5
- **EN**: Gain {StrengthPower:diff()} [gold]Strength[/gold] this turn.
- **CN**: 在本回合内获得{StrengthPower:diff()}点[gold]力量[/gold]。

### Hello World / 你好世界
- **ID**: `HELLO_WORLD` | **Type**: Power | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **EN**: At the start of your turn, add a random Common card into your [gold]Hand[/gold].
- **CN**: 在你的回合开始时，将一张随机普通牌加入你的[gold]手牌[/gold]。

### Mad Science / 疯狂科学
- **ID**: `MAD_SCIENCE` | **Type**: None | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **Values**: Damage: 12 | Block: 8 | SappingWeak: 2 | SappingVulnerable: 2 | ViolenceHits: 3 | ChokingDamage: 6 | EnergizedEnergy: 2 | WisdomCards: 3 | ExpertiseStrength: 2 | ExpertiseDexterity: 2 | CuriousReduction: 1
- **EN**: {CardType:choose(Attack|Skill|Power):Deal {Damage:diff()} damage{Violence: {ViolenceHits:diff()} times|}.|Gain {Block:diff()} [gold]Block[/gold].|}{HasRider:{Sapping:
Apply {SappingWeak:diff()} [gold]Weak[/gold].
Apply {SappingVulnerable:diff()} [gold]Vulnerable[/gold].|}{Choking:
Whenever you play a card this turn, the enemy loses {ChokingDamage:diff()} HP.|}{Energized:
Gain {EnergizedEnergy:energyIcons()}.|}{Wisdom:
Draw {WisdomCards:diff()} cards.|}{Chaos:
Add a random card into your [gold]Hand[/gold]. It costs 0 {energyPrefix:energyIcons(1)} this turn.|}{Expertise:Gain {ExpertiseStrength:diff()} [gold]Strength[/gold].
Gain {ExpertiseDexterity:diff()} [gold]Dexterity[/gold].|}{Curious:Powers cost {CuriousReduction:diff()} {energyPrefix:energyIcons(1)} less.|}{Improvement:At the end of combat, [gold]Upgrade[/gold] a random card.|}|{CardType:choose(Attack|Skill|Power):
???|
???|???}}
- **CN**: {CardType:choose(Attack|Skill|Power):造成{Damage:diff()}点伤害{Violence:{ViolenceHits:diff()}次|}。|获得{Block:diff()}点[gold]格挡[/gold]。|}{HasRider:{Sapping:
给予{SappingWeak:diff()}层[gold]虚弱[/gold]。
给予{SappingVulnerable:diff()}层[gold]易伤[/gold]。|}{Choking:
本回合，你每打出一张牌，该敌人失去{ChokingDamage:diff()}点生命。|}{Energized:
获得{EnergizedEnergy:energyIcons()}。|}{Wisdom:
抽{WisdomCards:diff()}张牌|}{Chaos:
将一张随机牌放入你的[gold]手牌[/gold]，这张牌在本回合耗能变为0{energyPrefix:energyIcons(1)}。|}{Expertise:获得{ExpertiseStrength:diff()}点[gold]力量[/gold]。
获得{ExpertiseDexterity:diff()}点[gold]敏捷[/gold]。|}{Curious:能力牌的耗能减少{CuriousReduction:diff()}{energyPrefix:energyIcons(1)}。|}{Improvement:在战斗结束时，[gold]升级[/gold]你牌组中的一张随机牌。|}|{CardType:choose(Attack|Skill|Power):
？？？|
？？？|？？？}}

### Metamorphosis / 羽化
- **ID**: `METAMORPHOSIS` | **Type**: Skill | **Rarity**: Event | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 3
- **EN**: Add {Cards:diff()} random Attacks into your [gold]Draw Pile[/gold]. They're free to play this combat.
- **CN**: 在你的[gold]抽牌堆[/gold]中加入{Cards:diff()}张随机攻击牌。它们在本场战斗中可以被免费打出。

### Outmaneuver / 抢占先机
- **ID**: `OUTMANEUVER` | **Type**: Skill | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **Values**: Energy: 2
- **EN**: Next turn, gain {Energy:energyIcons()}.
- **CN**: 在下个回合获得{Energy:energyIcons()}。

### Peck / 啄击
- **ID**: `PECK` | **Type**: Attack | **Rarity**: Event | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 2 | Repeat: 3
- **EN**: Deal {Damage:diff()} damage {Repeat:diff()} times.
- **CN**: 造成{Damage:diff()}点伤害{Repeat:diff()}次。

### Rebound / 弹回
- **ID**: `REBOUND` | **Type**: Attack | **Rarity**: Event | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 9
- **EN**: Deal {Damage:diff()} damage.
Put the next card you play this turn on top of your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将你在本回合打出的下一张牌放置到你的[gold]抽牌堆[/gold]顶部。

### Squash / 压扁
- **ID**: `SQUASH` | **Type**: Attack | **Rarity**: Event | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 10 | VulnerablePower: 2
- **EN**: Deal {Damage:diff()} damage.
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Stack / 堆栈
- **ID**: `STACK` | **Type**: Skill | **Rarity**: Event | **Cost**: 1 | **Target**: Self
- **Values**: CalculationBase: 0 | CalculationExtra: 1 | CalculatedBlock: 0
- **EN**: Gain [gold]Block[/gold] equal to the number of cards in your [gold]Discard Pile[/gold]{IfUpgraded:show: +{CalculationBase}|}.{InCombat:
(Gain {CalculatedBlock:diff()} [gold]Block[/gold].)|}
- **CN**: 获得等量于与你当前[gold]弃牌堆[/gold]中牌数{IfUpgraded:show:+{CalculationBase}|}的[gold]格挡[/gold]值。{InCombat:
（获得{CalculatedBlock:diff()}点[gold]格挡[/gold]。）|}

### Toric Toughness / 坚韧之环
- **ID**: `TORIC_TOUGHNESS` | **Type**: Skill | **Rarity**: Event | **Cost**: 2 | **Target**: Self
- **Values**: Turns: 2 | Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Gain {Block:diff()} [gold]Block[/gold] at the start of the next {Turns:diff()} {Turns:plural:turn|turns}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在接下来的{Turns:diff()}个回合开始时，获得{Block:diff()}点[gold]格挡[/gold]。

## Quest

### Byrdonis Egg / 多尼斯异鸟蛋
- **ID**: `BYRDONIS_EGG` | **Type**: Quest | **Rarity**: Quest | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **EN**: Can be hatched at a [gold]Rest Site[/gold].
- **CN**: 能在[gold]休息处[/gold]被孵化。

### Lantern Key / 灯火钥匙
- **ID**: `LANTERN_KEY` | **Type**: Quest | **Rarity**: Quest | **Cost**: -1 | **Target**: Self
- **Keywords**: Unplayable
- **EN**: Unlocks a special event in the next Act.
- **CN**: 在下一阶段解锁一个特殊事件。

### Spoils Map / 藏宝图
- **ID**: `SPOILS_MAP` | **Type**: Quest | **Rarity**: Quest | **Cost**: -1 | **Target**: Self
- **Keywords**: Unplayable
- **Values**: Gold: 600
- **EN**: Marks a site of {Gold:diff()} extra [gold]Gold[/gold] in the next Act.
- **CN**: 在下一阶段的地图上，标记一个有{Gold:diff()}额外[gold]金币[/gold]的地点。

## Rare

### Alchemize / 炼制药水
- **ID**: `ALCHEMIZE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Procure a random potion.
- **CN**: 获得一瓶随机药水。

### Anointed / 天选
- **ID**: `ANOINTED` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Put every [gold]Rare[/gold] card from your [gold]Draw Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 将你[gold]抽牌堆[/gold]中的所有[gold]稀有[/gold]牌放入你的[gold]手牌[/gold]。

### Beacon of Hope / 希望灯塔
- **ID**: `BEACON_OF_HOPE` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **EN**: Whenever you gain [gold]Block[/gold] on your turn, other players gain half that much [gold]Block[/gold].
- **CN**: 每当你在你的回合获得[gold]格挡[/gold]时，其他玩家获得相应一半的[gold]格挡[/gold]。

### Beat Down / 狠揍
- **ID**: `BEAT_DOWN` | **Type**: Skill | **Rarity**: Rare | **Cost**: 3 | **Target**: RandomEnemy
- **Values**: Cards: 3
- **EN**: Play {Cards:diff()} random Attacks from your [gold]Discard Pile[/gold].
- **CN**: 打出你[gold]弃牌堆[/gold]中的{Cards:diff()}张随机攻击牌。

### Bolas / 流星锤
- **ID**: `BOLAS` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 3
- **EN**: Deal {Damage:diff()} damage.
At the start of your next turn, return this to your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
在你的下个回合开始时，将此卡返回你的[gold]手牌[/gold]。

### Calamity / 灾祸
- **ID**: `CALAMITY` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **EN**: Whenever you play an Attack, add a random Attack into your [gold]Hand[/gold].
- **CN**: 每当你打出一张攻击牌时，将一张随机攻击牌添加到你的[gold]手牌[/gold]。

### Entropy / 熵
- **ID**: `ENTROPY` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: At the start of your turn, [gold]Transform[/gold] {Cards:diff()} {Cards:plural:card|cards} in your [gold]Hand[/gold].
- **CN**: 在你的回合开始时，[gold]变化[/gold]你[gold]手牌[/gold]中的{Cards:diff()}张牌。

### Eternal Armor / 永恒铠甲
- **ID**: `ETERNAL_ARMOR` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Values**: PlatingPower: 7
- **EN**: Gain {PlatingPower:diff()} [gold]Plating[/gold].
- **CN**: 获得{PlatingPower:diff()}层[gold]覆甲[/gold]。

### Gold Axe / 金斧
- **ID**: `GOLD_AXE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 0 | ExtraDamage: 1 | CalculatedDamage: 0
- **EN**: Deal damage equal to the number of cards played this combat.{InCombat:
(Deals {CalculatedDamage:diff()} damage)|}
- **CN**: 造成本场战斗中所打出牌数的伤害。{InCombat:
（造成{CalculatedDamage:diff()}点伤害）|}

### Hand of Greed / 贪婪之手
- **ID**: `HAND_OF_GREED` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 20 | Gold: 20
- **EN**: Deal {Damage:diff()} damage.
If [gold]Fatal[/gold], gain {Gold:diff()} [gold]Gold[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
[gold]斩杀[/gold]时，获得{Gold:diff()}[gold]金币[/gold]。

### Hidden Gem / 未掘宝石
- **ID**: `HIDDEN_GEM` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Replay: 2
- **EN**: A random card in your [gold]Draw Pile[/gold] gains [gold]Replay[/gold] {Replay:diff()}.
- **CN**: 你[gold]抽牌堆[/gold]中的一张随机牌获得{Replay:diff()}层[gold]重放[/gold]。

### Jackpot / 大奖
- **ID**: `JACKPOT` | **Type**: Attack | **Rarity**: Rare | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 25 | Cards: 3
- **EN**: Deal {Damage:diff()} damage.
Add {Cards:diff()} random{IfUpgraded:show: [gold]Upgraded[/gold]} 0{energyPrefix:energyIcons(1)} {Cards:plural:card|cards} into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将{Cards:diff()}张随机{IfUpgraded:show:[gold]升级过[/gold]的}0{energyPrefix:energyIcons(1)}的牌加入你的[gold]手牌[/gold]。

### Knockdown / 击倒
- **ID**: `KNOCKDOWN` | **Type**: Attack | **Rarity**: Rare | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 10 | KnockdownPower: 2
- **EN**: Deal {Damage:diff()} damage.
The enemy takes {IfUpgraded:show:triple|double} damage from other players this turn.
- **CN**: 造成{Damage:diff()}点伤害。
该敌人在本回合受到的来自其他玩家的伤害{IfUpgraded:show:翻三倍|翻倍}。

### Master of Strategy / 战略大师
- **ID**: `MASTER_OF_STRATEGY` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 3
- **EN**: Draw {Cards:diff()} cards.
- **CN**: 抽{Cards:diff()}张牌。

### Mayhem / 乱战
- **ID**: `MAYHEM` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **EN**: At the start of your turn, play the top card of your [gold]Draw Pile[/gold].
- **CN**: 在你的回合开始时，打出你[gold]抽牌堆[/gold]顶部的牌。

### Mimic / 拟态
- **ID**: `MIMIC` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyAlly
- **Keywords**: Exhaust
- **Values**: CalculationBase: 0 | CalculationExtra: 1 | CalculatedBlock: 0
- **EN**: Gain [gold]Block[/gold] equal to the [gold]Block[/gold] on another player.
- **CN**: 获得等同于另一位玩家[gold]格挡[/gold]值的[gold]格挡[/gold]。

### Nostalgia / 怀旧
- **ID**: `NOSTALGIA` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **EN**: The first Attack or Skill you play each turn is placed on top of your [gold]Draw Pile[/gold].
- **CN**: 将你每回合打出第一张攻击或技能牌，置于你的[gold]抽牌堆[/gold]顶端。

### Rally / 集结
- **ID**: `RALLY` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: AllAllies
- **Values**: Block: 12
- **EN**: ALL players gain {Block:diff()} [gold]Block[/gold].
- **CN**: 所有玩家获得{Block:diff()}点[gold]格挡[/gold]。

### Rend / 撕碎
- **ID**: `REND` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: CalculationBase: 15 | ExtraDamage: 5 | CalculatedDamage: 15
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each unique debuff on the enemy.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
该名敌人身上每有一种负面效果，就额外造成{ExtraDamage:diff()}点伤害。

### Rolling Boulder / 滚石
- **ID**: `ROLLING_BOULDER` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Values**: RollingBoulderPower: 5 | IncrementAmount: 5
- **EN**: At the start of your turn, deal {RollingBoulderPower:diff()} damage to ALL enemies and increase this damage by {IncrementAmount:diff()}.
- **CN**: 在你的回合开始时，对所有敌人造成{RollingBoulderPower:diff()}点伤害，然后将该伤害增加{IncrementAmount:diff()}点。

### Salvo / 箭雨
- **ID**: `SALVO` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 12
- **EN**: Deal {Damage:diff()} damage.
[gold]Retain[/gold] your [gold]Hand[/gold] this turn.
- **CN**: 造成{Damage:diff()}点伤害。
在本回合[gold]保留[/gold]你的[gold]手牌[/gold]。

### Scrawl / 潦草急就
- **ID**: `SCRAWL` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Draw cards until your [gold]Hand[/gold] is full.
- **CN**: 抽牌直到抽满[gold]手牌[/gold]。

### Secret Technique / 秘密技法
- **ID**: `SECRET_TECHNIQUE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Put a Skill from your [gold]Draw Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 从[gold]抽牌堆[/gold]中选择一张技能牌放入你的[gold]手牌[/gold]。

### Secret Weapon / 秘密武器
- **ID**: `SECRET_WEAPON` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Put an Attack from your [gold]Draw Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 从[gold]抽牌堆[/gold]中选择一张攻击牌放入你的[gold]手牌[/gold]。

### The Gambit / 孤注一掷
- **ID**: `THE_GAMBIT` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Values**: Block: 50
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
If you take unblocked attack damage this combat, die.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
如果你在本场战斗中受到未被格挡的伤害，则立刻死亡。

## Status

### Beckon / 呼唤
- **ID**: `BECKON` | **Type**: Status | **Rarity**: Status | **Cost**: 1 | **Target**: None
- **Values**: HpLoss: 6
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold],
 lose {HpLoss} HP.
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，
则失去{HpLoss}点生命。

### Burn / 灼伤
- **ID**: `BURN` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: Damage: 2
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], take {Damage:diff()} damage.
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，你受到{Damage:diff()}点伤害。

### Dazed / 晕眩
- **ID**: `DAZED` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Keywords**: Ethereal, Unplayable

### Debris / 碎屑
- **ID**: `DEBRIS` | **Type**: Status | **Rarity**: Status | **Cost**: 1 | **Target**: None
- **Keywords**: Exhaust

### Disintegration / 瓦解
- **ID**: `DISINTEGRATION` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Values**: DisintegrationPower: 6
- **EN**: At the end of your turn, take {DisintegrationPower:diff()} damage.
- **CN**: 在你的回合结束时，受到{DisintegrationPower:diff()}点伤害。

### Frantic Escape / 狂乱逃离
- **ID**: `FRANTIC_ESCAPE` | **Type**: Status | **Rarity**: Status | **Cost**: 1 | **Target**: Self
- **EN**: Get farther away.
Increase [gold]Sandpit[/gold] by 1.
Increase the cost of this card by 1.
- **CN**: 远离。
将[gold]沙坑[/gold]的计数加1。
这张牌的耗能加1。

### Infection / 感染
- **ID**: `INFECTION` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable
- **Values**: Damage: 3
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], take {Damage:diff()} damage.
- **CN**: 在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，则受到{Damage:diff()}点伤害。

### Mind Rot / 心灵腐化
- **ID**: `MIND_ROT` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Values**: MindRotPower: 1
- **EN**: Draw {MindRotPower:diff()} fewer {MindRotPower:plural:card|cards} each turn.
- **CN**: 每回合少抽{MindRotPower:diff()}张牌。

### Slimed / 黏液
- **ID**: `SLIMED` | **Type**: Status | **Rarity**: Status | **Cost**: 1 | **Target**: None
- **Keywords**: Exhaust
- **Values**: Cards: 1
- **EN**: Draw 1 card.
- **CN**: 抽1张牌。

### Sloth / 懒惰
- **ID**: `SLOTH` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Values**: SlothPower: 3
- **EN**: You cannot play more than {SlothPower:diff()} {SlothPower:plural:card|cards} each turn.
- **CN**: 你在每个回合不能打出超过{SlothPower:diff()}张牌。

### Soot / 煤灰
- **ID**: `SOOT` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable

### Toxic / 毒素
- **ID**: `TOXIC` | **Type**: Status | **Rarity**: Status | **Cost**: 1 | **Target**: None
- **Keywords**: Exhaust
- **Values**: Damage: 5
- **EN**: At the end of your turn, if this is in your [gold]Hand[/gold], take {Damage:diff()} damage.
- **CN**: 在你的回合结束时，这张牌在你的[gold]手牌[/gold]中，则受到{Damage:diff()}点伤害。

### Void / 虚空
- **ID**: `VOID` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable, Ethereal
- **Values**: Energy: 1
- **EN**: Whenever you draw this card, lose {Energy:energyIcons()}.
- **CN**: 每当你抽到这张牌时，失去{Energy:energyIcons()}。

### Waste Away / 衰朽
- **ID**: `WASTE_AWAY` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Values**: WasteAwayPower: 1
- **EN**: Gain {WasteAwayPower:diff()} less {energyPrefix:energyIcons(1)} per turn.
- **CN**: 每回合失去{WasteAwayPower:diff()}点{energyPrefix:energyIcons(1)}。

### Wound / 伤口
- **ID**: `WOUND` | **Type**: Status | **Rarity**: Status | **Cost**: -1 | **Target**: None
- **Keywords**: Unplayable

## Token

### Fuel / 燃料
- **ID**: `FUEL` | **Type**: Skill | **Rarity**: Token | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Energy: 1 | Cards: 1
- **EN**: Gain {Energy:energyIcons()}.
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 获得{Energy:energyIcons()}。
抽{Cards:diff()}张牌。

### Giant Rock / 巨石
- **ID**: `GIANT_ROCK` | **Type**: Attack | **Rarity**: Token | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 16
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Luminesce / 冷光
- **ID**: `LUMINESCE` | **Type**: Skill | **Rarity**: Token | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust, Retain
- **Values**: Energy: 2
- **EN**: Gain {Energy:energyIcons()}.
- **CN**: 获得{Energy:energyIcons()}。

### Minion Dive Bomb / 仆从俯冲
- **ID**: `MINION_DIVE_BOMB` | **Type**: Attack | **Rarity**: Token | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 13
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Minion Sacrifice / 仆从捐躯
- **ID**: `MINION_SACRIFICE` | **Type**: Skill | **Rarity**: Token | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Block: 9
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Minion Strike / 仆从打击
- **ID**: `MINION_STRIKE` | **Type**: Attack | **Rarity**: Token | **Cost**: 0 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 7 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 造成{Damage:diff()}点伤害。
抽{Cards:diff()}张牌。

### Shiv / 小刀
- **ID**: `SHIV` | **Type**: Attack | **Rarity**: Token | **Cost**: 0 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 4 | CalculationBase: 0 | CalculationExtra: 1 | FanOfKnivesAmount: 0
- **EN**: Deal {Damage:diff()} damage{FanOfKnivesAmount:cond:>0? to ALL enemies|}.
- **CN**: {FanOfKnivesAmount:cond:>0? 对所有敌人|}造成{Damage:diff()}点伤害。

### Soul / 灵魂
- **ID**: `SOUL` | **Type**: Skill | **Rarity**: Token | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 2
- **EN**: Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 抽{Cards:diff()}张牌。

### Sovereign Blade / 君王之剑
- **ID**: `SOVEREIGN_BLADE` | **Type**: Attack | **Rarity**: Token | **Cost**: 2 | **Target**: AnyEnemy
- **Keywords**: Retain
- **Values**: Damage: 10 | CalculationBase: 0 | CalculationExtra: 1 | SeekingEdgeAmount: 0 | Repeat: 1
- **EN**: Deal {Damage:diff()} damage{SeekingEdgeAmount:cond:>0? to ALL enemies|}{Repeat:plural:| {} times}.
- **CN**: {SeekingEdgeAmount:cond:>0?对所有敌人|}造成{Damage:diff()}点伤害{Repeat:choose(1):|{}次}。

### Sweeping Gaze / 扫荡凝视
- **ID**: `SWEEPING_GAZE` | **Type**: Attack | **Rarity**: Token | **Cost**: 0 | **Target**: RandomEnemy
- **Keywords**: Ethereal, Exhaust
- **Values**: OstyDamage: 10
- **EN**: [gold]Osty[/gold] deals {OstyDamage:diff()} damage to a random enemy.
- **CN**: [gold]奥斯提[/gold]对随机一名敌人造成{OstyDamage:diff()}点伤害。

## Uncommon

### Automation / 自动化
- **ID**: `AUTOMATION` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Energy: 1
- **EN**: Every 10 cards you draw, gain {Energy:energyIcons()}.
- **CN**: 你每抽10张牌，获得{Energy:energyIcons()}。

### Believe in You / 相信着你
- **ID**: `BELIEVE_IN_YOU` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyAlly
- **Values**: Energy: 3
- **EN**: Another player gains {Energy:energyIcons()}.
- **CN**: 另一名玩家获得{Energy:energyIcons()}。

### Catastrophe / 横祸
- **ID**: `CATASTROPHE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Cards: 2
- **EN**: Play {Cards:diff()} random {Cards:plural:card|cards} from your [gold]Draw Pile[/gold].
- **CN**: 从你的[gold]抽牌堆[/gold]中随机打出{Cards:diff()}张牌。

### Coordinate / 协同配合
- **ID**: `COORDINATE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyAlly
- **Values**: StrengthPower: 5
- **EN**: Give another player {StrengthPower:diff()} [gold]Strength[/gold] this turn.
- **CN**: 在本回合给予其他玩家{StrengthPower:diff()}点[gold]力量[/gold]。

### Dark Shackles / 黑暗镣铐
- **ID**: `DARK_SHACKLES` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: StrengthLoss: 9
- **EN**: Enemy loses {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
- **CN**: 使一名敌人在本回合失去{StrengthLoss:diff()}点[gold]力量[/gold]。

### Discovery / 发现
- **ID**: `DISCOVERY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Choose 1 of 3 random cards to add into your [gold]Hand[/gold]. It costs 0 {energyPrefix:energyIcons(1)} this turn.
- **CN**: 从3张随机牌中选择1张加入你的[gold]手牌[/gold]。这张牌在本回合的耗能为0{energyPrefix:energyIcons(1)}。

### Dramatic Entrance / 闪亮登场
- **ID**: `DRAMATIC_ENTRANCE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AllEnemies
- **Keywords**: Exhaust, Innate
- **Values**: Damage: 11
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。

### Equilibrium / 均衡
- **ID**: `EQUILIBRIUM` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Block: 13 | Equilibrium: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Retain[/gold] your [gold]Hand[/gold] this turn.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在本回合[gold]保留[/gold]你的[gold]手牌[/gold]。

### Fasten / 勒紧
- **ID**: `FASTEN` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: ExtraBlock: 5
- **EN**: Gain an additional {ExtraBlock:diff()} [gold]Block[/gold] from Defend cards.
- **CN**: 从“防御”牌中额外获得{ExtraBlock:diff()}点[gold]格挡[/gold]。

### Finesse / 妙计
- **ID**: `FINESSE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Block: 4 | Cards: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Draw 1 card.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
抽1张牌。

### Fisticuffs / 拳斗
- **ID**: `FISTICUFFS` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7
- **EN**: Deal {Damage:diff()} damage.
Gain [gold]Block[/gold] equal to damage dealt.
- **CN**: 造成{Damage:diff()}点伤害。
获得等量于所造成伤害的[gold]格挡[/gold]。

### Flash of Steel / 亮剑
- **ID**: `FLASH_OF_STEEL` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 5 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
Draw 1 card.
- **CN**: 造成{Damage:diff()}点伤害。
抽1张牌。

### Gang Up / 群起攻之
- **ID**: `GANG_UP` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 5 | ExtraDamage: 5 | CalculatedDamage: 5
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each time another player has attacked the enemy this turn.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
本回合其他玩家每攻击过一次该敌人，该牌造成的伤害就额外增加{ExtraDamage:diff()}点。

### Huddle Up / 抱团
- **ID**: `HUDDLE_UP` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AllAllies
- **Values**: Cards: 2
- **EN**: ALL allies draw {Cards:diff()} cards.
- **CN**: 所有玩家抽{Cards:diff()}张牌。

### Impatience / 急躁
- **ID**: `IMPATIENCE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Cards: 2
- **EN**: If you have no Attacks in your [gold]Hand[/gold], draw {Cards:diff()} cards.
- **CN**: 如果你的[gold]手牌[/gold]中没有攻击牌，抽{Cards:diff()}张牌。

### Intercept / 拦截
- **ID**: `INTERCEPT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyAlly
- **Values**: Block: 9
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Redirect all incoming attacks that would be dealt to another player this turn to you.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将本回合所有要对另一名玩家发起的攻击转移到你的身上。

### Jack of All Trades / 花样百出
- **ID**: `JACK_OF_ALL_TRADES` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 1
- **EN**: Add {Cards:diff()} random Colorless {Cards:plural:card|cards} into your [gold]Hand[/gold].
- **CN**: 将{Cards:diff()}张随机无色牌加入你的[gold]手牌[/gold]。

### Lift / 托举
- **ID**: `LIFT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyAlly
- **Values**: Block: 11
- **EN**: Give another player {Block:diff()} [gold]Block[/gold].
- **CN**: 给另一名玩家{Block:diff()}点[gold]格挡[/gold]。

### Mind Blast / 心灵震慑
- **ID**: `MIND_BLAST` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Innate
- **Values**: CalculationBase: 0 | ExtraDamage: 1 | CalculatedDamage: 0
- **EN**: Deal damage equal to the number of cards in your [gold]Draw Pile[/gold].{InCombat:
(Deals {CalculatedDamage:diff()} damage)|}
- **CN**: 造成你[gold]抽牌堆[/gold]中剩余牌数的伤害。{InCombat:
（造成{CalculatedDamage:diff()}点伤害）|}

### Omnislice / 万向斩
- **ID**: `OMNISLICE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 8
- **EN**: Deal {Damage:diff()} damage.
Damage ALL other enemies equal to the damage dealt.
- **CN**: 造成{Damage:diff()}点伤害。
对所有其他敌人造成等量的伤害。

### Panache / 神气制胜
- **ID**: `PANACHE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: PanacheDamage: 10
- **EN**: Every time you play 5 cards in a single turn, deal {PanacheDamage:diff()} damage to ALL enemies.
- **CN**: 每当你在一回合内打出五张牌时，对所有敌人造成{PanacheDamage:diff()}点伤害。

### Panic Button / 应急按钮
- **ID**: `PANIC_BUTTON` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Block: 30 | Turns: 2
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
You cannot gain [gold]Block[/gold] from cards for {Turns:diff()} {Turns:plural:turn|turns}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
你在接下来的{Turns:diff()}回合内无法再从卡牌中获得[gold]格挡[/gold]。

### Prep Time / 准备时间
- **ID**: `PREP_TIME` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: PrepTimePower: 4
- **EN**: At the start of your turn, gain {PrepTimePower:diff()} [gold]Vigor[/gold].
- **CN**: 在你的回合开始时，获得{PrepTimePower:diff()}点[gold]活力[/gold]。

### Production / 生产制造
- **ID**: `PRODUCTION` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Energy: 2
- **EN**: Gain {Energy:energyIcons()}.
- **CN**: 获得{Energy:energyIcons()}。

### Prolong / 延伸
- **ID**: `PROLONG` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Next turn, gain [gold]Block[/gold] equal to your current [gold]Block[/gold].
- **CN**: 在下个回合获得等量于你当前[gold]格挡[/gold]值的[gold]格挡[/gold]。

### Prowess / 非凡技艺
- **ID**: `PROWESS` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: StrengthPower: 1 | DexterityPower: 1
- **EN**: Gain {StrengthPower:diff()} [gold]Strength[/gold].
Gain {DexterityPower:diff()} [gold]Dexterity[/gold].
- **CN**: 获得{StrengthPower:diff()}点[gold]力量[/gold]。
获得{DexterityPower:diff()}点[gold]敏捷[/gold]。

### Purity / 净化
- **ID**: `PURITY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Retain, Exhaust
- **Values**: Cards: 3
- **EN**: Exhaust up to {Cards:diff()} cards in your [gold]Hand[/gold].
- **CN**: 从[gold]手牌[/gold]中选择最多{Cards:diff()}张牌消耗。

### Restlessness / 心神不宁
- **ID**: `RESTLESSNESS` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Retain
- **Values**: Cards: 2 | Energy: 2
- **EN**: If your [gold]Hand[/gold] is empty, draw {Cards:diff()} cards and gain {Energy:energyIcons()}.
- **CN**: 如果你的[gold]手牌[/gold]为空，则抽{Cards:diff()}张牌并获得{Energy:energyIcons()}。

### Rip and Tear / 狂乱撕扯
- **ID**: `RIP_AND_TEAR` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: RandomEnemy
- **Values**: Damage: 7
- **EN**: Deal {Damage:diff()} damage to a random enemy twice.
- **CN**: 随机对敌人造成{Damage:diff()}点伤害两次。

### Seeker Strike / 探寻打击
- **ID**: `SEEKER_STRIKE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6 | Cards: 3
- **EN**: Deal {Damage:diff()} damage.
Choose 1 of {Cards:diff()} cards in your [gold]Draw Pile[/gold] to add into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
从[gold]抽牌堆[/gold]的随机{Cards:diff()}张牌中选择一张加入你的[gold]手牌[/gold]。

### Shockwave / 震荡波
- **ID**: `SHOCKWAVE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AllEnemies
- **Keywords**: Exhaust
- **Values**: Power: 3
- **EN**: Apply {Power:diff()} [gold]Weak[/gold] and [gold]Vulnerable[/gold] to ALL enemies.
- **CN**: 给予所有敌人{Power:diff()}层[gold]虚弱[/gold]和[gold]易伤[/gold]。

### Splash / 飞溅
- **ID**: `SPLASH` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **EN**: Choose 1 of 3 random{IfUpgraded:show: [gold]Upgraded[/gold]} Attacks from another character to add into your [gold]Hand[/gold]. It's free to play this turn.
- **CN**: 从3张其他角色的{IfUpgraded:show:[gold]升级过的[/gold]}攻击牌中选择1张加入你的[gold]手牌[/gold]。这张牌在本回合免费打出。

### Stratagem / 计策
- **ID**: `STRATAGEM` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **EN**: Whenever you shuffle your [gold]Draw Pile[/gold], choose a card from it to put into your [gold]Hand[/gold].
- **CN**: 每当你的[gold]抽牌堆[/gold]打乱洗牌时，选择一张牌放入你的[gold]手牌[/gold]。

### Tag Team / 双打组合
- **ID**: `TAG_TEAM` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 11
- **EN**: Deal {Damage:diff()} damage.
The next Attack another player plays on the enemy is played an extra time.
- **CN**: 造成{Damage:diff()}点伤害。
其他玩家的下一张攻击牌将在该名敌人身上额外生效一次。

### The Bomb / 炸弹
- **ID**: `THE_BOMB` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Turns: 3 | BombDamage: 40
- **EN**: At the end of {Turns:diff()} {Turns:plural:turn|turns}, deal {BombDamage:diff()} damage to ALL enemies.
- **CN**: 在{Turns:diff()}回合结束后，对所有敌人造成{BombDamage:diff()}点伤害。

### Thinking Ahead / 深谋远虑
- **ID**: `THINKING_AHEAD` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 2
- **EN**: Draw {Cards:diff()} {Cards:plural:card|cards}.
Put 1 card from your [gold]Hand[/gold] on top of your [gold]Draw Pile[/gold].
- **CN**: 抽{Cards:diff()}张牌。
将[gold]手牌[/gold]中的一张牌放到你的[gold]抽牌堆[/gold]的顶端。

### Thrumming Hatchet / 无休手斧
- **ID**: `THRUMMING_HATCHET` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 11
- **EN**: Deal {Damage:diff()} damage.
At the start of your next turn, return this to your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
在你的下个回合开始时，将此卡返回你的[gold]手牌[/gold]。

### Ultimate Defend / 究极防御
- **ID**: `ULTIMATE_DEFEND` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 11
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Ultimate Strike / 究极打击
- **ID**: `ULTIMATE_STRIKE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 14
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Volley / 连射
- **ID**: `VOLLEY` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: RandomEnemy
- **Values**: Damage: 10
- **EN**: Deal {Damage:diff()} damage to a random enemy X times.
- **CN**: 随机对敌人造成{Damage:diff()}点伤害X次。

