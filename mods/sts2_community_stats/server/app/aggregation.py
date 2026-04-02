"""SQL aggregation queries — builds BulkStatsBundle from raw data."""

import logging
from datetime import datetime, timezone
import asyncpg
from .models import (
    BulkStatsBundle, CardStats, RelicStats,
    EventStats, EventOptionStats, EncounterStats,
)

logger = logging.getLogger("sts2stats.agg")


async def compute_bulk_stats(
    pool: asyncpg.Pool,
    character: str,
    version: str,
    min_asc: int,
    max_asc: int,
) -> BulkStatsBundle:
    """Run all aggregation queries and assemble a complete bundle."""

    async with pool.acquire() as conn:
        total_runs = await _count_runs(conn, character, version, min_asc, max_asc)
        cards = await _aggregate_cards(conn, character, version, min_asc, max_asc)
        relics = await _aggregate_relics(conn, character, version, min_asc, max_asc)
        events = await _aggregate_events(conn, character, version, min_asc, max_asc)
        encounters = await _aggregate_encounters(conn, character, version, min_asc, max_asc)

    bundle = BulkStatsBundle(
        generated_at=datetime.now(timezone.utc).isoformat(),
        total_runs=total_runs,
        cards=cards,
        relics=relics,
        events=events,
        encounters=encounters,
    )

    logger.info(
        "Aggregated %s asc%d-%d ver=%s: %d runs, %d cards, %d relics, %d events, %d encounters",
        character, min_asc, max_asc, version,
        total_runs, len(cards), len(relics), len(events), len(encounters),
    )
    return bundle


# ── Internal query functions ────────────────────────────────

_WHERE = """
    WHERE character = $1
      AND game_version = $2
      AND ascension BETWEEN $3 AND $4
"""


async def _count_runs(
    conn: asyncpg.Connection, char: str, ver: str, lo: int, hi: int,
) -> int:
    row = await conn.fetchval(
        f"SELECT COUNT(*) FROM runs {_WHERE}", char, ver, lo, hi,
    )
    return row or 0


async def _aggregate_cards(
    conn: asyncpg.Connection, char: str, ver: str, lo: int, hi: int,
) -> dict[str, CardStats]:
    # ── Pick rate & win rate from card_choices ──
    rows = await conn.fetch(f"""
        SELECT
            card_id,
            AVG(was_picked::int)::real       AS pick_rate,
            AVG(CASE WHEN was_picked THEN win::int END)::real AS win_rate,
            COUNT(*)                          AS sample_size
        FROM card_choices
        {_WHERE}
        GROUP BY card_id
    """, char, ver, lo, hi)

    result: dict[str, CardStats] = {}
    for r in rows:
        result[r["card_id"]] = CardStats(
            pick=r["pick_rate"] or 0,
            win=r["win_rate"] or 0,
            n=r["sample_size"],
        )

    # ── Removal rates ──
    removal_rows = await conn.fetch(f"""
        SELECT card_id, COUNT(*) AS cnt
        FROM card_removals
        {_WHERE}
        GROUP BY card_id
    """, char, ver, lo, hi)
    total_runs = await _count_runs(conn, char, ver, lo, hi)
    if total_runs > 0:
        for r in removal_rows:
            cid = r["card_id"]
            if cid in result:
                result[cid].removal = r["cnt"] / total_runs

    # ── Upgrade rates ──
    upgrade_rows = await conn.fetch(f"""
        SELECT card_id, COUNT(*) AS cnt
        FROM card_upgrades
        {_WHERE}
        GROUP BY card_id
    """, char, ver, lo, hi)
    if total_runs > 0:
        for r in upgrade_rows:
            cid = r["card_id"]
            if cid in result:
                result[cid].upgrade = r["cnt"] / total_runs

    # ── Shop buy rates ──
    shop_rows = await conn.fetch(f"""
        SELECT item_id, COUNT(*) AS cnt
        FROM shop_purchases
        {_WHERE} AND item_type = 'card'
        GROUP BY item_id
    """, char, ver, lo, hi)
    if total_runs > 0:
        for r in shop_rows:
            cid = r["item_id"]
            if cid in result:
                result[cid].shop_buy = r["cnt"] / total_runs

    return result


async def _aggregate_relics(
    conn: asyncpg.Connection, char: str, ver: str, lo: int, hi: int,
) -> dict[str, RelicStats]:
    rows = await conn.fetch(f"""
        SELECT
            relic_id,
            AVG(win::int)::real  AS win_rate,
            COUNT(*)             AS sample_size
        FROM relic_records
        {_WHERE}
        GROUP BY relic_id
    """, char, ver, lo, hi)

    total_runs = await _count_runs(conn, char, ver, lo, hi)

    result: dict[str, RelicStats] = {}
    for r in rows:
        pick = r["sample_size"] / total_runs if total_runs > 0 else 0
        result[r["relic_id"]] = RelicStats(
            win=r["win_rate"] or 0,
            pick=pick,
            n=r["sample_size"],
        )

    # ── Shop buy rates for relics ──
    shop_rows = await conn.fetch(f"""
        SELECT item_id, COUNT(*) AS cnt
        FROM shop_purchases
        {_WHERE} AND item_type = 'relic'
        GROUP BY item_id
    """, char, ver, lo, hi)
    if total_runs > 0:
        for r in shop_rows:
            rid = r["item_id"]
            if rid in result:
                result[rid].shop_buy = r["cnt"] / total_runs

    return result


async def _aggregate_events(
    conn: asyncpg.Connection, char: str, ver: str, lo: int, hi: int,
) -> dict[str, EventStats]:
    rows = await conn.fetch(f"""
        SELECT
            event_id,
            option_index,
            COUNT(*)::real / SUM(COUNT(*)) OVER (PARTITION BY event_id) AS selection_rate,
            AVG(win::int)::real AS win_rate,
            COUNT(*)            AS sample_size
        FROM event_choices
        {_WHERE}
        GROUP BY event_id, option_index
        ORDER BY event_id, option_index
    """, char, ver, lo, hi)

    result: dict[str, EventStats] = {}
    for r in rows:
        eid = r["event_id"]
        if eid not in result:
            result[eid] = EventStats()
        result[eid].options.append(EventOptionStats(
            idx=r["option_index"],
            sel=r["selection_rate"] or 0,
            win=r["win_rate"] or 0,
            n=r["sample_size"],
        ))

    return result


async def _aggregate_encounters(
    conn: asyncpg.Connection, char: str, ver: str, lo: int, hi: int,
) -> dict[str, EncounterStats]:
    rows = await conn.fetch(f"""
        SELECT
            encounter_id,
            encounter_type,
            AVG(damage_taken)::real      AS avg_dmg,
            AVG(turns_taken)::real       AS avg_turns,
            AVG(player_died::int)::real  AS death_rate,
            COUNT(*)                     AS sample_size
        FROM encounter_records
        {_WHERE}
        GROUP BY encounter_id, encounter_type
    """, char, ver, lo, hi)

    result: dict[str, EncounterStats] = {}
    for r in rows:
        result[r["encounter_id"]] = EncounterStats(
            type=r["encounter_type"],
            avg_dmg=r["avg_dmg"] or 0,
            death=r["death_rate"] or 0,
            avg_turns=r["avg_turns"] or 0,
            n=r["sample_size"],
        )

    return result
