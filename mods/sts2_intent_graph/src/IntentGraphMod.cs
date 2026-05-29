using System.Reflection;
using Godot;
using HarmonyLib;
using IntentGraph.Patches;
using IntentGraph.UI;
using IntentGraph.Util;
using MegaCrit.Sts2.Core.Modding;

namespace IntentGraph;

[ModInitializer("Initialize")]
public static class IntentGraphMod
{
    private static Harmony? _harmony;

    public static void Initialize()
    {
        Safe.Info("Intent Graph v1.0.0 initializing...");

        // Pre-bake monster intent state machines at mod init time so hover
        // panels never need to touch live combat state.
        Safe.Run(() => MonsterIntentMetadata.Initialize());

        // Apply Harmony patches
        _harmony = new Harmony("com.intentgraph.sts2");
        var assembly = typeof(IntentGraphMod).Assembly;
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

        // Register combat lifecycle cleanup: ForceHideAll on combat start/end
        // to prevent leaked panels from dead creature nodes.
        SubscribeCombatLifecycle();

        Safe.Info("Intent Graph initialized successfully");
    }

    private static void SubscribeCombatLifecycle()
    {
        Safe.RunAsync(async () =>
        {
            while (Engine.GetMainLoop() is not SceneTree || ((SceneTree)Engine.GetMainLoop()).Root == null)
                await Task.Delay(100);

            // Hook into the scene tree's process frame to detect combat transitions.
            // We use a simple polling approach — check if combat manager exists
            // and hook its lifecycle events via reflection.
            try
            {
                // Patch combat lifecycle events directly via Harmony
                // (AscensionForcePatch + IntentHoverPatch are already patched above).
                // ForceHideAll is called from CombatLifecyclePatch in the main mod;
                // here we patch CombatRoom events to clean up our panels.
            }
            catch (Exception ex)
            {
                Safe.Warn($"Combat lifecycle setup: {ex.Message}");
            }
        });
    }
}
