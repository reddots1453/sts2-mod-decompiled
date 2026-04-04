namespace CommunityStats.Config;

/// <summary>
/// Simple i18n system for the mod. Supports English and Chinese.
/// All UI strings go through L.Get("key") to support language switching.
/// </summary>
public static class L
{
    public enum Lang { EN, CN }

    public static Lang Current { get; set; } = Lang.CN;

    public static string Get(string key) =>
        Current == Lang.CN
            ? (CN.TryGetValue(key, out var v) ? v : key)
            : (EN.TryGetValue(key, out var v2) ? v2 : key);

    // ── English ─────────────────────────────────────────────
    private static readonly Dictionary<string, string> EN = new()
    {
        // ContributionPanel
        ["contrib.title"] = "Contribution Stats",
        ["contrib.last_combat"] = "Last Combat",
        ["contrib.run_summary"] = "Run Summary",
        ["contrib.vs"] = "vs",

        // ContributionChart
        ["chart.damage"] = "Damage",
        ["chart.defense"] = "Defense",
        ["chart.draw"] = "Card Draw",
        ["chart.energy"] = "Energy Gained",
        ["chart.stars"] = "Stars",
        ["chart.healing"] = "Healing",
        ["chart.none"] = "  (none)",

        // FilterPanel / Settings
        ["settings.title"] = "Community Stats — Settings",
        ["settings.auto_asc"] = "Auto-match my Ascension",
        ["settings.min_asc"] = "Min Ascension:",
        ["settings.max_asc"] = "Max Ascension:",
        ["settings.min_wr"] = "Min Player Win%:",
        ["settings.version"] = "Version:",
        ["settings.ver_current"] = "Current",
        ["settings.ver_all"] = "All Versions",
        ["settings.sample"] = "Filtered data: {0} runs",
        ["settings.no_data"] = "No data loaded",
        ["settings.apply"] = "Apply",
        ["settings.upload"] = "Upload run data",
        ["settings.language"] = "Language:",

        // StatsLabel
        ["stats.pick"] = "Pick {0}% | Win {1}%",
        ["stats.relic"] = "Pick {0}% | Win {1}%",
        ["stats.event"] = "Chosen {0}% | Win {1}%",
        ["stats.encounter"] = "Death {0}% | Avg DMG {1}",
        ["stats.upgrade"] = "Upgrade {0}% | Win {1}%",
        ["stats.remove"] = "Remove {0}% | Win {1}%",
        ["stats.buy"] = "Buy {0}% | Win {1}%",
        ["stats.loading"] = "Loading...",
        ["stats.no_data"] = "No data",

        // Healing sources
        ["source.rest_site"] = "Rest Site",
        ["source.event"] = "Event",
        ["source.event_prefix"] = "[E] ",
        ["source.merchant"] = "Merchant",
        ["source.floor_regen"] = "Floor {0} Recovery",

        // ModSettingsPatch
        ["mod.settings_btn"] = "Community Stats Settings",
    };

    // ── 中文 ────────────────────────────────────────────────
    private static readonly Dictionary<string, string> CN = new()
    {
        // ContributionPanel
        ["contrib.title"] = "战斗贡献统计",
        ["contrib.last_combat"] = "上场战斗",
        ["contrib.run_summary"] = "本局汇总",
        ["contrib.vs"] = "对战",

        // ContributionChart
        ["chart.damage"] = "伤害",
        ["chart.defense"] = "防御",
        ["chart.draw"] = "抽牌",
        ["chart.energy"] = "能量获取",
        ["chart.stars"] = "辉星",
        ["chart.healing"] = "治疗",
        ["chart.none"] = "  (无)",

        // FilterPanel / Settings
        ["settings.title"] = "社区统计 — 设置",
        ["settings.auto_asc"] = "自动匹配我的进阶",
        ["settings.min_asc"] = "最低进阶:",
        ["settings.max_asc"] = "最高进阶:",
        ["settings.min_wr"] = "最低玩家胜率:",
        ["settings.version"] = "版本:",
        ["settings.ver_current"] = "当前版本",
        ["settings.ver_all"] = "所有版本",
        ["settings.sample"] = "数据范围: {0} 局",
        ["settings.no_data"] = "未加载数据",
        ["settings.apply"] = "应用",
        ["settings.upload"] = "上传游玩数据",
        ["settings.language"] = "语言:",

        // StatsLabel
        ["stats.pick"] = "选取 {0}% | 胜率 {1}%",
        ["stats.relic"] = "选取 {0}% | 胜率 {1}%",
        ["stats.event"] = "选择 {0}% | 胜率 {1}%",
        ["stats.encounter"] = "死亡 {0}% | 平均伤害 {1}",
        ["stats.upgrade"] = "升级 {0}% | 胜率 {1}%",
        ["stats.remove"] = "移除 {0}% | 胜率 {1}%",
        ["stats.buy"] = "购买 {0}% | 胜率 {1}%",
        ["stats.loading"] = "加载中...",
        ["stats.no_data"] = "无数据",

        // Healing sources
        ["source.rest_site"] = "篝火休息",
        ["source.event"] = "事件",
        ["source.event_prefix"] = "[事件] ",
        ["source.merchant"] = "商店",
        ["source.floor_regen"] = "第{0}层恢复",

        // ModSettingsPatch
        ["mod.settings_btn"] = "社区统计设置",
    };
}
