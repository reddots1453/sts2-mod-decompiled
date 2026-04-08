using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;

namespace ContribTests;

[ModInitializerAttribute("Initialize")]
public static class ContribTestMod
{
    private static bool _f10Pressed;
    private static CancellationTokenSource? _runCts;

    public static void Initialize()
    {
        GD.Print("[ContribTest] Contribution Test mod initializing...");

        try
        {
            RegisterHotkey();
            GD.Print("[ContribTest] Initialized. Press F10 in combat to run tests.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ContribTest] Init failed: {ex}");
        }
    }

    private static void RegisterHotkey()
    {
        Task.Run(async () =>
        {
            try
            {
                while (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root == null)
                    await Task.Delay(200);

                var tree = (SceneTree)Engine.GetMainLoop();
                tree.ProcessFrame += OnProcessFrame;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ContribTest] Hotkey setup failed: {ex}");
            }
        });
    }

    private static void OnProcessFrame()
    {
        try
        {
            bool f10Now = Input.IsKeyPressed(Key.F10);
            if (f10Now && !_f10Pressed)
                OnF10Pressed();
            _f10Pressed = f10Now;
        }
        catch { }
    }

    private static void OnF10Pressed()
    {
        if (!CombatManager.Instance.IsInProgress)
        {
            GD.Print("[ContribTest] Not in combat. Enter a combat first, then press F10.");
            return;
        }

        // If already running, cancel
        if (_runCts != null)
        {
            GD.Print("[ContribTest] Cancelling running test suite...");
            _runCts.Cancel();
            _runCts = null;
            return;
        }

        _runCts = new CancellationTokenSource();
        var ct = _runCts.Token;

        Task.Run(async () =>
        {
            try
            {
                var runner = new TestRunner();
                await runner.RunAllAsync(ct);
            }
            catch (OperationCanceledException)
            {
                GD.Print("[ContribTest] Test run cancelled.");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ContribTest] Test run failed: {ex}");
            }
            finally
            {
                _runCts = null;
            }
        });
    }
}
