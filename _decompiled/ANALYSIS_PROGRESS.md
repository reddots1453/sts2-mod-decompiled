# Slay the Spire 2 反编译分析进度

## 基本信息
- **游戏版本**: v0.99.1 (commit 7ac1f450, 2026-03-13)
- **开发商**: MegaCrit
- **反编译工具**: ILSpy 9.1.0 (ilspycmd, 需 DOTNET_ROLL_FORWARD=LatestMajor)
- **反编译源**: `data_sts2_windows_x86_64/sts2.dll` (8.5MB)
- **反编译输出**: `_decompiled/sts2/` (3304 个 C# 文件)
- **未解包**: `SlayTheSpire2.pck` (1.7GB Godot 资源包，包含场景/着色器/美术资源)

## 技术栈
| 组件 | 技术 | 对应文件 |
|------|------|----------|
| 游戏引擎 | Godot Engine | SlayTheSpire2.exe, .pck |
| 编程语言 | C# (.NET) | sts2.dll, GodotSharp.dll |
| 音频 | FMOD | fmod.dll, fmodstudio.dll, libGodotFmod |
| 骨骼动画 | Spine | libspine_godot |
| Steam集成 | Steamworks.NET | Steamworks.NET.dll |
| 错误上报 | Sentry | Sentry.dll, libsentry |
| Mod支持 | Harmony + MonoMod | 0Harmony.dll, MonoMod.*.dll |
| 图形API | D3D12/Vulkan/OpenGL | launch_d3d12.bat 等 |
| 本地化 | SmartFormat | SmartFormat.dll |
| 序列化 | System.Text.Json | .NET 内置 |
| 图形底层 | Vortice (DirectX绑定) | Vortice.*.dll |

## 命名空间/模块结构 (根命名空间: MegaCrit.Sts2.Core)

### 核心系统
- **Context** — 游戏上下文/全局状态管理
- **Runs** — 单局游戏(Run)管理
- **Runs.History** — 历史记录
- **Runs.Metrics** — 数据指标收集 ⭐ (待深入分析)
- **Saves** — 存档系统 (含 Managers, Migrations, Runs, Validation)
- **Settings** — 游戏设置
- **Random** — 随机数系统(RNG)
- **Factories** — 工厂模式创建实体

### 实体系统 (三层架构: Model → Entity → Node)
- **Models.Cards / Entities.Cards / Nodes.Cards** — 卡牌
- **Models.Relics / Entities.Relics / Nodes.Relics** — 遗物
- **Models.Potions / Entities.Potions / Nodes.Potions** — 药水
- **Models.Powers / Entities.Powers** — 能力(Buff/Debuff)
- **Models.Monsters / Entities.Creatures** — 怪物/生物
- **Models.Encounters / Entities.Encounters** — 遭遇战
- **Models.Events / Events** — 事件
- **Models.Orbs / Entities.Orbs / Nodes.Orbs** — 球(Orb)
- **Models.Enchantments / Entities.Enchantments** — 附魔(新机制)
- **Models.Characters / Entities.Characters** — 角色
- **Models.Afflictions** — 苦难(新机制?)
- **Models.CardPools / PotionPools / RelicPools** — 对象池配置
- **Entities.Ascension** — 升阶系统
- **Entities.Gold** — 金币
- **Entities.Intents** — 怪物意图
- **Entities.Merchant** — 商店
- **Entities.RestSite** — 篝火
- **Entities.Rewards** — 奖励
- **Entities.CardRewardAlternatives** — 卡牌奖励替代选项
- **Entities.TreasureRelicPicking** — 宝箱遗物选择

### 战斗系统
- **Combat** — 战斗核心逻辑
- **Combat.History** — 战斗历史记录
- **Combat.History.Entries** — 历史条目
- **GameActions** — 游戏动作(Action队列系统)
- **GameActions.Multiplayer** — 多人动作
- **Commands** — 命令模式
- **Commands.Builders** — 命令构建器
- **MonsterMoves** — 怪物行动
- **MonsterMoves.Intents** — 怪物意图系统
- **MonsterMoves.MonsterMoveStateMachine** — 怪物AI状态机
- **CardSelection** — 卡牌选择逻辑
- **Odds** — 概率系统

### 多人系统 (STS2 新增!)
- **Multiplayer** — 多人核心
- **Multiplayer.Connection** — 连接管理
- **Multiplayer.Game** — 多人游戏逻辑
- **Multiplayer.Game.Lobby** — 大厅
- **Multiplayer.Game.PeerInput** — 对端输入
- **Multiplayer.Messages** — 消息协议
- **Multiplayer.Messages.Game.Checksums** — 校验和(防作弊)
- **Multiplayer.Messages.Game.Sync** — 同步
- **Multiplayer.Quality** — 网络质量
- **Multiplayer.Replay** — 回放
- **Multiplayer.Serialization** — 序列化
- **Multiplayer.Transport.ENet / Steam** — 传输层(ENet + Steam)

### UI/表现层 (Godot Nodes)
- **Nodes.Screens.*** — 各种游戏界面 (主菜单/角色选择/地图/商店/设置/GameOver等)
- **Nodes.Combat** — 战斗场景
- **Nodes.Cards / Cards.Holders** — 卡牌显示与手牌管理
- **Nodes.Vfx.*** — 视觉特效
- **Nodes.Animation** — 动画
- **Nodes.Audio** — 音频
- **Nodes.CommonUi** — 通用UI组件
- **Nodes.Pooling** — 对象池
- **Nodes.Rooms** — 房间场景

### 其他系统
- **Achievements** — 成就系统
- **Animation** — 动画系统
- **Assets** — 资源管理
- **Audio / Audio.Debug** — 音频系统
- **AutoSlay** — 自动战斗(测试用?)
- **ControllerInput** — 手柄支持
- **Daily** — 每日挑战
- **Debug / DevConsole** — 调试控制台
- **Hooks** — 钩子系统(Mod用)
- **HoverTips** — 悬停提示
- **Leaderboard** — 排行榜
- **Localization** — 本地化(含DynamicVars, Fonts, Formatters)
- **Logging** — 日志系统
- **Map** — 地图生成
- **Modding** — Mod加载系统
- **Platform / Platform.Steam / Platform.Null** — 平台抽象层
- **Rewards** — 奖励生成
- **RichTextTags** — 富文本标签
- **Rooms** — 房间逻辑
- **TextEffects** — 文字特效
- **Timeline / Timeline.Epochs / Timeline.Stories** — 时间线/解锁系统
- **Unlocks** — 解锁系统
- **ValueProps** — 数值属性系统
- **SourceGeneration** — 源代码生成
- **GameInfo** — 游戏信息

---

## 核心架构分析 (已完成)

### 设计模式总览

#### 1. Canonical/Mutable 模式 (核心创新)
- 每个 Model 类型 (Card, Relic, Power, Potion, Monster, Event 等) 继承 `AbstractModel`
- **Canonical 实例**: 只读引用定义，存储在 `ModelDb` 静态注册表中
- **Mutable 实例**: 通过 `.ToMutable()` 浅克隆创建运行时副本，可修改
- 通过 `IsMutable` 属性追踪，用 `AssertMutable()` / `AssertCanonical()` 强制约束
- **好处**: 内存高效、定义/运行时状态分离清晰

```
ModelDb (静态注册表，存所有 Canonical Models)
  ├── CardModel.ToMutable() → 运行时可修改副本
  ├── RelicModel.ToMutable() → ...
  └── PowerModel.ToMutable() → ...
```

#### 2. 三层架构: Model → Entity → Node

| 层 | 职责 | 示例 |
|----|------|------|
| **Model** | 数据定义 + 运行时状态 | `CardModel`, `RelicModel`, `PowerModel` |
| **Entity** | 游戏逻辑 + 状态组合 | `Player`, `Creature` (含 Powers[], Block, HP) |
| **Node** | Godot 可视化 | `NCard`, `NCreature` (Control 节点) |

- Model 持有本地化描述、动态变量、基础数值
- Entity 组合多个 Model，管理运行时交互
- Node 监听 Model 变化，通过 signal 更新视觉

#### 3. Hook 系统 (事件驱动扩展)
- `Hook.cs` 静态类提供广泛的钩子接口
- Models 可覆写虚方法: `BeforeAttack`, `AfterBlockGained`, `AfterCardDrawn` 等
- 钩子遍历所有 `ShouldReceiveCombatHooks = true` 的实体
- 两阶段执行 (early/late) 控制顺序
- **Mod 可以通过覆写钩子方法拦截游戏事件，无需 patch 核心逻辑**

#### 4. Command 模式
- `Cmd` 静态类: 异步命令基础设施 (Wait, CustomScaledWait)
- `AttackCommand` 建造者模式构建复杂攻击
- 领域命令: `CardCmd`, `PowerCmd`, `RelicCmd`, `PotionCmd`, `CreatureCmd`

#### 5. Factory 模式
- `CardFactory` — 卡牌创建 (含稀有度抽取、升级、多人过滤)
- `RelicFactory` — 从 grab bag 中选遗物
- `PotionFactory` — 药水创建
- 工厂封装平衡逻辑 (概率、稀有度) 与 Model 分离

### 游戏状态层级

```
RunManager (单例)
  ├── RunState (IRunState 接口)
  │   ├── Players[]
  │   ├── Acts[]
  │   ├── CurrentActIndex
  │   ├── Map (ActMap)
  │   ├── MapPointHistory
  │   ├── Modifiers[]
  │   └── RNG / Odds / RelicGrabBag
  │
  └── CombatState (每场战斗)
      ├── Encounter
      ├── Allies (Creatures)
      ├── Enemies (Creatures)
      ├── RoundNumber
      ├── CurrentSide (Player/Enemy)
      └── AllCards[]
```

**Player 状态**: Character, Creature, Relics, Potions, Deck, Gold, MaxEnergy, RunState, PlayerRng, PlayerOdds
**Creature 状态**: Block, CurrentHp, MaxHp, Powers[], CombatState, Side (Ally/Enemy)
**战斗中牌堆**: Hand / Draw / Discard / Exhaust

### RNG 管理
- `RunRngSet` — Run 级别共享 RNG (地图生成、遭遇战)
- `PlayerRngSet` — 每玩家 RNG (抽牌、奖励等)
- 独立 Rng 实例: Shops, Rewards, Draws, CardRewards
- **支持确定性回放和种子回放**

### 战斗/Action 处理

```
ActionExecutor (异步 Task 队列)
  → GameAction (状态机: None → WaitingForExecution → Executing → ...)
    → PlayCardAction / UsePotionAction / AttackCommand / ...
```

- `ActionQueueSet` 管理队列，顺序执行
- Action 生命周期: `BeforeExecuted → Execute() → AfterFinished`
- 回合顺序: Player Side → Enemy Side → RoundNumber++
- 伤害系统: `AttackCommand` 建造者 → Hook 修改 → `DamageResult` (attacker, target, amount, blocked, absorbed)

### Mod 系统

```
ModManager (单例)
  ├── 加载路径: {game_dir}/mods/ + Steam Workshop
  ├── ModManifest (id, version, dependencies)
  ├── Assembly (通过 AssemblyLoadContext 加载)
  └── Harmony patches 应用
```

**扩展方式**:
1. 覆写 Model 的虚 Hook 方法
2. `Hook.Modify*` 静态方法动态修改
3. 事件回调 `model.ExecutionFinished += handler`
4. Harmony 直接 patch

### 存档系统

```
SaveManager (单例)
  ├── SettingsSaveManager → SettingsSave (JSON)
  ├── PrefsSaveManager → PrefsSave
  ├── ProgressSaveManager → ProgressState (成就/解锁)
  ├── RunSaveManager → SerializableRun (JSON)
  ├── ProfileSaveManager → ProfileSave
  └── RunHistorySaveManager → RunHistory (统计)
```

**SerializableRun 结构**:
- schema_version, acts, modifiers, current_act_index
- players (SerializablePlayer), visited_map_coords
- map_point_history, rng state, odds, start_time, ascension

**关键设计**:
- Model 有 `.FromSerializable()` 工厂方法重建
- `MigrationManager` 处理存档版本升级
- `SnakeCaseJsonStringEnumConverter` 枚举序列化
- 支持云存档 (`ICloudSaveStore`)

### 多人系统关键设计
- `LocalContext.NetId` 标识本地玩家
- `IsMine(model)` 检查所有权，处理隐藏信息
- 传输层: ENet + Steam P2P
- Checksum 校验防作弊
- 确定性同步 (相同 RNG seed + Action 重放)

### 房间类型系统
```
AbstractRoom (接口, Enter/Exit/Resume 生命周期)
  ├── CombatRoom (战斗)
  ├── EventRoom (事件)
  ├── MapRoom (地图)
  ├── MerchantRoom (商店)
  ├── TreasureRoom (遗物选择)
  └── RestSiteRoom (休息/升级)
```

---

---

## 数据收集与指标分析 (已完成)

### 指标上传系统

**4 个上传端点** (PUT 请求到 `sts2-metric-uploads.herokuapp.com`):
1. `/record_data/` — Run 指标
2. `/record_achievement/` — 成就指标
3. `/record_epoch/` — 纪元/挑战指标
4. `/record_settings/` — 系统/设置指标

**上传前提条件 (全部满足才会发送)**:
- 用户在设置中主动启用 "Upload Gameplay Data" (`PrefsSave.UploadData`)
- 非 Debug/开发构建
- 非 Godot 编辑器
- 无 Mod 加载
- 无 Run Modifier 激活
- Run 至少完成 5 层

**关键代码**: `Runs.Metrics/MetricUtilities.cs`, `Nodes.Screens.Settings/NUploadDataTickbox.cs`

### 每局 Run 上传的指标 (RunMetrics)

| 字段 | 类型 | 说明 |
|------|------|------|
| BuildId | string | 游戏版本 |
| PlayerId | string | 唯一玩家标识 |
| Character | ModelId | 选择的角色 |
| Win | bool | 是否胜利 |
| NumPlayers | int | 多人数量 (1-2) |
| Team | List<ModelId> | 多人时的队伍角色 |
| Ascension | int | 升阶等级 (0-20) |
| TotalPlaytime | float | 累计游玩时间(秒) |
| TotalWinRate | float | 生涯胜率 |
| RunPlaytime | float | 本局游玩时间 |
| FloorReached | int | 到达层数 |
| KilledByEncounter | ModelId | 被谁击杀 |
| CardChoices | List | 卡牌选择 (offered + picked) |
| CampfireUpgrades | List | 篝火升级的卡 |
| EventChoices | List | 事件选择 |
| AncientChoices | List | 古老者选择 |
| RelicBuys / PotionBuys / ColorlessBuys | List | 购买记录 |
| PotionDiscards | List | 丢弃的药水 |
| Encounters | List | 每场战斗: id, damage taken (限制0-100), turns |
| ActWins | List | 每幕胜负 |
| Deck | IEnumerable | 最终卡组 |
| Relics | IEnumerable | 最终遗物 |

### 成就 & 纪元指标 (仅解锁时发送)

**成就**: BuildId, Achievement(枚举名), TotalRuns, TotalPlaytime, TotalAchievements
**纪元**: BuildId, Epoch(挑战名), TotalRuns, TotalPlaytime, TotalEpochs

### 系统/设置指标 (每 5 局发送一次)

| 字段 | 说明 |
|------|------|
| Os | 操作系统 |
| SystemRam | 系统内存 GB |
| LanguageCode | 语言 |
| FastModeType | 战斗速度 |
| Screenshake, ShowRunTimer, ShowCardIndices | 游戏选项 |
| DisplayCount, DisplayResolution, Fullscreen | 显示设置 |
| AspectRatio, VSync, FpsLimit, Msaa | 图形设置 |

### Sentry 错误上报

- **采样率**: Public 10%, Beta 20%, Private Beta 100%, Dev 0%
- **发送内容**: 未处理异常、错误日志、游戏状态上下文(场景/Act/层数/战斗状态)、Session ID
- **不发送条件**: UploadData=false / 编辑器中 / Mod 加载 / AutoSlay 超时异常
- **隐私**: `SendDefaultPii = false`

### 本地存储 vs 服务器上传

| 数据 | 去向 | 频率 | 用户控制 |
|------|------|------|----------|
| Run 指标(摘要) | herokuapp.com | 每局结束 | Upload 开关 |
| 成就解锁 | 同上 | 解锁时 | Upload 开关 |
| 纪元完成 | 同上 | 完成时 | Upload 开关 |
| 系统设置 | 同上 | 每5局 | Upload 开关 |
| 错误/崩溃 | Sentry.io | 报错时 | Upload 开关 + 采样 |

### 本地详细历史 (RunHistory, 不上传)

**完整的逐层记录 (PlayerMapPointHistoryEntry)**:

| 类别 | 字段 |
|------|------|
| 金币 | GoldGained, GoldSpent, GoldLost, GoldStolen, CurrentGold |
| 生命 | CurrentHp, MaxHp, DamageTaken, HpHealed, MaxHpLost, MaxHpGained |
| 选择 | CardChoices, RelicChoices, PotionChoices, AncientChoices, EventChoices, RestSiteChoices |
| 购买 | BoughtRelics, BoughtPotions, BoughtColorless |
| 卡牌变化 | CardsGained, CardsRemoved, UpgradedCards, DowngradedCards, CardsEnchanted, CardsTransformed |
| 其他 | PotionDiscarded, PotionUsed, RelicsRemoved, CompletedQuests |

**本地存储**: `history/` 目录, 文件名 `{UnixTimestamp}.run`, 云同步上限 5MB/100 文件

### 数据匿名化
- `IdAnonymizer` 类: 多人 Run 历史发送前将玩家 ID 映射为匿名 ID
- 遭遇伤害值限制在 0-100 范围

### 额外发现
- Mod 可通过 `ModManager.CallMetricsHooks()` 拦截指标上传
- 多人时 Team 列表仅在 NumPlayers > 1 时发送
- 不收集帧率/性能数据 (仅收集 FPS 上限设置)
- 包含完整 Seed 字符串用于重现

---

---

## Mod 系统完整分析 (已完成)

### Mod 加载生命周期

1. 发现阶段: 扫描 `[game]/mods/` (递归) + Steam Workshop 订阅
2. 排序: 按依赖图拓扑排序 (检测循环依赖)
3. 加载 DLL: `AssemblyLoadContext.LoadFromAssemblyPath()`
4. 初始化: 查找 `[ModInitializerAttribute]` 标记的静态方法并调用；若无此标记则自动调用 `Harmony.PatchAll()`
5. 加载 PCK: 通过 `ProjectSettings.LoadResourcePack()` 加载 Godot 资源
6. 触发 `OnModDetected` 事件

### Mod manifest.json 格式

```json
{
  "id": "unique_mod_id",        // 必填，小写+连字符
  "name": "Display Name",       // 可选
  "author": "Author",           // 可选
  "description": "...",         // 可选
  "version": "1.0.0",           // 可选
  "has_pck": false,             // 是否含 Godot PCK
  "has_dll": true,              // 是否含 C# DLL
  "dependencies": ["other_id"], // 依赖的 Mod ID
  "affects_gameplay": true      // false 则不计入 run 元数据(可用于数据收集Mod)
}
```

### 两种初始化方式

**方式A: ModInitializerAttribute (推荐)**
```csharp
[ModInitializerAttribute("InitializeMod")]
public class MyMod {
    public static void InitializeMod() {
        ModHelper.AddModelToPool<CardPoolModel, CustomCard>();
    }
}
```

**方式B: Harmony 自动 Patch**
- 无 ModInitializerAttribute 时，自动调用 `Harmony.PatchAll()` patch 整个程序集

### 内容注册 API

```csharp
ModHelper.AddModelToPool<TPoolType, TModelType>()
// 必须在初始化阶段调用，内容池冻结后无法新增
// TPoolType: CardPoolModel / RelicPoolModel / PotionPoolModel (含角色池如 IronCladCardPool)
// TModelType: 自定义的 CardModel / RelicModel / PotionModel 子类
```

**自动发现**: `ReflectionHelper.GetSubtypesInMods<T>()` 反射发现 Mod 程序集中所有 AbstractModel 子类型

### 可扩展的基类 (创建自定义内容)

| 基类 | 用途 |
|------|------|
| `CardModel` | 自定义卡牌 |
| `RelicModel` | 自定义遗物 |
| `PowerModel` | 自定义 Buff/Debuff |
| `MonsterModel` | 自定义怪物 |
| `PotionModel` | 自定义药水 |
| `AfflictionModel` | 自定义苦难效果 |
| `EnchantmentModel` | 自定义附魔 |
| `EventModel` | 自定义事件 |
| `AncientEventModel` | 自定义古老者事件 |
| `OrbModel` | 自定义球 |
| `AchievementModel` | 自定义成就 |

### Hook 系统 (100+ 虚方法)

所有 Hook 定义在 `AbstractModel` 基类上，通过 `Hook.cs` (1953行) 静态方法调度。
监听条件: `ShouldReceiveCombatHooks = true`

#### 战斗事件 Hook (Before/After)
- `BeforeAttack / AfterAttack`
- `BeforeCardPlayed / AfterCardPlayed / AfterCardPlayedLate`
- `BeforeCombatStart / AfterCombatEnd / AfterCombatVictory`
- `BeforeDamageReceived / AfterDamageReceived / AfterDamageGiven`
- `BeforeDeath / AfterDeath`
- `BeforeTurnEnd / AfterTurnEnd / AfterTurnEndLate`
- `BeforeSideTurnStart / AfterSideTurnStart`
- `BeforePlayPhaseStart`

#### 卡牌 Hook
- `AfterCardDrawn / AfterCardDiscarded / AfterCardExhausted / AfterCardRetained`
- `AfterCardChangedPiles / AfterCardEnteredCombat / AfterCardGeneratedForCombat`
- `BeforeCardAutoPlayed / BeforeCardRemoved`

#### 格挡 Hook
- `BeforeBlockGained / AfterBlockGained / AfterBlockBroken / AfterBlockCleared`

#### 能量/回合 Hook
- `AfterEnergyReset / AfterEnergySpent`
- `AfterPlayerTurnStart / AfterPlayerTurnStartLate`

#### 经济 Hook
- `AfterGoldGained / AfterStarsGained / AfterStarsSpent / AfterForge`

#### 药水 Hook
- `BeforePotionUsed / AfterPotionUsed / AfterPotionDiscarded / AfterPotionProcured`

#### 地图/房间 Hook
- `AfterActEntered / BeforeRoomEntered / AfterRoomEntered / AfterMapGenerated`
- `AfterRestSiteHeal / AfterRestSiteSmith`

#### 奖励 Hook
- `BeforeRewardsOffered / AfterRewardTaken / AfterItemPurchased`

### Modify Hook (40+ 数值拦截方法)

| 类别 | 方法 |
|------|------|
| 伤害 | `ModifyDamageAdditive / Multiplicative / Cap` |
| 格挡 | `ModifyBlockAdditive / Multiplicative` |
| 出牌 | `ModifyCardPlayCount / AttackHitCount / XValue` |
| 费用 | `ModifyEnergyCostInCombat / ModifyStarCost` |
| 能量 | `ModifyMaxEnergy / ModifyHandDraw` |
| Power | `ModifyPowerAmountGiven / TryModifyPowerAmountReceived` |
| 治疗 | `ModifyHealAmount / ModifyRestSiteHealAmount` |
| 卡牌奖励 | `TryModifyCardRewardOptions / ModifyCardRewardUpgradeOdds` |
| 商店 | `ModifyMerchantCardPool / Price / Rarity` |
| 地图 | `ModifyGeneratedMap / ModifyNextEvent` |
| 篝火 | `TryModifyRestSiteOptions / TryModifyRestSiteHealRewards` |
| 洗牌 | `ModifyShuffleOrder` |

### Should Hook (30+ 条件判断方法, 返回 bool)

- 卡牌: `ShouldAddToDeck / ShouldPlay / ShouldDraw / ShouldEtherealTrigger`
- 战斗: `ShouldAllowHitting / ShouldAllowTargeting / ShouldClearBlock / ShouldFlush`
- 死亡: `ShouldDie / ShouldDieLate / ShouldCreatureBeRemovedFromCombatAfterDeath`
- 经济: `ShouldGainGold / ShouldGainStars / ShouldPayExcessEnergyCostWithStars`
- 药水: `ShouldProcurePotion`
- 特殊: `ShouldAfflict / ShouldAllowAncient / ShouldGenerateTreasure / ShouldTakeExtraTurn`

### ⭐ 数据收集接口 (关键发现)

**OnMetricsUpload 委托**:
```csharp
public static event ModManager.MetricsUploadHook? OnMetricsUpload;
public delegate void MetricsUploadHook(SerializableRun run, bool isVictory, ulong localPlayerId);
```

Mod 可以订阅此事件获取:
- **完整的 SerializableRun 数据** (Run 全部序列化状态)
- 是否胜利
- 本地玩家 ID

### ⭐ 沙箱与安全限制 (关键发现)

**完全无沙箱**:
- Mod 与游戏运行在同一 AppDomain
- **可以自由访问文件系统** (读写任意路径)
- **可以自由发起网络请求** (HttpClient, WebSocket, TCP 等标准 .NET 库)
- 可以使用反射
- 可以创建线程
- 可以启动进程

**仅有的限制**:
- 用户必须手动启用 Mod (有警告提示)
- 不能在运行时动态加载 (仅初始化阶段)
- 循环依赖检测
- 重复 ID 阻止加载

### 关于 `affects_gameplay` 标志

- `true` (默认): Mod 被记录到 Run 元数据，且**此 Run 的指标不会上传到官方服务器**
- `false`: Mod 不计入 gameplay，Run 指标正常上传
- **对数据收集 Mod 的启示**: 设置 `affects_gameplay: false` 可以让 Mod 存在的同时不影响官方数据收集

---

## 待分析事项 (下次继续)

### 中优先级
1. **具体代码阅读** — 挑选关键实现文件深入阅读
2. **多人系统深入** — 网络同步架构、消息协议
3. **地图生成** — 算法实现
4. **RNG系统** — 随机数具体实现

### 低优先级
5. **解包 .pck** — Godot 资源包(需要 Godot RE Tools)
6. **.pck 中的 GDScript** — 是否有额外逻辑在场景脚本中

---

## Community Stats Mod 实现进度

### 已完成模块

#### 1. 项目结构 (Task 1) ✅
- `mods/sts2_community_stats/manifest.json` — Mod 元数据
- `mods/sts2_community_stats/sts2_community_stats.csproj` — .NET 9.0 项目，引用 sts2.dll, GodotSharp.dll, 0Harmony.dll

#### 2. Config 层 (Task 2) ✅
- `ModConfig.cs` — API URL, 超时, 缓存TTL, 数据目录 (%AppData%/sts2_community_stats/)
- `FilterSettings.cs` — 角色/层数/胜率筛选, ToQueryString(), Hash() 缓存键, JSON 持久化
- `VersionManager.cs` — 封装 ReleaseInfoManager.Instance.ReleaseInfo.Version

#### 3. Util 层 (Task 8) ✅
- `SafePatching.cs` — Safe.Run/RunAsync 异常吞咽 + Log.Info/Log.Warn 日志
- `OfflineQueue.cs` — 磁盘队列 (PendingDir), DrainAsync 指数退避重试

#### 4. API 层 (Task 3) ✅
- `ApiModels.cs` — 上传 DTO (RunUploadPayload + 8 子类型) + 查询响应 (BulkStatsBundle, CardStats, RelicStats, EventStats, EncounterStats)
- `ApiClient.cs` — HttpClient 封装, 上传/批量查询/按需查询, 超时处理, 离线回退
- `StatsCache.cs` — 内存(ConcurrentDictionary+TTL) + 磁盘(JSON+TTL) 两级缓存
- `StatsProvider.cs` — 数据分发核心: Run 开始预加载 bulk bundle → 缓存优先 → API 按需回退

#### 5. Collection 层 (Task 4) ✅
- `ContributionMap.cs` — Power→来源卡牌/遗物归因映射 (间接伤害追踪)
- `CombatTracker.cs` — 单战斗贡献追踪: 卡牌出场/伤害/格挡/抽牌/能量/治疗, 遗物上下文归因
- `RunContributionAggregator.cs` — Run 级累加: 遭遇战记录 + 全局贡献汇总 + 上传导出
- `RunDataCollector.cs` — OnMetricsUpload 钩子: 组装 RunUploadPayload (卡牌选择/事件/商店/移除/升级/战斗贡献)

#### 6. UI 组件 (Task 5) ✅
- `StatsLabel.cs` — 选卡/遗物/事件旁显示统计 (颜色编码: 绿/黄/红)
- `ContributionChart.cs` — 水平条状贡献图 (卡牌蓝+遗物金混合, 含归因伤害浅色)
- `ContributionPanel.cs` — 战斗结束弹窗 + Tab 切换 (单战斗/全局汇总)
- `MapPointOverlay.cs` — 地图节点死亡率/平均伤害叠加显示
- `FilterPanel.cs` — 筛选面板 (层数/版本/自动匹配), Apply 后触发数据重加载

#### 7. Harmony Patches (Task 6) ✅
- `RunLifecyclePatch.cs` — RunManager.SetUpNewSinglePlayer/MultiPlayer Postfix + ModManager.OnMetricsUpload 注册
- `CardRewardScreenPatch.cs` — NCardRewardSelectionScreen.RefreshOptions/SelectCard 统计显示+选卡记录
- `EventOptionPatch.cs` — NEventOptionButton.OnRelease/_Ready 事件选项记录+社区选择率显示
- `CombatHistoryPatch.cs` — CombatHistory 6个方法: CardPlay/Damage/Block/Power/Draw 贡献追踪
- `CombatLifecyclePatch.cs` — CombatRoom.Enter(Prefix)/OnCombatEnded(Postfix) 战斗生命周期
- `ShopPatch.cs` — NMerchantCard.OnSuccessfulPurchase(Prefix) 商店购买记录
- `CardRemovalPatch.cs` — NDeckCardSelectScreen.ConfirmSelection 卡牌移除记录
- `CardUpgradePatch.cs` — NDeckUpgradeSelectScreen.ConfirmSelection 卡牌升级记录
- `MapPointPatch.cs` — NMapPoint.RefreshVisualsInstantly 地图节点危险度叠加

#### 8. Mod 入口 (Task 7) ✅
- `CommunityStatsMod.cs` — [ModInitializer("Initialize")], Harmony.PatchAll, OnMetricsUpload 挂载, SceneTree.ProcessFrame 热键轮询 (F8/F9), 磁盘缓存清理

### 构建状态: ✅ 编译通过 (0 警告 0 错误)

### Bug 修复记录 (Phase 6)
1. **DLL 输出路径错误**: csproj `OutputPath` 从 `bin\` 改为 `.\`，STS2 的 ModManager 要求 DLL 与 manifest.json 同目录
2. **异步方法 Harmony 补丁**: CombatRoom.Enter (async Task) 从 Postfix 改为 Prefix；ShopPatch 从 OnTryPurchase (async) 改为 OnSuccessfulPurchase (sync)
3. **Godot 节点子类化问题**: 移除 HotkeyListener (partial class Node 需要 Godot 源生成器)，改用 SceneTree.ProcessFrame 信号 + Input.IsKeyPressed 轮询
4. **Harmony 参数名不匹配**: CardRemovalPatch/CardUpgradePatch 的 `ConfirmSelection` patch 声明了参数 `NButton button`，但反编译方法参数名是 `_`。移除 patch 方法中未使用的参数声明
5. **构建产物污染 mod 扫描**: STS2 递归扫描所有 `.json` 文件作为 mod manifest。csproj 加 `GenerateDependencyFile=false` 禁止生成 `.deps.json`，obj/ 加 `.gdignore`

### Phase 9 功能改进 (6 项) ✅
1. **Vulnerability/Debuff 贡献追踪**: ContributionMap 新增 `_creatureDebuffSources` 追踪每个生物的 debuff 来源；CombatTracker 新增 `AttributeDebuffBonuses()` 方法计算易伤额外伤害 (totalDamage/3)；CombatHistoryPatch.AfterDamageReceived 调用归因
2. **ContributionPanel 关闭按钮**: 头部添加 ✕ 按钮，布局改为 VBoxContainer(标题行 + TabContainer)
3. **Mod 设置界面集成**: 新增 `ModSettingsPatch.cs` patch NModdingScreen._Ready，在 mod 列表上方添加 "Community Stats Settings" 按钮，点击切换 FilterPanel；移除 F9 热键
4. **玩家胜率筛选 + 样本量显示**: FilterPanel 新增胜率 SpinBox (0-100%)；BulkStatsBundle 新增 `TotalRuns` 字段；StatsProvider 暴露 `TotalRunCount`；FilterPanel 显示 "Filtered data: N runs"
5. **缺失数据 N/A 显示**: CardRewardScreenPatch/EventOptionPatch 在数据未命中时显示 "No data" label，bundle 未加载时显示 "Loading..."
6. **卡组查看器统计**: 新增 `DeckViewPatch.cs` patch NDeckViewScreen.DisplayCards，遍历 NCardGrid.CurrentlyDisplayedCardHolders 在每张卡下方添加 StatsLabel

### 新增/修改文件清单 (Phase 9)
- `src/Patches/DeckViewPatch.cs` — 新增: 卡组查看界面卡牌统计
- `src/Patches/ModSettingsPatch.cs` — 新增: Mod 设置界面集成
- `src/Collection/ContributionMap.cs` — 修改: 增加 debuff 来源追踪
- `src/Collection/CombatTracker.cs` — 修改: 增加 debuff 贡献归因
- `src/Patches/CombatHistoryPatch.cs` — 修改: 调用 debuff 归因 + nullable 修复
- `src/Patches/EventOptionPatch.cs` — 修改: N/A 显示 + Index 通过 Traverse 访问
- `src/Patches/CardRewardScreenPatch.cs` — 修改: N/A 显示
- `src/UI/ContributionPanel.cs` — 修改: 关闭按钮
- `src/UI/FilterPanel.cs` — 修改: 胜率筛选 + 样本量 + 关闭按钮
- `src/Api/ApiModels.cs` — 修改: BulkStatsBundle.TotalRuns
- `src/Api/StatsProvider.cs` — 修改: TotalRunCount 属性
- `src/CommunityStatsMod.cs` — 修改: 移除 F9, FilterApplied 事件

### Phase 10 测试反馈修复 (2 项) ✅
1. **遗物胜率悬浮显示**: 新增 `RelicHoverPatch.cs`，Patch `NRelicBasicHolder.OnFocus/OnUnfocus`，鼠标悬停遗物时显示 Pick/Win rate StatsLabel，离开时移除
2. **地图节点仅已走过显示**: `MapPointPatch.cs` 增加 `__instance.State != MapPointState.Traveled` 检查，只有已经走过的路线节点才显示战损/死亡率统计

### 构建状态: ✅ 编译通过 (0 警告 0 错误)

### 测试基础设施 (Phase 6.5)
- `test/test_data.json` — 精心设计的 BulkStatsBundle 测试数据 (30 卡 / 16 遗物 / 7 事件 / 24 遭遇战)，覆盖全 5 角色，数值便于肉眼验证
- `test/mock_server.py` — Python 本地 Mock API 服务器，完整实现所有 6 个 API 端点，上传数据保存到 uploaded_runs.json
- `test/VERIFICATION_CHECKLIST.md` — 46 项验证清单，含预期数值、颜色、步骤
- `config.json` — 可选配置覆盖 (api_base_url 指向 localhost)，通过 `ModConfig.LoadOverrides()` 加载
- `ModConfig.cs` 新增 `LoadOverrides()` 方法，支持 config.json 覆盖 API 地址

---

## 反编译命令备忘

```bash
# 安装 ILSpy CLI
dotnet tool install -g ilspycmd

# 反编译 sts2.dll (需要 .NET roll-forward 因为只有 .NET 10)
DOTNET_ROLL_FORWARD=LatestMajor ilspycmd -p -o "./_decompiled/sts2" "./data_sts2_windows_x86_64/sts2.dll"

# 反编译其他 DLL (如有需要)
# DOTNET_ROLL_FORWARD=LatestMajor ilspycmd -p -o "./_decompiled/godotsharp" "./data_sts2_windows_x86_64/GodotSharp.dll"
```
