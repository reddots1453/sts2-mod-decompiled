using System.Collections.Generic;
using System.Linq;
using CommunityStats.Collection;
using CommunityStats.Config;
using Godot;

namespace CommunityStats.UI;

/// <summary>
/// "Stats the Spire" career stats block injected into the game's
/// 百科大全 → 角色数据 → 统计 page (PRD §3.11).
///
/// Layout sections (top to bottom):
///   - Title row "Stats the Spire" + total runs/wins summary
///   - Win rate trend (10 / 50 / all)
///   - Death cause Top 5 by Act
///   - Path stats per Act (cards & rooms)
///   - Ancient relic pick rates
///   - Boss damage taken summary
///
/// Reads CareerStatsData from RunHistoryAnalyzer (cached or loaded async).
/// Empty placeholder shown while loading; rebuilt when CareerStatsLoaded fires.
/// </summary>
public sealed partial class CareerStatsSection : VBoxContainer
{
    // ── Style ───────────────────────────────────────────────
    private static readonly Color Gold      = new("#EFC851");
    private static readonly Color Cream     = new("#FFF6E2");
    private static readonly Color Gray      = new(0.6f, 0.6f, 0.65f);
    private static readonly Color Green     = new(0.30f, 0.85f, 0.40f);
    private static readonly Color Red       = new(0.90f, 0.30f, 0.30f);
    private static readonly Color SectionBg = new(0.05f, 0.06f, 0.10f, 0.85f);
    private static readonly Color Border    = new(0.3f, 0.4f, 0.6f, 0.45f);

    private const int TitleSize    = 18;
    private const int SubtitleSize = 13;
    private const int LabelSize    = 12;
    private const int ValueSize    = 12;

    private string? _characterFilter;
    private CareerStatsData? _data;

    private CareerStatsSection() { }

    /// <summary>Build a new CareerStatsSection. characterFilter null = all characters.</summary>
    public static CareerStatsSection Create(string? characterFilter)
    {
        var s = new CareerStatsSection
        {
            Name = "ModCareerStatsSection",
            _characterFilter = characterFilter,
        };
        s.AddThemeConstantOverride("separation", 6);
        s.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        s.MouseFilter = MouseFilterEnum.Pass;

        // Try cached snapshot first
        s._data = RunHistoryAnalyzer.Instance.GetCached(characterFilter);
        s.Rebuild();

        // Subscribe for async result
        RunHistoryAnalyzer.Instance.CareerStatsLoaded += s.OnCareerStatsLoaded;
        s.TreeExiting += () => RunHistoryAnalyzer.Instance.CareerStatsLoaded -= s.OnCareerStatsLoaded;

        // Kick off async load if cache miss
        if (s._data == null || s._data.IsEmpty)
        {
            Util.Safe.RunAsync(async () =>
                await RunHistoryAnalyzer.Instance.LoadAllAsync(characterFilter));
        }

        return s;
    }

    private void OnCareerStatsLoaded(CareerStatsData data)
    {
        // Filter must match
        if ((data.CharacterFilter ?? "") != (_characterFilter ?? "")) return;
        // Marshal to main thread for UI rebuild
        Util.Safe.Run(() =>
        {
            _data = data;
            CallDeferred(nameof(Rebuild));
        });
    }

    // ── Build ───────────────────────────────────────────────

    private void Rebuild()
    {
        // Clear existing children
        for (int i = GetChildCount() - 1; i >= 0; i--)
            GetChild(i).QueueFree();

        AddChild(BuildHeader());

        if (_data == null)
        {
            AddChild(MakeLabel(L.Get("career.loading"), Gray, SubtitleSize));
            return;
        }

        if (_data.IsEmpty)
        {
            AddChild(MakeLabel(L.Get("career.loading"), Gray, SubtitleSize));
            return;
        }

        AddChild(BuildWinRateTrend());
        AddChild(BuildDeathCauses());
        AddChild(BuildDeckTable());
        AddChild(BuildPathTable());
        AddChild(BuildAncientPickRates());
        AddChild(BuildBossStats());
    }

    private Control BuildHeader()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 4);
        panel.AddChild(v);

        var title = MakeLabel(L.Get("career.title"), Gold, TitleSize);
        v.AddChild(title);

        if (_data != null && !_data.IsEmpty)
        {
            float overall = _data.TotalRuns > 0 ? (float)_data.Wins / _data.TotalRuns : 0f;
            var sub = MakeLabel(
                $"{_data.TotalRuns} runs · {_data.Wins} wins · {(overall * 100):F1}%",
                Gray, SubtitleSize);
            v.AddChild(sub);
        }

        return panel;
    }

    private Control BuildWinRateTrend()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 4);
        panel.AddChild(v);

        v.AddChild(MakeLabel(L.Get("career.win_trend"), Gold, SubtitleSize));

        if (_data == null) return panel;

        var grid = new HBoxContainer();
        grid.AddThemeConstantOverride("separation", 18);
        v.AddChild(grid);

        // 10 / 50 / All windows
        AppendWindowCell(grid, 10, _data.WinRateByWindow);
        AppendWindowCell(grid, 50, _data.WinRateByWindow);
        AppendWindowCell(grid, int.MaxValue, _data.WinRateByWindow);

        return panel;
    }

    private void AppendWindowCell(HBoxContainer parent, int window, IReadOnlyDictionary<int, float> map)
    {
        var cell = new VBoxContainer();
        cell.AddThemeConstantOverride("separation", 2);

        string header = window == int.MaxValue
            ? L.Get("career.all")
            : string.Format(L.Get("career.last_n"), window);
        cell.AddChild(MakeLabel(header, Gray, LabelSize));

        float rate = map.TryGetValue(window, out var v) ? v : 0f;
        var color = rate >= 0.6f ? Green : (rate >= 0.4f ? Cream : Red);
        cell.AddChild(MakeLabel($"{rate * 100:F1}%", color, ValueSize + 2));

        parent.AddChild(cell);
    }

    private Control BuildDeathCauses()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 3);
        panel.AddChild(v);

        v.AddChild(MakeLabel(L.Get("career.death_causes"), Gold, SubtitleSize));

        if (_data == null || _data.DeathCausesByAct.Count == 0)
        {
            v.AddChild(MakeLabel("—", Gray, LabelSize));
            return panel;
        }

        foreach (var act in _data.DeathCausesByAct.Keys.OrderBy(k => k))
        {
            v.AddChild(MakeLabel(Util.NameLookup.ActLabel(act), Cream, LabelSize));
            foreach (var entry in _data.DeathCausesByAct[act])
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 8);
                var name = MakeLabel("  · " + Util.NameLookup.DeathCause(entry.EncounterId), Cream, LabelSize);
                name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                var count = MakeLabel(
                    $"{entry.Count} ({entry.Share * 100:F1}%)",
                    Gray, LabelSize);
                row.AddChild(name);
                row.AddChild(count);
                v.AddChild(row);
            }
        }

        return panel;
    }

    // ── Deck construction table (round 5 — split from path stats) ──

    private static readonly Color[] DeckColors = new[]
    {
        new Color("#EFC851"),               // gold
        new Color(0.16f, 0.92f, 0.75f),     // aqua
        new Color(0.90f, 0.30f, 0.30f),     // red
        new Color(0.30f, 0.85f, 0.40f),     // green
    };
    private static readonly Color[] PathColors = new[]
    {
        new Color(0.90f, 0.30f, 0.30f),     // red — monsters
        new Color(0.95f, 0.55f, 0.25f),     // orange — elite
        new Color(0.74f, 0.55f, 0.95f),     // lavender — ?
        new Color("#EFC851"),               // gold — shop
    };

    private Control BuildDeckTable()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 3);
        panel.AddChild(v);

        v.AddChild(MakeLabel(L.Get("career.deck_section"), Gold, SubtitleSize));

        if (_data == null || _data.PathStatsByAct.Count == 0)
        {
            v.AddChild(MakeLabel(L.Get("career.no_data_short"), Gray, LabelSize));
            return panel;
        }

        var acts = _data.PathStatsByAct.Keys.OrderBy(k => k).ToList();
        var grid = NewActGrid(acts);
        v.AddChild(grid);

        AddPathRow(grid, acts, "career.cards_gained",   DeckColors[0], s => s.CardsGained);
        AddPathRow(grid, acts, "career.cards_bought",   DeckColors[1], s => s.CardsBought);
        AddPathRow(grid, acts, "career.cards_removed",  DeckColors[2], s => s.CardsRemoved);
        AddPathRow(grid, acts, "career.cards_upgraded", DeckColors[3], s => s.CardsUpgraded);
        return panel;
    }

    private Control BuildPathTable()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 3);
        panel.AddChild(v);

        v.AddChild(MakeLabel(L.Get("career.path_section"), Gold, SubtitleSize));

        if (_data == null || _data.PathStatsByAct.Count == 0)
        {
            v.AddChild(MakeLabel(L.Get("career.no_data_short"), Gray, LabelSize));
            return panel;
        }

        var acts = _data.PathStatsByAct.Keys.OrderBy(k => k).ToList();
        var grid = NewActGrid(acts);
        v.AddChild(grid);

        AddPathRow(grid, acts, "career.monster_rooms", PathColors[0], s => s.MonsterRooms);
        AddPathRow(grid, acts, "career.elite_rooms",   PathColors[1], s => s.EliteRooms);
        AddPathRow(grid, acts, "career.unknown_rooms", PathColors[2], s => s.UnknownRooms);
        AddPathRow(grid, acts, "career.shop_rooms",    PathColors[3], s => s.ShopRooms);
        return panel;
    }

    private GridContainer NewActGrid(List<int> acts)
    {
        var grid = new GridContainer { Columns = acts.Count + 1 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 2);
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        grid.AddChild(MakeLabel("", Gray, LabelSize));
        foreach (var act in acts)
            grid.AddChild(MakeLabel(Util.NameLookup.ActLabel(act), Gold, LabelSize));
        return grid;
    }

    private void AddPathRow(GridContainer grid, List<int> acts, string labelKey, Color rowColor,
        System.Func<ActPathStats, float> selector)
    {
        var label = MakeLabel(L.Get(labelKey), rowColor, LabelSize);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        grid.AddChild(label);

        foreach (var act in acts)
        {
            float v = _data!.PathStatsByAct.TryGetValue(act, out var stats)
                ? selector(stats)
                : 0f;
            var cell = MakeLabel($"{v:F1}", Cream, ValueSize);
            cell.HorizontalAlignment = HorizontalAlignment.Right;
            cell.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            grid.AddChild(cell);
        }
    }

    /// <summary>
    /// PRD §3.11 round 5: Ancient panel — dropdown of elders prefixed with
    /// their Act and a small icon. Each elder is split into 1-3 option pools
    /// (from `AncientPoolMap`); the per-pool list shows every relic with its
    /// pick rate / pick count / win rate / delta. Pulled icons via
    /// `AncientPoolMap.GetElderIcon` and `GetRelicIcon`.
    /// </summary>
    private Control BuildAncientPickRates()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 4);
        panel.AddChild(v);

        v.AddChild(MakeLabel(L.Get("career.ancient_title"), Gold, SubtitleSize));

        // Always show every elder we know about (even with 0 encounters) so the
        // dropdown is consistent across runs and shows the full menu structure.
        var allElders = Util.AncientPoolMap.AllElders
            .OrderBy(e => e.ActIndex == 0 ? 99 : e.ActIndex)
            .ThenBy(e => e.ElderId)
            .ToList();
        if (allElders.Count == 0)
        {
            v.AddChild(MakeLabel(L.Get("career.no_data_short"), Gray, LabelSize));
            return panel;
        }

        var dropdown = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Text = L.Get("career.elder_select"),
        };
        dropdown.AddThemeFontSizeOverride("font_size", LabelSize);
        for (int i = 0; i < allElders.Count; i++)
        {
            var elder = allElders[i];
            int encounters = (_data?.AncientByElder.TryGetValue(elder.ElderId, out var ee) ?? false)
                ? ee!.Encounters : 0;
            var actLabel = elder.ActIndex == 0 ? L.Get("ancient.shared_act") : Util.NameLookup.ActLabel(elder.ActIndex);
            var name = Util.NameLookup.Encounter(elder.ElderId);
            var label = $"{actLabel}  {name}  ({encounters} {L.Get("career.encounters")})";
            dropdown.AddItem(label, i);
            // Try to attach the elder icon to the dropdown item.
            var icon = Util.AncientPoolMap.GetElderIcon(elder.ElderId);
            if (icon != null)
            {
                try { dropdown.SetItemIcon(i, icon); } catch { }
            }
        }
        v.AddChild(dropdown);

        var detail = new VBoxContainer();
        detail.AddThemeConstantOverride("separation", 4);
        v.AddChild(detail);

        Action<int> showElder = idx =>
        {
            for (int i = detail.GetChildCount() - 1; i >= 0; i--)
                detail.GetChild(i).QueueFree();
            if (idx < 0 || idx >= allElders.Count) return;
            BuildElderDetail(detail, allElders[idx]);
        };

        dropdown.ItemSelected += i => showElder((int)i);
        dropdown.Selected = 0;
        showElder(0);

        return panel;
    }

    /// <summary>
    /// Render one elder's pools. The pool layout comes from `AncientPoolMap`
    /// (per-elder hand-curated metadata) and pick stats from `_data.AncientByElder`.
    /// </summary>
    private void BuildElderDetail(VBoxContainer parent, Util.AncientPoolMap.ElderInfo info)
    {
        // Resolve the player's per-relic counts for this elder, if any.
        var relicStats = new Dictionary<string, ElderRelicStats>();
        if (_data != null && _data.AncientByElder.TryGetValue(info.ElderId, out var entry) && entry != null)
        {
            foreach (var opt in entry.Options)
            foreach (var r in opt.Relics)
                relicStats[r.RelicId] = r;
        }

        // Header row: act + icon + name + encounters
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);

        var icon = Util.AncientPoolMap.GetElderIcon(info.ElderId);
        if (icon != null)
        {
            var iconRect = new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(32, 32),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
            };
            header.AddChild(iconRect);
        }

        var actLabel = info.ActIndex == 0 ? L.Get("ancient.shared_act") : Util.NameLookup.ActLabel(info.ActIndex);
        header.AddChild(MakeLabel(actLabel, Gold, LabelSize + 1));
        var nameLbl = MakeLabel(Util.NameLookup.Encounter(info.ElderId), Cream, LabelSize + 2);
        nameLbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(nameLbl);
        int totalEnc = (_data?.AncientByElder.TryGetValue(info.ElderId, out var ee2) ?? false) ? ee2!.Encounters : 0;
        header.AddChild(MakeLabel($"{totalEnc} {L.Get("career.encounters")}", Gray, LabelSize));
        parent.AddChild(header);

        // Render each pool as its own subsection.
        foreach (var pool in info.Pools)
        {
            var poolHeader = MakeLabel("• " + L.Get(pool.DisplayKey), Gold, LabelSize);
            parent.AddChild(poolHeader);

            int totalPicksInPool = pool.RelicIds.Sum(rid =>
                relicStats.TryGetValue(rid, out var rs) ? rs.Picks : 0);
            float avgWin = pool.RelicIds
                .Select(rid => relicStats.TryGetValue(rid, out var rs) ? rs.WinRate : 0f)
                .Where(w => w > 0).DefaultIfEmpty(0f).Average();

            foreach (var relicId in pool.RelicIds)
            {
                var stats = relicStats.TryGetValue(relicId, out var rs) ? rs : null;
                BuildRelicRow(parent, relicId, stats, totalPicksInPool, avgWin);
            }
        }
    }

    private void BuildRelicRow(VBoxContainer parent, string relicId, ElderRelicStats? stats,
        int poolTotalPicks, float poolAvgWin)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var icon = Util.AncientPoolMap.GetRelicIcon(relicId);
        if (icon != null)
        {
            var rect = new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(20, 20),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
            };
            row.AddChild(rect);
        }
        else
        {
            row.AddChild(new Control { CustomMinimumSize = new Vector2(20, 20) });
        }

        var name = MakeLabel(Util.NameLookup.Relic(relicId), Cream, LabelSize);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(name);

        if (stats != null && stats.Picks > 0)
        {
            float pickRate = poolTotalPicks > 0 ? (float)stats.Picks / poolTotalPicks : 0f;
            row.AddChild(MakeLabel($"{pickRate * 100:F1}%", Gold, LabelSize));
            row.AddChild(MakeLabel($"{stats.Picks}", Gray, LabelSize));

            var winColor = stats.WinRate >= 0.6f ? Green
                        : stats.WinRate >= 0.4f ? Cream : Red;
            row.AddChild(MakeLabel($"{stats.WinRate * 100:F1}%", winColor, LabelSize));

            float delta = (stats.WinRate - poolAvgWin) * 100f;
            var sign = delta >= 0 ? "+" : "";
            var dColor = MathF.Abs(delta) < 1f ? Gray : (delta >= 0 ? Green : Red);
            row.AddChild(MakeLabel($"{sign}{delta:F1}%", dColor, LabelSize));
        }
        else
        {
            row.AddChild(MakeLabel("—", Gray, LabelSize));
            row.AddChild(MakeLabel("0", Gray, LabelSize));
            row.AddChild(MakeLabel("—", Gray, LabelSize));
        }

        parent.AddChild(row);
    }

    /// <summary>
    /// PRD §3.11 round 5: Boss panel — dropdown listing bosses with the act
    /// prefix, encounter count and full localized name. The selected boss
    /// expands to a small detail card with avg damage / death rate / encounters.
    /// Boss icons are sourced from the encounter model when available.
    /// </summary>
    private Control BuildBossStats()
    {
        var panel = WrapInPanel();
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 4);
        panel.AddChild(v);

        v.AddChild(MakeLabel(L.Get("career.boss_title"), Gold, SubtitleSize));

        if (_data == null || _data.BossStats.Count == 0)
        {
            v.AddChild(MakeLabel(L.Get("career.no_data_short"), Gray, LabelSize));
            return panel;
        }

        // Sort by act first, then by encounter count desc.
        var ranked = _data.BossStats.Values
            .OrderBy(b => BossActIndex(b.EncounterId))
            .ThenByDescending(b => b.Encounters)
            .ToList();

        var dropdown = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Text = L.Get("career.boss_select"),
        };
        dropdown.AddThemeFontSizeOverride("font_size", LabelSize);
        for (int i = 0; i < ranked.Count; i++)
        {
            var b = ranked[i];
            int actIdx = BossActIndex(b.EncounterId);
            var actLabel = actIdx == 0 ? "" : Util.NameLookup.ActLabel(actIdx) + "  ";
            var name = Util.NameLookup.Encounter(b.EncounterId);
            var label = $"{actLabel}{name}  ({b.Encounters} {L.Get("career.encounters")})";
            dropdown.AddItem(label, i);

            var icon = Util.AncientPoolMap.GetEncounterIcon(b.EncounterId);
            if (icon != null)
            {
                try { dropdown.SetItemIcon(i, icon); } catch { }
            }
        }
        v.AddChild(dropdown);

        var detail = new VBoxContainer();
        detail.AddThemeConstantOverride("separation", 2);
        v.AddChild(detail);

        Action<int> showBoss = idx =>
        {
            for (int i = detail.GetChildCount() - 1; i >= 0; i--)
                detail.GetChild(i).QueueFree();
            if (idx < 0 || idx >= ranked.Count) return;
            var b = ranked[idx];

            var titleRow = new HBoxContainer();
            titleRow.AddThemeConstantOverride("separation", 8);
            var bossIcon = Util.AncientPoolMap.GetEncounterIcon(b.EncounterId);
            if (bossIcon != null)
            {
                titleRow.AddChild(new TextureRect
                {
                    Texture = bossIcon,
                    CustomMinimumSize = new Vector2(36, 36),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                });
            }
            int actIdx = BossActIndex(b.EncounterId);
            if (actIdx > 0)
                titleRow.AddChild(MakeLabel(Util.NameLookup.ActLabel(actIdx), Gold, LabelSize + 1));
            var nameLbl = MakeLabel(Util.NameLookup.Encounter(b.EncounterId), Cream, LabelSize + 2);
            nameLbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            titleRow.AddChild(nameLbl);
            detail.AddChild(titleRow);

            AddDetailRow(detail, L.Get("career.avg_damage"), $"{b.AverageDamageTaken:F1}", Gold);
            var dColor = b.DeathRate >= 0.15f ? Red : (b.DeathRate >= 0.05f ? Cream : Green);
            AddDetailRow(detail, L.Get("career.death_rate"), $"{b.DeathRate * 100:F1}%", dColor);
            AddDetailRow(detail, L.Get("career.encounters"), $"{b.Encounters}", Gray);
        };

        dropdown.ItemSelected += i => showBoss((int)i);
        dropdown.Selected = 0;
        showBoss(0);

        return panel;
    }

    private void AddDetailRow(VBoxContainer parent, string label, string value, Color valueColor)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(MakeLabel(label, Cream, LabelSize));
        var valLbl = MakeLabel(value, valueColor, LabelSize);
        valLbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        valLbl.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(valLbl);
        parent.AddChild(row);
    }

    /// <summary>
    /// Map a Boss encounter id to its act index using the game's ActModel
    /// hierarchy. Falls back to 0 (unknown) if the model can't be found.
    /// </summary>
    private static int BossActIndex(string encounterId)
    {
        try
        {
            var acts = MegaCrit.Sts2.Core.Models.ModelDb.Acts.ToList();
            for (int i = 0; i < acts.Count; i++)
            {
                foreach (var enc in acts[i].AllEncounters)
                {
                    if (enc.Id.Entry == encounterId) return i + 1;
                }
            }
        }
        catch { }
        return 0;
    }

    // ── UI helpers ──────────────────────────────────────────

    private static Label MakeLabel(string text, Color color, int size)
    {
        var l = new Label();
        l.Text = text;
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private static PanelContainer WrapInPanel()
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = SectionBg,
            BorderColor = Border,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        panel.AddThemeStyleboxOverride("panel", style);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return panel;
    }
}
