"""Postgres + pgvector storage for the RAG pipeline.

Owns three things:
  - schema bootstrap (CREATE EXTENSION + CREATE TABLE on startup)
  - chunk upserts at ingest time
  - top-k cosine similarity search at query time

`document_id` is provided by the .NET service (its `Documents` row PK). We
treat it as opaque — we don't validate it, and we don't store the file
content here (the file lives in GCS or local storage; we only store its
embeddings and a back-reference).
"""
from __future__ import annotations

import logging
from typing import Iterable

import asyncpg
from pgvector.asyncpg import register_vector

from config import settings
from tools.rag_chunker import Chunk
from tools.rag_embedder import EMBEDDING_DIM

logger = logging.getLogger(__name__)


def _normalise_url(database_url: str) -> str:
    """asyncpg expects postgresql://; strip postgres:// or +asyncpg schemes."""
    if database_url.startswith("postgresql+asyncpg://"):
        return "postgresql://" + database_url[len("postgresql+asyncpg://"):]
    if database_url.startswith("postgres://"):
        return "postgresql://" + database_url[len("postgres://"):]
    return database_url


async def _connect_raw() -> asyncpg.Connection | None:
    """Plain connection, no vector-type registration.

    Used by init_schema (which has to run BEFORE the vector type exists,
    since CREATE EXTENSION is what creates that type) and as a fallback
    when register_vector trips on a fresh DB.
    """
    if not settings.database_url:
        return None
    try:
        return await asyncpg.connect(_normalise_url(settings.database_url))
    except Exception as e:
        logger.warning("rag_store: cannot connect to Postgres (%s)", e)
        return None


async def _connect() -> asyncpg.Connection | None:
    """Connection with the pgvector type adapter wired up. Use for queries."""
    conn = await _connect_raw()
    if conn is None:
        return None
    try:
        await register_vector(conn)
        return conn
    except Exception as e:
        await conn.close()
        logger.warning(
            "rag_store: vector type not registered yet (%s) — has init_schema run?",
            e,
        )
        return None


async def init_schema() -> None:
    """Create extension + tables if missing. Called from FastAPI lifespan.

    Idempotent — safe to call repeatedly.
    """
    conn = await _connect_raw()
    if conn is None:
        logger.warning("rag_store: skipping schema init (DATABASE_URL unset or DB unreachable)")
        return
    try:
        await conn.execute("CREATE EXTENSION IF NOT EXISTS vector;")
        # Now that the type exists in this DB, the next-connection call to
        # register_vector() will succeed.
        await register_vector(conn)
        await conn.execute(
            f'''
            CREATE TABLE IF NOT EXISTS "DocumentChunks" (
                "Id"          bigserial    PRIMARY KEY,
                "DocumentId"  varchar(64)  NOT NULL,
                "ChunkIndex"  integer      NOT NULL,
                "HeaderPath"  text         NOT NULL DEFAULT '',
                "Text"        text         NOT NULL,
                "Embedding"   vector({EMBEDDING_DIM}) NOT NULL,
                "CreatedAt"   timestamptz  NOT NULL DEFAULT now(),
                UNIQUE ("DocumentId", "ChunkIndex")
            );
            '''
        )
        # Filter-by-document index — most queries scope to one document.
        await conn.execute(
            'CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_DocumentId" '
            'ON "DocumentChunks" ("DocumentId");'
        )
        # HNSW vector index for fast cosine similarity.
        await conn.execute(
            'CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_Embedding_HNSW" '
            'ON "DocumentChunks" USING hnsw ("Embedding" vector_cosine_ops);'
        )
        logger.info("rag_store: schema ready")
    except Exception as e:
        logger.error("rag_store: init_schema failed: %s", e)
        raise
    finally:
        await conn.close()


async def upsert_chunks(
    document_id: str,
    chunks: list[Chunk],
    embeddings: list[list[float]],
) -> int:
    """Replace all chunks for a document with the new ones. Returns row count.

    Replace (DELETE + INSERT) rather than UPSERT so re-ingesting a document
    cleanly removes any old chunks that no longer exist (e.g. user shortened
    the source).
    """
    if len(chunks) != len(embeddings):
        raise ValueError(
            f"chunks ({len(chunks)}) and embeddings ({len(embeddings)}) length mismatch"
        )

    conn = await _connect()
    if conn is None:
        raise RuntimeError("rag_store: DB unavailable, cannot ingest")

    try:
        async with conn.transaction():
            await conn.execute(
                'DELETE FROM "DocumentChunks" WHERE "DocumentId" = $1', document_id
            )
            if not chunks:
                return 0
            rows = [
                (document_id, c.chunk_index, c.header_path, c.text, e)
                for c, e in zip(chunks, embeddings)
            ]
            await conn.executemany(
                '''
                INSERT INTO "DocumentChunks"
                    ("DocumentId", "ChunkIndex", "HeaderPath", "Text", "Embedding")
                VALUES ($1, $2, $3, $4, $5);
                ''',
                rows,
            )
            return len(rows)
    finally:
        await conn.close()


async def search(
    document_id: str,
    query_embedding: list[float],
    top_k: int = 5,
) -> list[dict]:
    """Cosine-similarity search scoped to one document.

    Returns list of {chunk_index, header_path, text, score} where score is the
    cosine similarity (higher = more similar; 1.0 = identical direction).
    """
    conn = await _connect()
    if conn is None:
        return []

    try:
        # `<=>` is pgvector's cosine distance; similarity = 1 - distance.
        rows = await conn.fetch(
            '''
            SELECT
                "ChunkIndex",
                "HeaderPath",
                "Text",
                1 - ("Embedding" <=> $2) AS score
            FROM "DocumentChunks"
            WHERE "DocumentId" = $1
            ORDER BY "Embedding" <=> $2
            LIMIT $3;
            ''',
            document_id,
            query_embedding,
            top_k,
        )
        return [
            {
                "chunk_index": r["ChunkIndex"],
                "header_path": r["HeaderPath"],
                "text": r["Text"],
                "score": float(r["score"]),
            }
            for r in rows
        ]
    finally:
        await conn.close()


async def list_chunks(document_id: str, limit: int = 200) -> list[dict]:
    """All chunks of one document, ordered by their position in the source.

    Used by the Document curriculum agent at PLAN time — the LLM gets the full
    structure of the book (headers + previews) and decides how to slice it
    into lessons. We don't return embeddings (huge) and we cap text to keep
    the prompt under token limits.
    """
    conn = await _connect_raw()
    if conn is None:
        return []
    try:
        rows = await conn.fetch(
            '''
            SELECT "ChunkIndex", "HeaderPath", "Text"
            FROM "DocumentChunks"
            WHERE "DocumentId" = $1
            ORDER BY "ChunkIndex"
            LIMIT $2;
            ''',
            document_id,
            limit,
        )
        return [
            {
                "chunk_index": r["ChunkIndex"],
                "header_path": r["HeaderPath"],
                "text": r["Text"],
            }
            for r in rows
        ]
    finally:
        await conn.close()


async def delete_document(document_id: str) -> int:
    """Remove all chunks for a document. Returns deleted row count."""
    conn = await _connect()
    if conn is None:
        return 0
    try:
        result = await conn.execute(
            'DELETE FROM "DocumentChunks" WHERE "DocumentId" = $1', document_id
        )
        # asyncpg returns "DELETE n" — parse the count.
        try:
            return int(result.split()[-1])
        except (ValueError, IndexError):
            return 0
    finally:
        await conn.close()
