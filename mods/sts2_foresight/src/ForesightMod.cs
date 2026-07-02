using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace Foresight;

[ModInitializer(nameof(Init))]
public static class ForesightMod
{
    public const string ModId = "sts2_foresight";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        Logger.Info("Foresight Eye v0.1.0 initializing...");

        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // Standard Harmony patching
        var harmony = new Harmony("com.foresight.sts2");
        var patchCount = 0;
        foreach (var type in assembly.GetTypes())
        {
            bool hasAttr = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0
                || type.GetMethods().Any(m => m.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
            if (!hasAttr) continue;
            try { harmony.CreateClassProcessor(type).Patch(); patchCount++; }
            catch (Exception ex) { Logger.Warn($"Harmony failed: {type.Name}: {ex.Message}"); }
        }
        Logger.Info($"Foresight Eye ready — {patchCount} patches applied");
    }
}
