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
        ["settings.title"] = "Stats the Spire — Settings",
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
        ["stats.event_pick"] = "Chosen {0}%",
        ["stats.encounter"] = "Death {0}% | Avg DMG {1}",
        ["stats.upgrade"] = "Upgrade {0}% | Win {1}%",
        ["stats.remove"] = "Remove {0}% | Win {1}%",
        ["stats.buy"] = "Buy {0}% | Win {1}%",
        ["stats.loading"] = "Loading...",
        ["stats.no_data"] = "No data",

        // Unknown Room panel (§3.8)
        ["unknown.title"] = "Next [?] Room",
        ["unknown.subtitle"] = "Current encounter odds",
        ["room.event"] = "Event",
        ["room.monster"] = "Monster",
        ["room.elite"] = "Elite",
        ["room.treasure"] = "Treasure",
        ["room.shop"] = "Shop",

        // Potion drop odds (§3.9)
        ["potion.title"] = "Potion Chance",
        ["potion.subtitle"] = "Cumulative odds over N combats",
        ["potion.within"] = "Within {0} combats",

        // Card drop odds (§3.17)
        ["carddrop.title"] = "Card Drops (Next Combat)",
        ["carddrop.subtitle"] = "Probability of at least one per rarity",
        ["carddrop.rare"] = "Rare",
        ["carddrop.uncommon"] = "Uncommon",
        ["carddrop.common"] = "Common",
        ["carddrop.col_regular"] = "Regular",
        ["carddrop.col_elite"] = "Elite",

        // Shop prices (§3.16)
        ["shop.title"] = "Shop Prices",
        ["shop.subtitle"] = "Half off when discounted; colorless +15%",
        ["shop.removal"] = "Remove a card: {0}",
        ["shop.relics"] = "Relics",
        ["shop.cards"] = "Cards",
        ["shop.potions"] = "Potions",
        ["shop.col_common"] = "Common",
        ["shop.col_uncommon"] = "Uncommon",
        ["shop.col_rare"] = "Rare",

        // Healing sources
        ["source.rest_site"] = "Rest Site",
        ["source.event"] = "Event",
        ["source.event_prefix"] = "[E] ",
        ["source.merchant"] = "Merchant",
        ["source.floor_regen"] = "Floor {0} Recovery",

        // ModSettingsPatch
        ["mod.settings_btn"] = "Stats the Spire Settings",

        // Feature toggles (§3.14)
        ["toggle.contribution_panel"] = "Combat Contribution Panel (F8 + real-time)",
        ["toggle.card_library_stats"] = "Card Pick/Win Rate in Library",
        ["toggle.relic_stats"] = "Relic Win Rate / Delta Display",
        ["toggle.event_pick_rate"] = "Event Option Pick Rate",
        ["toggle.monster_danger"] = "Monster Room Danger Rating",
        ["toggle.unknown_room_odds"] = "Unknown Room Encounter Odds",
        ["toggle.potion_drop_odds"] = "Potion Drop Chance Display",
        ["toggle.card_drop_odds"] = "Card Drop Rarity Odds",
        ["toggle.shop_prices"] = "Shop Price Table",
        ["toggle.intent_state_machine"] = "Enemy Intent State Machine",
        ["toggle.career_stats"] = "Personal Career Stats",

        // ContributionPanel v2 (§3.7, §4.5)
        ["contrib.dps"] = "Avg Damage per Turn:",
        ["contrib.help_title"] = "Stats the Spire — Help",
        ["contrib.help_body"] = "Bar colors: Cards (blue), Relics (gold), Potions (green)\nHotkeys: F8 Toggle Panel, F9 Settings\nDrag title bar to reposition.",

        // Probability panels (§3.8, §3.9, §3.16, §3.17)
        ["prob.unknown_title"] = "Next [?] Room",
        ["prob.unknown_sub"] = "Current encounter odds",
        ["prob.event"] = "Event",
        ["prob.monster"] = "Monster",
        ["prob.elite"] = "Elite",
        ["prob.shop"] = "Shop",
        ["prob.treasure"] = "Treasure",
        ["prob.potion_title"] = "Potion Chance",
        ["prob.potion_sub"] = "Cumulative drop probability",
        ["prob.potion_within"] = "Within {0}",
        ["prob.card_title"] = "Card Drops (Next Combat)",
        ["prob.card_sub"] = "Probability of at least one",
        ["prob.normal"] = "Normal",
        ["prob.elite_col"] = "Elite",
        ["prob.rare"] = "Rare",
        ["prob.uncommon"] = "Uncommon",
        ["prob.common"] = "Common",
        ["prob.shop_title"] = "Shop Prices",
        ["prob.removal_cost"] = "Card Removal:",
        ["prob.discount_note"] = "50% sale, Colorless +15%",
        ["prob.cards"] = "Cards:",
        ["prob.relics"] = "Relics:",
        ["prob.potions"] = "Potions:",

        // Intent state machine (§3.10)
        ["intent.unavailable"] = "State machine data unavailable",
        ["intent.conditional"] = "conditional",

        // Career stats (§3.11)
        ["career.title"] = "Stats the Spire",
        ["career.win_trend"] = "Win Rate Trend",
        ["career.last_n"] = "Last {0}",
        ["career.all"] = "All",
        ["career.death_causes"] = "Top Death Causes",
        ["career.path_stats"] = "Path Stats (per Act avg)",
        ["career.cards_gained"] = "Cards Obtained",
        ["career.cards_bought"] = "Cards Purchased",
        ["career.cards_removed"] = "Cards Removed",
        ["career.cards_upgraded"] = "Cards Upgraded",
        ["career.unknown_rooms"] = "? Rooms",
        ["career.monster_rooms"] = "Monster Fights",
        ["career.elite_rooms"] = "Elite Fights",
        ["career.shop_rooms"] = "Shops",
        ["career.ancient_title"] = "Ancient Relic Pick Rate",
        ["career.boss_title"] = "Boss Damage Taken",
        ["career.avg_damage"] = "Avg Damage:",
        ["career.death_rate"] = "Death Rate:",
        ["career.pick_rate"] = "Pick Rate",
        ["career.times"] = "Times",
        ["career.win_rate"] = "Win Rate",
        ["career.delta"] = "Delta",
        ["career.loading"] = "Loading career data...",
        ["career.community_comparison"] = "Community Comparison",

        // Run History (§3.12)
        ["runhist.section_title"] = "Run Statistics",
        ["runhist.act_label"] = "Act {0}:",
        ["runhist.ancient_relic"] = "Ancient Relic:",
        ["runhist.boss_damage"] = "Boss Damage:",
        ["runhist.view_contrib"] = "View Contribution Chart",

        // Filter panel additions (§3.13)
        ["settings.my_data"] = "Show my data only",
        ["settings.toggles_title"] = "Feature Toggles",
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
        ["settings.title"] = "Stats the Spire — 设置",
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
        ["stats.event_pick"] = "选择率 {0}%",
        ["stats.encounter"] = "死亡 {0}% | 平均伤害 {1}",
        ["stats.upgrade"] = "升级 {0}% | 胜率 {1}%",
        ["stats.remove"] = "移除 {0}% | 胜率 {1}%",
        ["stats.buy"] = "购买 {0}% | 胜率 {1}%",
        ["stats.loading"] = "加载中...",
        ["stats.no_data"] = "无数据",

        // Unknown Room panel (§3.8)
        ["unknown.title"] = "下一个 [?] 房间",
        ["unknown.subtitle"] = "当前遭遇概率",
        ["room.event"] = "事件",
        ["room.monster"] = "怪物",
        ["room.elite"] = "精英",
        ["room.treasure"] = "宝箱",
        ["room.shop"] = "商店",

        // Potion drop odds (§3.9)
        ["potion.title"] = "药水概率",
        ["potion.subtitle"] = "多场累计掉落概率",
        ["potion.within"] = "{0} 场内",

        // Card drop odds (§3.17)
        ["carddrop.title"] = "卡牌掉落(下一场)",
        ["carddrop.subtitle"] = "各稀有度至少出现一张的概率",
        ["carddrop.rare"] = "稀有",
        ["carddrop.uncommon"] = "非普通",
        ["carddrop.common"] = "普通",
        ["carddrop.col_regular"] = "普通战斗",
        ["carddrop.col_elite"] = "精英战斗",

        // Shop prices (§3.16)
        ["shop.title"] = "商店价格",
        ["shop.subtitle"] = "打折 50%,无色牌 +15%",
        ["shop.removal"] = "删牌费用: {0}",
        ["shop.relics"] = "遗物",
        ["shop.cards"] = "卡牌",
        ["shop.potions"] = "药水",
        ["shop.col_common"] = "普通",
        ["shop.col_uncommon"] = "非普通",
        ["shop.col_rare"] = "稀有",

        // Healing sources
        ["source.rest_site"] = "篝火休息",
        ["source.event"] = "事件",
        ["source.event_prefix"] = "[事件] ",
        ["source.merchant"] = "商店",
        ["source.floor_regen"] = "第{0}层恢复",

        // ModSettingsPatch
        ["mod.settings_btn"] = "Stats the Spire 设置",

        // Feature toggles (§3.14)
        ["toggle.contribution_panel"] = "战斗贡献面板（F8 面板及实时刷新）",
        ["toggle.card_library_stats"] = "卡牌选取率/胜率统计显示",
        ["toggle.relic_stats"] = "遗物胜率/胜率浮动显示",
        ["toggle.event_pick_rate"] = "事件选项选择率显示",
        ["toggle.monster_danger"] = "怪物房间危险度显示",
        ["toggle.unknown_room_odds"] = "问号房间遭遇概率显示",
        ["toggle.potion_drop_odds"] = "药水掉落概率显示",
        ["toggle.card_drop_odds"] = "卡牌掉落概率显示",
        ["toggle.shop_prices"] = "商店价格显示",
        ["toggle.intent_state_machine"] = "敌人意图状态机显示",
        ["toggle.career_stats"] = "个人生涯统计",

        // ContributionPanel v2 (§3.7, §4.5)
        ["contrib.dps"] = "每回合平均伤害:",
        ["contrib.help_title"] = "Stats the Spire — 帮助",
        ["contrib.help_body"] = "柱状图颜色: 卡牌(蓝)、遗物(金)、药水(绿)\n快捷键: F8 切换面板, F9 设置\n拖拽标题栏移动位置。",

        // Probability panels (§3.8, §3.9, §3.16, §3.17)
        ["prob.unknown_title"] = "下一个 [?] 房间",
        ["prob.unknown_sub"] = "当前遭遇概率",
        ["prob.event"] = "事件",
        ["prob.monster"] = "怪物",
        ["prob.elite"] = "精英",
        ["prob.shop"] = "商店",
        ["prob.treasure"] = "宝箱",
        ["prob.potion_title"] = "药水掉落概率",
        ["prob.potion_sub"] = "多场累计概率",
        ["prob.potion_within"] = "{0} 场内",
        ["prob.card_title"] = "卡牌掉落（下场战斗）",
        ["prob.card_sub"] = "至少出现一张的概率",
        ["prob.normal"] = "普通",
        ["prob.elite_col"] = "精英",
        ["prob.rare"] = "稀有",
        ["prob.uncommon"] = "非普通",
        ["prob.common"] = "普通",
        ["prob.shop_title"] = "商店价格",
        ["prob.removal_cost"] = "删牌费用:",
        ["prob.discount_note"] = "打折50%, 无色牌贵15%",
        ["prob.cards"] = "卡牌:",
        ["prob.relics"] = "遗物:",
        ["prob.potions"] = "药水:",

        // Intent state machine (§3.10)
        ["intent.unavailable"] = "状态机数据不可用",
        ["intent.conditional"] = "条件",

        // Career stats (§3.11)
        ["career.title"] = "Stats the Spire",
        ["career.win_trend"] = "胜率趋势",
        ["career.last_n"] = "最近 {0} 局",
        ["career.all"] = "全部",
        ["career.death_causes"] = "死因排行",
        ["career.path_stats"] = "路径统计（每 Act 平均）",
        ["career.cards_gained"] = "获取卡牌",
        ["career.cards_bought"] = "购买卡牌",
        ["career.cards_removed"] = "删除卡牌",
        ["career.cards_upgraded"] = "升级卡牌",
        ["career.unknown_rooms"] = "? 房间数",
        ["career.monster_rooms"] = "小怪战斗数",
        ["career.elite_rooms"] = "精英战斗数",
        ["career.shop_rooms"] = "商店数",
        ["career.ancient_title"] = "先古遗物选择率",
        ["career.boss_title"] = "Boss 战损",
        ["career.avg_damage"] = "平均战损:",
        ["career.death_rate"] = "死亡率:",
        ["career.pick_rate"] = "选择率",
        ["career.times"] = "次数",
        ["career.win_rate"] = "胜率",
        ["career.delta"] = "浮动",
        ["career.loading"] = "加载生涯数据中...",
        ["career.community_comparison"] = "社区对比",

        // Run History (§3.12)
        ["runhist.section_title"] = "本局统计",
        ["runhist.act_label"] = "Act {0}:",
        ["runhist.ancient_relic"] = "先古遗物:",
        ["runhist.boss_damage"] = "Boss 战损:",
        ["runhist.view_contrib"] = "查看贡献图表",

        // Filter panel additions (§3.13)
        ["settings.my_data"] = "仅显示我的数据",
        ["settings.toggles_title"] = "功能开关",
    };
}
