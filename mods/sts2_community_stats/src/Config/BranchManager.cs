using MegaCrit.Sts2.Core.Platform;

namespace CommunityStats.Config;

/// <summary>
/// Detects whether the player is on the Steam "public" (release) branch
/// or a beta branch, and maps that to a short tag for upload and filtering.
/// </summary>
public static class BranchManager
{
    public const string Release = "release";
    public const string Beta = "beta";
    public const string Unknown = "unknown";
    public const string All = "all";

    /// <summary>
    /// The player's current branch. Steam "public" branch maps to "release";
    /// any non-public branch (beta branches) maps to "beta".
    /// </summary>
    public static string CurrentBranch
    {
        get
        {
            try
            {
                var platformBranch = PlatformUtil.GetPlatformBranch();
                // PlatformBranch is a game-side enum. "Public" = the default
                // Steam branch; anything else is a beta/preview branch.
                return platformBranch.ToString() == "Public" ? Release : Beta;
            }
            catch
            {
                return Release;
            }
        }
    }

    /// <summary>
    /// Resolves the effective branch for API queries.
    /// null/"auto" → user's current branch; "all" → all; specific → that branch.
    /// </summary>
    public static string GetEffectiveBranch(FilterSettings filter)
    {
        if (filter.Branch == "all") return All;
        if (filter.Branch == "release" || filter.Branch == "beta") return filter.Branch;
        return CurrentBranch; // null → auto
    }
}
