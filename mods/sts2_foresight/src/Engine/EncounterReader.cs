using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Foresight.Engine;

public sealed class EncounterReader
{
    public record Entry(string Id, string Name, string RoomType);

    /// <summary>
    /// Read upcoming normal and elite encounters from RoomSet without consuming them.
    /// </summary>
    public static List<Entry> GetNextNormals(RunState state, int count = 3)
    {
        var result = new List<Entry>();
        var rooms = GetRoomSet(state);
        if (rooms == null) return result;

        var list = Traverse.Create(rooms).Field("normalEncounters").GetValue<List<EncounterModel>>();
        var visited = Traverse.Create(rooms).Field("normalEncountersVisited").GetValue<int>();
        if (list == null || list.Count == 0) return result;

        for (int i = 0; i < count; i++)
        {
            var enc = list[(visited + i) % list.Count];
            result.Add(new Entry(enc.Id.Entry, enc.Title.GetFormattedText() ?? enc.Id.Entry, "Normal"));
        }
        return result;
    }

    public static List<Entry> GetNextElites(RunState state, int count = 3)
    {
        var result = new List<Entry>();
        var rooms = GetRoomSet(state);
        if (rooms == null) return result;

        var list = Traverse.Create(rooms).Field("eliteEncounters").GetValue<List<EncounterModel>>();
        var visited = Traverse.Create(rooms).Field("eliteEncountersVisited").GetValue<int>();
        if (list == null || list.Count == 0) return result;

        for (int i = 0; i < count; i++)
        {
            var enc = list[(visited + i) % list.Count];
            result.Add(new Entry(enc.Id.Entry, enc.Title.GetFormattedText() ?? enc.Id.Entry, "Elite"));
        }
        return result;
    }

    private static object? GetRoomSet(RunState state)
    {
        if (state.Acts == null || state.CurrentActIndex >= state.Acts.Count)
            return null;
        var act = state.Acts[state.CurrentActIndex];
        return Traverse.Create(act).Field("_rooms").GetValue();
    }
}
