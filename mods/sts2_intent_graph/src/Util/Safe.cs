using MegaCrit.Sts2.Core.Logging;

namespace IntentGraph.Util;

public static class Safe
{
    private const string Tag = "[IntentGraph]";

    public static void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { Log.Warn($"{Tag} {ex.Message}\n{ex.StackTrace}"); }
    }

    public static T? Run<T>(Func<T> func, T? fallback = default)
    {
        try { return func(); }
        catch (Exception ex)
        {
            Log.Warn($"{Tag} {ex.Message}\n{ex.StackTrace}");
            return fallback;
        }
    }

    public static async void RunAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { Log.Warn($"{Tag} Async error: {ex.Message}\n{ex.StackTrace}"); }
    }

    public static void Info(string msg) => Log.Info($"{Tag} {msg}");
    public static void Warn(string msg) => Log.Warn($"{Tag} {msg}");
}
