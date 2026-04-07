# STS2 Mod Development — Claude Code 项目指令

## 项目结构

```
Sts2-mod-decompiled/
├── _decompiled/              ← 游戏反编译源码（只读参考，不编译）
│   └── sts2/                 ← ILSpy 反编译的 ~3300 个 C# 文件
├── mods/
│   ├── sts2_community_stats/ ← 主项目：社区统计 Mod
│   │   ├── src/              ← Mod 源代码
│   │   ├── DesignDoc/        ← PRD 和 PLAN 文档
│   │   ├── server/           ← 后端服务器代码
│   │   └── test/             ← 测试数据
│   ├── sts2_dev_tools/       ← 开发工具：一键解锁
│   └── sts2_kb_extractor/    ← 知识库提取器
├── KnowledgeBase/            ← STS2 游戏知识库（JSON + Markdown）
└── CLAUDE.md                 ← 本文件
```

## 知识库查询

项目包含一个从游戏运行时提取的 STS2 知识库，位于 `KnowledgeBase/`。
**当需要查询游戏实体信息时，使用以下方式**：

### 精确查询（按 ID）
```
Grep "BASH" KnowledgeBase/cards.json              → 卡牌：重击
Grep "BURNING_BLOOD" KnowledgeBase/relics.json     → 遗物：燃烧之血
Grep "FIRE_POTION" KnowledgeBase/potions.json      → 药水：火焰药水
Grep "INKLET" KnowledgeBase/monsters.json          → 怪物：墨滴
Grep "QUEEN" KnowledgeBase/encounters.json         → 遭遇：女王
Grep "SLIPPERY_BRIDGE" KnowledgeBase/events.json   → 事件：滑溜桥
Grep "VULNERABLE" KnowledgeBase/docs/keywords.md   → 力量/状态：易伤
```

### 模糊搜索（按名称或属性，支持中文）
```
Grep "Ironclad" KnowledgeBase/cards.json           → 所有铁甲战士卡牌
Grep "打击" KnowledgeBase/cards.json               → 所有名称含"打击"的卡牌
Grep "Rare" KnowledgeBase/relics.json              → 所有稀有遗物
Grep "Boss" KnowledgeBase/encounters.json          → 所有 Boss 遭遇
Grep "Exhaust" KnowledgeBase/cards.json            → 所有带消耗关键词的卡牌
```

### 浏览概览（Markdown 文档）
```
Read KnowledgeBase/docs/keywords.md                → 力量/状态效果速查（260个Power + 游戏术语 + 标签）
Read KnowledgeBase/glossary.md                     → 基础枚举速查（卡牌类型/稀有度/关键词）
Read KnowledgeBase/docs/cards_ironclad.md          → 铁甲战士全卡牌列表
Read KnowledgeBase/docs/cards_silent.md            → 猎手全卡牌列表
Read KnowledgeBase/docs/relics.md                  → 全遗物列表
Read KnowledgeBase/docs/potions.md                 → 全药水列表
Read KnowledgeBase/docs/bestiary.md                → 怪物图鉴（含 AI 行为）
Read KnowledgeBase/docs/encounters.md              → 遭遇列表（含幕归属）
Read KnowledgeBase/docs/events.md                  → 事件列表（含选项）
```

### JSON Schema
- **cards.json**: `{ "ID": { id, name{en,cn}, description{en,cn}, type, rarity, cost, target, character, keywords[], tags[], vars{Name:[base,upgraded]} } }`
- **relics.json**: `{ "ID": { id, name{en,cn}, description{en,cn}, flavor{en,cn}, rarity, character, vars{} } }`
- **potions.json**: `{ "ID": { id, name{en,cn}, description{en,cn}, rarity, usage, target, character, vars{} } }`
- **monsters.json**: `{ "ID": { id, name{en,cn}, hp{min,max}, moves{MOVE:{intents[],followUp?}}, ai } }`
- **encounters.json**: `{ "ID": { id, name{en,cn}, roomType, isWeak, monsters[], acts[] } }`
- **events.json**: `{ "ID": { id, name{en,cn}, description{en,cn}, options[{index,text_en,isLocked}] } }`

### 数据说明
- 所有文本双语（EN + CN），描述中 `{Damage:diff()}` 等为 SmartFormat 模板变量
- 卡牌 `vars` 中 `[base, upgraded]` 为基础值和升级后值
- 怪物 `ai` 字段为 AI 行为的文字摘要
- 遭遇 `acts` 标明该遭遇出现在哪个幕（OVERGROWTH/HIVE/GLORY/UNDERDOCKS）

## 构建命令

```bash
# 编译 Community Stats mod
cd mods/sts2_community_stats && dotnet build

# 编译 Dev Tools mod
cd mods/sts2_dev_tools && dotnet build

# 编译 KB Extractor mod
cd mods/sts2_kb_extractor && dotnet build
```

## 设计文档

核心设计文档位于 `mods/sts2_community_stats/DesignDoc/`：
- `WORKFLOW.md` — 文档驱动 AI 编程工作流
- `PRD_00_MASTER.md` — 项目总体 PRD
- `PRD_01_SERVER_DEPLOYMENT.md` — 服务器部署 PRD
- `PRD_02_COMBAT_STATS.md` — 战斗统计 PRD
- `PRD_03_UI_POLISH.md` — UI 打磨 PRD
- `PLAN_COMBAT_STATS.md` — 战斗贡献系统实现计划

## 反编译代码查询

当需要查看游戏源码时：
```
Read _decompiled/sts2/MegaCrit.Sts2.Core.Models.Cards/Bash.cs     → 具体卡牌实现
Read _decompiled/sts2/MegaCrit.Sts2.Core.Models/CardModel.cs      → 卡牌基类
Read _decompiled/sts2/MegaCrit.Sts2.Core.Commands/CreatureCmd.cs   → 伤害/治疗命令
Read _decompiled/sts2/MegaCrit.Sts2.Core.Hooks/Hook.cs            → 全局钩子系统
Read _decompiled/sts2/MegaCrit.Sts2.Core.Combat/CombatManager.cs  → 战斗管理器
```

## 关键约定

- Harmony patch 参数名必须匹配**编译后** DLL 的参数名（可能与反编译名不同）
- 异步方法的 Harmony Postfix 在第一个 `await` 处触发，不是方法完成后
- 所有 patch 方法必须用 `Safe.Run()` 包裹，避免异常导致游戏崩溃
- 调用 Godot API（如 `TranslationServer`、`Input`）必须在主线程
