# Flows

End-to-end sequence diagrams for the AI-orchestrated paths. Each flow file picks up at the .NET → Python boundary and ends at the response back to the UI.

- [lesson-plan.md](lesson-plan.md) — Plan generation. Variants by `lessonType` (Default / Technical / Language) covered in one file.
- [lesson-content.md](lesson-content.md) — Content generation. Same lessonType variants; framework grounding (Technical) and RAG grounding (when `documentId` is set) layer in.
- [exercise-generate.md](exercise-generate.md) — Per-user exercise creation.
- [exercise-retry.md](exercise-retry.md) — Retry with prior-review feedback.
- [exercise-review.md](exercise-review.md) — Submit answer → AI scores + reviews.
- [resources.md](resources.md) — YouTube + books + docs (two-agent crew).

## Cross-references

- The .NET handover: [../backend/06-flows.md](../backend/06-flows.md)
- The CrewAI orchestration: [../ai/03-services-and-crews.md](../ai/03-services-and-crews.md)
- RAG ingestion + search: [../rag/](../rag/)
