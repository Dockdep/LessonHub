# AI — 01 Architecture

A FastAPI process that exposes 8 HTTP endpoints. Each adapts the HTTP request → an internal service call → orchestrates one or more CrewAI crews → returns a Pydantic response.

> **Source files**: [main.py](../../lessons-ai-api/main.py), [routes/](../../lessons-ai-api/routes/), [services/](../../lessons-ai-api/services/), [crews/](../../lessons-ai-api/crews/), [agents/](../../lessons-ai-api/agents/), [tasks/](../../lessons-ai-api/tasks/), [tools/](../../lessons-ai-api/tools/), [models/](../../lessons-ai-api/models/), [templates/](../../lessons-ai-api/templates/), [config.py](../../lessons-ai-api/config.py).

## Layered diagram

```mermaid
flowchart TD
  classDef ext stroke-dasharray: 5 5,fill:#fff8e7,color:#1a1a1a
  classDef route fill:#e3f2fd,color:#1a1a1a
  classDef svc fill:#e8f5e9,color:#1a1a1a
  classDef crew fill:#bbdefb,color:#1a1a1a
  classDef tool fill:#fff3e0,color:#1a1a1a
  classDef data fill:#f3e5f5,color:#1a1a1a

  client[.NET API client]:::ext

  subgraph fastapi[FastAPI app]
    subgraph routes[routes/]
      lessons_r[lessons.py]:::route
      rag_r[rag.py]:::route
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
      ex_c[exercise crews]:::crew
      res_c[run_resources_crew]:::crew
      qc[run_quality_check]:::crew
      fa[framework_analysis_crew]:::crew
    end

    subgraph tools_layer[tools/]
      dsearch[documentation_search]:::tool
      rag_pipe[rag_chunker / embedder / store]:::tool
      doc_cache_t[doc_cache]:::tool
      doc_storage_t[doc_storage]:::tool
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
  res --> res_c
  cur_c --> fa
  con_c --> fa
  cur_c --> qc
  con_c --> qc
  ex_c --> qc
  res_c --> qc
  cur_c --> dsearch
  con_c --> dsearch
  con_c --> rag_pipe
  rag_r --> rag_pipe
  rag_r --> doc_storage_t
  dsearch --> ddg
  dsearch --> doc_cache_t
  rag_pipe --> gemini
  rag_pipe --> pg
  doc_cache_t --> pg
  yt --> yta
```

**Reading order**: HTTP request → route adapts to context dataclass (`PlanContext`/`LessonContext`/`ExerciseSpec`) → service constructs an LLM and calls a crew → crew (often) calls the framework analyzer first, then runs the writer agent inside a quality retry loop → returns response.

## Module responsibilities

| Module | Role |
|---|---|
| `routes/` | FastAPI `APIRouter`s; per-endpoint Pydantic→context conversion |
| `services/` | Thin static facades — pick an LLM (per-task model from `config.py`), forward to a crew |
| `crews/` | The actual orchestration. Build agent + task, run via CrewAI, wrap with quality retry |
| `agents/` | Agent factory functions; most templates-based, a few Python-inline (quality, framework analyzer, youtube researcher) |
| `tasks/` | Task factory functions — build the `Task.description` from Jinja templates |
| `templates/` | Jinja2 prompt templates, one per (task, agent_type). Includes the shared `_document_context.jinja2` partial for RAG-grounded chunks |
| `tools/` | Cross-cutting helpers: web search, doc cache, RAG chunk/embed/store, document storage, YouTube tool |
| `models/` | Pydantic request/response DTOs + internal dataclass contexts |
| `config.py` | Pydantic-Settings: model names + temperatures per task, doc-cache TTL, max-quality-retries |

## Per-task model selection

`config.py` defines `(model, temperature)` per task type. Plan generation uses Gemini Pro (better reasoning for course design); content / exercise / review / resources use Flash (cheaper, faster); the quality checker uses Flash-Lite. Override per-deployment via env vars (`PLAN_MODEL`, `CONTENT_MODEL`, …).

## Authentication

The AI service is protected at the Cloud Run layer — only callers with `roles/run.invoker` on the AI service can reach it. The .NET service holds that role; other GCP identities don't. There's no per-user auth inside the AI service. The user's identity flows through the request body (`google_api_key` for billing the right user, `correlation_id` for log correlation), not via JWTs. This is intentional — the AI service is internal.

## Startup

The `lifespan` hook calls `init_schema()` for both `DocumentationCache` and `DocumentChunks` (idempotent `IF NOT EXISTS`). If `DATABASE_URL` is unset, both log a warning and continue without the cache/RAG features (graceful degradation; lesson endpoints still work, just without grounding).
