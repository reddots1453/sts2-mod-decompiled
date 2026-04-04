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
    private static bool _f9Pressed;

    public static void Initialize()
    {
        Safe.Info($"Community Stats v{ModConfig.ModVersion} initializing...");

        // Load config overrides (e.g. api_base_url for local testing)
        ModConfig.LoadOverrides();
        Safe.Info($"API endpoint: {ModConfig.ApiBaseUrl}");

        // Sync language setting
        L.Current = ModConfig.Language == "EN" ? L.Lang.EN : L.Lang.CN;

        // Ensure data directories exist
        ModConfig.EnsureDirectories();

        // Load saved filter settings
        Safe.Run(() =>
        {
            ModConfig.CurrentFilter = FilterSettings.Load() ?? new FilterSettings();
        });

        // Load bundled test data immediately so stats are available for Neow etc.
        Safe.Run(() => StatsProvider.Instance.EnsureTestDataLoaded());

        // Apply Harmony patches — patch each class individually so one failure
        // doesn't prevent the rest from loading.
        _harmony = new Harmony("com.communitystats.sts2");
        var assembly = typeof(CommunityStatsMod).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            try
            {
                var processor = _harmony.CreateClassProcessor(type);
                processor.Patch();
            }
            catch (Exception ex)
            {
                Safe.Warn($"[Harmony] Failed to patch {type.Name}: {ex}");
            }
        }

        // Apply manual hook context patches (individual try/catch per method)
        RelicHookContextPatcher.PatchAll(_harmony);
        PowerHookContextPatcher.PatchAll(_harmony);
        KillingBlowPatcher.PatchAll(_harmony);

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
        Safe.RunAsync(async () =>
        {
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
            // F8 = Toggle contribution panel
            bool f8Now = Input.IsKeyPressed(Key.F8);
            if (f8Now && !_f8Pressed)
            {
                ContributionPanel.Toggle();
            }
            _f8Pressed = f8Now;

            // F9 = Toggle settings/filter panel
            bool f9Now = Input.IsKeyPressed(Key.F9);
            if (f9Now && !_f9Pressed)
            {
                FilterPanel.Toggle();
            }
            _f9Pressed = f9Now;
        });
    }
}
