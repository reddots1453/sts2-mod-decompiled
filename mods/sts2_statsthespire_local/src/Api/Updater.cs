using System.IO;
using System.Net.Http;
using System.Text.Json;
using CommunityStats.Config;
using CommunityStats.Util;
using Godot;

namespace CommunityStats.Api;

/// <summary>
/// Auto-update: the mod checks for new versions and downloads the DLL.
/// DLL replacement is handled by a batch script (update.bat) that the
/// user runs after closing the game — no in-process file replacement.
/// </summary>
public sealed class Updater
{
    public static Updater Instance { get; } = new();

    public string Edition { get; set; } = "community";

    /// <summary>
    /// Called from mod init. Checks for .new file + existing update.bat
    /// and shows a reminder if an update was downloaded but not yet applied.
    /// </summary>
    public static void RemindPendingUpdate()
    {
        Safe.Run(() =>
        {
            var dllDir = GetDllDirectory();
            var newPath = Path.Combine(dllDir, "sts2_community_stats.dll.new");
            if (!File.Exists(newPath)) return;

            var batPath = Path.Combine(dllDir, "update.bat");
            if (File.Exists(batPath))
            {
                Callable.From(() => ShowMessageDialog(
                    L.Get("update.pending"))).CallDeferred();
            }
        });
    }

    /// <summary>
    /// Background check. If a newer version is found, shows a dialog
    /// asking the user whether to download.
    /// </summary>
    public async System.Threading.Tasks.Task CheckForUpdateAsync()
    {
        if (!ModConfig.AutoUpdate)
        {
            Safe.Info("[Updater] Auto-update disabled via config");
            return;
        }

        UpdateInfo? info;
        try
        {
            info = await FetchUpdateInfoAsync();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Update check failed: {ex.Message}");
            return;
        }

        if (info == null || !info.UpdateAvailable) return;

        Safe.Info($"[Updater] New version available: {info.Latest} (current: {ModConfig.ModVersion})");
        Callable.From(() => ShowUpdateDialog(info.Latest, info.DownloadUrl)).CallDeferred();
    }

    // ── Dialog ──────────────────────────────────────────────

    private static void ShowUpdateDialog(string version, string downloadUrl)
    {
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null) return;

            var backdrop = CreateBackdrop(tree);
            var vbox = CreatePanel(backdrop, 440);

            AddTitle(vbox, "Stats the Spire");
            AddSeparator(vbox);

            var msg = string.Format(L.Get("update.found"), version, ModConfig.ModVersion);
            AddMessage(vbox, msg, 400);

            var btnRow = AddButtonRow(vbox);
            var downloadBtn = MakeButton(L.Get("update.download_yes"), new Color(0.2f, 0.6f, 0.2f));
            var laterBtn = MakeButton(L.Get("update.download_no"), new Color(0.35f, 0.35f, 0.35f));
            btnRow.AddChild(downloadBtn);
            btnRow.AddChild(laterBtn);

            downloadBtn.Pressed += () =>
            {
                backdrop.QueueFree();
                StartDownload(version, downloadUrl);
            };
            laterBtn.Pressed += () => backdrop.QueueFree();
            RegisterEscClose(backdrop);
            downloadBtn.GrabFocus();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Failed to show dialog: {ex.Message}");
        }
    }

    private static async void StartDownload(string version, string downloadUrl)
    {
        Safe.Info($"[Updater] Downloading {version}...");

        bool ok;
        try
        {
            ok = await DownloadDllAsync(downloadUrl);
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Download failed: {ex.Message}");
            ShowMessageDialog(string.Format(L.Get("update.failed"), version));
            return;
        }

        if (!ok)
        {
            ShowMessageDialog(string.Format(L.Get("update.failed"), version));
            return;
        }

        // Write update.bat that the user runs after closing the game.
        var dllDir = GetDllDirectory();
        var batPath = Path.Combine(dllDir, "update.bat");
        var batContent =
            "@echo off\r\n" +
            "echo Stats the Spire — Applying Update...\r\n" +
            "cd /d \"%~dp0\"\r\n" +
            "if not exist \"sts2_community_stats.dll.new\" (\r\n" +
            "  echo No update found.\r\n" +
            "  pause\r\n" +
            "  exit /b 1\r\n" +
            ")\r\n" +
            "echo Replacing sts2_community_stats.dll ...\r\n" +
            "move /Y \"sts2_community_stats.dll.new\" \"sts2_community_stats.dll\"\r\n" +
            "if %errorlevel% equ 0 (\r\n" +
            "  echo Update applied successfully!\r\n" +
            "  del \"%~nx0\" 2>nul\r\n" +
            ") else (\r\n" +
            "  echo ERROR: Could not replace DLL. Is the game still running?\r\n" +
            ")\r\n" +
            "pause\r\n";
        await File.WriteAllTextAsync(batPath, batContent);

        ShowMessageDialog(string.Format(L.Get("update.ready"), version));
    }

    private static void ShowMessageDialog(string message)
    {
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null) return;

            var backdrop = CreateBackdrop(tree);
            var vbox = CreatePanel(backdrop, 400);

            AddTitle(vbox, "Stats the Spire");
            AddSeparator(vbox);
            AddMessage(vbox, message, 360);

            var btnRow = AddButtonRow(vbox);
            var okBtn = MakeButton("OK", new Color(0.35f, 0.35f, 0.35f));
            btnRow.AddChild(okBtn);
            okBtn.Pressed += () => backdrop.QueueFree();
            RegisterEscClose(backdrop);
            okBtn.GrabFocus();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[Updater] Failed to show message: {ex.Message}");
        }
    }

    // ── UI helpers ──────────────────────────────────────────

    private static ColorRect CreateBackdrop(SceneTree tree)
    {
        var backdrop = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.5f),
            AnchorRight = 1, AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        tree.Root.AddChild(backdrop);
        return backdrop;
    }

    private static VBoxContainer CreatePanel(Node parent, float minWidth)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", StyleBox(new Color(0.08f, 0.08f, 0.1f, 0.95f)));
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(minWidth, 0);
        parent.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        panel.AddChild(vbox);
        return vbox;
    }

    private static void AddTitle(VBoxContainer vbox, string text)
    {
        var lbl = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        lbl.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.2f));
        lbl.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(lbl);
    }

    private static void AddSeparator(VBoxContainer vbox) => vbox.AddChild(new HSeparator());

    private static void AddMessage(VBoxContainer vbox, string text, float minWidth)
    {
        var lbl = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(minWidth, 0),
        };
        lbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.88f, 0.82f));
        lbl.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(lbl);
    }

    private static HBoxContainer AddButtonRow(VBoxContainer vbox)
    {
        var row = new HBoxContainer();
        row.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(row);
        return row;
    }

    private static void RegisterEscClose(ColorRect backdrop)
    {
        backdrop.GuiInput += e =>
        {
            if (e is InputEventKey key && key.Keycode == Key.Escape && key.Pressed)
                backdrop.QueueFree();
        };
    }

    private static StyleBoxFlat StyleBox(Color bg) => new()
    {
        BgColor = bg,
        CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
        CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        BorderWidthLeft = 1, BorderWidthRight = 1,
        BorderWidthTop = 1, BorderWidthBottom = 1,
        BorderColor = new Color(0.3f, 0.3f, 0.35f, 0.6f),
    };

    private static Button MakeButton(string text, Color bgColor)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(100, 36) };
        btn.AddThemeStyleboxOverride("normal", StyleBox(bgColor));
        btn.AddThemeStyleboxOverride("hover", StyleBox(new Color(
            bgColor.R + 0.1f, bgColor.G + 0.1f, bgColor.B + 0.1f)));
        btn.AddThemeFontSizeOverride("font_size", 14);
        return btn;
    }

    // ── Network ─────────────────────────────────────────────

    private static async Task<UpdateInfo?> FetchUpdateInfoAsync()
    {
        var url = $"meta/update-info?edition={Uri.EscapeDataString(Instance.Edition)}"
                + $"&current={Uri.EscapeDataString(ModConfig.ModVersion)}";

        using var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(ModConfig.ApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = System.TimeSpan.FromSeconds(10),
        };
        client.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);

        var json = await client.GetStringAsync(url);
        return JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    private static async Task<bool> DownloadDllAsync(string downloadUrl)
    {
        using var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(ModConfig.ApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = System.TimeSpan.FromSeconds(60),
        };
        client.DefaultRequestHeaders.Add("X-Mod-Version", ModConfig.ModVersion);

        var bytes = await client.GetByteArrayAsync(downloadUrl);
        if (bytes.Length < 100_000)
        {
            Safe.Warn($"[Updater] Downloaded DLL too small ({bytes.Length} bytes), ignoring");
            return false;
        }

        var dllDir = GetDllDirectory();
        var newPath = Path.Combine(dllDir, "sts2_community_stats.dll.new");
        await File.WriteAllBytesAsync(newPath, bytes);
        Safe.Info($"[Updater] Downloaded to {newPath} ({bytes.Length} bytes)");
        return true;
    }

    private static string GetDllDirectory() =>
        Path.GetDirectoryName(typeof(Updater).Assembly.Location)!;

    private class UpdateInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("latest")]
        public string Latest { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("update_available")]
        public bool UpdateAvailable { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = "";
    }
}
