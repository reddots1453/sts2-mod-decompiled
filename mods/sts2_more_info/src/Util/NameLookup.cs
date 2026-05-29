using System;
using MegaCrit.Sts2.Core.Localization;

namespace MoreInfo.Util;

/// <summary>
/// Resolves localized display names for game model IDs from the game's
/// localization tables. Always returns names in the game's active language.
/// Returns the input ID unchanged when the lookup fails so the UI never
/// shows an empty cell.
/// </summary>
public static class NameLookup
{
    public static string Relic(string id)   => Lookup("relics", id, ".title");
    public static string Monster(string id) => Lookup("monsters", id, ".name");

    // ── internal ──────────────────────────────────────────────

    private static string Lookup(string table, string id, string suffix)
    {
        return TryLookup(table, id, suffix) ?? id;
    }

    private static string? TryLookup(string table, string id, string suffix)
    {
        if (string.IsNullOrEmpty(id)) return null;
        try
        {
            var key = id + suffix;
            if (!LocString.Exists(table, key)) return null;
            return LocManager.Instance.GetTable(table).GetRawText(key);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
