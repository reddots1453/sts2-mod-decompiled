# History

## Description
- Record each Git change or local file change summary.
- Each entry includes reason, impact scope, and related links (put links in Notes when applicable).
- If owner is not specified, default to `<project-name>-agent-1`.
- Use datetime format `YYYY-MM-DD HH:MM` (24h).

## Mandatory Action
- MUST: When this table reaches 50 entries, compress the records into shorter and more general summaries, keeping stable and reusable change points.

## Record Template
| Date Time | Type | Summary | Reason | Impact Scope | Owner Id | Notes |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| 2026-04-19 | fix | CombatTracker OnCombatEnd 对称清零 AsyncLocal 上下文 + 抽取 ClearAsyncContext() | Beta 审查发现战后 BurningBlood/休息点治疗可能读到上一场最后一张牌的残留 active* 句柄 | CombatTracker.cs (OnCombatStart/OnCombatEnd/新 ClearAsyncContext) | sts2_community_stats-member-01KPJ60YC4SEP7QS6R1EKN0B80 | 仅本成员作用域编译无 error;CareerStatsCache.cs 错误属其他成员并行工作 |
| 2026-04-19 | fix | orb 归因泄漏 4 连修:null-source 早返回泄漏、async Postfix 过早清理、power/relic 钩子作用域残留 | Owner 报告 orb 有时把其他实体效果归到充能球通道卡上(尤其毒伤/反伤/回合末遗物) | CombatHistoryPatch.cs (SetOrbContextForPassive/SetOrbEvokeContext/移除 AfterOrbPassive postfix/SetPowerContext/SetRelicContext) | sts2_community_stats-member-01KPJ60YC4SEP7QS6R1EKN0B80 | dotnet build Build succeeded,0 error |
| 2026-04-19 14:46 | fix | H1 导入断点续传：ModConfig 新增 HistoryImportInProgress + ImportedRunHashes；HistoryImporter 同意后立即落盘 in-progress，每次成功上传按 run_hash 持久化；下次启动静默续跑并按哈希跳过已上传条目 | 修复 Beta 审查 H1：50+局×6.5s 导入过程中关游戏会重复弹同意框并从 0 重跑全部上传 | mods/sts2_community_stats/src/Config/ModConfig.cs, mods/sts2_community_stats/src/Api/HistoryImporter.cs | sts2_community_stats-member-01KPJ61BNQ0NBM3HX0B6B9JREA | Build succeeded（临时 checkout HEAD 版 UnknownRoomPanel.cs 验证后还原他人在途改动，未覆盖）|
