using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Foresight.Engine;

public sealed class PotionPredictor
{
    public record PotionEntry(bool WillDrop, float DropChance, string? PotionId);

    public static List<PotionEntry> PredictNext(Player player, RngSnapshot snapshot, int count = 5)
    {
        var results = new List<PotionEntry>();
        var rng = snapshot.CreateRewardsRng();
        var potionOdds = snapshot.CreatePotionOdds(rng);

        for (int i = 0; i < count; i++)
        {
            float currentValue = potionOdds.CurrentValue;
            float roll = rng.NextFloat();
            bool willDrop = roll < currentValue;

            potionOdds.OverrideCurrentValue(willDrop
                ? currentValue - 0.1f
                : currentValue + 0.1f);

            string? potionId = null;
            if (willDrop)
            {
                try
                {
                    float rarityRoll = rng.NextFloat();
                    var rarity = rarityRoll <= 0.1f ? PotionRarity.Rare
                        : rarityRoll <= 0.35f ? PotionRarity.Uncommon
                        : PotionRarity.Common;

                    var options = PotionFactory.GetPotionOptions(player, Array.Empty<PotionModel>())
                        .Where(p => p.Rarity == rarity)
                        .ToList();
                    var selected = rng.NextItem(options);
                    potionId = selected?.Id.Entry;
                }
                catch { }
            }

            results.Add(new PotionEntry(willDrop, currentValue, potionId));
        }

        return results;
    }
}
