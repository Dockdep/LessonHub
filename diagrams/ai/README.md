# AI Service — `lessons-ai-api` (Python FastAPI + CrewAI)

The Python service that does all LLM work. Uses [CrewAI](https://github.com/crewAIInc/crewAI) for multi-agent orchestration and [pgvector](https://github.com/pgvector/pgvector) for RAG.

## Index

- [01-architecture.md](01-architecture.md) — FastAPI layering: routes → services → crews → agents/tasks → tools
- [02-endpoints.md](02-endpoints.md) — All 8 endpoints + Pydantic request/response models
- [03-services-and-crews.md](03-services-and-crews.md) — 4 services + 6 crews + the analyzer + quality retry loop
- [04-agents.md](04-agents.md) — 8 agent personas + role/goal/backstory pattern
- [05-tools.md](05-tools.md) — `documentation_search`, `rag_*`, `doc_cache`, `doc_storage`, `document_context`, `youtube_search_tool`

## Cross-references

- The .NET service that calls in: [../backend/04-infrastructure.md](../backend/04-infrastructure.md) (`LessonsAiApiClient`, `RagApiClient`, `IamAuthHandler`)
- The Postgres `LessonsAi` database: [../03-database.md](../03-database.md)
- RAG pipeline: [../rag/](../rag/)
