# RAG (Retrieval-Augmented Generation)

Document-grounded lesson generation. Users upload books, articles, or notes; the AI service chunks + embeds them into pgvector; lesson generation pulls the most relevant chunks into the writer's prompt.

- [ingest.md](ingest.md) — Upload → chunk → embed → store. Triggered when a user uploads a document.
- [search.md](search.md) — Lesson topic → query embed → cosine search → format chunks. Triggered during lesson generation when `documentId` is set.

## Cross-references

- `Document` entity: [../backend/02-domain-model.md](../backend/02-domain-model.md)
- `DocumentChunks` table + `LessonsAi` DB: [../03-database.md](../03-database.md)
- Python tools: [../ai/05-tools.md](../ai/05-tools.md)
- AI-side orchestration that consumes chunks: [../ai/03-services-and-crews.md](../ai/03-services-and-crews.md)
