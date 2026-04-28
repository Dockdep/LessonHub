"""Gemini embedding client for RAG.

Uses `gemini-embedding-001` via the user's own API key (same per-user-billing
model as everything else — the user pays for embedding their own books).

Two task types matter for retrieval quality:
  - RETRIEVAL_DOCUMENT: when embedding chunks at ingest time
  - RETRIEVAL_QUERY:    when embedding the user's search query

Mixing them will work but degrades recall. Always pass the right one.
"""
from __future__ import annotations

from typing import Iterable

from google import genai
from google.genai import types

# Default dim baked into the schema (`vector(768)`). Don't change without a
# pgvector column-type migration.
EMBEDDING_DIM = 768
EMBEDDING_MODEL = "gemini-embedding-001"

# BatchEmbedContents caps each request at 100 inputs. Long books chunk into
# many more than that, so we slice the input into pages and call sequentially.
MAX_BATCH_SIZE = 100


async def embed_documents(texts: list[str], api_key: str) -> list[list[float]]:
    """Embed a batch of chunks for ingestion. Returns one vector per text."""
    return await _embed_batch(texts, api_key, task_type="RETRIEVAL_DOCUMENT")


async def embed_query(text: str, api_key: str) -> list[float]:
    """Embed a single search query. Returns one vector."""
    vectors = await _embed_batch([text], api_key, task_type="RETRIEVAL_QUERY")
    return vectors[0]


async def _embed_batch(
    texts: list[str],
    api_key: str,
    task_type: str,
) -> list[list[float]]:
    if not texts:
        return []
    if not api_key:
        raise ValueError("Gemini API key is required for embedding")

    client = genai.Client(api_key=api_key)
    config = types.EmbedContentConfig(
        task_type=task_type,
        output_dimensionality=EMBEDDING_DIM,
    )

    vectors: list[list[float]] = []
    for start in range(0, len(texts), MAX_BATCH_SIZE):
        page = texts[start:start + MAX_BATCH_SIZE]
        response = await client.aio.models.embed_content(
            model=EMBEDDING_MODEL,
            contents=page,
            config=config,
        )
        vectors.extend(list(e.values) for e in response.embeddings)
    return vectors
