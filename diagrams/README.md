# LessonsHub — Architecture Diagrams

Mermaid-only architecture documentation for the LessonsHub project. Every diagram is a fenced ` ```mermaid ` block — diagrams render inline in GitHub, GitLab, VS Code, and most other markdown viewers.

## Index

### Cross-tier (start here)

- [01-cloud-architecture.md](01-cloud-architecture.md) — Cloud Run × 3 + Cloud SQL + GCS + external integrations.
- [02-infrastructure-terraform.md](02-infrastructure-terraform.md) — GCP resources Terraform provisions, plus the GitHub Actions WIF flow.
- [03-database.md](03-database.md) — ER diagrams for both Postgres databases (`LessonsHub` for the .NET app, `LessonsAi` for the Python service's RAG cache + chunks).

### Per-tier deep dives

- [backend/](backend/) — .NET 8 solution: Domain / Application / Infrastructure / API.
- [ai/](ai/) — Python FastAPI + CrewAI. Routes → services → crews → agents/tasks → tools.
- [frontend/](frontend/) — Angular 21 (standalone components + signals + SSR).

### RAG pipeline

- [rag/](rag/) — Document upload → chunk → embed → pgvector. Per-lesson chunk retrieval at generation time.
