using Godot;
using HarmonyLib;
using MoreInfo.Patches;
using MoreInfo.UI;
using MoreInfo.Util;
using MegaCrit.Sts2.Core.Modding;

namespace MoreInfo;

[ModInitializer("Initialize")]
public static class MoreInfoMod
{
    private static Harmony? _harmony;

    public static void Initialize()
    {
        Safe.Info("More Info v1.0.0 initializing...");

        // Apply Harmony patches
        _harmony = new Harmony("com.moreinfo.sts2");
        var assembly = typeof(MoreInfoMod).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            try
            {
                var processor = _harmony.CreateClassProcessor(type);
                processor.Patch();
            }
            catch (Exception ex)
            {
                Safe.Warn($"[Harmony] Failed to patch {type.Name}: {ex.Message}");
            }
        }

        Safe.Info("More Info initialized successfully");
    }
}
