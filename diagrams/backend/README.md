# Backend — `lessonshub` (.NET 8 API)

The .NET API that the Angular UI talks to. Owns the `LessonsHub` Postgres database. Uses a repository + facade architecture: HTTP-thin controllers → service facades that return `ServiceResult<T>` → aggregate-specific repositories → EF Core `DbContext`.

## Index

- [01-architecture.md](01-architecture.md) — 4-project solution layout + DI graph + startup
- [02-domain-model.md](02-domain-model.md) — Entities, relationships, constraints
- [03-application-layer.md](03-application-layer.md) — `I*Service` interfaces, `ServiceResult<T>`, `ICurrentUser`, mappers
- [04-infrastructure.md](04-infrastructure.md) — `LessonsHubDbContext`, `RepositoryBase`, repos, external clients
- [05-api-controllers.md](05-api-controllers.md) — All 7 controllers and their endpoints
- [06-flows.md](06-flows.md) — Auth, plan CRUD, lesson edit, sharing, document upload sequences

## Cross-references

- Cloud architecture: [../01-cloud-architecture.md](../01-cloud-architecture.md)
- Database schema: [../03-database.md](../03-database.md)
- AI service the API calls into: [../ai/01-architecture.md](../ai/01-architecture.md)
- End-to-end lesson flows: [../flows/](../flows/)
