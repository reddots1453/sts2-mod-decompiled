using Godot;

namespace IntentGraph.Config;

public static class Loc
{
    public enum Lang { EN, CN }

    public static Lang Current
    {
        get
        {
            try { return TranslationServer.GetLocale().StartsWith("zh") ? Lang.CN : Lang.EN; }
            catch { return Lang.CN; }
        }
    }

    public static string Get(string key) =>
        Current == Lang.CN
            ? (CN.TryGetValue(key, out var v) ? v : key)
            : (EN.TryGetValue(key, out var v2) ? v2 : key);

    private static readonly Dictionary<string, string> EN = new()
    {
        ["intent.no_metadata"] = "(no intent metadata)",
        ["intent.initial"] = "Initial",
        ["intent.conditional"] = "Conditional",
        ["intent.random"] = "(random)",
    };

    private static readonly Dictionary<string, string> CN = new()
    {
        ["intent.no_metadata"] = "(无意图元数据)",
        ["intent.initial"] = "初始",
        ["intent.conditional"] = "条件",
        ["intent.random"] = "(随机)",
    };
}
