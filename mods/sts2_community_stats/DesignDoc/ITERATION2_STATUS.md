# 第二轮迭代 — 进度与上下文

> 最后更新：2026-04-09  
> 此文件供跨设备会话使用，确保新会话能完整理解当前状态

---

## 当前位置

**工作流阶段**：Stage 2 完成（PRD 已写）→ 下一步 Stage 3（Plan + Review）

**核心文档**：
- `PRD_04_ITERATION2.md` — 第二轮迭代完整需求文档（v2.0，用户已审阅修订）
- `PRD_00_MASTER.md` — 总体需求文档（含第一轮已实现的 *NEW* 标记）
- `WORKFLOW.md` — 文档驱动工作流定义（六阶段）
- `PLAN_NEW_FEATURES.md` — 第一轮 NEW 功能实施计划（已完成，含已知限制）
- `PLAN_COMBAT_STATS.md` — 第一轮战斗统计实施计划（已完成）

---

## 第一轮迭代成果（已完成）

- 战斗贡献统计核心：36/36 自动化测试通过（sts2_contrib_tests mod）
- 测试覆盖：直接伤害(6)、修正拆分(6)、防御归因(8)、间接伤害(3)、来源优先级(2)、跨角色(4)、NEW功能(4)、一致性(3)
- NEW 功能已实现：免费能量/辉星归因（两层架构 source tag + play-time 比较）、最大 HP 治疗（GainMaxHp patch + heal 抑制 flag）、Stars 获得追踪（PlayerCmd.GainStars patch）
- 关键修复：block pool 回合同步（AfterBlockCleared）、Intangible/Colossus 在 ModifyDamage 阶段追踪、GetPowerSource 全局 fallback、debug 日志已清除

---

## 第二轮迭代需求概要（PRD-04）

### 15 个功能需求

| # | 功能 | 复杂度 |
|---|------|:---:|
| 3.1 | Mod 重命名 → "Stats the Spire" | 低 |
| 3.2 | Compendium 卡牌图书馆：选取率+胜率 | 中 |
| 3.3 | Compendium 遗物收藏：胜率+胜率浮动 | 中 |
| 3.4 | 遗物胜率浮动（全局所有 tooltip） | 低 |
| 3.5 | 事件选项：仅显示选择率 | 低 |
| 3.6 | 实时贡献更新（每次打牌后刷新） | 中 |
| 3.7 | 每回合平均伤害 | 低 |
| 3.8 | 问号房间遭遇概率（地图悬停） | 中 |
| 3.9 | 药水掉落概率（屏幕顶部边角） | 中 |
| 3.10 | 敌人意图状态机（悬停弹出面板） | 高 |
| 3.11 | 个人生涯统计（注入百科大全→角色数据→统计栏） | 高 |
| 3.12 | Run History 增强（本局统计+贡献回看按钮） | 高 |
| 3.13 | 筛选面板"我的数据"（本地 RunHistory 聚合） | 中 |
| 3.14 | 功能开关（每个子功能独立开关） | 中 |
| 3.15 | 多人游戏基础兼容 | 低 |

### §4 PRD-00 补全项（6 条）
- 4.1 多乘法修正器超算：按比例缩放，约束 Direct+Modifier=Total
- 4.2 Confused/SneckoEye：计入能量贡献，可为负（3 个来源实体）
- 4.3 Enlightenment 部分降费也追踪
- 4.4 Buffer 防御计算补充数值示例
- 4.5 贡献面板 UI：左侧、可拖拽、子来源自动展开可收起
- 4.6 离线队列限制 10 条/7 天、不上传 Steam ID

---

## 第一阶段探索成果（Stage 1 发散需求已完成）

### Compendium 钩子
- 卡牌图书馆：`NCardLibraryGrid.InitGrid()` postfix + `AssignCardsToRow()` postfix
- 遗物收藏：`NRelicCollectionEntry.OnFocus()` postfix
- 现有 StatsLabel + meta 标记模式可复用

### 问号房间概率
- `UnknownMapPointOdds` 有公开属性 MonsterOdds/EliteOdds/TreasureOdds/ShopOdds/EventOdds
- 基础：事件 85%、怪物 10%、宝箱 2%、商店 3%，怜悯机制动态调整
- 现有 MapPointOverlay + MapPointPatch 基础设施

### 敌人意图状态机
- MonsterMoveStateMachine：States(public)、_currentState(private)、StateLog(public)
- 状态类型：MoveState(叶子)、RandomBranchState(加权随机)、ConditionalBranchState(条件)
- NIntent.OnHovered() 已有 hover tip 机制，可扩展
- UI 参考：STS1 mod 截图（Cultist 简单循环、Jaw Worm 随机分支+概率+约束）

### 药水掉落概率
- PotionRewardOdds.CurrentValue（公开属性）
- 基础 40%，怜悯 ±10%/次，精英 +12.5%
- UI：屏幕顶部边角药水图标+百分比，悬停展开未来 1/2/3 场累计概率

### RunHistory 数据结构
- SaveManager.GetAllRunHistoryNames() + LoadRunHistory() 公开 API
- RunHistory 字段：Win, KilledByEncounter/Event, Ascension, Character, MapPointHistory
- PlayerMapPointHistoryEntry：CardsGained, CardsRemoved, RelicsBought, DamageTaken 等
- MapPointType 含 Ancient 类型，可统计先古遗物选择

---

## 待办：UI 设计

PRD-04 §3.11（个人统计）和 §3.12（Run History）的 UI 设计标注为"待完善"。
建议：从反编译代码提取 STS2 UI 设计规范（Theme/StyleBox/Font/颜色方案），然后用 Figma 或其他工具设计 mockup。

---

## 下一步操作

```
1. 阅读 PRD_04_ITERATION2.md（完整需求）
2. 阅读 WORKFLOW.md §3（Plan + Review 流程）
3. 启动 Stage 3：
   a. Plan Agent：读 PRD-04 + 源代码，产出实施计划
   b. Review Agent：审查计划，2 轮循环
4. 可选：先提取 UI 设计规范（从反编译代码中的 Theme/StyleBox）
5. 进入 Stage 4 实现
```
