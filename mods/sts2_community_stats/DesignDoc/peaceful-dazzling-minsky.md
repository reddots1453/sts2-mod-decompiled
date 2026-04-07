# GAP Analysis: PRD vs Code 现状对齐报告

> 产出: 差异分析 + 修正建议
> 范围: PRD-00 / PRD-01 / PRD-02 / PRD-03 全部验收标准

---

## PRD-00: 总体需求

### AC-01: 端到端数据流

| # | 验收标准 | 状态 | 说明 |
|---|---------|------|------|
| 1 | 服务器稳定运行 | ❌ | 服务器未部署，config.json 指向 localhost:8080 |
| 2 | 客户端局结束后数据上传成功 | ⚠️ | 代码完成（RunDataCollector→ApiClient），无服务器验证 |
| 3 | 其他玩家能查询统计数据 | ⚠️ | 代码完成（StatsProvider→ApiClient），当前使用 test_data.json 兜底 |
| 4 | 服务器不可达时不崩溃，进入离线队列 | ✅ | OfflineQueue 磁盘序列化 + 指数退避重试 |

### AC-02: 战斗统计准确性

| # | 验收标准 | 状态 | 说明 |
|---|---------|------|------|
| 1 | 5 角色卡牌贡献正确追踪 | ⚠️ | 代码存在，PRD 自身标记"已知遗漏" |
| 2 | 遗物被动贡献正确归因 | ⚠️ | 60+ relic hooks，但覆盖率未系统验证 |
| 3 | 药水贡献正确归因 | ✅ | PotionContextPatch + PotionUsedPatch 已修复 async bug |
| 4 | 间接伤害正确归因到来源 | ✅ | PowerHookContextPatcher 60+ hooks |
| 5 | Osty 召唤物伤害/防御正确归因 | ✅ | LIFO 栈追踪 + 专用处理 |
| 6 | 击杀最后敌人的伤害不遗漏 | ✅ | KillingBlowPatcher（手动 Harmony patch） |
| 7 | 自伤卡牌显示负防御 | ✅ | SelfDamage 字段 + 红色条 |
| 8 | 修正器加成正确拆分 | ✅ | ModifierDamage/ModifierBlock 分离追踪 |
| 9 | 生成/变化卡牌显示子条 | ✅ | CardOriginMap + OriginSourceId |
| 10 | 条形图颜色正确 | ✅ | 7+ 种颜色区分不同来源类型 |

### AC-03: UI 质量

| # | 验收标准 | 状态 | 说明 |
|---|---------|------|------|
| 1 | 贡献图表不遮挡关键游戏 UI | ⚠️ | 面板在左侧，无自适应碰撞检测 |
| 2 | 统计标签各分辨率不溢出 | ⚠️ | 硬编码像素位置，无缩放 |
| 3 | 长名称不截断或溢出 | ⚠️ | 固定宽度 110px + ClipText，需实际验证 |
| 4 | 颜色在深浅背景上可读 | ✅ | 深底浅字，对比度足够 |

### AC-04: 稳定性

| # | 验收标准 | 状态 | 说明 |
|---|---------|------|------|
| 1 | Mod 不导致游戏崩溃 | ✅ | 每个 patch 独立 try/catch + Safe.Run |
| 2 | Mod 不导致卡顿 | ✅ | 所有网络请求 async |
| 3 | Mod 加载无错误 | ⚠️ | 需运行时验证 |
| 4 | 卸载 Mod 后游戏正常 | ✅ | 纯运行时 patch，无游戏文件修改 |

---

## PRD-01: 服务器部署

### AC-01: 服务可用

| # | 验收标准 | 状态 | 说明 |
|---|---------|------|------|
| 1 | GET /health 返回 healthy | ✅ 代码就绪 ❌ 未部署 | server/app/main.py 有 health endpoint |
| 2 | HTTPS 证书有效 | ✅ 配置就绪 ❌ 未部署 | certbot + nginx SSL 配置完整 |
| 3 | 服务自启动/自恢复 | ✅ | docker-compose restart: unless-stopped |

### AC-02: 数据上传

| # | 验收标准 | 状态 | 说明 |
|---|---------|------|------|
| 1 | 种子脚本 100 条写入成功 | ✅ 脚本就绪 | server/scripts/seed_test_data.py |
| 2 | 客户端真实数据上传成功 | ❌ | 被部署阻塞 |
| 3 | 上传响应 < 2s | ❌ | 无法验证 |
| 4 | 畸形数据被拒绝 (422) | ✅ | Pydantic 校验 |

### AC-03~06 (查询/集成/容错/运维)

全部 **代码/配置级就绪**，被"服务器未部署"阻塞无法端到端验证。

**关键阻塞**: 服务器未部署是 PRD-01 所有验收标准的共同阻塞项。

---

## PRD-02: 战斗统计准确性

### 测试矩阵对照

#### 伤害归因 (D1-D10)

| 测试 | 场景 | 状态 | 问题 |
|------|------|------|------|
| D1 | 单体攻击 | ✅ | 代码路径完整 |
| D2 | AOE 攻击 | ✅ | 每个目标独立 DamageReceived |
| D3 | 多段攻击 | ✅ | 每次命中独立记录 |
| D4 | 击杀（过量伤害封顶） | ⚠️ | KillingBlowPatcher 捕获，但 TotalDamage 是否含过量伤害需验证 |
| D5 | 力量加成 | ✅ | DamageModifierPatch 正确拆分 |
| D6 | 易伤加成 | ⚠️ | 乘算贡献使用 baseDmg 而非累积后值，可能低估 |
| D7 | **中毒伤害** | **⚠️ 归类错误** | **PoisonPower 已有 hook 上下文，但伤害进入 DirectDamage 而非 AttributedDamage。OnPowerDamage() 存在但从未被调用。总和正确但分类不对** |
| D8 | 荆棘伤害 | ⚠️ | 同 D7，伤害进入 DirectDamage 而非 AttributedDamage |
| D9 | **升级增量** | **❌ 死代码** | **GetUpgradeDelta() 从未被调用。UpgradeDamage/UpgradeBlock 永远为 0。橙色升级段永远不显示** |
| D10 | 遗物被动伤害 | ✅ | RelicHookContextPatcher 上下文正确 |

#### 防御归因 (B1-B10)

| 测试 | 场景 | 状态 | 问题 |
|------|------|------|------|
| B1 | 基础格挡 | ✅ | FIFO 池正确 |
| B2 | 格挡溢出 | ✅ | FIFO 只消耗可用量 |
| B3 | FIFO 顺序 | ✅ | 从索引 0 开始消耗 |
| B4 | 敏捷加成 | ⚠️ | 近似分配 `totalBonus / count`，非精确 |
| B5 | 虚弱减伤 | ⚠️ | 公式 `actualDamage / 3` 是近似值（应为 25/75） |
| B6 | 缓冲吸收 | ✅ | BufferPower ModifyHpLost patch |
| B7 | 无实体减伤 | ✅ | IntangiblePower ModifyHpLost patch |
| B8 | **力量削减** | **❌ BUG** | **公式 `entry.Amount / totalReduction * totalReduction` 约分后等于 `entry.Amount`，错误地将原始力量削减量而非实际减伤量记录为防御** |
| B9 | 遗物被动格挡 | ✅ | RelicHookContextPatcher |
| B10 | 自伤 | ✅ | SelfDamage 字段 + 红色条 |

#### Osty 特殊场景 (O1-O3)

| 测试 | 状态 | 说明 |
|------|------|------|
| O1 Osty 吸收 | ✅ | LIFO 栈追踪 |
| O2 Osty 攻击 | ✅ | 攻击卡归因 + OSTY 子条 |
| O3 Doom 击杀 | ✅ | DoomKillPatch + AttributedDamage |

#### 特殊交互 (S1-S4)

| 测试 | 状态 | 说明 |
|------|------|------|
| S1 寻刃 | ✅ | SeekingEdge 上下文 + 目标重定向 |
| S2 卡牌变化子条 | ✅ | CardOriginMap + TagCardOrigin |
| S3 药水贡献 | ✅ | PotionContextPatch async bug 已修复 |
| S4 多修正器叠加 | ⚠️ | 乘算使用 baseDmg，近似值 |

#### 角色覆盖

| 角色 | 状态 | 说明 |
|------|------|------|
| Ironclad | ✅ | 力量/自伤/Rage/FlameBarrier |
| Silent | ⚠️ | 中毒归类错误（DirectDamage 而非 AttributedDamage） |
| Defect | ✅ | Orb 追踪 + Focus 拆分完整 |
| **Watcher** | **❌ 无覆盖** | **无姿态追踪（愤怒/神性/平静），无 Watcher 专用 patch** |
| Regent | ⚠️ | 辉星追踪有限（仅 VoidForm 费用节省） |
| Necrobinder | ✅ | Osty + Doom + 13 个 Power hook |

### PRD-02 验收标准

| AC | 状态 | 关键问题 |
|----|------|---------|
| AC-01 伤害归因 | ⚠️ | D7 中毒归类错/D9 升级增量死代码/D6 乘算近似 |
| AC-02 防御归因 | ⚠️ | B8 力量削减公式 BUG |
| AC-03 特殊机制 | ✅ | Osty/Doom/SeekingEdge 完整 |
| AC-04 角色覆盖 | ⚠️ | **Watcher 无覆盖** |
| AC-05 数据一致性 | ✅ | 战斗/Run/上传流水线一致 |
| AC-06 无崩溃 | ✅ | Safe.Run + TryPatch |

---

## PRD-03: UI 打磨

### AC-01: ContributionPanel

| # | 要求 | 状态 | 说明 |
|---|------|------|------|
| 1 | 面板居中 60%×70% | ❌ | 当前左对齐 510px，非居中 |
| 2 | Tab 切换 | ✅ | TabContainer |
| 3 | 条形图按值排序 + 尾部数值 | ✅ | OrderByDescending + 百分比标签 |
| 4 | **悬停工具提示** | **❌** | **无 MouseEntered/Tooltip 逻辑** |
| 5 | 零值来源隐藏 | ✅ | value <= 0 过滤 |
| 6 | **子条展开/折叠** | **❌** | **子条永远显示，无切换** |
| 7 | 内容可滚动 | ✅ | ScrollContainer 包裹 |
| 8 | 点击外部关闭 | ⚠️ | X 按钮有，外部点击无 |
| 9 | **分类折叠/展开** | **❌** | **分类标题是纯 Label，不可点击，无总计显示** |

### AC-02: StatsLabel

| # | 要求 | 状态 | 说明 |
|---|------|------|------|
| 1 | 标签位置正确 | ⚠️ | 商店标签在上方非下方，事件标签无显式位置 |
| 2 | 多分辨率可读 | ⚠️ | 硬编码 px，无缩放 |
| 3 | 加载/失败状态 | ✅ | ForLoading / ForUnavailable |
| 4 | **百分比一位小数 + 样本量** | **❌** | **使用 F0（整数），无样本量 n=X,XXX** |
| 5 | **低样本淡色/斜体** | **❌** | **未实现** |

### AC-03: FilterPanel

| # | 要求 | 状态 | 说明 |
|---|------|------|------|
| 1 | F9 开关 | ✅ | Toggle() |
| 2 | 进阶滑块 | ⚠️ | 用 SpinBox 不是 Slider |
| 3 | 筛选刷新统计 | ✅ | FilterApplied 事件 |
| 4 | **重置按钮** | **❌** | **只有 Apply，无 Reset** |

### AC-04: 颜色可读性 — ✅ 全部满足

### AC-05: 性能

| # | 要求 | 状态 | 说明 |
|---|------|------|------|
| 1 | 面板开关无卡顿 | ⚠️ | 每次 show 重建全部节点 |
| 2 | 50+ 条滚动流畅 | ⚠️ | MaxBarsPerSection=10 限制，部分数据被隐藏 |
| 3 | 标签不影响帧率 | ✅ | 纯 Label |

---

## 修正建议汇总（按优先级）

### 🔴 HIGH — 必须修复才能发布

| # | 问题 | 影响范围 | 修正动作 |
|---|------|---------|---------|
| H1 | B8 力量削减公式 BUG | CombatTracker.cs:240 | 公式改为 `entry.Amount / totalReduction * actualMitigated` |
| H2 | D9 升级增量死代码 | CombatTracker + ContributionMap | 在 OnCardPlayStarted 中消费 GetUpgradeDelta，或移除死代码和橙色段 |
| H3 | 服务器未部署 | config.json + server/ | 部署到 VPS，切换 config.json 到生产 URL |
| H4 | Watcher 无角色覆盖 | CombatHistoryPatch.cs | 添加 Wrath/Divinity 姿态上下文追踪 |

### 🟡 MEDIUM — 显著影响用户体验

| # | 问题 | 影响范围 | 修正动作 |
|---|------|---------|---------|
| M1 | D7/D8 中毒/荆棘归类错误 | CombatTracker.cs | 在 KillingBlowPatcher 或专用 patch 中调用 OnPowerDamage 而非走 DirectDamage |
| M2 | 面板非居中 | ContributionPanel.cs:40-45 | 改为居中 anchor，60%×70% 尺寸 |
| M3 | 无悬停工具提示 | ContributionChart.cs | 添加 MouseEntered/MouseExited + 格式化 Tooltip |
| M4 | 百分比整数 + 无样本量 | StatsLabel.cs 所有工厂方法 | F0→F1，添加 (n=X,XXX) |
| M5 | 无分类折叠/展开 | ContributionChart.cs | 分类标题改为可点击 + 折叠容器 |
| M6 | 无子条展开/折叠 | ContributionChart.cs | 添加切换图标 + 默认折叠 |
| M7 | 无 FilterPanel 重置按钮 | FilterPanel.cs | 添加 Reset 按钮 |

### 🟢 LOW — 改进但不阻塞发布

| # | 问题 | 影响范围 | 修正动作 |
|---|------|---------|---------|
| L1 | 乘算修正器近似 | CombatHistoryPatch.cs | 使用累积后值替代 baseDmg |
| L2 | 虚弱减伤近似 | CombatTracker.cs:330 | 改为 `actualDamage * 25 / 75` |
| L3 | 多分辨率未适配 | StatsLabel + 各 Patch | 相对定位或缩放 |
| L4 | 点击外部关闭面板 | ContributionPanel.cs | 添加全屏透明背景 |
| L5 | 低样本淡色/斜体 | StatsLabel.cs | 检查 n<50 并应用样式 |
| L6 | FilterPanel 滑块替代 SpinBox | FilterPanel.cs | HSlider 替代 SpinBox |
| L7 | 面板每次 show 重建节点 | ContributionPanel.cs | 缓存避免重建 |

---

## 统计总览

| PRD | ✅ Met | ⚠️ Partial | ❌ Not Met | 关键阻塞 |
|-----|--------|-----------|-----------|---------|
| PRD-00 | 11 | 8 | 1 | 服务器未部署 |
| PRD-01 | 14 | 0 | 2 | 服务器未部署（配置全就绪） |
| PRD-02 | 18 | 8 | 2 | B8 公式 BUG + D9 死代码 + Watcher 无覆盖 |
| PRD-03 | 11 | 8 | 6 | 面板居中/工具提示/折叠/样本量 |
| **合计** | **54** | **24** | **11** | |
