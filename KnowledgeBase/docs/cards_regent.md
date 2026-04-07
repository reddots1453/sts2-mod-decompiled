# REGENT Cards

## Ancient

### Meteor Shower / 流星雨
- **ID**: `METEOR_SHOWER` | **Type**: Attack | **Rarity**: Ancient | **Cost**: 0 | **Target**: AllEnemies
- **Values**: Damage: 14 | VulnerablePower: 2 | WeakPower: 2
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
Apply {WeakPower:diff()} [gold]Weak[/gold] and [gold]Vulnerable[/gold] to ALL enemies.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
给予所有敌人{WeakPower:diff()}层[gold]虚弱[/gold]和[gold]易伤[/gold]。

### The Sealed Throne / 封印王座
- **ID**: `THE_SEALED_THRONE` | **Type**: Power | **Rarity**: Ancient | **Cost**: 1 | **Target**: Self
- **EN**: Whenever you play a card, gain {singleStarIcon}.
- **CN**: 你每打出一张牌，获得{singleStarIcon}。

## Basic

### Defend / 防御
- **ID**: `DEFEND_REGENT` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Falling Star / 陨星
- **ID**: `FALLING_STAR` | **Type**: Attack | **Rarity**: Basic | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 7 | VulnerablePower: 1 | WeakPower: 1
- **EN**: Deal {Damage:diff()} damage.
Apply {WeakPower:diff()} [gold]Weak[/gold].
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{WeakPower:diff()}层[gold]虚弱[/gold]。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Strike / 打击
- **ID**: `STRIKE_REGENT` | **Type**: Attack | **Rarity**: Basic | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Venerate / 崇拜
- **ID**: `VENERATE` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **Values**: Stars: 2
- **EN**: Gain {Stars:starIcons()}.
- **CN**: 获得{Stars:starIcons()}。

## Common

### Astral Pulse / 星界脉冲
- **ID**: `ASTRAL_PULSE` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AllEnemies
- **Values**: Damage: 14
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。

### BEGONE! / 下去！
- **ID**: `BEGONE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 4
- **EN**: Deal {Damage:diff()} damage.
Choose a card in your [gold]Hand[/gold] to [gold]Transform[/gold] into [gold]{IfUpgraded:show:Minion Dive Bomb+|Minion Dive Bomb}[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
选择你[gold]手牌[/gold]中的一张牌，将其[gold]变化[/gold]为[gold]{IfUpgraded:show:仆从俯冲+|仆从俯冲}[/gold]。

### Celestial Might / 天穹之力
- **ID**: `CELESTIAL_MIGHT` | **Type**: Attack | **Rarity**: Common | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 6 | Repeat: 3
- **EN**: Deal {Damage:diff()} damage {Repeat:diff()} {Repeat:plural:time|times}.
- **CN**: 造成{Damage:diff()}点伤害{Repeat:diff()}次。

### Cloak of Stars / 群星斗篷
- **ID**: `CLOAK_OF_STARS` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: Self
- **Values**: Block: 7
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Collision Course / 碰撞轨迹
- **ID**: `COLLISION_COURSE` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 9
- **EN**: Deal {Damage:diff()} damage.
Add a [gold]Debris[/gold] into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将一张[gold]碎屑[/gold]添加至你的[gold]手牌[/gold]。

### Cosmic Indifference / 宇宙冷漠
- **ID**: `COSMIC_INDIFFERENCE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 6
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Put a card from your [gold]Discard Pile[/gold] on top of your [gold]Draw Pile[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将[gold]弃牌堆[/gold]中的一张牌放到你的[gold]抽牌堆[/gold]的顶部。

### Crescent Spear / 新月长矛
- **ID**: `CRESCENT_SPEAR` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 6 | ExtraDamage: 2 | CalculatedDamage: 6
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for ALL your cards that have a {singleStarIcon} cost.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
你每有一张拥有{singleStarIcon}耗能的卡牌，这张牌就额外造成{ExtraDamage:diff()}点伤害。

### Crush Under / 下砸
- **ID**: `CRUSH_UNDER` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 7 | StrengthLoss: 1
- **EN**: Deal {Damage:diff()} damage to ALL enemies. All enemies lose {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。所有敌人在本回合失去{StrengthLoss:diff()}点[gold]力量[/gold]。

### Gather Light / 收集光辉
- **ID**: `GATHER_LIGHT` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 7 | Stars: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Gain {Stars:starIcons()}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
获得{Stars:starIcons()}。

### Glitterstream / 流光溢彩
- **ID**: `GLITTERSTREAM` | **Type**: Skill | **Rarity**: Common | **Cost**: 2 | **Target**: Self
- **Values**: Block: 11 | BlockNextTurn: 4
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Next turn, gain {BlockNextTurn:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在下一回合获得{BlockNextTurn:diff()}点[gold]格挡[/gold]。

### Glow / 辉光
- **ID**: `GLOW` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Stars: 1 | Cards: 2
- **EN**: Gain {Stars:starIcons()}.
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 获得{Stars:starIcons()}。
抽{Cards:diff()}张牌。

### Guiding Star / 引导之星
- **ID**: `GUIDING_STAR` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 12 | Cards: 2
- **EN**: Deal {Damage:diff()} damage.
Next turn, draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 造成{Damage:diff()}点伤害。
在下一回合抽{Cards:diff()}张牌。

### Hidden Cache / 隐秘藏品
- **ID**: `HIDDEN_CACHE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Stars: 1 | StarNextTurnPower: 3
- **EN**: Gain {Stars:starIcons()}.
Next turn, gain {StarNextTurnPower:starIcons()}.
- **CN**: 获得{Stars:starIcons()}。
在下个回合获得{StarNextTurnPower:starIcons()}。

### Know Thy Place / 何人僭越
- **ID**: `KNOW_THY_PLACE` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: WeakPower: 1 | VulnerablePower: 1
- **EN**: Apply {WeakPower:diff()} [gold]Weak[/gold].
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 给予{WeakPower:diff()}层[gold]虚弱[/gold]。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Patter / 星星点点
- **ID**: `PATTER` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 8 | VigorPower: 2
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Gain {VigorPower:diff()} [gold]Vigor[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
获得{VigorPower:diff()}点[gold]活力[/gold]。

### Photon Cut / 光子切割
- **ID**: `PHOTON_CUT` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 10 | Cards: 1 | PutBack: 1
- **EN**: Deal {Damage:diff()} damage.
Draw {Cards:diff()} {Cards:plural:card|cards}.
Put 1 card from your [gold]Hand[/gold] on top of your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
抽{Cards:diff()}张牌。
将[gold]手牌[/gold]中的一张牌放到[gold]抽牌堆[/gold]顶部。

### Refine Blade / 淬炼刀刃
- **ID**: `REFINE_BLADE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Forge: 6 | Energy: 1
- **EN**: [gold]Forge[/gold] {Forge:diff()}.
Next turn, gain {Energy:energyIcons()}.
- **CN**: [gold]铸造[/gold]{Forge:diff()}。
在下个回合获得{Energy:energyIcons()}。

### Solar Strike / 太阳打击
- **ID**: `SOLAR_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 8 | Stars: 1
- **EN**: Deal {Damage:diff()} damage.
Gain {Stars:starIcons()}.
- **CN**: 造成{Damage:diff()}点伤害。
获得{Stars:starIcons()}。

### Spoils of Battle / 战利品
- **ID**: `SPOILS_OF_BATTLE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Forge: 10
- **EN**: [gold]Forge[/gold] {Forge:diff()}.
- **CN**: [gold]铸造[/gold]{Forge:diff()}。

### Wrought in War / 战火铸就
- **ID**: `WROUGHT_IN_WAR` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7 | Forge: 5
- **EN**: Deal {Damage:diff()} damage.
[gold]Forge[/gold] {Forge:diff()}.
- **CN**: 造成{Damage:diff()}点伤害。
[gold]铸造[/gold]{Forge:diff()}。

## Rare

### Arsenal / 武器库
- **ID**: `ARSENAL` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: ArsenalPower: 1
- **EN**: Whenever you play a Colorless card, gain {ArsenalPower:diff()} [gold]Strength[/gold].
- **CN**: 你每打出一张无色牌，都获得{ArsenalPower:diff()}点[gold]力量[/gold]。

### Beat into Shape / 锻打成型
- **ID**: `BEAT_INTO_SHAPE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 5 | CalculationBase: 5 | CalculationExtra: 5 | CalculatedForge: 5
- **EN**: Deal {Damage:diff()} damage.
[gold]Forge[/gold] {CalculatedForge:diff()}.
[gold]Forges[/gold] an additional {CalculationExtra:diff()} for every other time you've hit the enemy this turn.
- **CN**: 造成{Damage:diff()}点伤害。
[gold]铸造[/gold]{CalculatedForge:diff()}。
本回合此前你每击中过该敌人一次，[gold]铸造[/gold]值就上升{CalculationExtra:diff()}。

### Big Bang / 大爆炸
- **ID**: `BIG_BANG` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 1 | Energy: 1 | Stars: 1 | Forge: 5
- **EN**: Draw {Cards:diff()} {Cards:plural:card|cards}.
Gain {Energy:energyIcons()}.
Gain {Stars:starIcons()}.
[gold]Forge[/gold] {Forge:diff()}.
- **CN**: 抽{Cards:diff()}张牌。
获得{Energy:energyIcons()}。
获得{Stars:starIcons()}。
[gold]铸造[/gold]{Forge:diff()}。

### Bombardment / 轰击
- **ID**: `BOMBARDMENT` | **Type**: Attack | **Rarity**: Rare | **Cost**: 3 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 18
- **EN**: Deal {Damage:diff()} damage.
At the start of your turn, plays from the [gold]Exhaust Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
在你的回合开始时，从[gold]消耗牌堆[/gold]打出这张牌。

### Bundle of Joy / 新生之喜
- **ID**: `BUNDLE_OF_JOY` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 3
- **EN**: Add {Cards:diff()} random Colorless {Cards:plural:card|cards} into your [gold]Hand[/gold].
- **CN**: 将{Cards:diff()}张随机无色牌添加到你的[gold]手牌[/gold]。

### Comet / 彗星
- **ID**: `COMET` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 33 | VulnerablePower: 3 | WeakPower: 3
- **EN**: Deal {Damage:diff()} damage.
Apply {WeakPower:diff()} [gold]Weak[/gold].
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{WeakPower:diff()}层[gold]虚弱[/gold]。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Crash Landing / 迫降
- **ID**: `CRASH_LANDING` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 21
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
Fill your [gold]Hand[/gold] with [gold]Debris[/gold].
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
用[gold]碎屑[/gold]填满你的[gold]手牌[/gold]。

### Decisions, Decisions / 抉择，抉择
- **ID**: `DECISIONS_DECISIONS` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Cards: 3 | Repeat: 3
- **EN**: Draw {Cards:diff()} cards.
Choose a Skill in your [gold]Hand[/gold] and play it {Repeat:diff()} times.
- **CN**: 抽{Cards:diff()}张牌。
选择你[gold]手牌[/gold]中的一张技能牌，并将其打出{Repeat:diff()}次。

### Dying Star / 星灭
- **ID**: `DYING_STAR` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AllEnemies
- **Keywords**: Ethereal
- **Values**: Damage: 9 | StrengthLoss: 9
- **EN**: Deal {Damage:diff()} damage to ALL enemies. ALL enemies lose {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。所有敌人在本回合中失去{StrengthLoss:diff()}点[gold]力量[/gold]。

### Foregone Conclusion / 既定事项
- **ID**: `FOREGONE_CONCLUSION` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 2
- **EN**: Next turn, put {Cards:diff()} cards from your [gold]Draw Pile[/gold] into your [gold]Hand[/gold].
- **CN**: 在下个回合，从你的[gold]抽牌堆[/gold]中选择{Cards:diff()}张牌放入你的[gold]手牌[/gold]。

### Genesis / 创世纪
- **ID**: `GENESIS` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: StarsPerTurn: 2
- **EN**: At the start of your turn, gain {StarsPerTurn:starIcons()}.
- **CN**: 在你的回合开始时，获得{StarsPerTurn:starIcons()}。

### GUARDS!!! / 护驾！！！
- **ID**: `GUARDS` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: [gold]Transform[/gold] any number of cards in your [gold]Hand[/gold] into [gold]{IfUpgraded:show:Minion Sacrifice+|Minion Sacrifice}[/gold].
- **CN**: 将你[gold]手牌[/gold]中的任意张牌[gold]变化[/gold]为[gold]{IfUpgraded:show:仆从捐躯+|仆从捐躯}[/gold]。

### Hammer Time / 锤子时间
- **ID**: `HAMMER_TIME` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **EN**: Whenever you [gold]Forge[/gold], all allies [gold]Forge[/gold] as well.
- **CN**: 每当你[gold]铸造[/gold]时，所有盟友也都[gold]铸造[/gold]相同的数值。

### Heavenly Drill / 天际钻头
- **ID**: `HEAVENLY_DRILL` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 8 | Energy: 4
- **EN**: Deal {Damage:diff()} damage X times.
Double X if it's {Energy:diff()} or more.
- **CN**: 造成{Damage:diff()}点伤害X次。
如果X的最终数值为{Energy:diff()}或以上，则将X的数值翻倍。

### Heirloom Hammer / 传承之锤
- **ID**: `HEIRLOOM_HAMMER` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 17 | Repeat: 1
- **EN**: Deal {Damage:diff()} damage.
Choose a Colorless card in your [gold]Hand[/gold]. Add a copy of that card into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
选择你[gold]手牌[/gold]中的一张无色牌。将这张牌的一张复制品放入你的[gold]手牌[/gold]。

### I Am Invincible / 所向无敌
- **ID**: `I_AM_INVINCIBLE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Block: 9
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
At the end of your turn, if this is on top of your [gold]Draw Pile[/gold], play it.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在你的回合结束时，如果这张牌位于[gold]抽牌堆[/gold]顶部，则将其打出。

### Make It So / 如此甚好
- **ID**: `MAKE_IT_SO` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 6 | Cards: 3
- **EN**: Deal {Damage:diff()} damage.
Every {Cards:diff()} {Cards:plural:Skill|Skills} you play in a turn, put this into your [gold]Hand[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
你每在一回合内打出{Cards:diff()}张技能牌，就将这张牌放入你的[gold]手牌[/gold]。

### Monarch's Gaze / 王之凝视
- **ID**: `MONARCHS_GAZE` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Values**: StrengthLoss: 1
- **EN**: Whenever you attack an enemy, it loses {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
- **CN**: 每当你攻击敌人的时候，这名敌人在本回合失去{StrengthLoss:diff()}点[gold]力量[/gold]。

### Neutron Aegis / 中子护盾
- **ID**: `NEUTRON_AEGIS` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: PlatingPower: 8
- **EN**: Gain {PlatingPower:diff()} [gold]Plating[/gold].
- **CN**: 获得{PlatingPower:diff()}层[gold]覆甲[/gold]。

### Royalties / 王国资产
- **ID**: `ROYALTIES` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Gold: 30
- **EN**: At the end of combat, gain {Gold:diff()} [gold]Gold[/gold].
- **CN**: 在战斗结束时，获得{Gold:diff()}[gold]金币[/gold]。

### Seeking Edge / 追踪之刃
- **ID**: `SEEKING_EDGE` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Forge: 7
- **EN**: [gold]Forge[/gold] {Forge:diff()}.
[gold]Sovereign Blade[/gold] now deals damage to ALL enemies.
- **CN**: [gold]铸造[/gold]{Forge:diff()}。
[gold]君王之剑[/gold]现在会对所有敌人造成伤害。

### Seven Stars / 七星
- **ID**: `SEVEN_STARS` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AllEnemies
- **Values**: Damage: 7 | Repeat: 7
- **EN**: Deal {Damage:diff()} damage to ALL enemies {Repeat:plural:{} time|{} times}.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害{Repeat}次。

### Sword Sage / 剑圣
- **ID**: `SWORD_SAGE` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: SwordSagePower: 1
- **EN**: Increase the cost of [gold]Sovereign Blade[/gold] by 1. [gold]Sovereign Blade[/gold] now hits an additional time.
- **CN**: [gold]君王之剑[/gold]的耗能加1。[gold]君王之剑[/gold]现在会额外命中一次。

### The Smith / 铸剑者
- **ID**: `THE_SMITH` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Forge: 30
- **EN**: [gold]Forge[/gold] {Forge:diff()}.
- **CN**: [gold]铸造[/gold]{Forge:diff()}。

### Tyranny / 暴政
- **ID**: `TYRANNY` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **EN**: At the start of your turn, draw 1 card and [gold]Exhaust[/gold] 1 card from your [gold]Hand[/gold].
- **CN**: 在你的回合开始时，抽一张牌，并从你的[gold]手牌[/gold]中[gold]消耗[/gold]1张牌。

### Void Form / 虚空形态
- **ID**: `VOID_FORM` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Values**: VoidFormPower: 2
- **EN**: End your turn.
The first {VoidFormPower:plural:card|{VoidFormPower:diff()} cards} you play each turn {VoidFormPower:plural:is|are} free to play.
- **CN**: 结束你的回合。
你可以免费打出每回合的前{VoidFormPower:diff()}张牌。

## Uncommon

### Alignment / 星位序列
- **ID**: `ALIGNMENT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Energy: 2
- **EN**: Gain {Energy:energyIcons()}.
- **CN**: 获得{Energy:energyIcons()}。

### Black Hole / 黑洞
- **ID**: `BLACK_HOLE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: BlackHolePower: 3
- **EN**: Whenever you spend or gain {singleStarIcon}, deal {BlackHolePower:diff()} damage to ALL enemies.
- **CN**: 每当你花费或获得{singleStarIcon}时，对所有敌人造成{BlackHolePower:diff()}点伤害。

### Bulwark / 铸墙
- **ID**: `BULWARK` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Block: 13 | Forge: 10
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Forge[/gold] {Forge:diff()}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
[gold]铸造[/gold]{Forge:diff()}。

### CHARGE!! / 冲锋！！
- **ID**: `CHARGE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 2
- **EN**: Choose {Cards:diff()} {Cards:plural:card|cards} in your [gold]Draw Pile[/gold] to [gold]Transform[/gold] into
[gold]{Cards:plural:{IfUpgraded:show:Minion Strike+|Minion Strike}[/gold]|{IfUpgraded:show:Minion Strikes+|Minion Strikes}[/gold]}.
- **CN**: 选择你[gold]抽牌堆[/gold]中的{Cards:diff()}张 牌，将其[gold]变化[/gold]为
[gold]{IfUpgraded:show:仆从打击+|仆从打击}[/gold]。

### Child of the Stars / 群星之子
- **ID**: `CHILD_OF_THE_STARS` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: BlockForStars: 2
- **EN**: Whenever you spend {singleStarIcon}, gain {BlockForStars:diff()} [gold]Block[/gold] for each {singleStarIcon} spent.
- **CN**: 每当你花费{singleStarIcon}时，每花费一点{singleStarIcon}，获得{BlockForStars:diff()}点[gold]格挡[/gold]。

### Conqueror / 征服者
- **ID**: `CONQUEROR` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Forge: 3
- **EN**: [gold]Forge[/gold] {Forge:diff()}.
[gold]Sovereign Blade[/gold] deals double damage to the enemy this turn.
- **CN**: [gold]铸造[/gold]{Forge:diff()}。
[gold]君王之剑[/gold]在本回合对敌人造成双倍伤害。

### Convergence / 汇流
- **ID**: `CONVERGENCE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Energy: 1 | Stars: 1
- **EN**: Next turn,
gain {Energy:energyIcons()} and {Stars:starIcons()}.
[gold]Retain[/gold] your [gold]Hand[/gold] this turn.
- **CN**: 在本回合[gold]保留[/gold]你的[gold]手牌[/gold]。
在下个回合，
获得{Energy:energyIcons()}与{Stars:starIcons()}。

### Devastate / 葬送
- **ID**: `DEVASTATE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 30
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Furnace / 熔炉
- **ID**: `FURNACE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Forge: 4
- **EN**: At the start of your turn, [gold]Forge[/gold] {Forge:diff()}.
- **CN**: 在你的回合开始时，[gold]铸造[/gold]{Forge:diff()}。

### Gamma Blast / 伽马爆破
- **ID**: `GAMMA_BLAST` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 13 | VulnerablePower: 2 | WeakPower: 2
- **EN**: Deal {Damage:diff()} damage.
Apply {WeakPower:diff()} [gold]Weak[/gold].
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{WeakPower:diff()}层[gold]虚弱[/gold]。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Glimmer / 微光
- **ID**: `GLIMMER` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 3 | PutBack: 1
- **EN**: Draw {Cards:diff()} {Cards:plural:card|cards}.
Put {PutBack:diff()} {PutBack:plural:card|cards} from your [gold]Hand[/gold] on top of your [gold]Draw Pile[/gold].
- **CN**: 抽{Cards:diff()}张牌。
将你[gold]手牌[/gold]中的{PutBack:diff()}张牌放到[gold]抽牌堆[/gold]顶部。

### Hegemony / 霸权
- **ID**: `HEGEMONY` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 15 | Energy: 2
- **EN**: Deal {Damage:diff()} damage.
Next turn, gain {Energy:energyIcons()}.
- **CN**: 造成{Damage:diff()}点伤害。
在下个回合获得{Energy:energyIcons()}。

### Kingly Kick / 王者之踢
- **ID**: `KINGLY_KICK` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 4 | **Target**: AnyEnemy
- **Values**: Damage: 24
- **EN**: Deal {Damage:diff()} damage.
Whenever you draw this card, reduce its cost by 1.
- **CN**: 造成{Damage:diff()}点伤害。
每当你抽到这张牌时，这张牌的耗能减少1。

### Kingly Punch / 王者之拳
- **ID**: `KINGLY_PUNCH` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 8 | Increase: 3
- **EN**: Deal {Damage:diff()} damage.
Whenever you draw this card, increase its damage by {Increase:diff()} this combat.
- **CN**: 造成{Damage:diff()}点伤害。
每当你抽到这张牌时，在这场战斗中其伤害增加{Increase:diff()}。

### Knockout Blow / 决胜一击
- **ID**: `KNOCKOUT_BLOW` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 30 | Stars: 5
- **EN**: Deal {Damage:diff()} damage.
If this kills an enemy, gain {Stars:starIcons()}.
- **CN**: 造成{Damage:diff()}点伤害。
如果此牌击杀了敌人，获得{Stars:starIcons()}。

### Largesse / 慷慨捐助
- **ID**: `LARGESSE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyAlly
- **EN**: Another player adds 1 random{IfUpgraded:show: [gold]Upgraded[/gold]} Colorless card to their [gold]Hand[/gold].
- **CN**: 将一张随机的{IfUpgraded:show:[gold]升级过的[/gold]}无色牌添加至一位其他玩家的[gold]手牌[/gold]中。

### Lunar Blast / 月面射击
- **ID**: `LUNAR_BLAST` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 4 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: Deal {Damage:diff()} damage for each Skill already played this turn.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: 本回合中每打出过一张技能牌，此牌额外造成{Damage:diff()}点伤害一次。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Manifest Authority / 君权自授
- **ID**: `MANIFEST_AUTHORITY` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 7
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Add 1 random{IfUpgraded:show: [gold]Upgraded[/gold]} Colorless card into your [gold]Hand[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将一张随机{IfUpgraded:show:[gold]升级过的[/gold]}无色牌加入你的[gold]手牌[/gold]。

### Monologue / 独白
- **ID**: `MONOLOGUE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Power: 1
- **EN**: Whenever you play a card this turn, gain {Power:diff()} [gold]Strength[/gold] this turn.
- **CN**: 每当你在本回合打出卡牌时，在本回合获得{Power:diff()}点[gold]力量[/gold]。

### Orbit / 环绕轨道
- **ID**: `ORBIT` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Energy: 1
- **EN**: Every {energyPrefix:energyIcons(4)} you spend,
gain {Energy:energyIcons()}.
- **CN**: 你每花费{energyPrefix:energyIcons(4)}，
就获得{Energy:energyIcons()}。

### Pale Blue Dot / 暗淡蓝点
- **ID**: `PALE_BLUE_DOT` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1 | CardPlay: 5
- **EN**: If you play {CardPlay} or more cards in a turn, draw {Cards:diff()} {Cards:plural:card|cards} at the start of your next turn.
- **CN**: 如果你在一回合内打出了大于等于{CardPlay}张牌，在下个回合开始时抽{Cards:diff()}张牌。

### Parry / 招架
- **ID**: `PARRY` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: ParryPower: 6
- **EN**: Whenever you play [gold]Sovereign Blade[/gold], gain {ParryPower:diff()} [gold]Block[/gold].
- **CN**: 每当你打出[gold]君王之剑[/gold]时，获得{ParryPower:diff()}点[gold]格挡[/gold]。

### Particle Wall / 粒子墙
- **ID**: `PARTICLE_WALL` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Block: 9
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Return this card to your [gold]Hand[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
将此牌返回你的[gold]手牌[/gold]。

### Pillar of Creation / 创世之柱
- **ID**: `PILLAR_OF_CREATION` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 3
- **EN**: Whenever you create a card, gain {Block:diff()} [gold]Block[/gold].
- **CN**: 每当你生成一张卡牌, 就获得{Block:diff()}点[gold]格挡[/gold]。

### Prophesize / 预言
- **ID**: `PROPHESIZE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Cards: 6
- **EN**: Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 抽{Cards:diff()}张牌。

### Quasar / 类星体
- **ID**: `QUASAR` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **EN**: Choose 1 of 3 random{IfUpgraded:show: [gold]Upgraded[/gold]|} Colorless cards to add into your [gold]Hand[/gold].
- **CN**: 从3张随机{IfUpgraded:show:[gold]升级过[/gold]的|}无色牌中选择1张加入你的[gold]手牌[/gold]。

### Radiate / 辐射
- **ID**: `RADIATE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AllEnemies
- **Values**: Damage: 3 | Stars: 1 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: Deal {Damage:diff()} damage to ALL enemies for each {Stars:starIcons()} gained this turn.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}
- **CN**: 你在本回合每获得一点{Stars:starIcons()}，则此牌就所有敌人造成{Damage:diff()}点伤害一次。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Reflect / 倒映
- **ID**: `REFLECT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 17
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Blocked attack damage is reflected to your attacker this turn.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在本回合将你格挡掉的攻击伤害反弹给攻击者。

### Resonance / 共鸣
- **ID**: `RESONANCE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AllEnemies
- **Values**: StrengthPower: 1
- **EN**: Gain {StrengthPower:diff()} [gold]Strength[/gold]. ALL enemies lose 1 [gold]Strength[/gold].
- **CN**: 获得{StrengthPower:diff()}点[gold]力量[/gold]。所有敌人失去1点[gold]力量[/gold]。

### Royal Gamble / 胜券在王
- **ID**: `ROYAL_GAMBLE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Stars: 9
- **EN**: Gain {Stars:diff()} {singleStarIcon}.
- **CN**: 获得{Stars:diff()}{singleStarIcon}。

### Shining Strike / 明耀打击
- **ID**: `SHINING_STRIKE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 8 | Stars: 2
- **EN**: Deal {Damage:diff()} damage.
Gain {Stars:starIcons()}.
Put this card on top of your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
获得{Stars:starIcons()}。
将这张牌放置于你的[gold]抽牌堆[/gold]顶部。

### Spectrum Shift / 光谱偏移
- **ID**: `SPECTRUM_SHIFT` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Cards: 1
- **EN**: At the start of your turn, add {Cards:diff()} random Colorless {Cards:plural:card|cards} into your [gold]Hand[/gold].
- **CN**: 在你的回合开始时，将{Cards:diff()}张随机无色牌添加到你的[gold]手牌[/gold]中。

### Stardust / 星尘
- **ID**: `STARDUST` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: RandomEnemy
- **Values**: Damage: 5
- **EN**: Deal {Damage:diff()} damage to a random enemy X times.
- **CN**: 随机对敌人造成{Damage:diff()}点伤害X次。

### Summon Forth / 征召上前
- **ID**: `SUMMON_FORTH` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Forge: 8
- **EN**: [gold]Forge[/gold] {Forge:diff()}.
Put [gold]Sovereign Blade[/gold] into your [gold]Hand[/gold] from anywhere.
- **CN**: [gold]铸造[/gold]{Forge:diff()}。
不论何处，将[gold]君王之剑[/gold]放入你的[gold]手牌[/gold]。

### Supermassive / 超质量体
- **ID**: `SUPERMASSIVE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 5 | ExtraDamage: 3 | CalculatedDamage: 5
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each card you created this combat.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
你在本场战斗中每生成过一张牌，这张牌就额外造成{ExtraDamage:diff()}点伤害。

### Terraforming / 地形改造
- **ID**: `TERRAFORMING` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: VigorPower: 6
- **EN**: Gain {VigorPower:diff()} [gold]Vigor[/gold].
- **CN**: 获得{VigorPower:diff()}点[gold]活力[/gold]。

