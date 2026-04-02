"""Application configuration — loaded from environment variables."""

import os
from dotenv import load_dotenv

load_dotenv()

# ── Database ────────────────────────────────────────────────
DATABASE_URL: str = os.getenv(
    "DATABASE_URL",
    "postgresql://sts2stats:changeme@localhost:5432/sts2stats",
)

# asyncpg wants a DSN without the +asyncpg suffix that SQLAlchemy uses
DATABASE_DSN: str = DATABASE_URL.replace("postgresql+asyncpg://", "postgresql://")

DB_MIN_POOL: int = int(os.getenv("DB_MIN_POOL", "2"))
DB_MAX_POOL: int = int(os.getenv("DB_MAX_POOL", "10"))

# ── Redis ───────────────────────────────────────────────────
REDIS_URL: str = os.getenv("REDIS_URL", "redis://localhost:6379/0")
CACHE_TTL_SECONDS: int = int(os.getenv("CACHE_TTL_SECONDS", "900"))  # 15 minutes

# ── Precompute ──────────────────────────────────────────────
PRECOMPUTE_INTERVAL_MINUTES: int = int(os.getenv("PRECOMPUTE_INTERVAL_MINUTES", "10"))

CHARACTERS: list[str] = [
    "IRONCLAD", "SILENT", "DEFECT", "WATCHER", "VAGABOND", "NECROMANCER",
]
ASC_RANGES: list[tuple[int, int]] = [
    (0, 4), (5, 9), (10, 14), (15, 19), (20, 20),
]

# ── Security ────────────────────────────────────────────────
BLOCKED_MOD_VERSIONS: set[str] = set(
    os.getenv("BLOCKED_MOD_VERSIONS", "").split(",")
) - {""}

RATE_LIMIT_UPLOAD: str = os.getenv("RATE_LIMIT_UPLOAD", "10/minute")
RATE_LIMIT_QUERY: str = os.getenv("RATE_LIMIT_QUERY", "60/minute")

# ── Server ──────────────────────────────────────────────────
HOST: str = os.getenv("HOST", "0.0.0.0")
PORT: int = int(os.getenv("PORT", "8080"))
WORKERS: int = int(os.getenv("WORKERS", "1"))
LOG_LEVEL: str = os.getenv("LOG_LEVEL", "info")
