using System.Collections.Generic;
using System.Linq;
using CommunityStats.Config;
using CommunityStats.Util;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace CommunityStats.UI;

/// <summary>
/// Hover panel displaying a monster's intent state machine. PRD §3.10
/// round 8 rewrite — Jaw Worm style:
///
///   ┌─── Monster (HP X/Y) ──────────────────┐
///   │                                       │
///   │  [▶ CURRENT]   ──→   [25% ≤1 ATK 11]  │
///   │   ATK 12              [45% BUFF]      │
///   │                       [30% ≤2 BLK 7]  │
///   │                                       │
///   └───────────────────────────────────────┘
///
/// Data path (round 8):
///   1. Always pass the live `Monster` instance to MonsterIntentMetadata.
///      The cache prefers the live `MoveStateMachine` and falls back to
///      a canonical-clone bake — combat-time state is always populated.
///   2. Render the metadata as two columns: current state on the left,
///      branch list on the right.
/// </summary>
public static class IntentStateMachinePanel
{
    private static readonly Color GoldColor   = new("#EFC851");
    private static readonly Color CreamColor  = new("#FFF6E2");
    private static readonly Color AquaColor   = new(0.16f, 0.92f, 0.75f);
    private static readonly Color GrayColor   = new(0.55f, 0.55f, 0.65f);
    private static readonly Color RedColor    = new(0.92f, 0.30f, 0.25f);
    private static readonly Color BlueColor   = new(0.36f, 0.58f, 0.95f);
    private static readonly Color PurpleColor = new(0.74f, 0.55f, 0.95f);
    private static readonly Color GreenColor  = new(0.40f, 0.82f, 0.55f);

    public static InfoModPanel? Create(Creature owner)
    {
        if (owner == null) return null;
        var monster = owner.Monster;
        if (monster == null) return null;

        string monsterId;
        try { monsterId = monster.Id?.Entry ?? "?"; }
        catch { monsterId = "?"; }

        var entry = MonsterIntentMetadata.Get(monsterId, monster);
        var displayName = TryName(monster, monsterId);
        var hpLine = $"HP {owner.CurrentHp}/{owner.MaxHp}";

        var panel = InfoModPanel.Create($"{displayName}  ({hpLine})", L.Get("intent.subtitle"));
        panel.AddSeparator();

        if (entry == null || entry.States.Count == 0)
        {
            // Fall back to whatever the live NextMove gives us so the user
            // at least sees the current intent.
            RenderLiveIntentOnly(panel, monster);
            return panel;
        }

        // Round 8: identify the current state. Live NextMove.Id is the most
        // accurate source; if it's not in the metadata cache, use the
        // initial state from the cache.
        string? currentStateId = null;
        try { currentStateId = monster.NextMove?.Id; } catch { }
        if (string.IsNullOrEmpty(currentStateId) || !entry.States.Any(s => s.Id == currentStateId))
            currentStateId = entry.InitialStateId;

        // Two-column layout: current state | branch list.
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 14);
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddCustom(hbox);

        // Left column: current state highlight.
        hbox.AddChild(BuildCurrentStateColumn(entry, currentStateId));

        // Center column: arrow.
        var arrow = new Label { Text = "──→" };
        arrow.AddThemeColorOverride("font_color", GoldColor);
        arrow.AddThemeFontSizeOverride("font_size", 18);
        arrow.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        hbox.AddChild(arrow);

        // Right column: every reachable next state.
        hbox.AddChild(BuildBranchColumn(entry, currentStateId));

        // Footer: full state list with branching, useful when the monster
        // has many states (multi-phase Boss).
        if (entry.States.Count > 4)
        {
            panel.AddSeparator();
            panel.AddLabel(L.Get("intent.state_machine"), AquaColor);
            foreach (var s in entry.States)
                DescribeStateRow(panel, s, currentStateId);
        }

        return panel;
    }

    // ── Column builders ─────────────────────────────────────

    private static Control BuildCurrentStateColumn(MonsterIntentMetadata.MonsterEntry entry, string? currentStateId)
    {
        var box = WrapInBox(AquaColor, 0.18f);
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 4);
        box.AddChild(v);

        var label = new Label { Text = "▶ " + (currentStateId ?? "?") };
        label.AddThemeColorOverride("font_color", AquaColor);
        label.AddThemeFontSizeOverride("font_size", 13);
        v.AddChild(label);

        var current = entry.States.FirstOrDefault(s => s.Id == currentStateId);
        if (current != null)
        {
            switch (current.Kind)
            {
                case MonsterIntentMetadata.StateKind.Move:
                    foreach (var i in current.Intents)
                        v.AddChild(NewIntentBadge(i));
                    break;
                case MonsterIntentMetadata.StateKind.RandomBranch:
                    var hint = new Label { Text = "(random)" };
                    hint.AddThemeColorOverride("font_color", GrayColor);
                    hint.AddThemeFontSizeOverride("font_size", 11);
                    v.AddChild(hint);
                    break;
                default:
                    break;
            }
        }
        return box;
    }

    private static Control BuildBranchColumn(MonsterIntentMetadata.MonsterEntry entry, string? currentStateId)
    {
        var box = WrapInBox(new Color(0.4f, 0.5f, 0.7f, 0.5f), 0.12f);
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 6);
        box.AddChild(v);

        // Find the next reachable states from the current state.
        var nextStates = ResolveNextStates(entry, currentStateId);
        if (nextStates.Count == 0)
        {
            var lbl = new Label { Text = L.Get("intent.no_next") };
            lbl.AddThemeColorOverride("font_color", GrayColor);
            lbl.AddThemeFontSizeOverride("font_size", 11);
            v.AddChild(lbl);
            return box;
        }

        foreach (var (stateId, weightPct, maxTimes) in nextStates)
        {
            var state = entry.States.FirstOrDefault(s => s.Id == stateId);
            if (state == null) continue;

            var row = new VBoxContainer();
            row.AddThemeConstantOverride("separation", 1);

            // Header: percent + state id + max-times constraint
            var header = new HBoxContainer();
            header.AddThemeConstantOverride("separation", 6);
            if (weightPct >= 0f)
            {
                var pctLbl = new Label { Text = $"{weightPct:F0}%" };
                pctLbl.AddThemeColorOverride("font_color", GoldColor);
                pctLbl.AddThemeFontSizeOverride("font_size", 12);
                header.AddChild(pctLbl);
            }
            if (maxTimes > 0)
            {
                var maxLbl = new Label { Text = $"≤{maxTimes}" };
                maxLbl.AddThemeColorOverride("font_color", GrayColor);
                maxLbl.AddThemeFontSizeOverride("font_size", 11);
                header.AddChild(maxLbl);
            }
            var idLbl = new Label { Text = stateId };
            idLbl.AddThemeColorOverride("font_color", CreamColor);
            idLbl.AddThemeFontSizeOverride("font_size", 11);
            header.AddChild(idLbl);
            row.AddChild(header);

            // Intents inside the target state
            if (state.Kind == MonsterIntentMetadata.StateKind.Move)
            {
                foreach (var i in state.Intents)
                    row.AddChild(NewIntentBadge(i));
            }

            v.AddChild(row);
        }
        return box;
    }

    /// <summary>
    /// Decode the "next reachable states" from the current state. For Move
    /// states with FollowUpStateId, that's a single deterministic transition
    /// (100%). For RandomBranchState, it's the per-branch weight distribution.
    /// </summary>
    private static List<(string id, float pct, int maxTimes)> ResolveNextStates(
        MonsterIntentMetadata.MonsterEntry entry, string? currentStateId)
    {
        var result = new List<(string, float, int)>();
        if (string.IsNullOrEmpty(currentStateId)) return result;

        var current = entry.States.FirstOrDefault(s => s.Id == currentStateId);
        if (current == null) return result;

        // Move state with deterministic follow-up.
        if (current.Kind == MonsterIntentMetadata.StateKind.Move
            && !string.IsNullOrEmpty(current.FollowUpStateId))
        {
            var target = entry.States.FirstOrDefault(s => s.Id == current.FollowUpStateId);
            if (target == null)
            {
                result.Add((current.FollowUpStateId!, 100f, 0));
                return result;
            }
            // If the follow-up is itself a random branch, decode its branches.
            if (target.Kind == MonsterIntentMetadata.StateKind.RandomBranch)
            {
                AddBranches(target, result);
                return result;
            }
            result.Add((current.FollowUpStateId!, 100f, 0));
            return result;
        }

        // Random branch — list every branch.
        if (current.Kind == MonsterIntentMetadata.StateKind.RandomBranch)
        {
            AddBranches(current, result);
            return result;
        }

        return result;
    }

    private static void AddBranches(MonsterIntentMetadata.StateInfo branch,
        List<(string, float, int)> result)
    {
        float total = branch.Branches.Sum(b => b.Weight);
        if (total <= 0f) total = 1f;
        foreach (var b in branch.Branches)
        {
            float pct = b.Weight / total * 100f;
            result.Add((b.TargetStateId, pct, b.MaxTimes));
        }
    }

    // ── Render helpers ──────────────────────────────────────

    private static void RenderLiveIntentOnly(InfoModPanel panel, MegaCrit.Sts2.Core.Models.MonsterModel monster)
    {
        try
        {
            var nextMove = monster.NextMove;
            if (nextMove?.Intents == null || nextMove.Intents.Count == 0)
            {
                panel.AddLabel(L.Get("intent.no_move"), GrayColor);
                return;
            }
            panel.AddLabel(L.Get("intent.current"), AquaColor);
            foreach (var intent in nextMove.Intents)
                panel.AddCustom(NewLiveIntentBadge(intent));
        }
        catch
        {
            panel.AddLabel(L.Get("intent.no_move"), GrayColor);
        }
    }

    private static Control NewIntentBadge(MonsterIntentMetadata.IntentInfo intent)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);

        var typeLabel = new Label { Text = AbbreviateIntentName(intent.IntentTypeName) };
        typeLabel.AddThemeColorOverride("font_color", IntentColor(intent.IntentType));
        typeLabel.AddThemeFontSizeOverride("font_size", 11);
        hbox.AddChild(typeLabel);

        if (intent.Damage.HasValue && intent.Damage.Value > 0)
        {
            var dmg = new Label { Text = intent.Repeats > 1 ? $"{intent.Damage}×{intent.Repeats}" : intent.Damage!.ToString() };
            dmg.AddThemeColorOverride("font_color", CreamColor);
            dmg.AddThemeFontSizeOverride("font_size", 11);
            hbox.AddChild(dmg);
        }
        return hbox;
    }

    private static Control NewLiveIntentBadge(AbstractIntent intent)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        var lbl = new Label { Text = "  · " + AbbreviateIntentName(intent.GetType().Name) };
        lbl.AddThemeColorOverride("font_color", IntentColor(intent.IntentType));
        lbl.AddThemeFontSizeOverride("font_size", 11);
        hbox.AddChild(lbl);
        try
        {
            if (intent is AttackIntent atk && atk.DamageCalc != null)
            {
                var dmg = new Label { Text = ((int)atk.DamageCalc()).ToString() };
                dmg.AddThemeColorOverride("font_color", CreamColor);
                dmg.AddThemeFontSizeOverride("font_size", 11);
                hbox.AddChild(dmg);
            }
        }
        catch { }
        return hbox;
    }

    private static string AbbreviateIntentName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        // SingleAttackIntent → ATTACK, BuffIntent → BUFF, etc.
        if (name.EndsWith("Intent")) name = name.Substring(0, name.Length - "Intent".Length);
        if (name.Contains("Attack")) return "ATTACK";
        if (name.Contains("Buff"))   return "BUFF";
        if (name.Contains("Debuff")) return "DEBUFF";
        if (name.Contains("Block"))  return "BLOCK";
        if (name.Contains("Status")) return "STATUS";
        if (name.Contains("Sleep"))  return "SLEEP";
        if (name.Contains("Stun"))   return "STUN";
        return name.ToUpperInvariant();
    }

    private static Color IntentColor(IntentType type) => type switch
    {
        IntentType.Attack    => RedColor,
        IntentType.DeathBlow => RedColor,
        IntentType.Buff      => GreenColor,
        IntentType.Debuff    => PurpleColor,
        _                    => BlueColor,
    };

    private static void DescribeStateRow(InfoModPanel panel, MonsterIntentMetadata.StateInfo state, string? currentId)
    {
        bool isCurrent = state.Id == currentId;
        var marker = isCurrent ? "▶ " : "  ";
        var summary = state.Kind switch
        {
            MonsterIntentMetadata.StateKind.Move =>
                string.Join(" ", state.Intents.Select(i => AbbreviateIntentName(i.IntentTypeName) +
                    (i.Damage.HasValue ? " " + i.Damage : ""))),
            MonsterIntentMetadata.StateKind.RandomBranch =>
                $"random ({state.Branches.Count})",
            MonsterIntentMetadata.StateKind.ConditionalBranch =>
                "conditional",
            _ => state.Kind.ToString(),
        };
        panel.AddRow(marker + state.Id, summary,
            isCurrent ? AquaColor : CreamColor,
            isCurrent ? GoldColor : CreamColor);
    }

    private static PanelContainer WrapInBox(Color border, float bgAlpha)
    {
        var p = new PanelContainer();
        var s = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.12f, 0.18f, bgAlpha + 0.55f),
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        p.AddThemeStyleboxOverride("panel", s);
        return p;
    }

    private static string TryName(MegaCrit.Sts2.Core.Models.MonsterModel monster, string fallback)
    {
        try { return monster.Title?.GetFormattedText() ?? fallback; }
        catch { return fallback; }
    }
}
