using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Foresight.Engine;

public sealed class EventReader
{
    public record Entry(string Id, string Name);

    /// <summary>
    /// Read upcoming events from RoomSet.events without consuming them.
    /// </summary>
    public static List<Entry> GetNextEvents(RunState state, int count = 3)
    {
        var result = new List<Entry>();
        if (state.Acts == null || state.CurrentActIndex >= state.Acts.Count)
            return result;

        var act = state.Acts[state.CurrentActIndex];
        var rooms = Traverse.Create(act).Field("_rooms").GetValue();
        if (rooms == null) return result;

        var events = Traverse.Create(rooms).Field("events").GetValue<List<EventModel>>();
        var visited = Traverse.Create(rooms).Field("eventsVisited").GetValue<int>();
        if (events == null || events.Count == 0) return result;

        for (int i = 0; i < count && events.Count > 0; i++)
        {
            var idx = (visited + i) % events.Count;
            var evt = events[idx];
            result.Add(new Entry(evt.Id.Entry, evt.Title.GetFormattedText() ?? evt.Id.Entry));
        }
        return result;
    }
}
