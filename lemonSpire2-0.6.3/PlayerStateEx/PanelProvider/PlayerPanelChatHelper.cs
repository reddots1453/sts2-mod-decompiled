using lemonSpire2.Chat;
using lemonSpire2.Chat.Message;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

internal static class PlayerPanelChatHelper
{
    public static string GetPlayerName(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var runManager = RunManager.Instance;
        return PlatformUtil.GetPlayerName(runManager.NetService.Platform, player.NetId);
    }

    public static void SendPlayerItemToChat(Player player, string locEntryKey, TooltipSegment tooltipSegment)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(locEntryKey);
        ArgumentNullException.ThrowIfNull(tooltipSegment);

        ChatStore.SendToChat(
            new RichTextSegment { Text = GetPlayerName(player) },
            new LocSegment("gameplay_ui", locEntryKey),
            tooltipSegment
        );
    }
}
