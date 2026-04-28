# RAG — Search

Per-lesson chunk retrieval at generation time. The Python crews call the embedder + the store directly (not the HTTP `/api/rag/search` endpoint — that endpoint is for ad-hoc external use; internal callers go through the Python helpers).

> **Source files**: [routes/rag.py:rag_search_endpoint](../../lessons-ai-api/routes/rag.py) (HTTP entry), [tools/rag_embedder.py:embed_query](../../lessons-ai-api/tools/rag_embedder.py), [tools/rag_store.py:search](../../lessons-ai-api/tools/rag_store.py), [tools/document_context.py:format_chunks_for_lesson](../../lessons-ai-api/tools/document_context.py), [crews/content_crew.py](../../lessons-ai-api/crews/content_crew.py), [crews/exercise_crew.py:_fetch_document_context](../../lessons-ai-api/crews/exercise_crew.py), [crews/curriculum_crew.py](../../lessons-ai-api/crews/curriculum_crew.py).

## Two consumer paths

### Plan-time (curriculum crew, before any lesson exists)

```mermaid
sequenceDiagram
  autonumber
  participant Crew as run_curriculum_crew
  participant Store as rag_store
  participant Format as format_outline_for_plan
  participant PG as Postgres
  participant Template as lesson_plan_*.jinja2

  Note over Crew: plan.document_id is set
  Crew->>Store: list_chunks(document_id, limit=200)
  Store->>PG: SELECT ChunkIndex, HeaderPath, Text<br/>FROM DocumentChunks<br/>WHERE DocumentId=$1<br/>ORDER BY ChunkIndex<br/>LIMIT 200
  PG-->>Store: chunks (no embeddings)
  Store-->>Crew: list[dict]
  Crew->>Format: format_outline_for_plan(chunks)
  Format-->>Crew: document_context (markdown outline)
  Crew->>Template: render with document_context
```

The curriculum agent doesn't need the *content* of every chunk — it just needs the document's *structure* to design lessons that follow it. `format_outline_for_plan` extracts unique `header_path`s and renders a tree-like outline plus a one-line preview per top-level heading.

### Lesson-time (content + exercise crews, per-lesson)

```mermaid
sequenceDiagram
  autonumber
  participant Crew as content/exercise crew
  participant Embed as embed_query
  participant Store as rag_store
  participant Format as format_chunks_for_lesson
  participant Gem as Gemini text-embedding-004
  participant PG as Postgres pgvector

  Note over Crew: plan.document_id is set + api_key available
  Crew->>Crew: query_text = lesson.topic + lesson.name + lesson.description
  alt query_text is empty
    Crew-->>Crew: document_context = ""
  else
    Crew->>Embed: embed_query(query_text, api_key)
    Embed->>Gem: embed (RETRIEVAL_QUERY task type)
    Gem-->>Embed: vector(768)
    Embed-->>Crew: query_vec
    Crew->>Store: search(document_id, query_vec, top_k=5)
    Store->>PG: SELECT ChunkIndex, HeaderPath, Text,<br/>1 - (Embedding <=> $vec) AS score<br/>FROM DocumentChunks<br/>WHERE DocumentId=$1<br/>ORDER BY Embedding <=> $vec<br/>LIMIT 5
    PG-->>Store: top-5 hits
    Store-->>Crew: list[dict]
    Crew->>Format: format_chunks_for_lesson(hits)
    Format-->>Crew: document_context (markdown block)
  end
```

The HNSW index on `Embedding` makes this query sub-millisecond even on 100k+ chunks.

## Public HTTP endpoint

[routes/rag.py:rag_search_endpoint](../../lessons-ai-api/routes/rag.py) exposes the same logic via HTTP for non-CrewAI callers (e.g. the .NET service if it ever wanted ad-hoc retrieval, or a debugging/admin UI):

```mermaid
sequenceDiagram
  participant Caller
  participant Route as routes/rag.py
  participant Embed
  participant Store
  participant PG

  Caller->>Route: POST /api/rag/search { documentId, query, topK, googleApiKey }
  Route->>Embed: embed_query(query, googleApiKey)
  Embed-->>Route: query_vec
  Route->>Store: search(documentId, query_vec, top_k)
  Store->>PG: cosine similarity
  PG-->>Store: hits
  Store-->>Route: list[dict]
  Route-->>Caller: { documentId, hits: [{chunk_index, header_path, text, score}] }
```

The CrewAI internal path bypasses this endpoint — going direct from `embed_query` → `rag_store.search` is one fewer hop and avoids serialization.

## Cosine similarity scoring

```mermaid
flowchart LR
  v1[query vector]
  v2[chunk vector]
  cos["cos similarity = 1 - cosine_distance<br/>= 1 - (v1 <=> v2)"]
  range["[0, 2]:<br/>1.0 = identical direction<br/>0.0 = orthogonal<br/>-1.0 = opposite"]

  v1 --> cos
  v2 --> cos
  cos --> range
```

In practice, embeddings of related text from the same document score around 0.5–0.85. The top-5 are usually decent matches; more than 5 starts pulling in tangentially-related content.

## Why the query is `lesson.topic + lesson.name + lesson.description`

```mermaid
flowchart TD
  classDef good fill:#e8f5e9,color:#1a1a1a
  classDef bad fill:#ffe0e0,color:#1a1a1a

  q1["Just lesson.name<br/>'Lesson 5'"]:::bad
  q2["Just lesson.topic<br/>'Pipes'"]:::bad
  q3["Combined<br/>'Pipes Lesson 5: Decorators in Angular'"]:::good

  q1 --> r1["Vague — embeds toward generic 'lesson 5' content"]
  q2 --> r2["Ambiguous — could match Python's pipes module if doc has both"]
  q3 --> r3["Specific — embeds toward the topic in context"]
```

The combination biases the query vector toward chunks that match the lesson's *specific* angle. Chunks about generic "pipes" rank lower than chunks about "Angular pipes / decorators" when the query has both.

## Top-k tuning

```python
rag_top_k_per_lesson: int = 5  # in config.py
```

| `top_k` | Tradeoff |
|---|---|
| 3 | Sharp; only the most relevant chunks. May miss useful context. |
| 5 | Default; balanced. |
| 10 | Verbose; more context but more tokens. |
| 20+ | Token-heavy; LLM may lose focus. |

5 is the right default for most documents. Bump for very large books where any single passage is unlikely to be sufficient.

## Format output

[document_context.format_chunks_for_lesson](../../lessons-ai-api/tools/document_context.py) renders the hits as a single markdown block:

```markdown
## Source Document — Use as Primary Source of Information

### Chapter 1 > Section 2: Pipes
{chunk text}

### Chapter 3: Decorators
{chunk text}

...
```

This block is included in the writer's prompt via `templates/_document_context.jinja2`. The prompt's "use as primary source" instruction tells the LLM to cite the document's claims rather than its training data.

## Failure modes

- **Embedding API down** — `embed_query` raises; the calling crew catches and falls back to `document_context = ""` (the lesson generates without RAG grounding).
- **DB unavailable** — `rag_store.search` returns `[]`; same fallback as above.
- **No chunks for the document** — happens if ingestion produced 0 chunks (extractor failed). Returns `[]` quickly; lesson generates ungrounded.
- **Stale embeddings** — if the embedding model changes (`EMBEDDING_DIM` mismatch), pgvector rejects the query with a dimension error. Solution: re-ingest all documents.
