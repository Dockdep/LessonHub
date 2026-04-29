# Backend — `lessonshub` (.NET 8 API)

The .NET API that the Angular UI talks to. Owns the `LessonsHub` Postgres database. Repository + facade architecture: HTTP-thin controllers → service facades returning `ServiceResult<T>` → aggregate-specific repositories → EF Core `DbContext`. SignalR + a `BackgroundService` drive the AI-generation job pipeline.

## Index

- [01-architecture.md](01-architecture.md) — 4-project solution layout + DI + auth wiring
- [02-domain-model.md](02-domain-model.md) — Entities, relationships, invariants
- [03-application-layer.md](03-application-layer.md) — `I*Service` interfaces, `ServiceResult<T>`, mappers
- [04-infrastructure.md](04-infrastructure.md) — `LessonsHubDbContext`, repos, AI clients, resilience pipeline, SignalR + job pipeline
- [05-api-controllers.md](05-api-controllers.md) — Controllers + endpoints + sync vs async (job) patterns
- [06-flows.md](06-flows.md) — Auth, plan delete, sharing, document upload, AI hand-off

## Cross-references

- Cloud architecture: [../01-cloud-architecture.md](../01-cloud-architecture.md)
- Database schema: [../03-database.md](../03-database.md)
- AI service the API calls into: [../ai/01-architecture.md](../ai/01-architecture.md)
