using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.util;

/// <summary>
///     卡牌相关工具方法
/// </summary>
public static class CardUtils
{
    /// <summary>
    ///     按 ID + 升级等级 + 附魔 分组卡牌
    ///     分组后按稀有度降序、标题升序排列
    /// </summary>
    public static IEnumerable<IGrouping<CardGroupKey, CardModel>> GroupCards(IEnumerable<CardModel> cards)
    {
        return cards
            .GroupBy(c => new CardGroupKey(c))
            .OrderByDescending(g => g.Key.Rarity)
            .ThenBy(g => g.Key.Title);
    }
}
