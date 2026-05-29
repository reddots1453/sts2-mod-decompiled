using Godot;

namespace MoreInfo.Config;

public static class Loc
{
    public enum Lang { EN, CN }

    public static Lang Current
    {
        get
        {
            try { return TranslationServer.GetLocale().StartsWith("zh") ? Lang.CN : Lang.EN; }
            catch { return Lang.CN; }
        }
    }

    public static string Get(string key) =>
        Current == Lang.CN
            ? (CN.TryGetValue(key, out var v) ? v : key)
            : (EN.TryGetValue(key, out var v2) ? v2 : key);

    private static readonly Dictionary<string, string> EN = new()
    {
        ["unknown.title"] = "Next [?] Room",
        ["unknown.subtitle"] = "Current encounter odds",
        ["unknown.relic_modified"] = "Modified by an active relic (e.g. Juzu Bracelet, Golden Compass)",
        ["room.event"] = "Event",
        ["room.monster"] = "Monster",
        ["room.elite"] = "Elite",
        ["room.treasure"] = "Treasure",
        ["room.shop"] = "Shop",
        ["potion.title"] = "Potion Drop (Next Combat)",
        ["potion.subtitle"] = "Cumulative odds over N combats",
        ["potion.within"] = "Within {0} combats",
        ["potion.elite"] = "Elite combat drop",
        ["carddrop.title"] = "Card Drops (Next Combat)",
        ["carddrop.subtitle"] = "Probability of at least one per rarity",
        ["carddrop.rare"] = "Rare",
        ["carddrop.uncommon"] = "Uncommon",
        ["carddrop.common"] = "Common",
        ["carddrop.col_regular"] = "Regular",
        ["carddrop.col_elite"] = "Elite",
        ["shop.title"] = "Shop Prices",
        ["shop.subtitle"] = "Merchant price table",
        ["shop.removal"] = "Remove a card: {0}",
        ["shop.relics"] = "Relics",
        ["shop.cards"] = "Cards",
        ["shop.potions"] = "Potions",
        ["shop.col_common"] = "Common",
        ["shop.col_uncommon"] = "Uncommon",
        ["shop.col_rare"] = "Rare",
        ["shop.discount_active"] = "Active discounts:",
        ["shop.colorless_note"] = "Colorless cards cost +15%",
    };

    private static readonly Dictionary<string, string> CN = new()
    {
        ["unknown.title"] = "下一个 [?] 房间",
        ["unknown.subtitle"] = "当前遭遇概率",
        ["unknown.relic_modified"] = "已被遗物影响（如：佛珠手链、金罗盘）",
        ["room.event"] = "事件",
        ["room.monster"] = "怪物",
        ["room.elite"] = "精英",
        ["room.treasure"] = "宝箱",
        ["room.shop"] = "商店",
        ["potion.title"] = "药水掉落（下一场）",
        ["potion.subtitle"] = "多场累计掉落概率",
        ["potion.within"] = "{0} 场内",
        ["potion.elite"] = "精英战掉落",
        ["carddrop.title"] = "卡牌掉落(下一场)",
        ["carddrop.subtitle"] = "各稀有度至少出现一张的概率",
        ["carddrop.rare"] = "稀有",
        ["carddrop.uncommon"] = "罕见",
        ["carddrop.common"] = "普通",
        ["carddrop.col_regular"] = "普通战斗",
        ["carddrop.col_elite"] = "精英战斗",
        ["shop.title"] = "商店价格",
        ["shop.subtitle"] = "商店价格表",
        ["shop.removal"] = "删牌费用: {0}",
        ["shop.relics"] = "遗物",
        ["shop.cards"] = "卡牌",
        ["shop.potions"] = "药水",
        ["shop.col_common"] = "普通",
        ["shop.col_uncommon"] = "罕见",
        ["shop.col_rare"] = "稀有",
        ["shop.discount_active"] = "当前折扣：",
        ["shop.colorless_note"] = "无色牌价格 +15%",
    };
}
