# AI — 01 Architecture

A FastAPI process that exposes 8 HTTP endpoints. Each endpoint adapts the HTTP request → an internal service call → orchestrates one or more CrewAI crews → returns a Pydantic response.

> **Source files**: [main.py](../../lessons-ai-api/main.py), [routes/](../../lessons-ai-api/routes/), [services/](../../lessons-ai-api/services/), [crews/](../../lessons-ai-api/crews/), [agents/](../../lessons-ai-api/agents/), [tasks/](../../lessons-ai-api/tasks/), [tools/](../../lessons-ai-api/tools/), [models/](../../lessons-ai-api/models/), [templates/](../../lessons-ai-api/templates/), [factories/](../../lessons-ai-api/factories/), [config.py](../../lessons-ai-api/config.py).

## Layered diagram

```mermaid
flowchart TD
  classDef ext stroke-dasharray: 5 5,fill:#fff8e7,color:#1a1a1a
  classDef route fill:#e3f2fd,color:#1a1a1a
  classDef svc fill:#e8f5e9,color:#1a1a1a
  classDef crew fill:#bbdefb,color:#1a1a1a
  classDef agent fill:#fce4ec,color:#1a1a1a
  classDef tool fill:#fff3e0,color:#1a1a1a
  classDef data fill:#f3e5f5,color:#1a1a1a

  client[.NET API client]:::ext

  subgraph fastapi[FastAPI app]
    direction TB

    subgraph routes[routes/]
      lessons_r[lessons.py<br/>6 endpoints + _resolve_language]:::route
      rag_r[rag.py<br/>2 endpoints]:::route
      health[/health/]:::route
    end

    subgraph services[services/]
      cur[CurriculumService]:::svc
      con[ContentService]:::svc
      ex[ExerciseService]:::svc
      res[ResearchService]:::svc
    end

    subgraph crews[crews/]
      cur_c[run_curriculum_crew]:::crew
      con_c[run_content_crew]:::crew
      ex_c[run_exercise_crew]:::crew
      ex_r[run_exercise_retry_crew]:::crew
      rev_c[run_exercise_review_crew]:::crew
      res_c[run_resources_crew]:::crew
      qc[run_quality_check]:::crew
      fa["analyze_for_search_queries<br/>framework analysis"]:::crew
    end

    subgraph aa[agents/ + tasks/]
      ag[8 agent factories]:::agent
      tk[7 task factories]:::agent
    end

    subgraph tools_layer[tools/]
      dsearch[documentation_search]:::tool
      rag_chunker[rag_chunker]:::tool
      rag_emb[rag_embedder]:::tool
      rag_store_t[rag_store]:::tool
      doc_cache_t[doc_cache]:::tool
      doc_storage_t[doc_storage]:::tool
      doc_ctx[document_context]:::tool
      yt[youtube_search_tool]:::tool
    end
  end

  pg[(LessonsAi DB<br/>+ pgvector)]:::data
  gemini((Gemini API)):::ext
  ddg((DuckDuckGo)):::ext
  yta((YouTube Data API)):::ext

  client --> lessons_r
  client --> rag_r

  lessons_r --> cur
  lessons_r --> con
  lessons_r --> ex
  lessons_r --> res

  cur --> cur_c
  con --> con_c
  ex --> ex_c
  ex --> ex_r
  ex --> rev_c
  res --> res_c

  cur_c --> fa
  con_c --> fa
  cur_c --> qc
  con_c --> qc
  ex_c --> qc
  ex_r --> qc
  rev_c --> qc
  res_c --> qc

  cur_c --> ag
  con_c --> ag
  ex_c --> ag
  rev_c --> ag
  res_c --> ag
  fa --> ag
  qc --> ag

  ag --> tk
  cur_c --> dsearch
  con_c --> dsearch
  con_c --> rag_emb
  con_c --> rag_store_t
  ex_c --> rag_emb
  ex_c --> rag_store_t

  rag_r --> rag_chunker
  rag_r --> rag_emb
  rag_r --> rag_store_t
  rag_r --> doc_storage_t

  rag_emb --> gemini
  ag --> gemini
  dsearch --> ddg
  dsearch --> doc_cache_t
  rag_store_t --> pg
  doc_cache_t --> pg
  yt --> yta
```

**Reading order**: HTTP request lands at a route → route adapts to service-call shape (Pydantic → dataclass `PlanContext`/`LessonContext`/`ExerciseSpec`) → service constructs an LLM and calls a crew → crew (often) calls the framework analyzer first, then runs the writer agent inside a quality retry loop → returns response.

## Module responsibilities

| Module | Role | Notable files |
|---|---|---|
| `routes/` | FastAPI `APIRouter`s. Per-endpoint Pydantic→context conversion. | [routes/lessons.py](../../lessons-ai-api/routes/lessons.py), [routes/rag.py](../../lessons-ai-api/routes/rag.py) |
| `services/` | Thin static facades — pick an LLM (per-task model from `config.py`), forward to a crew. | [services/curriculum_service.py](../../lessons-ai-api/services/curriculum_service.py), [services/content_service.py](../../lessons-ai-api/services/content_service.py), [services/exercise_service.py](../../lessons-ai-api/services/exercise_service.py), [services/research_service.py](../../lessons-ai-api/services/research_service.py) |
| `crews/` | The actual orchestration. Build agent + task, run via CrewAI, wrap with quality retry. | [crews/](../../lessons-ai-api/crews/) (6 user-facing + `quality_crew` + `framework_analysis_crew`) |
| `agents/` | Agent factory functions. Most templates-based; quality-checker + framework-analyzer + youtube-researcher are Python-inline. | [agents/](../../lessons-ai-api/agents/), [agents/utils.py](../../lessons-ai-api/agents/utils.py) (template resolver) |
| `tasks/` | Task factory functions — build the `Task.description` from Jinja templates. | [tasks/](../../lessons-ai-api/tasks/) |
| `templates/` | Jinja2 prompt templates, one per (task, agent_type). Include the shared `_document_context.jinja2` partial for RAG-grounded chunks. | [templates/agents/](../../lessons-ai-api/templates/agents/), [templates/tasks/](../../lessons-ai-api/templates/tasks/) |
| `factories/` | `TemplateManager` (Jinja env + per-(role, type) template path resolution). | [factories/template_manager.py](../../lessons-ai-api/factories/template_manager.py) |
| `tools/` | Cross-cutting helpers: web search, doc cache, RAG chunk/embed/store, document storage abstraction, YouTube tool. | [tools/](../../lessons-ai-api/tools/) |
| `models/` | Pydantic request/response DTOs ([requests.py](../../lessons-ai-api/models/requests.py), [responses.py](../../lessons-ai-api/models/responses.py)) + internal dataclass contexts ([contexts.py](../../lessons-ai-api/models/contexts.py)). |
| `config.py` | Pydantic-Settings: model names + temperatures per task, doc-cache TTL, max-quality-retries, default language. |

## Startup (lifespan)

```mermaid
sequenceDiagram
  autonumber
  participant Uvicorn
  participant App as FastAPI app
  participant LF as lifespan
  participant DC as doc_cache.init_schema
  participant RS as rag_store.init_schema
  participant PG as Postgres

  Uvicorn->>App: start
  App->>LF: enter
  LF->>DC: init_schema()
  DC->>PG: CREATE TABLE IF NOT EXISTS DocumentationCache
  LF->>RS: init_schema()
  RS->>PG: CREATE EXTENSION IF NOT EXISTS vector
  RS->>PG: CREATE TABLE IF NOT EXISTS DocumentChunks
  RS->>PG: CREATE INDEX IF NOT EXISTS HNSW + DocumentId
  LF-->>App: ready
  App->>App: include_router(lessons_router)
  App->>App: include_router(rag_router)
  Uvicorn->>App: serving on :8000
```

Both schema bootstraps are idempotent (`IF NOT EXISTS`). If `DATABASE_URL` is unset, both log a warning and continue without the cache/RAG features (graceful degradation; the lesson endpoints still work, just without grounding).

## Per-task model selection

[config.py](../../lessons-ai-api/config.py) defines five `(model, temperature)` tuples — one per task type:

| Task | Default model | Default temp |
|---|---|---|
| Plan generation | `gemini/gemini-3.1-pro-preview` | 0.5 |
| Content generation | `gemini/gemini-3-flash-preview` | 0.5 |
| Exercise generation | `gemini/gemini-3-flash-preview` | 0.5 |
| Exercise review | `gemini/gemini-3-flash-preview` | 0.5 |
| Resource research | `gemini/gemini-3-flash-preview` | 0.5 |
| Quality checker | `gemini/gemini-3.1-flash-lite-preview` | 0.3 |

Plan generation uses Pro (better reasoning for course design); everything else uses Flash (cheaper, faster). Override per-deployment via env vars: `PLAN_MODEL`, `CONTENT_MODEL`, etc.

## Authentication (server-to-server)

The AI service is protected at the Cloud Run layer — only callers with `roles/run.invoker` on the AI service can reach it. The .NET service holds that role (bound by the deploy workflow); other GCP identities don't.

There's no per-user auth inside the AI service. The user's identity flows through the request body (`google_api_key` field for billing the right user; `correlation_id` for log correlation), not via JWTs. This is intentional — the AI service is an internal service.
