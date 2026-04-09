# STS2 UI 设计规范 — Stats the Spire

> 版本：1.0 | 日期：2026-04-09
> 来源：STS2 反编译源码 + 现有 Mod 代码
> 用途：所有 Stats the Spire UI 设计的约束文档

---

## 1. 色彩系统

### 1.1 StsColors 核心色板

来源：`_decompiled/sts2/MegaCrit.Sts2.Core.Helpers/StsColors.cs`

| 语义 | 字段名 | 值 | 用途 |
|------|--------|-----|------|
| **主文本** | cream | #FFF6E2 | 正文、数据标签 |
| **半透明主文本** | halfTransparentCream | #FFF6E280 | 次要文本 |
| **强调/金色** | gold | #EFC851 | 标题、强调数值、箭头 |
| **水色** | aqua | #2AEBBE | 高亮、链接、循环标记 |
| **红色** | red | #FF5555 | 负值、伤害、警告 |
| **绿色** | green | #7FFF00 | 正值、治疗、增益 |
| **蓝色** | blue | #87CEEB | 格挡值 |
| **深蓝** | darkBlue | #67AEEB | — |
| **橙色** | orange | #FFA518 | 约束标签、次要强调 |
| **粉色** | pink | #FF78A0 | — |
| **紫色** | purple | #EE82EE | — |
| **浅灰** | lightGray | rgba(191,191,191) | 禁用文本 |
| **灰色** | gray | rgba(127,127,127) | 禁用按钮 |

### 1.2 透明度/遮罩色

| 字段名 | 值 | 用途 |
|--------|-----|------|
| screenBackdrop | rgba(0,0,0,0.8) | 模态遮罩 |
| halfTransparentBlack | rgba(0,0,0,0.5) | 半透明覆盖 |
| ninetyPercentBlack | rgba(0,0,0,0.9) | 深色背景 |
| quarterTransparentBlack | rgba(0,0,0,0.25) | 轻覆盖 |
| halfTransparentWhite | rgba(255,255,255,0.5) | 半透明白 |
| quarterTransparentWhite | rgba(255,255,255,0.25) | 轻白覆盖 |

### 1.3 特殊 UI 色

| 字段名 | 值 | 用途 |
|--------|-----|------|
| settingTabsButtonOutline | #B1F8FF | 设置标签按钮描边 |
| energyBlue | #40FFFF | 能量蓝 |
| targetingArrowEnemy | #E61E1B | 敌方瞄准箭头 |
| targetingArrowAlly | #36C78A | 友方瞄准箭头 |
| merchantBlue | #516ACF | 商人蓝 |
| legendText | #2B3152 | 图例文字 |

### 1.4 卡牌稀有度描边色

| 稀有度 | 字段名 | 值 |
|--------|--------|-----|
| Common | cardTitleOutlineCommon | #4D4B40FF |
| Uncommon | cardTitleOutlineUncommon | #005C75FF |
| Rare | cardTitleOutlineRare | #6B4B00FF |
| Curse | cardTitleOutlineCurse | #550B9EFF |

### 1.5 Mod 图表色

来源：`mods/sts2_community_stats/src/UI/ContributionChart.cs`

| 语义 | 变量名 | 值（RGBA float） | 近似 Hex |
|------|--------|------------------|----------|
| 卡牌 | CardBarColor | (0.3, 0.5, 0.9) | #4D80E5 |
| 遗物 | RelicBarColor | (0.9, 0.75, 0.2) | #E5BF33 |
| 药水 | PotionBarColor | (0.2, 0.85, 0.6) | #33D999 |
| 归因伤害 | AttrBarColor | (0.5, 0.7, 1.0) | #80B3FF |
| 修正伤害 | ModifierBarColor | (0.7, 0.5, 0.9) | #B380E5 |
| 减伤 | MitigateBarColor | (0.4, 0.8, 0.5) | #66CC80 |
| 力量削减 | StrReduceBarColor | (0.9, 0.5, 0.2) | #E58033 |
| 辉星 | StarBarColor | (1.0, 0.85, 0.3) | #FFD94D |
| 自伤 | SelfDmgBarColor | (0.9, 0.25, 0.2) | #E5403C |
| 治疗 | HealBarColor | (0.2, 0.9, 0.3) | #33E54D |
| Osty | OstyBarColor | (0.6, 0.9, 0.5) | #99E580 |
| 子来源 | SubBarColor | (0.4, 0.6, 0.8, 0.7) | #6699CC B2 |
| 标题 | HeaderColor | (1, 1, 1) | #FFFFFF |
| 出牌次数 | PlayCountColor | (0.6, 0.6, 0.7) | #9999B3 |

---

## 2. 排版系统

### 2.1 字号层级

| 层级 | 大小 | 用途 |
|------|------|------|
| **标题** | 16px | 面板标题、模块标题 |
| **节标题** | 14px | 分类标题（伤害/防御/抽牌）、InfoMod 面板标题 |
| **正文** | 12px | 数据行、名称标签、概率值 |
| **辅助** | 11px | 子来源名、出牌次数、副标题、约束标签 |

### 2.2 文本颜色层级

| 层级 | RGBA float | 用途 |
|------|-----------|------|
| 高亮 | (0.9, 0.9, 0.9) | 顶级来源名 |
| 标准 | (0.8, 0.8, 0.8) | 节标题 |
| 次要 | (0.7, 0.7, 0.8) | 子来源名 |
| 暗淡 | (0.5, 0.5, 0.6) | 副标题、空状态"(无)" |

### 2.3 字体

- **MegaLabel**：标准标签，支持 Godot 主题系统
- **MegaRichTextLabel**：富文本标签，支持 BBCode（颜色、图标内联）
- 图标内联语法：`[img=top]res://images/packed/sprite_fonts/gold_icon.png[/img]`

---

## 3. 面板系统

### 3.1 主面板（ContributionPanel 风格）

来源：`mods/sts2_community_stats/src/UI/ContributionPanel.cs`

```
StyleBoxFlat:
  BgColor:     rgba(0.05, 0.05, 0.1, 0.92)    // 深蓝黑，92% 不透明
  BorderColor: rgba(0.3, 0.4, 0.7, 0.6)        // 浅蓝，60% 不透明
  BorderWidth: 1px (all sides)
  CornerRadius: 8px (all corners)
  ContentMargin: Left 16px, Right 16px, Top 12px, Bottom 12px
  CustomMinimumSize: 500×400px
```

### 3.2 次面板（FilterPanel / InfoMod 风格）

来源：`mods/sts2_community_stats/src/UI/FilterPanel.cs`

```
StyleBoxFlat:
  BgColor:     rgba(0.08, 0.08, 0.12, 0.95)    // 深蓝黑，95% 不透明
  BorderColor: rgba(0.4, 0.4, 0.6, 0.5)         // 浅蓝灰，50% 不透明
  BorderWidth: 1px (all sides)
  CornerRadius: 6px (all corners)
  ContentMargin: Left 12px, Right 12px, Top 10px, Bottom 10px
  CustomMinimumSize: 340×420px
```

### 3.3 InfoMod 风格面板（概率显示/商店价格等）

基于次面板微调，用于 §3.8/3.9/3.16/3.17 等 STS1 InfoMod 风格的悬停面板：

```
StyleBoxFlat:
  BgColor:     rgba(0.08, 0.08, 0.12, 0.95)
  BorderColor: rgba(0.3, 0.4, 0.6, 0.4)         // 略淡边框
  BorderWidth: 1px (all sides)
  CornerRadius: 6px (all corners)
  ContentMargin: Left 12px, Right 12px, Top 10px, Bottom 10px
  分隔线: rgba(0.3, 0.3, 0.4) 1px HSeparator
```

### 3.4 子面板/分支框（意图状态机内部框）

```
StyleBoxFlat:
  BgColor:     rgba(0.1, 0.12, 0.18, 0.8)
  BorderColor: rgba(0.4, 0.5, 0.7, 0.5)
  BorderWidth: 1px
  CornerRadius: 4px
```

### 3.5 当前状态高亮框

```
StyleBoxFlat:
  BgColor:     rgba(0.15, 0.2, 0.3, 0.8)
  LeftBorder:  3px aqua #2AEBBE                  // 左侧高亮边框
```

---

## 4. 交互模式

### 4.1 悬停/按下动画

来源：`_decompiled/sts2/.../NTickbox.cs`, `NButton.cs`

| 动作 | 属性 | 值 | 时长 | 缓动 |
|------|------|-----|------|------|
| 悬停 | scale | 1.05× | 0.05s | — |
| 取消悬停 | scale | 1.0× | 0.5s | Expo.Out |
| 按下 | scale | 0.95× | 0.5s | Expo.Out |
| HSV 亮度 | V | 1.2 | 0.05s | — |

### 4.2 面板动画

| 动作 | 属性 | 值 | 时长 | 缓动 |
|------|------|-----|------|------|
| 面板淡入 | modulate | 0→1 | 0.5s | Cubic.Out |
| 模态遮罩 | opacity | 0→0.8 | 0.3s | — |

### 4.3 音效

| 动作 | 事件路径 |
|------|---------|
| 勾选开启 | `event:/sfx/ui/clicks/ui_checkbox_on` |
| 勾选关闭 | `event:/sfx/ui/clicks/ui_checkbox_off` |

---

## 5. Tooltip 系统

来源：`_decompiled/sts2/.../NHoverTipSet.cs`

| 属性 | 值 |
|------|-----|
| 固定宽度 | 360px |
| 间距 | 5px |
| 底部安全区 | 50px（防止溢出视口） |
| 对齐模式 | Left / Right / Center（自适应） |
| 容器类型 | VFlowContainer (文本) + NHoverTipCardContainer (卡牌) |
| 定位逻辑 | 若 GlobalX < 40% 屏幕宽度 → 左侧显示；否则右侧显示 |

### 5.1 Tooltip 内容格式

- 使用 `MarginContainer` + `VBoxContainer` 布局
- 支持 `RichTextLabel` 图标内联：`[img=top]path[/img]`
- 分隔线：HSeparator 或 `────────` 文本

---

## 6. 控件参考

### 6.1 NTickbox（复选框）

来源：`_decompiled/sts2/.../NTickbox.cs`

- 视觉：`%TickboxVisuals` 包含勾选/未勾选图片
- 悬停 scale 1.05×、按下 scale 0.95×
- ShaderMaterial HSV 调整（悬停 V=1.2）
- 信号：`Toggled(NTickbox)`

### 6.2 NButton（按钮）

- 基础交互按钮，StyleBox 样式
- 按下时长 0.3s，取消悬停 0.5s

### 6.3 NStatEntry（统计条目）

来源：`_decompiled/sts2/.../NStatEntry.cs`

- 结构：Icon (TextureRect) + TopLabel (MegaRichTextLabel) + BottomLabel (MegaRichTextLabel)
- 悬停 scale 1.05× in 0.05s
- Tooltip 定位：GlobalX < 40% → 左偏 392px；否则右偏 532px

### 6.4 NDropdownPositioner（下拉选单）

- 用于先古遗物选择、Boss 选择等下拉菜单
- 支持游戏原生图标

### 6.5 NPaginator / NSettingsSlider

- 用于设置面板中的分页/滑块控件

---

## 7. 图标资源路径

### 7.1 内联图标（RichTextLabel 用）

| 图标 | 路径 |
|------|------|
| 金币 | `res://images/packed/sprite_fonts/gold_icon.png` |
| 卡牌 | `res://images/packed/sprite_fonts/card_icon.png` |
| 遗物/宝箱 | `res://images/packed/sprite_fonts/chest_icon.png` |
| 药水 | `res://images/packed/sprite_fonts/potion_icon.png` |

### 7.2 统计图标（Atlas）

| 图标 | 路径 |
|------|------|
| 时钟（游玩时间） | `atlases/stats_screen_atlas.sprites/stats_clock.tres` |
| 剑（胜负） | `atlases/stats_screen_atlas.sprites/stats_swords.tres` |
| 链条（连胜） | `atlases/stats_screen_atlas.sprites/stats_chain.tres` |

---

## 8. 图表布局常量

来源：`mods/sts2_community_stats/src/UI/ContributionChart.cs`

| 常量 | 值 | 用途 |
|------|-----|------|
| BarHeight | 20px | 条形图高度 |
| MaxBarWidth | 260px | 条形图最大宽度 |
| MaxBarsPerSection | 10 | 每节最多显示条数 |
| SourceNameWidth | 110px (顶级) / 100px (子级) | 来源名列宽 |
| PlayCountWidth | 30px | 出牌次数列宽 |
| RowSeparation | 4px | 行间距 |
| SectionSeparation | 2px | 节分隔线 |

---

## 9. 全局约定

1. **所有百分比数值显示到小数点后 1 位**（如 45.2%、52.1%）
2. **颜色必须来自本文档色板**，不得自创颜色
3. **注入 UI 必须通过 Harmony postfix**，不修改游戏原始节点树
4. **Tooltip 宽度固定 360px**，使用 NHoverTipSet 系统
5. **字号不超过 4 级**：16/14/12/11px
6. **交互动画使用标准时长**：hover 0.05s、press 0.5s Expo.Out、fade 0.3-0.5s
