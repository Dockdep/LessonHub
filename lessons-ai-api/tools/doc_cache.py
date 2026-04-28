"""Postgres-backed cache for documentation lookups.

This service owns the `DocumentationCache` table — the schema is created on
startup via `init_schema()` and lives in this service's own database
(`LessonsAi`), not the .NET service's `LessonsHub` DB.

We use raw asyncpg rather than an ORM because the table is trivial and we
want zero schema-drift surprises with the SQL we actually run.

The cache is best-effort: any DB error is swallowed and the caller falls back to
a live search. We never want a missing/down DB to break lesson generation.
"""
from __future__ import annotations

import json
import logging
from datetime import datetime, timedelta, timezone
from typing import Any

import asyncpg

from config import settings

logger = logging.getLogger(__name__)


def _normalise_url(database_url: str) -> str:
    """asyncpg expects postgresql://; strip postgres:// or postgresql+asyncpg:// schemes."""
    if database_url.startswith("postgresql+asyncpg://"):
        return "postgresql://" + database_url[len("postgresql+asyncpg://"):]
    if database_url.startswith("postgres://"):
        return "postgresql://" + database_url[len("postgres://"):]
    return database_url


async def _connect() -> asyncpg.Connection | None:
    if not settings.database_url:
        return None
    try:
        return await asyncpg.connect(_normalise_url(settings.database_url))
    except Exception as e:
        logger.warning("doc_cache: cannot connect to Postgres (%s) — running cache-disabled", e)
        return None


async def get(query_key: str) -> Any:
    """Return cached value for a query, or None if missing / expired / unreachable.

    Cached values can be either a list (snippet results) or a dict (library
    metadata). The caller knows what shape to expect for each key prefix.
    """
    conn = await _connect()
    if conn is None:
        return None
    try:
        row = await conn.fetchrow(
            'SELECT "JsonResults", "ExpiresAt" FROM "DocumentationCache" WHERE "QueryKey" = $1',
            query_key,
        )
        if row is None:
            return None
        if row["ExpiresAt"] <= datetime.now(timezone.utc):
            return None
        return json.loads(row["JsonResults"])
    except Exception as e:
        logger.warning("doc_cache: read failed for %s: %s", query_key, e)
        return None
    finally:
        await conn.close()


async def put(query_key: str, value: Any) -> None:
    """Insert-or-update a cache row. Silent on failure. Value must be JSON-serialisable."""
    conn = await _connect()
    if conn is None:
        return
    try:
        now = datetime.now(timezone.utc)
        expires = now + timedelta(days=settings.doc_cache_ttl_days)
        await conn.execute(
            '''
            INSERT INTO "DocumentationCache" ("QueryKey", "JsonResults", "ExpiresAt", "RefreshedAt")
            VALUES ($1, $2, $3, $4)
            ON CONFLICT ("QueryKey")
            DO UPDATE SET
                "JsonResults" = EXCLUDED."JsonResults",
                "ExpiresAt"   = EXCLUDED."ExpiresAt",
                "RefreshedAt" = EXCLUDED."RefreshedAt";
            ''',
            query_key,
            json.dumps(value),
            expires,
            now,
        )
    except Exception as e:
        logger.warning("doc_cache: write failed for %s: %s", query_key, e)
    finally:
        await conn.close()


async def invalidate(query_key: str) -> None:
    """Remove a cache row so the next lookup re-fetches. Silent on failure."""
    conn = await _connect()
    if conn is None:
        return
    try:
        await conn.execute('DELETE FROM "DocumentationCache" WHERE "QueryKey" = $1', query_key)
    except Exception as e:
        logger.warning("doc_cache: invalidate failed for %s: %s", query_key, e)
    finally:
        await conn.close()


async def init_schema() -> None:
    """Create the DocumentationCache table if it doesn't exist.

    Called from the FastAPI lifespan hook on startup. Safe to call repeatedly —
    `IF NOT EXISTS` is a no-op when the table is already present.
    """
    conn = await _connect()
    if conn is None:
        logger.warning("doc_cache: skipping schema init (DATABASE_URL unset or DB unreachable)")
        return
    try:
        await conn.execute(
            '''
            CREATE TABLE IF NOT EXISTS "DocumentationCache" (
                "QueryKey"    varchar(500) PRIMARY KEY,
                "JsonResults" text         NOT NULL,
                "ExpiresAt"   timestamptz  NOT NULL,
                "RefreshedAt" timestamptz  NOT NULL
            );
            '''
        )
        logger.info("doc_cache: schema ready")
    except Exception as e:
        logger.warning("doc_cache: init_schema failed: %s", e)
    finally:
        await conn.close()
