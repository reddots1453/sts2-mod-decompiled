using CommunityStats.Api;
using CommunityStats.Config;
using CommunityStats.Patches;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace CommunityStats;

/// <summary>
/// Community Stats mod entry point.
/// Initializes Harmony patches, registers metrics upload hook,
/// loads user settings, and attaches UI panels to the scene tree.
/// </summary>
[ModInitializerAttribute("Initialize")]
public static class CommunityStatsMod
{
    private static Harmony? _harmony;
    private static bool _f8Pressed;

    public static void Initialize()
    {
        Safe.Info($"Community Stats v{ModConfig.ModVersion} initializing...");

        // Load config overrides (e.g. api_base_url for local testing)
        ModConfig.LoadOverrides();
        Safe.Info($"API endpoint: {ModConfig.ApiBaseUrl}");

        // Ensure data directories exist
        ModConfig.EnsureDirectories();

        // Load saved filter settings
        Safe.Run(() =>
        {
            ModConfig.CurrentFilter = FilterSettings.Load() ?? new FilterSettings();
        });

        // Apply Harmony patches
        _harmony = new Harmony("com.communitystats.sts2");
        _harmony.PatchAll(typeof(CommunityStatsMod).Assembly);

        // Register ModManager.OnMetricsUpload hook for run data upload
        RunLifecyclePatch.RegisterMetricsHook();

        // Listen for filter changes to trigger data re-fetch
        FilterPanel.FilterApplied += OnFilterApplied;

        // Register hotkeys and attach UI panels to scene tree
        Safe.Run(() => RegisterHotkeys());

        // Cleanup stale disk cache
        StatsCache.Instance.CleanupDisk();

        Safe.Info("Community Stats initialized successfully");
    }

    private static void OnFilterApplied()
    {
        Safe.RunAsync(async () =>
        {
            await StatsProvider.Instance.OnFilterChangedAsync(null, ModConfig.CurrentFilter);
            FilterPanel.Instance.UpdateSampleSizeLabel();
        });
    }

    private static void RegisterHotkeys()
    {
        // Wait for scene tree, then use ProcessFrame signal for input polling.
        // This avoids subclassing Godot.Node (which requires source generators
        // that aren't available in mod assemblies).
        Safe.RunAsync(async () =>
        {
            // Wait for scene tree to be ready
            while (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root == null)
            {
                await Task.Delay(100);
            }

            var tree = (SceneTree)Engine.GetMainLoop();
            tree.ProcessFrame += OnProcessFrame;

            // Add UI panels to root
            var root = tree.Root;
            root.CallDeferred(Node.MethodName.AddChild, ContributionPanel.Instance);
            root.CallDeferred(Node.MethodName.AddChild, FilterPanel.Instance);
        });
    }

    private static void OnProcessFrame()
    {
        Safe.Run(() =>
        {
            bool f8Now = Input.IsKeyPressed(Key.F8);

            if (f8Now && !_f8Pressed)
            {
                ContributionPanel.Toggle();
            }

            _f8Pressed = f8Now;
        });
    }
}
