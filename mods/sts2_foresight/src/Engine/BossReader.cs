using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Foresight.Engine;

public sealed class BossReader
{
    public record Entry(string ActId, string BossName);

    public static List<Entry> GetAllBosses(RunState state)
    {
        var results = new List<Entry>();
        if (state.Acts == null) return results;

        foreach (var act in state.Acts)
        {
            var rooms = Traverse.Create(act).Field("_rooms").GetValue();
            if (rooms == null) continue;

            var boss = Traverse.Create(rooms).Property("Boss").GetValue<EncounterModel>();
            if (boss != null)
                results.Add(new Entry(act.Id.Entry, boss.Title.GetFormattedText() ?? boss.Id.Entry));
        }
        return results;
    }
}
