using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Foresight.Engine;

public sealed class RelicSequenceReader
{
    public record RelicEntry(string Rarity, string? RelicId);

    public static List<RelicEntry> ReadNext(Player player, RngSnapshot snapshot, int count = 8)
    {
        var results = new List<RelicEntry>();
        var rng = snapshot.CreateRewardsRng();

        var deques = Traverse.Create(player.RelicGrabBag)
            .Field("_deques")
            .GetValue<Dictionary<RelicRarity, List<RelicModel>>>();

        if (deques == null) return results;

        var simDeques = deques.ToDictionary(
            kvp => kvp.Key,
            kvp => new List<RelicModel>(kvp.Value));

        for (int i = 0; i < count; i++)
        {
            float roll = rng.NextFloat();
            var rarity = roll < 0.5f ? RelicRarity.Common
                : roll < 0.83f ? RelicRarity.Uncommon
                : RelicRarity.Rare;

            string? relicId = null;
            if (simDeques.TryGetValue(rarity, out var deque) && deque.Count > 0)
            {
                relicId = deque[0].Id.Entry;
                deque.RemoveAt(0);
            }
            results.Add(new RelicEntry(rarity.ToString(), relicId));
        }

        return results;
    }
}
