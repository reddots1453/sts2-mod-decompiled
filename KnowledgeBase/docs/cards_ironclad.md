# IRONCLAD Cards

## Ancient

### Break / 破击
- **ID**: `BREAK` | **Type**: Attack | **Rarity**: Ancient | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 20 | VulnerablePower: 5
- **EN**: Deal {Damage:diff()} damage.
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Corruption / 腐化
- **ID**: `CORRUPTION` | **Type**: Power | **Rarity**: Ancient | **Cost**: 3 | **Target**: Self
- **Values**: Power: 1
- **EN**: Skills cost 0 {energyPrefix:energyIcons(1)}.
Whenever you play a Skill, [gold]Exhaust[/gold] it.
- **CN**: 技能牌消耗变为0{energyPrefix:energyIcons(1)}。
每当你打出一张技能牌时，将其[gold]消耗[/gold]。

## Basic

### Bash / 痛击
- **ID**: `BASH` | **Type**: Attack | **Rarity**: Basic | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 8 | VulnerablePower: 2
- **EN**: Deal {Damage:diff()} damage.
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Defend / 防御
- **ID**: `DEFEND_IRONCLAD` | **Type**: Skill | **Rarity**: Basic | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Strike / 打击
- **ID**: `STRIKE_IRONCLAD` | **Type**: Attack | **Rarity**: Basic | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

## Common

### Anger / 愤怒
- **ID**: `ANGER` | **Type**: Attack | **Rarity**: Common | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
Add a copy of this card into your [gold]Discard Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将一张此牌的复制品加入你的[gold]弃牌堆[/gold]。

### Armaments / 武装
- **ID**: `ARMAMENTS` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Upgrade[/gold] {IfUpgraded:show:ALL cards|a card} in your [gold]Hand[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
[gold]升级[/gold]你[gold]手牌[/gold]中的{IfUpgraded:show:所有牌|一张牌}。

### Blood Wall / 血墙
- **ID**: `BLOOD_WALL` | **Type**: Skill | **Rarity**: Common | **Cost**: 2 | **Target**: Self
- **Values**: HpLoss: 2 | Block: 16
- **EN**: Lose {HpLoss:diff()} HP.
Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 失去{HpLoss:diff()}点生命。
获得{Block:diff()}点[gold]格挡[/gold]。

### Bloodletting / 放血
- **ID**: `BLOODLETTING` | **Type**: Skill | **Rarity**: Common | **Cost**: 0 | **Target**: Self
- **Values**: HpLoss: 3 | Energy: 2
- **EN**: Lose {HpLoss:diff()} HP.
Gain {Energy:energyIcons()}.
- **CN**: 失去{HpLoss:diff()}点生命。
获得{Energy:energyIcons()}。

### Body Slam / 全身撞击
- **ID**: `BODY_SLAM` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 0 | ExtraDamage: 1 | CalculatedDamage: 0
- **EN**: Deal damage equal to your [gold]Block[/gold].{InCombat:
(Deals {CalculatedDamage:diff()} damage)|}
- **CN**: 造成你当前[gold]格挡[/gold]值的伤害。{InCombat:
（造成{CalculatedDamage:diff()}点伤害）|}

### Breakthrough / 突破
- **ID**: `BREAKTHROUGH` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 9 | HpLoss: 1
- **EN**: Lose {HpLoss:diff()} HP.
Deal {Damage:diff()} damage to ALL enemies.
- **CN**: 失去{HpLoss:diff()}点生命。
对所有敌人造成{Damage:diff()}点伤害。

### Cinder / 余烬
- **ID**: `CINDER` | **Type**: Attack | **Rarity**: Common | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 17 | CardsToExhaust: 1
- **EN**: Deal {Damage:diff()} damage.
[gold]Exhaust[/gold] the top card of your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
[gold]消耗[/gold]你的[gold]抽牌堆[/gold]顶部的牌。

### Havoc / 破灭
- **ID**: `HAVOC` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **EN**: Play the top card of your [gold]Draw Pile[/gold] and [gold]Exhaust[/gold] it.
- **CN**: 打出[gold]抽牌堆[/gold]顶部的牌并将其[gold]消耗[/gold]。

### Headbutt / 头槌
- **ID**: `HEADBUTT` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 9
- **EN**: Deal {Damage:diff()} damage.
Put a card from your [gold]Discard Pile[/gold] on top of your [gold]Draw Pile[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将你[gold]弃牌堆[/gold]中的一张牌放到[gold]抽牌堆[/gold]顶部。

### Iron Wave / 铁斩波
- **ID**: `IRON_WAVE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 5 | Block: 5
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Deal {Damage:diff()} damage.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
造成{Damage:diff()}点伤害。

### Molten Fist / 熔融之拳
- **ID**: `MOLTEN_FIST` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 10
- **EN**: Deal {Damage:diff()} damage.
Double the enemy's [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
将该敌人身上的[gold]易伤[/gold]层数翻倍。

### Perfected Strike / 完美打击
- **ID**: `PERFECTED_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: CalculationBase: 6 | ExtraDamage: 2 | CalculatedDamage: 6
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for ALL your cards containing “Strike”.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
你每有一张名字中含有“打击”的牌，伤害+{ExtraDamage:diff()}。

### Pommel Strike / 剑柄打击
- **ID**: `POMMEL_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 9 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
Draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 造成{Damage:diff()}点伤害。
抽{Cards:diff()}张牌。

### Setup Strike / 预备打击
- **ID**: `SETUP_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7 | StrengthPower: 2
- **EN**: Deal {Damage:diff()} damage.
Gain {StrengthPower:diff()} [gold]Strength[/gold] this turn.
- **CN**: 造成{Damage:diff()}点伤害。
在本回合内获得{StrengthPower:diff()}点[gold]力量[/gold]。

### Shrug It Off / 耸肩无视
- **ID**: `SHRUG_IT_OFF` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 8 | Cards: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Draw {Cards:diff()} card.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
抽{Cards:diff()}张牌。

### Sword Boomerang / 飞剑回旋镖
- **ID**: `SWORD_BOOMERANG` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: RandomEnemy
- **Values**: Damage: 3 | Repeat: 3
- **EN**: Deal {Damage:diff()} damage to a random enemy {Repeat:diff()} times.
- **CN**: 随机对敌人造成{Damage:diff()}点伤害{Repeat:diff()}次。

### Thunderclap / 闪电霹雳
- **ID**: `THUNDERCLAP` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AllEnemies
- **Values**: Damage: 4 | VulnerablePower: 1
- **EN**: Deal {Damage:diff()} damage and apply {VulnerablePower:diff()} [gold]Vulnerable[/gold] to ALL enemies.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害，给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Tremble / 战栗
- **ID**: `TREMBLE` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: VulnerablePower: 2
- **EN**: Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### True Grit / 坚毅
- **ID**: `TRUE_GRIT` | **Type**: Skill | **Rarity**: Common | **Cost**: 1 | **Target**: Self
- **Values**: Block: 7
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
[gold]Exhaust[/gold] 1 card{IfUpgraded:show:| at random}.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
{IfUpgraded:show:| 随机}[gold]消耗[/gold]1张牌。

### Twin Strike / 双重打击
- **ID**: `TWIN_STRIKE` | **Type**: Attack | **Rarity**: Common | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 5
- **EN**: Deal {Damage:diff()} damage twice.
- **CN**: 造成{Damage:diff()}点伤害两次。

## Rare

### Aggression / 好勇斗狠
- **ID**: `AGGRESSION` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **EN**: At the start of your turn, put a random Attack from your [gold]Discard Pile[/gold] into your [gold]Hand[/gold] and [gold]Upgrade[/gold] it.
- **CN**: 在你的回合开始时，将你[gold]弃牌堆[/gold]的一张随机攻击牌放入你的[gold]手牌[/gold]并将其[gold]升级[/gold]。

### Barricade / 壁垒
- **ID**: `BARRICADE` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **EN**: [gold]Block[/gold] is not removed at the start of your turn.
- **CN**: [gold]格挡[/gold]不再在你的回合开始时消失。

### Brand / 烙印
- **ID**: `BRAND` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Values**: HpLoss: 1 | StrengthPower: 1
- **EN**: Lose {HpLoss:diff()} HP.
[gold]Exhaust[/gold] 1 card.
Gain {StrengthPower:diff()} [gold]Strength[/gold].
- **CN**: 失去{HpLoss:diff()}点生命。
[gold]消耗[/gold]1张牌。
获得{StrengthPower:diff()}点[gold]力量[/gold]。

### Cascade / 倾泻
- **ID**: `CASCADE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **EN**: Play the top X{IfUpgraded:show:+1} cards of your [gold]Draw Pile[/gold].
- **CN**: 打出你[gold]抽牌堆[/gold]顶部的X{IfUpgraded:show:+1}张牌。

### Colossus / 巨像
- **ID**: `COLOSSUS` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5 | Colossus: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
You receive 50% less damage from [gold]Vulnerable[/gold] enemies this turn.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
在本回合中，有[gold]易伤[/gold]状态的敌人对你造成的伤害降低50%。

### Conflagration / 焚烧
- **ID**: `CONFLAGRATION` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AllEnemies
- **Values**: CalculationBase: 8 | ExtraDamage: 2 | CalculatedDamage: 8
- **EN**: Deal {CalculatedDamage:diff()} damage to ALL enemies.
Deals {ExtraDamage:diff()} additional damage for each other Attack you've played this turn.
- **CN**: 对所有敌人造成{CalculatedDamage:diff()}点伤害。
你在本回合中每打出过一张其他攻击牌，这张牌的伤害就提升{ExtraDamage:diff()}点。

### Crimson Mantle / 绯红披风
- **ID**: `CRIMSON_MANTLE` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: CrimsonMantlePower: 8
- **EN**: At the start of your turn, lose 1 HP and gain {CrimsonMantlePower:diff()} [gold]Block[/gold].
- **CN**: 在你的回合开始时，失去1点生命并获得{CrimsonMantlePower:diff()}点[gold]格挡[/gold]。

### Cruelty / 残酷
- **ID**: `CRUELTY` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: CrueltyPower: 25
- **EN**: [gold]Vulnerable[/gold] enemies take an additional {CrueltyPower:diff()}% damage.
- **CN**: 有[gold]易伤[/gold]状态的敌人额外受到{CrueltyPower:diff()}%的伤害。

### Dark Embrace / 黑暗之拥
- **ID**: `DARK_EMBRACE` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **EN**: Whenever a card is [gold]Exhausted[/gold],
draw 1 card.
- **CN**: 每当有一张牌被[gold]消耗[/gold]时，
抽1张牌。

### Demon Form / 恶魔形态
- **ID**: `DEMON_FORM` | **Type**: Power | **Rarity**: Rare | **Cost**: 3 | **Target**: Self
- **Values**: StrengthPower: 2
- **EN**: At the start of your turn, gain {StrengthPower:diff()} [gold]Strength[/gold].
- **CN**: 在你的回合开始时，获得{StrengthPower:diff()}点[gold]力量[/gold]。

### Feed / 狂宴
- **ID**: `FEED` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 10 | MaxHp: 3
- **EN**: Deal {Damage:diff()} damage.
If [gold]Fatal[/gold], raise your Max HP by {MaxHp:diff()}.
- **CN**: 造成{Damage:diff()}点伤害。
[gold]斩杀[/gold]时，永久获得{MaxHp:diff()}点最大生命值。

### Fiend Fire / 恶魔之焰
- **ID**: `FIEND_FIRE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: Damage: 7
- **EN**: [gold]Exhaust[/gold] your [gold]Hand[/gold].
Deal {Damage:diff()} damage for each card [gold]Exhausted[/gold].
- **CN**: [gold]消耗[/gold]所有[gold]手牌[/gold]。
每张被[gold]消耗[/gold]的牌造成{Damage:diff()}点伤害。

### Hellraiser / 地狱狂徒
- **ID**: `HELLRAISER` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **EN**: Whenever you draw a card containing “Strike”, it is played against a random enemy.
- **CN**: 每当你抽到名字中有“打击”的牌时，对一名随机敌人打出这张牌。

### Impervious / 岿然不动
- **ID**: `IMPERVIOUS` | **Type**: Skill | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: Block: 30
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。

### Juggernaut / 势不可当
- **ID**: `JUGGERNAUT` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: JuggernautPower: 5
- **EN**: Whenever you gain [gold]Block[/gold], deal {JuggernautPower:diff()} damage to a random enemy.
- **CN**: 每当你获得[gold]格挡[/gold]时，对随机敌人造成{JuggernautPower:diff()}点伤害。

### Mangle / 凌虐
- **ID**: `MANGLE` | **Type**: Attack | **Rarity**: Rare | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 15 | StrengthLoss: 10
- **EN**: Deal {Damage:diff()} damage.
Enemy loses {StrengthLoss:diff()} [gold]Strength[/gold] this turn.
- **CN**: 造成{Damage:diff()}点伤害。
敌人在本回合失去{StrengthLoss:diff()}点[gold]力量[/gold]。

### Offering / 祭品
- **ID**: `OFFERING` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **Keywords**: Exhaust
- **Values**: HpLoss: 6 | Energy: 2 | Cards: 3
- **EN**: Lose {HpLoss:diff()} HP.
Gain {Energy:energyIcons()}.
Draw {Cards:diff()} cards.
- **CN**: 失去{HpLoss:diff()}点生命。
获得{Energy:energyIcons()}。
抽{Cards:diff()}张牌。

### One-Two Punch / 连环拳
- **ID**: `ONE_TWO_PUNCH` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Values**: Attacks: 1
- **EN**: This turn, your next {Attacks:cond:>1?{Attacks:diff()} Attacks are|Attack is} played an extra time.
- **CN**: 在这个回合，你打出的下{Attacks:diff()}张攻击牌会被额外打出一次。

### Pact's End / 契约终结
- **ID**: `PACTS_END` | **Type**: Attack | **Rarity**: Rare | **Cost**: 0 | **Target**: AllEnemies
- **Values**: Damage: 17 | Cards: 3
- **EN**: Can only be played if you have {Cards:diff()} or more {Cards:plural:card|cards} in your [gold]Exhaust Pile[/gold].
Deal {Damage:diff()} damage to ALL enemies.
- **CN**: 只有在你的[gold]消耗牌堆[/gold]拥有大于等于{Cards:diff()}张牌的时候才能被打出。
对所有敌人造成{Damage:diff()}点伤害。

### Primal Force / 原始力量
- **ID**: `PRIMAL_FORCE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 0 | **Target**: Self
- **EN**: Transform all Attacks in your [gold]Hand[/gold] into [gold]{IfUpgraded:show:Giant Rock+|Giant Rock}[/gold].
- **CN**: 将[gold]手牌[/gold]中的所有攻击牌变化为[gold]{IfUpgraded:show:巨石+|巨石}[/gold]。

### Pyre / 薪火之源
- **ID**: `PYRE` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **Values**: Energy: 1
- **EN**: Gain {Energy:energyIcons()} at the start of each turn.
- **CN**: 在回合开始时，获得{Energy:energyIcons()}。

### Stoke / 添柴
- **ID**: `STOKE` | **Type**: Skill | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: [gold]Exhaust[/gold] your [gold]Hand[/gold].
Draw a card for each card [gold]Exhausted[/gold].
- **CN**: [gold]消耗[/gold]所有[gold]手牌[/gold]。
然后抽相同数量张牌。

### Tank / 肉盾
- **ID**: `TANK` | **Type**: Power | **Rarity**: Rare | **Cost**: 1 | **Target**: Self
- **EN**: Take double damage from enemies.
Allies take half damage from enemies.
- **CN**: 承受双倍来自敌人的伤害。
敌人的伤害对盟友减半。

### Tear Asunder / 扯碎
- **ID**: `TEAR_ASUNDER` | **Type**: Attack | **Rarity**: Rare | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 5 | Repeat: 1 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedHits: 0
- **EN**: Deal {Damage:diff()} damage.
Hits an additional time for each time you lost HP this combat.{InCombat:
(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times}.)|}
- **CN**: 造成{Damage:diff()}点伤害。
在本场战斗中，你每失去过一次生命值，这张牌就额外造成一次伤害。{InCombat:
（命中{CalculatedHits:diff()}次）|}

### Thrash / 痛殴
- **ID**: `THRASH` | **Type**: Attack | **Rarity**: Rare | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 4
- **EN**: Deal {Damage:diff()} damage twice.
[gold]Exhaust[/gold] a random Attack in your [gold]Hand[/gold] and add its damage to this card.
- **CN**: 造成{Damage:diff()}点伤害两次。
[gold]消耗[/gold]你的[gold]手牌[/gold]中随机一张攻击牌，并将它的伤害添加给这张牌。

### Unmovable / 坚定不移
- **ID**: `UNMOVABLE` | **Type**: Power | **Rarity**: Rare | **Cost**: 2 | **Target**: Self
- **EN**: The first time you gain [gold]Block[/gold] from a card each turn, double the amount gained.
- **CN**: 翻倍你每回合第一次从卡牌中获得的[gold]格挡[/gold]。

## Uncommon

### Ashen Strike / 灰烬打击
- **ID**: `ASHEN_STRIKE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: CalculationBase: 6 | ExtraDamage: 3 | CalculatedDamage: 6
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each card in your [gold]Exhaust Pile[/gold].
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
你的[gold]消耗牌堆[/gold]中每有一张牌，伤害增加{ExtraDamage:diff()}。

### Battle Trance / 战斗专注
- **ID**: `BATTLE_TRANCE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Cards: 3
- **EN**: Draw {Cards:diff()} cards.
You cannot draw additional cards this turn.
- **CN**: 抽{Cards:diff()}张牌。
你在本回合内不能再抽任何牌。

### Bludgeon / 重锤
- **ID**: `BLUDGEON` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 3 | **Target**: AnyEnemy
- **Values**: Damage: 32
- **EN**: Deal {Damage:diff()} damage.
- **CN**: 造成{Damage:diff()}点伤害。

### Bully / 欺凌
- **ID**: `BULLY` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: CalculationBase: 4 | ExtraDamage: 2 | CalculatedDamage: 4
- **EN**: Deal {CalculatedDamage:diff()} damage.
Deals {ExtraDamage:diff()} additional damage for each [gold]Vulnerable[/gold] on the enemy.
- **CN**: 造成{CalculatedDamage:diff()}点伤害。
该敌人身上每有一层[gold]易伤[/gold]就额外造成{ExtraDamage:diff()}点伤害。

### Burning Pact / 燃烧契约
- **ID**: `BURNING_PACT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 2
- **EN**: [gold]Exhaust[/gold] 1 card.
Draw {Cards:diff()} cards.
- **CN**: [gold]消耗[/gold]1张牌。
抽{Cards:diff()}张牌。

### Demonic Shield / 恶魔护盾
- **ID**: `DEMONIC_SHIELD` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyAlly
- **Keywords**: Exhaust
- **Values**: CalculationBase: 0 | HpLoss: 1 | CalculationExtra: 1 | CalculatedBlock: 0
- **EN**: Lose {HpLoss:diff()} HP.
Give another player [gold]Block[/gold] equal to your [gold]Block[/gold].{InCombat:
(Gives {CalculatedBlock:diff()} [gold]Block[/gold])|}
- **CN**: 失去{HpLoss:diff()}点生命。
给予另一位玩家你当前[gold]格挡[/gold]值的[gold]格挡[/gold]。{InCombat:
（给予{CalculatedBlock:diff()}点[gold]格挡[/gold]）|}

### Dismantle / 拆卸
- **ID**: `DISMANTLE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 8
- **EN**: Deal {Damage:diff()} damage.
If the enemy is [gold]Vulnerable[/gold], hits twice.
- **CN**: 造成{Damage:diff()}点伤害。
如果该敌人有[gold]易伤[/gold]状态，则攻击两次。

### Dominate / 主宰
- **ID**: `DOMINATE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Keywords**: Exhaust
- **Values**: StrengthPerVulnerable: 1
- **EN**: Gain {StrengthPerVulnerable:diff()} [gold]Strength[/gold] for each [gold]Vulnerable[/gold] on the enemy.
- **CN**: 敌人身上每有一层[gold]易伤[/gold]，就获得{StrengthPerVulnerable:diff()}点[gold]力量[/gold]。

### Drum of Battle / 战鼓
- **ID**: `DRUM_OF_BATTLE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Cards: 2 | DrumOfBattlePower: 1
- **EN**: Draw {Cards:diff()} cards.
At the start of your turn, [gold]Exhaust[/gold] the top card of your [gold]Draw Pile[/gold].
- **CN**: 抽{Cards:diff()}张牌。
在你的回合开始时，[gold]消耗[/gold]你的[gold]抽牌堆[/gold]顶部的牌。

### Evil Eye / 邪眼
- **ID**: `EVIL_EYE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 8
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Gain another {Block:diff()} [gold]Block[/gold] if you have [gold]Exhausted[/gold] a card this turn.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
如果你在本回合[gold]消耗[/gold]过卡牌，则额外获得{Block:diff()}点[gold]格挡[/gold]。

### Expect a Fight / 跃跃欲试
- **ID**: `EXPECT_A_FIGHT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Energy: 0 | CalculationBase: 0 | CalculationExtra: 1 | CalculatedEnergy: 0
- **EN**: Gain {energyPrefix:energyIcons(1)} for each Attack in your [gold]Hand[/gold].{InCombat:
(Gain {CalculatedEnergy:energyIcons()}.)|}
- **CN**: 你的[gold]手牌[/gold]中每有一张攻击牌，就会获得{energyPrefix:energyIcons(1)}。{InCombat:
（获得{CalculatedEnergy:energyIcons()}。）|}

### Feel No Pain / 无惧疼痛
- **ID**: `FEEL_NO_PAIN` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Power: 3
- **EN**: Whenever a card is [gold]Exhausted[/gold], gain {Power:diff()} [gold]Block[/gold].
- **CN**: 每当有一张牌被[gold]消耗[/gold]时，获得{Power:diff()}点[gold]格挡[/gold]。

### Fight Me! / 与我一战！
- **ID**: `FIGHT_ME` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 5 | Repeat: 2 | StrengthPower: 2 | EnemyStrength: 1
- **EN**: Deal {Damage:diff()} damage twice.
Gain {StrengthPower:diff()} [gold]Strength[/gold].
The enemy gains {EnemyStrength:diff()} [gold]Strength[/gold].
- **CN**: 造成{Damage:diff()}点伤害两次。
获得{StrengthPower:diff()}点[gold]力量[/gold]。
该敌人获得{EnemyStrength:diff()}点[gold]力量[/gold]。

### Flame Barrier / 火焰屏障
- **ID**: `FLAME_BARRIER` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Block: 12 | DamageBack: 4
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Whenever you are attacked this turn, deal {DamageBack:diff()} damage back.
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
你在这个回合每受到一次攻击，都会对攻击者造成{DamageBack:diff()}点伤害。

### Forgotten Ritual / 被遗忘的仪式
- **ID**: `FORGOTTEN_RITUAL` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Energy: 3
- **EN**: If you [gold]Exhausted[/gold] a card this turn, gain {Energy:energyIcons()}.
- **CN**: 如果你在本回合[gold]消耗过[/gold]卡牌，则获得{Energy:energyIcons()}。

### Grapple / 擒拿
- **ID**: `GRAPPLE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 7 | GrapplePower: 5
- **EN**: Deal {Damage:diff()} damage.
Whenever you gain [gold]Block[/gold] this turn, deal {GrapplePower:diff()} damage to the enemy.
- **CN**: 造成{Damage:diff()}点伤害。
当你在本回合获得[gold]格挡[/gold]时，对该敌人造成{GrapplePower:diff()}点伤害。

### Hemokinesis / 御血术
- **ID**: `HEMOKINESIS` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: HpLoss: 2 | Damage: 14
- **EN**: Lose {HpLoss:diff()} HP.
Deal {Damage:diff()} damage.
- **CN**: 失去{HpLoss:diff()}点生命。
造成{Damage:diff()}点伤害。

### Howl from Beyond / 彼岸咆哮
- **ID**: `HOWL_FROM_BEYOND` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 3 | **Target**: AllEnemies
- **Values**: Damage: 16
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
At the start of your turn, plays from the [gold]Exhaust Pile[/gold].
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
在你的回合开始时，从[gold]消耗牌堆[/gold]将此牌打出。

### Infernal Blade / 地狱之刃
- **ID**: `INFERNAL_BLADE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Keywords**: Exhaust
- **EN**: Add a random Attack into your [gold]Hand[/gold]. It's free to play this turn.
- **CN**: 将一张随机攻击牌加入你的[gold]手牌[/gold]。这张牌在本回合内可以免费打出。

### Inferno / 狱火
- **ID**: `INFERNO` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: InfernoPower: 6
- **EN**: At the start of your turn, lose 1 HP.
Whenever you lose HP on your turn, deal {InfernoPower:diff()} damage to ALL enemies.
- **CN**: 在你的回合开始时，失去1点生命。
每当你在你的回合内失去生命时，对所有敌人造成{InfernoPower:diff()}点伤害。

### Inflame / 燃烧
- **ID**: `INFLAME` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: StrengthPower: 2
- **EN**: Gain {StrengthPower:diff()} [gold]Strength[/gold].
- **CN**: 获得{StrengthPower:diff()}点[gold]力量[/gold]。

### Juggling / 杂耍
- **ID**: `JUGGLING` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **EN**: Add a copy of the third [gold]Attack[/gold] you play each turn into your [gold]Hand[/gold].
- **CN**: 将你在每回合打出的第三张[gold]攻击牌[/gold]的复制品加入你的[gold]手牌[/gold]。

### Pillage / 劫掠
- **ID**: `PILLAGE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 6
- **EN**: Deal {Damage:diff()} damage.
Draw cards until you draw a non-Attack card.
- **CN**: 造成{Damage:diff()}点伤害。
抽牌直到你抽到一张非攻击牌。

### Rage / 狂怒
- **ID**: `RAGE` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 0 | **Target**: Self
- **Values**: Power: 3
- **EN**: Whenever you play an Attack this turn, gain {Power:diff()} [gold]Block[/gold].
- **CN**: 打出此牌后，你在这个回合内每打出一张攻击牌，获得{Power:diff()}点[gold]格挡[/gold]。

### Rampage / 暴走
- **ID**: `RAMPAGE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Damage: 9 | Increase: 5
- **EN**: Deal {Damage:diff()} damage.
Increase this card's damage by {Increase:diff()} this combat.
- **CN**: 造成{Damage:diff()}点伤害。
将这张牌在本场战斗中的伤害增加{Increase:diff()}。

### Rupture / 撕裂
- **ID**: `RUPTURE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: StrengthPower: 1
- **EN**: Whenever you lose HP on your turn, gain {StrengthPower:diff()} [gold]Strength[/gold].
- **CN**: 每当你在你的回合失去生命值时, 获得{StrengthPower:diff()}点[gold]力量[/gold]。

### Second Wind / 重振精神
- **ID**: `SECOND_WIND` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Block: 5
- **EN**: [gold]Exhaust[/gold] all non-Attack cards in your [gold]Hand[/gold]. Gain {Block:diff()} [gold]Block[/gold] for each card [gold]Exhausted[/gold].
- **CN**: [gold]消耗[/gold][gold]手牌[/gold]中所有非攻击牌，每张获得{Block:diff()}点[gold]格挡[/gold]。

### Spite / 怨恨
- **ID**: `SPITE` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AnyEnemy
- **Values**: Damage: 6 | Cards: 1
- **EN**: Deal {Damage:diff()} damage.
If you lost HP this turn, draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 造成{Damage:diff()}点伤害。
如果你在本回合失去过生命值，则抽{Cards:diff()}张牌。

### Stampede / 惊逃
- **ID**: `STAMPEDE` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 2 | **Target**: Self
- **Values**: Power: 1
- **EN**: At the end of your turn, {Power:diff()} random {Power:plural:Attack in your [gold]Hand[/gold] is played against a random enemy.|Attacks in your [gold]Hand[/gold] are played against random enemies.}
- **CN**: 在你的回合结束时，随机打出你[gold]手牌[/gold]中的{Power:diff()}张攻击牌攻击随机敌人。

### Stomp / 踩踏
- **ID**: `STOMP` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 3 | **Target**: AllEnemies
- **Values**: Damage: 12
- **EN**: Deal {Damage:diff()} damage to ALL enemies.
Costs 1 less {energyPrefix:energyIcons(1)} for each Attack played this turn.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害。
你在本回合中每打出过一张攻击牌，其耗能减少1{energyPrefix:energyIcons(1)}。

### Stone Armor / 岩石铠甲
- **ID**: `STONE_ARMOR` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: PlatingPower: 4
- **EN**: Gain {PlatingPower:diff()} [gold]Plating[/gold].
- **CN**: 获得{PlatingPower:diff()}层[gold]覆甲[/gold]。

### Taunt / 挑衅
- **ID**: `TAUNT` | **Type**: Skill | **Rarity**: Uncommon | **Cost**: 1 | **Target**: AnyEnemy
- **Values**: Block: 7 | VulnerablePower: 1
- **EN**: Gain {Block:diff()} [gold]Block[/gold].
Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].
- **CN**: 获得{Block:diff()}点[gold]格挡[/gold]。
给予{VulnerablePower:diff()}层[gold]易伤[/gold]。

### Unrelenting / 无情猛攻
- **ID**: `UNRELENTING` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 12
- **EN**: Deal {Damage:diff()} damage.
The next Attack you play costs 0 {energyPrefix:energyIcons(1)}.
- **CN**: 造成{Damage:diff()}点伤害。
你打出的下一张攻击牌耗能变为0{energyPrefix:energyIcons(1)}。

### Uppercut / 上勾拳
- **ID**: `UPPERCUT` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 2 | **Target**: AnyEnemy
- **Values**: Damage: 13 | Power: 1
- **EN**: Deal {Damage:diff()} damage.
Apply {Power:diff()} [gold]Weak[/gold].
Apply {Power:diff()} [gold]Vulnerable[/gold].
- **CN**: 造成{Damage:diff()}点伤害。
给予{Power:diff()}层[gold]虚弱[/gold]。
给予{Power:diff()}层[gold]易伤[/gold]。

### Vicious / 凶恶
- **ID**: `VICIOUS` | **Type**: Power | **Rarity**: Uncommon | **Cost**: 1 | **Target**: Self
- **Values**: Cards: 1
- **EN**: Whenever you apply [gold]Vulnerable[/gold], draw {Cards:diff()} {Cards:plural:card|cards}.
- **CN**: 每当你给予[gold]易伤[/gold]时，抽{Cards:diff()}张牌。

### Whirlwind / 旋风斩
- **ID**: `WHIRLWIND` | **Type**: Attack | **Rarity**: Uncommon | **Cost**: 0 | **Target**: AllEnemies
- **Values**: Damage: 5
- **EN**: Deal {Damage:diff()} damage to ALL enemies X times.
- **CN**: 对所有敌人造成{Damage:diff()}点伤害X次。

