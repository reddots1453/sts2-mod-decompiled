using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CommunityStats.Util;

/// <summary>
/// Pre-baked monster intent state-machine metadata.
///
/// Background (PRD §3.10 round 6): the previous hover panel reached into
/// `creature.Monster.MoveStateMachine` at hover time and frequently saw a
/// null backing field — a Godot signal-callback timing race nobody could
/// reproduce reliably. Instead of fighting the timing, we walk every
/// monster prototype at **mod init** and snapshot its state machine into a
/// flat, immutable description that the hover panel can render without
/// touching any live combat state.
///
/// At init time we:
///   1. Iterate `ModelDb.Monsters` (~121 entries).
///   2. Mutable-clone each monster (the canonical instances are read-only).
///   3. Call `SetUpForCombat()` which assigns the `MoveStateMachine` field.
///   4. Walk every state, decode `MoveState` / `RandomBranchState` /
///      `ConditionalBranchState`, and copy the relevant fields into a POCO.
///   5. Store the result keyed by the monster id.
///
/// Init failures are tolerated per monster — a missing entry simply means
/// the hover panel falls back to "no metadata" instead of crashing.
/// </summary>
public static class MonsterIntentMetadata
{
    public enum StateKind { Move, RandomBranch, ConditionalBranch, Other }

    public sealed class IntentInfo
    {
        public string IntentTypeName { get; init; } = "";
        public IntentType IntentType { get; init; }
        public int? Damage { get; init; }
        public int? Block { get; init; }
        public int Repeats { get; init; } = 1;
    }

    public sealed class BranchInfo
    {
        public string TargetStateId { get; init; } = "";
        public float Weight { get; init; } = 1f;
        public int MaxTimes { get; init; }
    }

    public sealed class StateInfo
    {
        public string Id { get; init; } = "";
        public StateKind Kind { get; init; }
        public List<IntentInfo> Intents { get; init; } = new();
        public string? FollowUpStateId { get; init; }
        public List<BranchInfo> Branches { get; init; } = new();
    }

    public sealed class MonsterEntry
    {
        public string MonsterId { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string? InitialStateId { get; init; }
        public List<StateInfo> States { get; init; } = new();
    }

    // Round 8: lazy cache. Negative entries (failures) live in here too so
    // we don't keep retrying broken monsters.
    private static readonly Dictionary<string, MonsterEntry?> _cache = new();
    public static int Count => _cache.Count;

    /// <summary>
    /// Look up a monster's pre-baked state-machine snapshot. Round 8: tries
    /// to use the LIVE mutable monster instance first (which always has its
    /// SM populated during combat), then falls back to baking from the
    /// canonical instance. Negative results are cached.
    /// </summary>
    public static MonsterEntry? Get(string monsterId) =>
        Get(monsterId, liveMonster: null);

    public static MonsterEntry? Get(string monsterId, MonsterModel? liveMonster)
    {
        if (string.IsNullOrEmpty(monsterId)) return null;

        // Live monster path: prefer the in-combat instance because its SM
        // is guaranteed populated and matches whatever the game is using.
        if (liveMonster != null)
        {
            try
            {
                if (liveMonster.MoveStateMachine != null)
                {
                    var live = BuildEntryFromInstance(liveMonster);
                    if (live != null && live.States.Count > 0)
                    {
                        _cache[monsterId] = live;
                        return live;
                    }
                }
            }
            catch (Exception ex)
            {
                Safe.Warn($"[IntentMeta] live build failed for {monsterId}: {ex.Message}");
            }
        }

        // Cache hit (positive or negative).
        if (_cache.TryGetValue(monsterId, out var cached)) return cached;

        // Cache miss: try to bake from canonical instance.
        var entry = TryBakeFromCanonical(monsterId);
        _cache[monsterId] = entry; // store negative result too
        if (entry == null)
            Safe.Warn($"[IntentMeta] no metadata could be built for {monsterId}");
        return entry;
    }

    /// <summary>
    /// Backwards-compat shim for the old eager init call site. Round 8 is
    /// fully lazy so this is now a no-op kept to avoid breaking the mod
    /// initialization order.
    /// </summary>
    public static void Initialize()
    {
        // Lazy mode — see Get(monsterId, liveMonster).
    }

    private static MonsterEntry? TryBakeFromCanonical(string monsterId)
    {
        try
        {
            MonsterModel? canonical = null;
            foreach (var m in ModelDb.Monsters)
            {
                if (m?.Id?.Entry == monsterId) { canonical = m; break; }
            }
            if (canonical == null) return null;
            return Bake(canonical);
        }
        catch (Exception ex)
        {
            Safe.Warn($"[IntentMeta] canonical bake failed for {monsterId}: {ex.Message}");
            return null;
        }
    }

    private static MonsterEntry? BuildEntryFromInstance(MonsterModel m)
    {
        var sm = m.MoveStateMachine;
        if (sm?.States == null || sm.States.Count == 0) return null;

        string? initialId = null;
        try
        {
            var init = Traverse.Create(sm).Field("_initialState").GetValue<MonsterState>();
            initialId = init?.Id;
        }
        catch { }

        var states = new List<StateInfo>();
        foreach (var kv in sm.States)
        {
            try { states.Add(BuildState(kv.Key, kv.Value)); }
            catch (Exception ex) { Safe.Warn($"[IntentMeta] {m.Id.Entry}.{kv.Key}: {ex.Message}"); }
        }
        return new MonsterEntry
        {
            MonsterId = m.Id.Entry,
            DisplayName = TryName(m, m.Id.Entry),
            InitialStateId = initialId,
            States = states,
        };
    }

    private static MonsterEntry? Bake(MonsterModel canonical)
    {
        if (canonical == null) return null;
        var id = canonical.Id?.Entry;
        if (string.IsNullOrEmpty(id)) return null;

        // Mutable clone — canonical instances are immutable so SetUpForCombat
        // would throw `AssertMutable`. We create a fresh mutable copy purely
        // to populate its MoveStateMachine field.
        MonsterModel mut;
        try
        {
            mut = canonical.ToMutable();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[IntentMeta] {id}: ToMutable failed: {ex.Message}");
            return null;
        }

        try
        {
            mut.SetUpForCombat();
        }
        catch (Exception ex)
        {
            Safe.Warn($"[IntentMeta] {id}: SetUpForCombat failed: {ex.Message}");
            return null;
        }

        var sm = mut.MoveStateMachine;
        if (sm == null || sm.States == null || sm.States.Count == 0)
            return null;

        // Resolve initial state via reflection (private field _initialState).
        string? initialId = null;
        try
        {
            var init = Traverse.Create(sm).Field("_initialState").GetValue<MonsterState>();
            initialId = init?.Id;
        }
        catch { }

        var states = new List<StateInfo>();
        foreach (var kv in sm.States)
        {
            try { states.Add(BuildState(kv.Key, kv.Value)); }
            catch (Exception ex) { Safe.Warn($"[IntentMeta] {id}.{kv.Key}: {ex.Message}"); }
        }

        return new MonsterEntry
        {
            MonsterId = id!,
            DisplayName = TryName(canonical, id!),
            InitialStateId = initialId,
            States = states,
        };
    }

    private static string TryName(MonsterModel m, string fallback)
    {
        try { return m.Title?.GetFormattedText() ?? fallback; }
        catch { return fallback; }
    }

    private static StateInfo BuildState(string id, MonsterState state)
    {
        switch (state)
        {
            case MoveState move:
                return new StateInfo
                {
                    Id = id,
                    Kind = StateKind.Move,
                    Intents = move.Intents?.Select(BuildIntent).ToList() ?? new(),
                    FollowUpStateId = move.FollowUpStateId ?? move.FollowUpState?.Id,
                };
            case RandomBranchState rnd:
                var branches = new List<BranchInfo>();
                foreach (var sw in rnd.States)
                {
                    float w = 1f;
                    try { w = sw.GetWeight(); } catch { }
                    branches.Add(new BranchInfo
                    {
                        TargetStateId = sw.stateId,
                        Weight = Math.Max(0f, w),
                        MaxTimes = sw.maxTimes,
                    });
                }
                return new StateInfo { Id = id, Kind = StateKind.RandomBranch, Branches = branches };
            case ConditionalBranchState cond:
                // Conditional branches store lambda predicates we can't display;
                // we list the candidate target ids if reachable via reflection.
                var condBranches = new List<BranchInfo>();
                try
                {
                    var rawList = Traverse.Create(cond).Field("_branches").GetValue<System.Collections.IEnumerable>();
                    if (rawList != null)
                    {
                        foreach (var entry in rawList)
                        {
                            var t = Traverse.Create(entry);
                            var stateId = t.Field("stateId").GetValue<string>();
                            if (!string.IsNullOrEmpty(stateId))
                                condBranches.Add(new BranchInfo { TargetStateId = stateId });
                        }
                    }
                }
                catch { }
                return new StateInfo { Id = id, Kind = StateKind.ConditionalBranch, Branches = condBranches };
            default:
                return new StateInfo { Id = id, Kind = StateKind.Other };
        }
    }

    private static IntentInfo BuildIntent(AbstractIntent intent)
    {
        var info = new IntentInfo
        {
            IntentType = intent.IntentType,
            IntentTypeName = intent.IntentType.ToString(),
        };

        // Use reflection on the abstract DamageCalc and Repeats since the
        // labelling helpers want a live target/owner pair.
        try
        {
            if (intent is AttackIntent atk)
            {
                int repeats = atk.Repeats > 0 ? atk.Repeats : 1;
                int? baseDamage = null;
                var calc = atk.DamageCalc;
                if (calc != null)
                {
                    try { baseDamage = (int)calc(); } catch { }
                }
                return new IntentInfo
                {
                    IntentType = atk.IntentType,
                    IntentTypeName = atk.GetType().Name,
                    Damage = baseDamage,
                    Repeats = repeats,
                };
            }
        }
        catch { }
        return info;
    }
}
