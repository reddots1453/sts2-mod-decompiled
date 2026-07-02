# 预见之眼 (Foresight Eye) Mod — 实现计划 v2

## 概述

新增遗物 "预见之眼" (Foresight Eye)，基于 **RitsuLib** 框架注册，游戏开始时自动获得，悬停时通过 `HoverTipHelper` 显示未来预测信息，同时 Patch 抽牌堆实现"冻结之眼"效果。

---

## 1. 项目结构

```
mods/sts2_foresight/
├── manifest.json
├── sts2_foresight.csproj
├── localization/zhs/relics.json          ← 本地化
├── images/relics/foresight_eye.png        ← 遗物图标 (85x85 + 256x256)
└── src/
    ├── ForesightMod.cs                    ← ModInitializer 入口
    ├── Relic/
    │   └── ForesightEye.cs               ← ModRelicTemplate 子类
    ├── Engine/
    │   ├── RngSimulator.cs               ← RNG 克隆/模拟
    │   ├── EncounterReader.cs            ← RoomSet 遭遇序列
    │   ├── EventReader.cs                ← RoomSet 事件序列
    │   ├── RelicSequenceReader.cs        ← RelicGrabBag 反射读取
    │   ├── CardRewardPredictor.cs        ← 精确卡牌预测（含卡池模拟）
    │   ├── PotionPredictor.cs            ← 药水掉落序列
    │   └── BossReader.cs                 ← Boss 信息（所有幕）
    ├── UI/
    │   └── ForesightTooltipBuilder.cs    ← 构造悬停文本
    └── Patches/
        └── DrawPileSortPatch.cs          ← RitsuLib IPatchMethod
```

---

## 2. RitsuLib 集成方案

### 2.1 .csproj 配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>sts2_foresight</AssemblyName>
    <RootNamespace>Foresight</RootNamespace>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <DefineConstants>STS2_GE_V105</DefineConstants>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <OutputPath>.\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
    <GenerateDependencyFile>false</GenerateDependencyFile>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="sts2">
      <HintPath>..\..\..\data_sts2_windows_x86_64\sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="GodotSharp">
      <HintPath>..\..\..\data_sts2_windows_x86_64\GodotSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>..\..\..\data_sts2_windows_x86_64\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <!-- RitsuLib: 本地引用 -->
    <Reference Include="STS2-RitsuLib">
      <HintPath>..\STS2-RitsuLib\STS2-RitsuLib.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

### 2.2 manifest.json

```json
{
  "id": "sts2_foresight",
  "name": "预见之眼 (Foresight Eye)",
  "author": "reddots",
  "description": "预知未来的遗物：查看接下来的卡牌、遗物、遭遇、事件、Boss 和药水信息。",
  "version": "0.1.0",
  "has_dll": true,
  "has_pck": false,
  "affects_gameplay": false,
  "dependencies": [
    { "id": "STS2-RitsuLib", "min_version": "0.2.27" }
  ]
}
```

### 2.3 入口类

```csharp
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;

namespace Foresight;

[ModInitializer(nameof(Init))]
public static class ForesightMod
{
    public const string ModId = "sts2_foresight";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. 注册 Godot 脚本（如果 .cs 文件有 ScriptPath 属性）
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

        // 2. 启用自动注册（注解式 [RegisterRelic] 等）
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 3. 监听 RunStartedEvent — 游戏开始时自动获得遗物
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);

        // 4. RitsuLib 补丁 — 冻结之眼
        var patcher = RitsuLibFramework.CreatePatcher(ModId, "core-patches");
        patcher.RegisterPatch<DrawPileSortPatch>();
        if (!patcher.PatchAll())
            throw new InvalidOperationException("Foresight: critical patches failed.");
    }

    private static void OnRunStarted(RunStartedEvent evt)
    {
        // 给每个玩家添加预见之眼遗物
        foreach (var player in evt.RunState.Players)
        {
            var relic = ModelDb.GetById<RelicModel>(ModelDb.GetId<ForesightEye>());
            // TODO: 确认 RitsuLib 提供的添加遗物 API
        }
    }
}
```

### 2.4 遗物定义

```csharp
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Foresight.Relic;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class ForesightEye : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;

    // 无战斗数值变量
    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://sts2_foresight/images/relics/foresight_eye.png",
        IconOutlinePath: "res://sts2_foresight/images/relics/foresight_eye.png",
        BigIconPath: "res://sts2_foresight/images/relics/foresight_eye_big.png"
    );
}
```

### 2.5 悬停信息 — 使用 HoverTipHelper

RitsuLib 提供了 `HoverTipHelper.AddTipToOwner(owner, title, body)`，可以直接在已有悬浮提示上追加文本。这是最简洁的方式：

```csharp
// 在 NRelicInventoryHolder.OnFocus 时
HoverTipHelper.AddTipToOwner(holder, "预见之眼", BuildTooltipText());
```

备选方案：如果 `HoverTipHelper` 不够灵活，则使用 `ModNodeAttachmentRegistry` 挂载自定义 `Control` 子节点。

---

## 3. 各功能实现方案（修订版）

### 3.1 精确卡牌预测 — `CardRewardPredictor`

用户要求显示**具体卡名**而非概率。方案：

**核心思路**：克隆 `PlayerRng.Rewards` 的 (Seed, Counter)，模拟 `CardReward.Populate()` 的完整 RNG 调用序列。

**预测流程**：

```
1. 读取当前状态：
   - cardRarityOdds.CurrentValue (float)
   - rewardsRng.Seed (uint)
   - rewardsRng.Counter (int)
   - player.Character → CardPool

2. 对每个未来战斗（普通/精英交替预测）：
   a. 克隆 RNG: var simRng = new Rng(seed, counter);
   b. 模拟稀有度投骰: simRng.NextFloat() + CardRarityOdds 逻辑 → rarity
   c. 更新模拟的 rarityOffset (如果出了稀有 → -0.05, 否则 +growth)
   d. 获取该稀有度的卡池（通过角色 CardPool + 解锁过滤）
   e. 选3张卡: 每张 simRng.NextItem(filteredPool), 排除已选
   f. 每张卡升级判定: simRng.NextFloat() < upgradeOdds
   g. 记录结果 (counter -> 战斗后的 counter)
3. 格式化为文本
```

**关键数据获取**：
- `CardRarityOdds`：`player.PlayerOdds.CardRarity`（公开）
- `PlayerRng.Rewards`：公开属性，`Seed` 和 `Counter` 公开
- **CardPool**：`player.Character` 的卡池 → `ModelDb.AllCardPools.First(p => p.AllCardIds…)`。实际可通过 `player` 的 `CardPool` 或者查询 `ModelDb`
- **卡池过滤**：需要复制 `CardFactory.PullCardsFromPool` 的逻辑（过滤已拥有、过滤稀有度、过滤不可见等）

**需要研究**：`CardFactory.PullCardsFromPool` 的具体实现，了解每一步的 RNG 消耗和过滤逻辑。

### 3.2 遗物序列 — `RelicSequenceReader`

**变更**：除了反射读取 `RelicGrabBag._deques`，也可以调用 `RelicGrabBag.ToSerializable()` 获取序列化的遗物 ID 列表。

```csharp
// 反射获取 _deques
var field = typeof(RelicGrabBag).GetField("_deques", 
    BindingFlags.NonPublic | BindingFlags.Instance);
var deques = field.GetValue(player.RelicGrabBag) 
    as Dictionary<RelicRarity, List<RelicModel>>;

// 列出每个稀有度的前 N 个遗物
foreach (var rarity in deques.Keys) {
    var upcoming = deques[rarity].Take(5);
    // 显示名称
}
```

**注意**：`RelicGrabBag` 是在进入新幕时 `Populate()` 的，所以第一幕开始前可能为空。需做空检查。

### 3.3 遭遇序列 — `EncounterReader`

**同 v1**，反射访问 `ActModel._rooms`（protected field），然后公开读取 `RoomSet`：
- `normalEncounters[i..]` — 当前索引开始的普通怪
- `eliteEncounters[i..]` — 当前索引开始的精英怪
- 使用 `RoomSet.normalEncountersVisited` / `eliteEncountersVisited` 作为当前 index

### 3.4 药水序列 — `PotionPredictor`

用户要求精确预测。模拟 `PotionRewardOdds.Roll()` 和 `PotionFactory.CreateRandomPotion()`：

1. 读取 `player.PlayerOdds.PotionReward.CurrentValue`
2. 对接下来 3 场战斗，克隆 Rewards RNG 并模拟：
   - `simRng.NextFloat() < (currentValue + eliteBonus)` → 是否掉落
   - 如果掉落：`simRng.NextFloat()` 决定药水稀有度，`simRng.NextItem()` 选具体药水
   - 更新 `CurrentValue`（掉落 -0.1，不掉落 +0.1）
3. 同时显示下一场是否掉落的概率

### 3.5 Boss — `BossReader`

**变更**：遍历所有幕。

研究 `RunState` 结构：
- 当前幕：`runState.Act.BossEncounter` / `SecondBossEncounter`
- 未来幕：`RunState` 可能通过 `Acts` 或类似属性持有所有 ActModel

**实现方式**：
- 先检查 `RunState` 是否有 `AllActs` / `Acts` 属性
- 如果有，遍历所有 Act
- 如果没有，只显示当前幕 Boss，并在进入新幕时更新

### 3.6 事件序列 — `EventReader`

**同 v1**，读取 `RoomSet.events[eventsVisited..]`，显示接下来的事件名。

---

### 3.7 冻结之眼 — `DrawPileSortPatch`

**使用 RitsuLib IPatchMethod 接口**：

```csharp
public sealed class DrawPileSortPatch : IPatchMethod
{
    public static string PatchId => "foresight_draw_pile_sort";
    public static string Description => "Remove rarity/ID sort on draw pile view";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => [
        new(typeof(NCardPileScreen), nameof(NCardPileScreen.OnPileContentsChanged))
    ];

    // Prefix: 在 OnPileContentsChanged 执行前，临时标记跳过排序
    // 或者 Postfix: 在排序后重新设置为未排序顺序
    public static void Postfix(NCardPileScreen __instance)
    {
        // 反射获取 _grid 和 Pile，重新设置卡片顺序
        var pile = Traverse.Create(__instance).Property("Pile").GetValue<CardPile>();
        if (pile?.Type != PileType.Draw) return;
        
        var grid = Traverse.Create(__instance).Field("_grid").GetValue<NCardGrid>();
        var cards = pile.Cards.ToList(); // 不排序！
        grid.SetCards(cards, pile.Type, new List<SortingOrders> { SortingOrders.Ascending });
    }
}
```

**备选方案**（更干净）：直接用 Prefix 阻止排序，返回 false 跳过原方法。

但实际上最干净的方式是用 Transpiler 找到 `.Sort()` 调用并替换为 nop。但 RitsuLib IPatchMethod 对 Transpiler 的支持需要在方法名上标注。

---

## 4. RNG 模拟引擎 — `RngSimulator`

```csharp
public static class RngSimulator
{
    /// <summary>
    /// 在给定 RNG 的 (Seed, Counter) 快照基础上创建独立副本。
    /// 对副本的任何调用不会影响原 RNG。
    /// </summary>
    public static Rng Snapshot(Rng source)
    {
        return new Rng(source.Seed, source.Counter);
    }

    /// <summary>
    /// 模拟 FutureCalls 次 NextFloat() 调用，返回所有结果。
    /// </summary>
    public static float[] PeekNextFloats(Rng source, int futureCalls)
    {
        var clone = Snapshot(source);
        var results = new float[futureCalls];
        for (int i = 0; i < futureCalls; i++)
            results[i] = clone.NextFloat();
        return results;
    }

    /// <summary>
    /// 模拟 FutureCalls 次 NextInt(max) 调用。
    /// </summary>
    public static int[] PeekNextInts(Rng source, int futureCalls, int maxExclusive)
    {
        var clone = Snapshot(source);
        var results = new int[futureCalls];
        for (int i = 0; i < futureCalls; i++)
            results[i] = clone.NextInt(maxExclusive);
        return results;
    }
}
```

---

## 5. 需要进一步研究的源码

| 源码 | 目的 |
|---|---|
| `CardFactory.PullCardsFromPool()` | 完整了解卡牌奖励的 RNG 消耗 + 过滤逻辑 |
| `CardReward.Populate()` | 了解稀有度投骰 → 选卡 → 升级的完整流程 |
| `PotionFactory.CreateRandomPotion()` | 药水生成的 RNG 消耗 |
| `RunState` 的 Acts 属性 | 确认能否遍历所有幕获取 Boss |
| `RelicGrabBag.ToSerializable()` | 确认序列化输出格式 |
| `HoverTipHelper.AddTipToOwner()` | 确认参数格式和是否能满足显示需求 |

---

## 6. 迭代计划

### Round 1: 基础框架 + 遗物注册
- [ ] 创建项目结构（.csproj, manifest.json, 本地化文件, 图片占位）
- [ ] `ForesightMod.cs` — 入口 + RitsuLib 初始化
- [ ] `ForesightEye.cs` — 遗物定义 + `[RegisterRelic]` + `ModRelicTemplate`
- [ ] 验证：编译通过，遗物能在游戏中出现

### Round 2: 预生成数据 + 悬停 UI
- [ ] `EncounterReader` — RoomSet 遭遇序列
- [ ] `BossReader` — 所有幕 Boss
- [ ] `EventReader` — 事件序列
- [ ] `RelicSequenceReader` — RelicGrabBag 反射读取
- [ ] `ForesightTooltipBuilder` — 构造悬停文本
- [ ] 集成 HoverTipHelper 到遗物的 OnFocus

### Round 3: RNG 模拟预测
- [ ] `RngSimulator` — RNG 快照
- [ ] 深入阅读 `CardFactory` 源码，理解完整 RNG 消耗
- [ ] `CardRewardPredictor` — 精确卡名预测
- [ ] `PotionPredictor` — 药水掉落+类型预测

### Round 4: 冻结之眼
- [ ] `DrawPileSortPatch` — RitsuLib IPatchMethod
- [ ] 测试：抽牌堆按实际顺序展示

### Round 5: 打磨
- [ ] 本地化完善
- [ ] 遗物图标
- [ ] 边界情况处理（无战斗记录、新存档、多人模式等）

---

## 7. 已确认事项

| 问题 | 答案 |
|---|---|
| RitsuLib 注册 API | `[RegisterRelic(typeof(SharedRelicPool))]` + `ModRelicTemplate` |
| 游戏开始时获得 | `RunStartedEvent` 生命周期事件 |
| 卡牌预测方式 | 精确卡名（通过 RNG 克隆 + 卡池模拟） |
| Boss 显示范围 | 显示所有幕的 Boss |
| 悬停 UI 方案 | 优先用 `HoverTipHelper.AddTipToOwner()` |
| 补丁方案 | RitsuLib `IPatchMethod` + `ModPatchTarget` |
