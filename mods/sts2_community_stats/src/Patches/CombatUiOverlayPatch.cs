using System.Linq;
using CommunityStats.Collection;
using CommunityStats.Config;
using CommunityStats.UI;
using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace CommunityStats.Patches;

/// <summary>
/// Manages the top-right potion-drop and card-drop odds indicators
/// (PRD §3.9 + §3.17). Round-5 rewrite (manual feedback):
///
/// - Indicators are now attached to the scene tree ROOT (singletons),
///   not to NCombatUi. The previous NCombatUi parenting was unreliable —
///   indicators never appeared in the user's testing because Godot's
///   `Activate` postfix ran before the layout was finalized and the
///   anchor offsets resolved against an empty rect.
/// - Visibility is driven by `CombatManager.SetUpCombat` (Postfix) and
///   `CombatRoom.OnCombatEnded` (Postfix). They show during combat and
///   hide afterwards (or when the toggle is off).
/// - Values are refreshed at every combat start and on every
///   `CombatTracker.CombatDataUpdated` event so pity progress is live.
/// </summary>
[HarmonyPatch]
public static class CombatUiOverlayPatch
{
    private static PotionOddsIndicator? _potion;
    private static CardDropOddsIndicator? _cardDrop;
    private static bool _subscribed;

    /// <summary>
    /// Called from CommunityStatsMod.Initialize. Subscribes to live data
    /// updates; the actual indicator nodes are constructed and parented
    /// inside `EnsureAttachedToTopBar` lazily on the first combat start
    /// because the NTopBar instance only exists once a run is active.
    /// </summary>
    public static void Attach()
    {
        Safe.Run(() =>
        {
            if (!_subscribed)
            {
                CombatTracker.Instance.CombatDataUpdated += OnCombatDataUpdated;
                _subscribed = true;
            }
        });
    }

    /// <summary>
    /// Lazily build the indicators and parent them to the same HBox that
    /// holds NTopBar.Map / NTopBar.Deck so they live in the natural top-bar
    /// flow with matching size and position. PRD §3.9 / §3.17 round 7.
    /// </summary>
    private static void EnsureAttachedToTopBar()
    {
        var topBar = NRun.Instance?.GlobalUi?.TopBar;
        if (topBar == null) return;
        var mapBtn = topBar.Map;
        if (mapBtn == null) return;
        var hostHbox = mapBtn.GetParent();
        if (hostHbox == null) return;

        if (_potion == null)
        {
            _potion = PotionOddsIndicator.Create();
            _potion.Visible = false;
            _potion.ZIndex = 100;
        }
        if (_potion.GetParent() != hostHbox)
        {
            _potion.GetParent()?.RemoveChild(_potion);
            hostHbox.AddChild(_potion);
            try { hostHbox.MoveChild(_potion, mapBtn.GetIndex() + 1); } catch { }
        }

        if (_cardDrop == null)
        {
            _cardDrop = CardDropOddsIndicator.Create();
            _cardDrop.Visible = false;
            _cardDrop.ZIndex = 100;
        }
        if (_cardDrop.GetParent() != hostHbox)
        {
            _cardDrop.GetParent()?.RemoveChild(_cardDrop);
            hostHbox.AddChild(_cardDrop);
            try { hostHbox.MoveChild(_cardDrop, mapBtn.GetIndex() + 2); } catch { }
        }
    }

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
    [HarmonyPostfix]
    public static void AfterSetUpCombat(CombatManager __instance, CombatState state)
    {
        Safe.Run(() => ShowFor(state));
    }

    [HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OnCombatEnded))]
    [HarmonyPostfix]
    public static void AfterCombatEnded(CombatRoom __instance)
    {
        Safe.Run(HideAll);
    }

    private static void OnCombatDataUpdated()
    {
        Safe.Run(() =>
        {
            var state = CombatManager.Instance?.DebugOnlyGetState();
            if (state == null) return;
            RefreshValues(state);
        });
    }

    private static void ShowFor(CombatState? state)
    {
        if (state == null) return;
        var me = ResolveLocalPlayer(state);
        if (me == null) return;

        // Round 7: lazy-attach to NTopBar so the indicators live next to the
        // native Map / Deck buttons (correct size + position).
        EnsureAttachedToTopBar();

        if (ModConfig.Toggles.PotionDropOdds && _potion != null)
        {
            _potion.Visible = true;
        }
        if (ModConfig.Toggles.CardDropOdds && _cardDrop != null)
        {
            _cardDrop.Visible = true;
        }
        RefreshValues(state);
    }

    private static void RefreshValues(CombatState state)
    {
        var me = ResolveLocalPlayer(state);
        if (me == null) return;

        if (_potion != null && _potion.Visible)
        {
            try
            {
                var v = me.PlayerOdds?.PotionReward?.CurrentValue ?? 0.4f;
                _potion.UpdateOdds((float)v);
            }
            catch { }
        }
        if (_cardDrop != null && _cardDrop.Visible)
        {
            try
            {
                var v = me.PlayerOdds?.CardRarity?.CurrentValue ?? -0.05f;
                _cardDrop.UpdateOffset((float)v);
            }
            catch { }
        }
    }

    private static void HideAll()
    {
        if (_potion != null) _potion.Visible = false;
        if (_cardDrop != null) _cardDrop.Visible = false;
    }

    private static Player? ResolveLocalPlayer(CombatState state)
    {
        // Prefer the local-context player; fall back to first player so
        // single-player runs (where NetId may not be set yet) still work.
        Player? me = null;
        try { me = LocalContext.GetMe(state); } catch { }
        if (me == null) me = state.Players?.FirstOrDefault();
        return me;
    }
}
