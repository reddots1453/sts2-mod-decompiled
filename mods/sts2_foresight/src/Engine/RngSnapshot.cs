using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;

namespace Foresight.Engine;

public readonly struct RngSnapshot
{
    public uint RewardsSeed { get; }
    public int RewardsCounter { get; }
    public float CardRarityPity { get; }
    public float PotionDropPity { get; }

    public RngSnapshot(uint seed, int counter, float cardRarityPity, float potionDropPity)
    {
        RewardsSeed = seed;
        RewardsCounter = counter;
        CardRarityPity = cardRarityPity;
        PotionDropPity = potionDropPity;
    }

    public static RngSnapshot FromPlayer(Player player)
    {
        var rewards = player.PlayerRng.Rewards;
        return new RngSnapshot(
            rewards.Seed,
            rewards.Counter,
            player.PlayerOdds.CardRarity.CurrentValue,
            player.PlayerOdds.PotionReward.CurrentValue
        );
    }

    public Rng CreateRewardsRng() => new(RewardsSeed, RewardsCounter);

    public CardRarityOdds CreateCardRarityOdds(Rng rng)
    {
        var odds = new CardRarityOdds(rng);
        odds.OverrideCurrentValue(CardRarityPity);
        return odds;
    }

    public PotionRewardOdds CreatePotionOdds(Rng rng)
    {
        var odds = new PotionRewardOdds(rng);
        odds.OverrideCurrentValue(PotionDropPity);
        return odds;
    }
}
