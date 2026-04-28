# AI — 05 Tools

Cross-cutting helpers under [lessons-ai-api/tools/](../../lessons-ai-api/tools/). Some are CrewAI `tools` (LLM-callable), most are plain Python helpers used by crews/services directly.

## Module map

```mermaid
flowchart LR
  classDef tool fill:#fff3e0,color:#1a1a1a
  classDef ext stroke-dasharray: 5 5,fill:#fff8e7,color:#1a1a1a
  classDef data fill:#f3e5f5,color:#1a1a1a

  ds[documentation_search.py]:::tool
  rc[rag_chunker.py]:::tool
  re[rag_embedder.py]:::tool
  rs[rag_store.py]:::tool
  rx[rag_extractors.py]:::tool
  dc[doc_cache.py]:::tool
  dst[doc_storage.py]:::tool
  dctx[document_context.py]:::tool
  yt[youtube_search_tool.py]:::tool

  ddg((DuckDuckGo)):::ext
  gem((Gemini Embeddings<br/>text-embedding-004)):::ext
  yta((YouTube Data API v3)):::ext
  pg[(Postgres LessonsAi)]:::data
  fs[(GCS / local FS)]:::data

  ds --> ddg
  ds --> dc
  re --> gem
  rs --> pg
  rs --> rc
  rs --> re
  dc --> pg
  dst --> fs
  rx --> dst
  yt --> yta
  dctx --> rs
```

## `documentation_search` ([tools/documentation_search.py](../../lessons-ai-api/tools/documentation_search.py))

Web search + page fetch for grounding Technical lessons. The framework-analyzer agent decides *what* to search for; this module just runs the queries it produces.

```mermaid
flowchart LR
  classDef in fill:#e3f2fd,color:#1a1a1a
  classDef step fill:#fff3e0,color:#1a1a1a
  classDef out fill:#e8f5e9,color:#1a1a1a

  in_q[queries: list~str~]:::in
  bypass[bypass_cache flag]:::in
  cache[doc_cache.get<br/>per-query key]:::step
  ddg[_ddg_search<br/>concurrent]:::step
  fetch[_fetch_page_content<br/>trafilatura]:::step
  cap[cap to doc_page_max_chars]:::step
  store[doc_cache.put]:::step
  out_r[list~dict~ url, title, content_excerpt]:::out

  in_q --> cache
  bypass --> cache
  cache -- HIT --> out_r
  cache -- MISS --> ddg --> fetch --> cap --> store --> out_r
```

| Function | Purpose |
|---|---|
| `search_for_queries(queries, bypass_cache)` | Public entry. Strips blanks, runs each query concurrently, flattens results. |
| `_run_one_query(query, bypass_cache)` | Per-query: cache lookup → DDG → page-fetch → cache write. Failures yield `[]`, never raise. |
| `_ddg_search(query)` | Wraps the sync `ddgs.text()` library in a thread. One retry with 2s backoff on rate-limit / `202` / timeout. |
| `_fetch_page_content(url)` | `httpx` GET (8s timeout, follow redirects) → `trafilatura.extract` (favors recall) → cap to `doc_page_max_chars`. |
| `format_docs_for_prompt(results)` | Renders the `## Reference Documentation (use these, not your training data)` block injected into the writer's prompt and the validator's prompt. |
| `format_sources_section(results)` | Renders the trailing `## Sources` markdown list appended to lesson content. Dedupes by URL. |

## RAG pipeline tools

### `rag_chunker.py` ([tools/rag_chunker.py](../../lessons-ai-api/tools/rag_chunker.py))

`chunk_text(source_text, *, is_markdown, chunk_words, overlap_words) -> list[Chunk]`. Splits raw text into ~800-word chunks with ~100-word overlap. When `is_markdown=True`, splits on headings first so each chunk gets a `header_path` like `"Chapter 1 > Section 2"`.

```mermaid
flowchart TD
  src[Raw source text]
  md{is_markdown<br/>and has headings?}
  hsplit[_split_markdown_by_headings]
  flat["Single section, header_path empty"]
  win[_window_split<br/>~800 words, ~100 overlap]
  chunks[list~Chunk~]

  src --> md
  md -- yes --> hsplit
  md -- no --> flat
  hsplit --> win
  flat --> win
  win --> chunks
```

### `rag_embedder.py` ([tools/rag_embedder.py](../../lessons-ai-api/tools/rag_embedder.py))

Wraps Gemini `text-embedding-004`. Two task-typed entrypoints:

- `embed_documents(texts, api_key)` — `task_type="RETRIEVAL_DOCUMENT"`. Used at ingest.
- `embed_query(text, api_key)` — `task_type="RETRIEVAL_QUERY"`. Returns one vector. Used at search time.

Internally uses `_embed_batch(texts, api_key, task_type)` with `MAX_BATCH_SIZE=100` per Gemini call. `EMBEDDING_DIM=768`.

### `rag_store.py` ([tools/rag_store.py](../../lessons-ai-api/tools/rag_store.py))

Postgres + pgvector storage.

| Function | Purpose |
|---|---|
| `init_schema()` | `CREATE EXTENSION vector` + `CREATE TABLE DocumentChunks` + HNSW + B-tree indexes. Idempotent. |
| `upsert_chunks(document_id, chunks, embeddings)` | Replace strategy: `DELETE ... WHERE DocumentId=$1` + `INSERT` new rows. Re-ingestion cleanly removes stale chunks. |
| `search(document_id, query_embedding, top_k=5)` | Cosine-similarity top-k, scoped to a single document. Returns `[{chunk_index, header_path, text, score}]`. |
| `list_chunks(document_id, limit=200)` | Full chunk list ordered by index. Used by curriculum crew when generating Document-grounded plans (it gets the *outline*, not embeddings). |
| `delete_document(document_id)` | Removes all chunks for a document. Returns deleted row count. |

The `vector_cosine_ops` HNSW index is what makes per-document top-k search fast even with hundreds of thousands of chunks.

### `rag_extractors.py` ([tools/rag_extractors.py](../../lessons-ai-api/tools/rag_extractors.py))

Format-aware text extractors used at ingest. Picks the right extractor based on file extension:

- `.md`, `.markdown`, `.txt` — pass-through
- `.pdf` — `pypdf` extractor
- `.docx` — `python-docx` (preserves Heading 1/2/3 → `#`, `##`, `###`)
- `.epub`, `.mobi`, `.azw`, `.azw3` — `ebooklib` (one synthesized `# Title` heading per chapter)

The extracted text becomes the `source_text` input to `chunk_text`. `is_markdown` is `True` for the formats that produce heading-structured output (so the chunker respects them).

### `document_context.py` ([tools/document_context.py](../../lessons-ai-api/tools/document_context.py))

Two formatters that render RAG-fetched chunks into prompt-ready blocks:

- `format_chunks_for_lesson(hits)` — used by content/exercise crews. Wraps each chunk in `### {header_path}` headings + the chunk text. Capped per `settings.rag_top_k_per_lesson` (default 5).
- `format_outline_for_plan(chunks)` — used by curriculum crew when a `document_id` is set. Renders just the unique `header_path`s as a tree, plus a one-line preview per top-level heading. Gives the LLM the *structure* of the source book without flooding the context.

Both are called from inside the Jinja2 `_document_context.jinja2` partial that every lesson template includes.

## `doc_cache.py` ([tools/doc_cache.py](../../lessons-ai-api/tools/doc_cache.py))

Generic key-value cache backed by the `LessonsAi.DocumentationCache` table. JSON-serialized values, TTL'd via `ExpiresAt`.

| Function | Purpose |
|---|---|
| `init_schema()` | `CREATE TABLE IF NOT EXISTS DocumentationCache`. Idempotent. |
| `get(query_key)` | Returns the cached value if present and not expired; `None` otherwise. |
| `put(query_key, value)` | Insert-or-update with `ExpiresAt = now + doc_cache_ttl_days`. |
| `invalidate(query_key)` | Manually nuke a row. |

Failure mode: any DB error logs a warning and returns `None` (read) or silently drops (write). Lesson generation continues without the cache benefit; we never want a missing/down DB to break lessons.

## `doc_storage.py` ([tools/doc_storage.py](../../lessons-ai-api/tools/doc_storage.py))

Storage-URI abstraction — given a `gs://bucket/path` or `file:///path` URI, returns the file's text.

```mermaid
flowchart LR
  uri[document_uri str]
  parse{starts with<br/>gs://?}
  gcs[Google.Cloud.Storage<br/>blob.download_as_bytes]
  local[Path.read_bytes]
  ext{file extension}
  raw[utf-8 decode]
  pdf[rag_extractors.extract_pdf]
  docx[rag_extractors.extract_docx]
  ep[rag_extractors.extract_epub]
  txt[Source text]

  uri --> parse
  parse -- gs:// --> gcs
  parse -- file:// --> local
  gcs --> ext
  local --> ext
  ext -- .md/.txt --> raw
  ext -- .pdf --> pdf
  ext -- .docx --> docx
  ext -- .epub/.mobi/.azw* --> ep
  raw --> txt
  pdf --> txt
  docx --> txt
  ep --> txt
```

`read_document(uri)` is the single public entry. It hides the `gs://` vs `file://` choice from the caller — same code path works in Cloud Run and docker-compose.

## `youtube_search_tool.py` ([tools/youtube_search_tool.py](../../lessons-ai-api/tools/youtube_search_tool.py))

A CrewAI-tagged `@tool` that the youtube_researcher agent can invoke. Wraps the YouTube Data API v3 `search.list` endpoint, returns a list of `{title, channel, description, url}` for the LLM to choose from.

The agent decides *which* videos to recommend; this tool just gives it the search hits.
