using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.util;

/// <summary>
///     卡牌分组键（相同ID、升级等级、附魔的卡牌视为相同）
/// </summary>
public readonly record struct CardGroupKey(CardModel Card)
{
    public CardRarity Rarity { get; } = Card.Rarity;
    public string Title { get; } = Card.Title;

    public bool Equals(CardGroupKey other)
    {
        return Card.Id.Equals(other.Card.Id) &&
               Card.CurrentUpgradeLevel == other.Card.CurrentUpgradeLevel &&
               Card.Enchantment?.Id == other.Card.Enchantment?.Id &&
               Card.Enchantment?.Amount == other.Card.Enchantment?.Amount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Card.Id, Card.CurrentUpgradeLevel, Card.Enchantment?.Id, Card.Enchantment?.Amount);
    }
}
