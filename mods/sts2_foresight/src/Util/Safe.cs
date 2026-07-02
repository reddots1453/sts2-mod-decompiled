namespace Foresight.Util;

public static class Safe
{
    public static void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { ForesightMod.Logger.Warn($"Safe.Run: {ex.Message}"); }
    }

    public static void RunAsync(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try { await action(); }
            catch (Exception ex) { ForesightMod.Logger.Warn($"Safe.RunAsync: {ex.Message}"); }
        });
    }

    public static void Info(string msg) => ForesightMod.Logger.Info(msg);
    public static void Warn(string msg) => ForesightMod.Logger.Warn(msg);
}
