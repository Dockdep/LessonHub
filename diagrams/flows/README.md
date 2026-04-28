# Flows

End-to-end sequence diagrams for the AI-orchestrated paths. Each flow file picks up at the .NET → Python boundary and ends at the response back to the UI.

## Lesson plan generation (3 lesson types)

- [lesson-plan-default.md](lesson-plan-default.md) — Default. No grounding, no language-toggle plumbing.
- [lesson-plan-technical.md](lesson-plan-technical.md) — Technical. Framework analyzer → DDG search → quality retry loop.
- [lesson-plan-language.md](lesson-plan-language.md) — Language. `useNativeLanguage` toggle changes the rendering language; `nativeLanguage` + `languageToLearn` are passed to all task templates.

## Lesson content generation (3 lesson types)

- [lesson-content-default.md](lesson-content-default.md) — Lazy generation on first GET; basic prompt (no grounding).
- [lesson-content-technical.md](lesson-content-technical.md) — Per-lesson framework analyzer; per-lesson RAG chunks if `documentId` set.
- [lesson-content-language.md](lesson-content-language.md) — Branching template per `useNativeLanguage`; immersive vs. native modes.

## Exercise lifecycle

- [exercise-generate.md](exercise-generate.md) — Per-user exercise creation.
- [exercise-retry.md](exercise-retry.md) — Retry with prior-review feedback.
- [exercise-review.md](exercise-review.md) — Submit answer → AI scores + reviews.

## Resources research

- [resources.md](resources.md) — YouTube + books + docs (two-agent crew).

## Cross-references

- The .NET handover: [../backend/06-flows.md](../backend/06-flows.md)
- The CrewAI orchestration shape: [../ai/03-services-and-crews.md](../ai/03-services-and-crews.md)
- The agents: [../ai/04-agents.md](../ai/04-agents.md)
- RAG ingestion + search (the consumer side of the document grounding): [../rag/](../rag/)
