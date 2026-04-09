using CommunityStats.Util;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CommunityStats.UI;

/// <summary>
/// Hover panel displaying a monster's intent state machine. PRD 3.10.
///
/// Attempts to read MonsterMoveStateMachine via reflection and show the
/// current state plus branch probabilities. Falls back gracefully when
/// the state machine data is not available.
/// </summary>
public static class IntentStateMachinePanel
{
    private static readonly Color GoldColor = new("#EFC851");
    private static readonly Color CreamColor = new("#FFF6E2");
    private static readonly Color AquaColor = new(0.16f, 0.92f, 0.75f);

    public static InfoModPanel? Create(Creature owner)
    {
        if (owner == null) return null;

        var monster = owner.Monster;
        var sm = monster?.MoveStateMachine;
        if (sm == null) return null;

        var monsterName = monster?.Name ?? "Unknown";
        var panel = InfoModPanel.Create(monsterName, "Intent state machine");
        panel.AddSeparator();

        // Read private _currentState via Traverse
        MonsterState? currentState = null;
        try
        {
            currentState = Traverse.Create(sm).Field("_currentState").GetValue<MonsterState>();
        }
        catch (System.Exception ex)
        {
            Safe.Warn($"IntentStateMachinePanel: failed to read _currentState: {ex.Message}");
        }

        if (currentState == null)
        {
            panel.AddLabel("(状态机数据不可用)", CreamColor);
            return panel;
        }

        // Current state row
        var currentName = currentState.GetType().Name;
        panel.AddRow("当前状态", currentName, AquaColor, GoldColor);

        // Enumerate reachable states
        var states = sm.States;
        if (states != null && states.Count > 0)
        {
            panel.AddSeparator();
            foreach (var kvp in states)
            {
                DescribeState(panel, kvp.Key, kvp.Value);
            }
        }

        return panel;
    }

    private static void DescribeState(InfoModPanel panel, string id, MonsterState state)
    {
        switch (state)
        {
            case MoveState move:
                DescribeMoveState(panel, id, move);
                break;
            case RandomBranchState rnd:
                DescribeRandomBranchState(panel, id, rnd);
                break;
            default:
                panel.AddRow(id, state.GetType().Name, CreamColor, CreamColor);
                break;
        }
    }

    private static void DescribeMoveState(InfoModPanel panel, string id, MoveState move)
    {
        var intents = move.Intents;
        var summary = intents != null
            ? string.Join(" ", System.Linq.Enumerable.Select(intents, (AbstractIntent i) => i.IntentType.ToString()))
            : "(no intents)";
        panel.AddRow(id, summary, GoldColor, CreamColor);
    }

    private static void DescribeRandomBranchState(InfoModPanel panel, string id, RandomBranchState rnd)
    {
        var stateWeights = rnd.States;
        if (stateWeights == null || stateWeights.Count == 0)
        {
            panel.AddRow(id, "(empty branch)", GoldColor, CreamColor);
            return;
        }

        // Compute normalized weights safely
        float total = 0f;
        var weights = new float[stateWeights.Count];
        for (int i = 0; i < stateWeights.Count; i++)
        {
            float w = 1f;
            try { w = stateWeights[i].GetWeight(); } catch { w = 1f; }
            if (w < 0f) w = 0f;
            weights[i] = w;
            total += w;
        }
        if (total <= 0f) total = 1f;

        panel.AddRow(id + " (随机)", "", AquaColor, CreamColor);
        for (int i = 0; i < stateWeights.Count; i++)
        {
            var sw = stateWeights[i];
            var pct = (weights[i] / total * 100f).ToString("F0") + "%";
            var constraint = sw.maxTimes > 0 ? $" ≤{sw.maxTimes}" : "";
            panel.AddRow("  → " + sw.stateId + constraint, pct, CreamColor, GoldColor);
        }
    }
}
