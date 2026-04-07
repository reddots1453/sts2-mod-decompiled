using System.Reflection;
using Godot;
using lemonSpire2.Chat;
using MegaCrit.Sts2.Core.Nodes.Potions;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.util.Ui;

/// <summary>
///     Provider 工具类
///     提供公共的工具方法，消除重复代码
/// </summary>
public static class UiUtils
{
    private static Logger Log => ChatUiPatch.Log;

    /// <summary>
    ///     清空 Control 的所有子节点
    /// </summary>
    public static void ClearChildren(Control container)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child?.QueueFree();
        }
    }

    /// <summary>
    ///     设置 NPotionHolder 的缩放（封装反射）
    ///     需要在 AddPotion 之后调用
    /// </summary>
    public static void SetPotionScale(NPotionHolder holder, float scale)
    {
        var scaleField = typeof(NPotionHolder).GetField("_potionScale",
            BindingFlags.NonPublic | BindingFlags.Instance);
        scaleField?.SetValue(holder, Vector2.One * scale);
    }
}
