"""Redis cache layer."""

import logging
import redis.asyncio as aioredis
from . import config

logger = logging.getLogger("sts2stats.cache")

_redis: aioredis.Redis | None = None


async def init_redis() -> aioredis.Redis:
    global _redis
    if _redis is None:
        logger.info("Connecting to Redis: %s", config.REDIS_URL)
        _redis = aioredis.from_url(config.REDIS_URL, decode_responses=True)
        await _redis.ping()
        logger.info("Redis connected")
    return _redis


async def close_redis() -> None:
    global _redis
    if _redis:
        await _redis.aclose()
        _redis = None


def get_redis() -> aioredis.Redis:
    assert _redis is not None, "Redis not initialized — call init_redis() first"
    return _redis


# ── Cache key helpers ───────────────────────────────────────

def bulk_key(character: str, asc_range: str, version: str) -> str:
    return f"bulk:{character}:{asc_range}:{version}"


def map_asc_range(min_asc: int, max_asc: int) -> str:
    """Map arbitrary ascension range to nearest precomputed bucket."""
    for lo, hi in config.ASC_RANGES:
        if lo <= min_asc and max_asc <= hi:
            return f"{lo}-{hi}"
    # fallback: pick the bucket whose midpoint is closest
    mid = (min_asc + max_asc) / 2
    best = config.ASC_RANGES[0]
    for lo, hi in config.ASC_RANGES:
        if abs((lo + hi) / 2 - mid) < abs((best[0] + best[1]) / 2 - mid):
            best = (lo, hi)
    return f"{best[0]}-{best[1]}"
