# Frontend — `lessonshub-ui` (Angular 21 SSR)

Standalone-component Angular app with signal-based state, server-side rendering, and Material Design components. The UI talks to the .NET API at `/api/*`; the AI service is invisible to the browser.

## Index

- [01-architecture.md](01-architecture.md) — Standalone components + signals + SSR + Material + interceptors/guards
- [02-routing.md](02-routing.md) — Routes, guards, lazy chunks
- [03-components.md](03-components.md) — 8 pages + 5 dialogs (component graph + per-component class diagrams)
- [04-services.md](04-services.md) — 8 services + LessonDataStore + endpoint mapping
- [05-models.md](05-models.md) — TypeScript interfaces (class diagram)
- [06-flows.md](06-flows.md) — Login, generate plan, edit lesson, share, schedule, upload doc

## Cross-references

- The .NET endpoints these services hit: [../backend/05-api-controllers.md](../backend/05-api-controllers.md)
- Cloud topology (UI runs as Node SSR): [../01-cloud-architecture.md](../01-cloud-architecture.md)
