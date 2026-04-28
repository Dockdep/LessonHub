# Backend — 01 Architecture

The .NET 8 solution is split into four projects following Clean Architecture conventions.

> **Source files**: [LessonsHub.sln](../../LessonsHub.sln), [LessonsHub/Program.cs](../../LessonsHub/Program.cs), [LessonsHub/Extensions/DependencyInjection.cs](../../LessonsHub/Extensions/DependencyInjection.cs).

## Solution layout

```mermaid
flowchart TD
  classDef proj fill:#e3f2fd,color:#1a1a1a
  classDef test fill:#fce4ec,color:#1a1a1a

  api[LessonsHub<br/>ASP.NET Core API host]:::proj
  app[LessonsHub.Application<br/>Service facades + DTOs + abstractions]:::proj
  inf[LessonsHub.Infrastructure<br/>EF Core, repos, external clients]:::proj
  dom[LessonsHub.Domain<br/>Entities only — no deps]:::proj
  tst[LessonsHub.Tests<br/>xUnit + Moq + SQLite in-memory]:::test

  api --> app
  api --> inf
  app --> dom
  inf --> app
  tst --> api
  tst --> app
  tst --> inf
  tst --> dom
```

**The dependency rule**: Domain has zero project deps. Application depends only on Domain. Infrastructure depends on Application (so it can implement its interfaces). API (the host) depends on both Application and Infrastructure, wiring them together at startup.

## Per-project responsibilities

| Project | Purpose | Notable files |
|---|---|---|
| `LessonsHub.Domain` | Pure entity classes — no behaviour, no external deps. EF treats them as POCOs. | All under [Entities/](../../LessonsHub.Domain/Entities/) |
| `LessonsHub.Application` | Abstractions (`IRepository`, `I*Service`, `ICurrentUser`), service implementations (the facades), DTOs, mappers, `ServiceResult<T>`. The framework-agnostic core of the app. | [Abstractions/](../../LessonsHub.Application/Abstractions/), [Services/](../../LessonsHub.Application/Services/), [Models/](../../LessonsHub.Application/Models/) |
| `LessonsHub.Infrastructure` | EF Core `DbContext`, repository implementations, external clients (Google ID-token validator, AI HTTP client, document storage), JWT issuer, EF migrations. | [Data/](../../LessonsHub.Infrastructure/Data/), [Repositories/](../../LessonsHub.Infrastructure/Repositories/), [Services/](../../LessonsHub.Infrastructure/Services/), [Auth/](../../LessonsHub.Infrastructure/Auth/), [Migrations/](../../LessonsHub.Infrastructure/Migrations/) |
| `LessonsHub` | ASP.NET host — controllers, DI registration, JWT bearer auth, CORS, Swagger. Composition root. | [Controllers/](../../LessonsHub/Controllers/), [Program.cs](../../LessonsHub/Program.cs), [Extensions/](../../LessonsHub/Extensions/) |
| `LessonsHub.Tests` | Integration tests against SQLite-in-memory DbContext. Tests construct the full controller→service→repo stack via [TestStack.cs](../../LessonsHub.Tests/TestSupport/TestStack.cs). | [Controllers/](../../LessonsHub.Tests/Controllers/), [TestSupport/](../../LessonsHub.Tests/TestSupport/) |

## DI registration

Composition happens in [Program.cs](../../LessonsHub/Program.cs) via three extension methods on `IServiceCollection` defined in [Extensions/DependencyInjection.cs](../../LessonsHub/Extensions/DependencyInjection.cs):

```mermaid
flowchart LR
  classDef boot fill:#fff3e0,color:#1a1a1a
  classDef ext fill:#e8f5e9,color:#1a1a1a

  prog[Program.cs]:::boot

  cu[AddCurrentUser]:::ext
  rp[AddRepositories]:::ext
  ap[AddApplicationServices]:::ext

  hca[IHttpContextAccessor]
  icu[ICurrentUser → CurrentUser]

  ur[IUserRepository → UserRepository]
  lpr[ILessonPlanRepository → LessonPlanRepository]
  lr[ILessonRepository → LessonRepository]
  ldr[ILessonDayRepository → LessonDayRepository]
  lsr[ILessonPlanShareRepository → LessonPlanShareRepository]
  dr[IDocumentRepository → DocumentRepository]
  er[IExerciseRepository → ExerciseRepository]
  ear[IExerciseAnswerRepository → ExerciseAnswerRepository]

  as[IAuthService → AuthService]
  ups[IUserProfileService → UserProfileService]
  lpss[ILessonPlanShareService → LessonPlanShareService]
  lds[ILessonDayService → LessonDayService]
  ds[IDocumentService → DocumentService]
  lps[ILessonPlanService → LessonPlanService]
  ls[ILessonService → LessonService]
  es[IExerciseService → ExerciseService]

  prog --> cu --> hca
  cu --> icu
  prog --> rp
  rp --> ur
  rp --> lpr
  rp --> lr
  rp --> ldr
  rp --> lsr
  rp --> dr
  rp --> er
  rp --> ear
  prog --> ap
  ap --> as
  ap --> ups
  ap --> lpss
  ap --> lds
  ap --> ds
  ap --> lps
  ap --> ls
  ap --> es
```

All registrations are `Scoped` — same lifetime as `LessonsHubDbContext`, which means all repos within a request share one `DbContext` (and therefore one EF Core change-tracker / unit of work).

## Startup sequence

```mermaid
sequenceDiagram
  autonumber
  participant Host as Kestrel host
  participant Prog as Program.cs
  participant DI as Service collection
  participant DB as LessonsHubDbContext

  Host->>Prog: Main()
  Prog->>DI: AddDbContext<LessonsHubDbContext>(Npgsql)
  Prog->>DI: AddCurrentUser()
  Prog->>DI: AddRepositories()
  Prog->>DI: AddApplicationServices()
  Prog->>DI: AddHttpClient<ILessonsAiApiClient>(IamAuthHandler + StandardResilienceHandler)
  Prog->>DI: AddHttpClient<IRagApiClient>(IamAuthHandler + StandardResilienceHandler)
  Prog->>DI: AddSingleton<JwtSettings, GoogleAuthSettings, LessonsAiApiSettings, DocumentStorageSettings>
  Prog->>DI: AddScoped<ITokenService, IGoogleTokenValidator, IUserApiKeyProvider, IAiCostLogger, IDocumentStorage>
  Prog->>DI: AddAuthentication(JwtBearer)
  Prog->>DI: AddCors("AllowAngular")
  Prog->>DI: AddSwaggerGen + Bearer security
  Prog->>Host: builder.Build()
  Prog->>DB: db.Database.Migrate() [retry × 10 with 3s sleep]
  Prog->>Host: app.UseRouting / UseCors / UseAuthentication / UseAuthorization / MapControllers
  Host->>Host: Listen
```

The `db.Database.Migrate()` retry loop handles the case where Cloud SQL is briefly unreachable on cold start.

## Authentication wiring

```mermaid
flowchart LR
  classDef ext stroke-dasharray: 5 5,fill:#fff8e7,color:#1a1a1a
  classDef internal fill:#e3f2fd,color:#1a1a1a

  google((Google OAuth)):::ext
  ui[Angular UI]:::internal
  jwtb[JWT Bearer middleware]:::internal
  authc[AuthController]:::internal
  ts["ITokenService<br/>TokenService"]:::internal
  gtv["IGoogleTokenValidator<br/>GoogleTokenValidator"]:::internal
  cu["ICurrentUser<br/>CurrentUser"]:::internal

  ui -->|id_token| google
  ui -->|POST /api/auth/google + id_token| authc
  authc --> gtv -->|validate| google
  authc --> ts -->|sign + return JWT| ui
  ui -->|Bearer JWT in subsequent requests| jwtb
  jwtb --> cu
```

- `JwtBearerDefaults.AuthenticationScheme` is the default scheme.
- `JwtSettings` (issuer, audience, secret, expiration) is a singleton bound from `JwtSettings:*` config.
- `ICurrentUser` reads the `NameIdentifier` claim from `IHttpContextAccessor.HttpContext.User`, throws `InvalidOperationException` if absent (every facade method assumes auth is required; `[Authorize]` on the controller enforces it before the facade runs).
