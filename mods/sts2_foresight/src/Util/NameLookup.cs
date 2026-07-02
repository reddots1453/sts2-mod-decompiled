using System.Text.Json;

namespace Foresight.Util;

public static class NameLookup
{
    private static Dictionary<string, NamedEntry>? _cards;
    private static Dictionary<string, NamedEntry>? _relics;
    private static Dictionary<string, NamedEntry>? _potions;
    private static Dictionary<string, NamedEntry>? _encounters;
    private static Dictionary<string, NamedEntry>? _events;
    private static bool _loaded;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var kb = FindKnowledgeBase();
        if (kb == null) return;

        _cards = LoadDict(Path.Combine(kb, "cards.json"));
        _relics = LoadDict(Path.Combine(kb, "relics.json"));
        _potions = LoadDict(Path.Combine(kb, "potions.json"));
        _encounters = LoadDict(Path.Combine(kb, "encounters.json"));
        _events = LoadDict(Path.Combine(kb, "events.json"));
    }

    private static string? FindKnowledgeBase()
    {
        var dir = Path.GetDirectoryName(typeof(NameLookup).Assembly.Location);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            var kb = Path.Combine(dir, "KnowledgeBase");
            if (Directory.Exists(kb)) return kb;
            kb = Path.Combine(dir, "..", "..", "Sts2-mod-decompiled", "KnowledgeBase");
            if (Directory.Exists(Path.GetFullPath(kb))) return Path.GetFullPath(kb);
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static Dictionary<string, NamedEntry>? LoadDict(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, NamedEntry>>(File.ReadAllText(path));
        }
        catch { return null; }
    }

    private static string? Lookup(Dictionary<string, NamedEntry>? dict, string id)
    {
        if (dict == null) return null;
        return dict.TryGetValue(id, out var e) ? (e.name?.cn ?? e.name?.en) : null;
    }

    public static string Card(string id)
    {
        EnsureLoaded();
        return Lookup(_cards, id) ?? id;
    }

    public static string Relic(string id)
    {
        EnsureLoaded();
        return Lookup(_relics, id) ?? id;
    }

    public static string Potion(string id)
    {
        EnsureLoaded();
        return Lookup(_potions, id) ?? id;
    }

    public static string Encounter(string id)
    {
        EnsureLoaded();
        return Lookup(_encounters, id) ?? id;
    }

    public static string Event(string id)
    {
        EnsureLoaded();
        return Lookup(_events, id) ?? id;
    }

    private class NamedEntry { public NamePair? name { get; set; } }
    private class NamePair { public string? en { get; set; } public string? cn { get; set; } }
}
