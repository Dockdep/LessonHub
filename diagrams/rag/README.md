# RAG (Retrieval-Augmented Generation)

Document-grounded lesson generation. Users upload books, articles, or notes; the AI service chunks + embeds them into pgvector; lesson generation pulls the most relevant chunks into the writer's prompt.

## Index

- [ingest.md](ingest.md) — Upload → chunk → embed → store. Triggered when a user uploads a document.
- [search.md](search.md) — Lesson topic → query embed → cosine search → format chunks. Triggered during lesson plan/content/exercise generation when `documentId` is set.

## Cross-references

- The `Document` entity (.NET): [../backend/02-domain-model.md](../backend/02-domain-model.md)
- The `DocumentChunks` table + `LessonsAi` DB: [../03-database.md](../03-database.md)
- The Python tools: [../ai/05-tools.md](../ai/05-tools.md) (`rag_chunker`, `rag_embedder`, `rag_store`, `document_context`)
- The .NET `DocumentsController` flow: [../backend/06-flows.md](../backend/06-flows.md#document-upload--ingest-trigger)
- Where chunks land in lesson generation: [../flows/lesson-content-technical.md](../flows/lesson-content-technical.md), [../flows/exercise-generate.md](../flows/exercise-generate.md)
