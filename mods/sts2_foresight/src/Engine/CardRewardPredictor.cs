using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Foresight.Engine;

public sealed class CardRewardPredictor
{
    public record CardEntry(string Rarity, string? CardId);
    public record FightReward(string FightType, List<CardEntry> Cards);

    public static List<FightReward> PredictNext(Player player, RngSnapshot snapshot, int numFights = 3)
    {
        var results = new List<FightReward>();
        var rng = snapshot.CreateRewardsRng();
        var cardOdds = snapshot.CreateCardRarityOdds(rng);

        for (int f = 0; f < numFights; f++)
        {
            // Normal fight
            results.Add(SimulateFight(player, cardOdds, rng, CardRarityOddsType.RegularEncounter, "Regular"));

            // Elite fight
            results.Add(SimulateFight(player, cardOdds, rng, CardRarityOddsType.EliteEncounter, "Elite"));
        }

        return results;
    }

    private static FightReward SimulateFight(Player player, CardRarityOdds cardOdds, Rng rng, CardRarityOddsType type, string label)
    {
        var cards = new List<CardEntry>();
        for (int c = 0; c < 3; c++)
        {
            var rarity = cardOdds.Roll(type);
            string? cardId = PickCard(player, rarity, rng);
            rng.NextFloat(); // upgrade roll — consume but don't track
            cards.Add(new CardEntry(rarity.ToString(), cardId));
        }
        return new FightReward(label, cards);
    }

    private static string? PickCard(Player player, CardRarity rarity, Rng rng)
    {
        try
        {
            var pool = player.Character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Concat(ModelDb.CardPool<ColorlessCardPool>()
                    .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
                .Where(c => c.Rarity == rarity
                    && c.Rarity != CardRarity.Basic
                    && c.Rarity != CardRarity.Ancient
                    && c.Rarity != CardRarity.Event
                    && c.Rarity != CardRarity.Token)
                .Distinct()
                .ToList();

            if (pool.Count == 0) return null;
            return rng.NextItem(pool)?.Id.Entry;
        }
        catch { return null; }
    }
}
