# STS2 Knowledge Base / 杀戮尖塔2 知识库

> 版本: v0.102.0 | 提取日期: 2026-04-07
> 数据来源: 游戏运行时通过 sts2_kb_extractor mod 提取
> 语言: EN + CN 双语

## 数据统计

| 类别 | 数量 | JSON 文件 | Markdown 文档 |
|------|------|-----------|--------------|
| 卡牌 Cards | 577 | cards.json (383KB) | docs/cards_{character}.md |
| 遗物 Relics | 289 | relics.json (221KB) | docs/relics.md |
| 药水 Potions | 64 | potions.json (36KB) | docs/potions.md |
| 怪物 Monsters | 102 | monsters.json (48KB) | docs/bestiary.md |
| 遭遇 Encounters | 81 | encounters.json (25KB) | docs/encounters.md |
| 事件 Events | 57 | events.json (73KB) | docs/events.md |
| 能力 Powers | — | powers.json (待修复) | docs/powers.md |
| 术语 Glossary | — | — | glossary.md |

## 文件说明

### JSON 文件（机器查询用）
每个 JSON 文件是一个字典，key 为实体 ID（大写蛇形命名如 `BASH`、`BURNING_BLOOD`）。

**查询示例**:
```
Grep "BASH" KnowledgeBase/cards.json        → 查找重击卡牌
Grep "BURNING_BLOOD" KnowledgeBase/relics.json → 查找燃烧之血遗物
Grep "INKLET" KnowledgeBase/monsters.json    → 查找墨滴怪物
```

### Markdown 文档（人类阅读用）
按角色/类别分组，双语对照，包含升级数值（括号内为升级后值）。

### glossary.md
枚举/术语速查表：CardType、Rarity、TargetType、Keywords、角色、幕。

## JSON Schema

### 卡牌 (cards.json)
```json
{
  "CARD_ID": {
    "id": "CARD_ID",
    "name": { "en": "English Name", "cn": "中文名" },
    "description": { "en": "...", "cn": "..." },
    "type": "Attack|Skill|Power|Status|Curse",
    "rarity": "Basic|Common|Uncommon|Rare|Ancient|...",
    "cost": 1,
    "target": "Self|AnyEnemy|AllEnemies|...",
    "character": "IRONCLAD|SILENT|DEFECT|NECROBINDER|REGENT|Colorless",
    "keywords": ["Exhaust", "Ethereal", ...],
    "tags": ["Strike", "Defend", ...],
    "vars": { "Damage": [base, upgraded], "Block": [base, upgraded] }
  }
}
```

### 遗物 (relics.json)
```json
{
  "RELIC_ID": {
    "id": "RELIC_ID",
    "name": { "en": "...", "cn": "..." },
    "description": { "en": "...", "cn": "..." },
    "flavor": { "en": "...", "cn": "..." },
    "rarity": "Starter|Common|Uncommon|Rare|Shop|Event|Ancient",
    "character": "IRONCLAD|...|Shared",
    "vars": { "VarName": value }
  }
}
```

### 怪物 (monsters.json)
```json
{
  "MONSTER_ID": {
    "id": "MONSTER_ID",
    "name": { "en": "...", "cn": "..." },
    "hp": { "min": 11, "max": 17 },
    "moves": {
      "MOVE_NAME": { "intents": ["Attack(5)", "Buff"], "followUp": "NEXT_MOVE" }
    },
    "ai": "Random(Move1, Move2) → Move3 → loop"
  }
}
```

### 遭遇 (encounters.json)
```json
{
  "ENCOUNTER_ID": {
    "id": "ENCOUNTER_ID",
    "name": { "en": "...", "cn": "..." },
    "roomType": "Monster|Elite|Boss",
    "isWeak": false,
    "monsters": ["MONSTER_ID_1", "MONSTER_ID_2"],
    "acts": ["OVERGROWTH", "HIVE"]
  }
}
```

### 事件 (events.json)
```json
{
  "EVENT_ID": {
    "id": "EVENT_ID",
    "name": { "en": "...", "cn": "..." },
    "description": { "en": "...", "cn": "..." },
    "options": [
      { "index": 0, "text_en": "Option text", "isLocked": false }
    ]
  }
}
```

## 更新知识库

1. 安装 `sts2_kb_extractor` mod 到游戏 `mods/` 目录
2. 将游戏语言设为**中文**（确保双语提取）
3. 启动游戏，进入主菜单
4. 按 **F6** 触发提取
5. 数据输出到本目录
