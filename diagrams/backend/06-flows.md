# Backend — 06 Flows

End-to-end sequences for the .NET API. AI-orchestrated flows (lesson plan / content / exercise) live in [../flows/](../flows/). This file covers .NET-only paths plus the orchestration boundary.

## Auth: Google One-Tap → JWT

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular UI
  participant API as AuthController
  participant Svc as AuthService
  participant GTV as GoogleTokenValidator
  participant Goog as Google OAuth
  participant Repo as IUserRepository
  participant TS as TokenService

  User->>UI: click Sign in with Google
  UI->>Goog: One-Tap flow
  Goog-->>UI: id_token (JWT)
  UI->>API: POST /api/auth/google { idToken }
  API->>Svc: LoginWithGoogleAsync(req)
  Svc->>GTV: ValidateAsync(idToken)
  GTV->>Goog: GET certs (cached)
  GTV-->>Svc: GoogleTokenPayload
  Svc->>Repo: GetByGoogleIdAsync(sub)
  alt new user
    Svc->>Repo: Add(new User) + SaveChangesAsync
  end
  Svc->>TS: CreateToken(user)
  TS-->>Svc: JWT
  Svc-->>API: ServiceResult<LoginResponseDto>
  API-->>UI: 200 { token, user }
```

Subsequent authenticated requests carry `Authorization: Bearer <jwt>`; the JWT bearer middleware validates, populates `HttpContext.User`, and `ICurrentUser` reads `Id` from there. SignalR uses `?access_token=<jwt>` on the WS upgrade URL since browsers can't set headers on WS handshakes — `JwtBearerEvents.OnMessageReceived` extracts it only on `/hubs/*` paths.

## Plan delete with day cleanup

```mermaid
sequenceDiagram
  autonumber
  participant API as LessonPlanController
  participant Svc as LessonPlanService
  participant LPR as ILessonPlanRepository
  participant LDR as ILessonDayRepository

  API->>Svc: DeleteAsync(planId)
  Svc->>LPR: GetOwnedWithLessonsAsync(planId, userId)
  alt not owner
    Svc-->>API: NotFound
  else found
    Note over Svc: Collect distinct affected day IDs from plan.Lessons
    Svc->>LPR: Remove(plan) + SaveChangesAsync (cascades)
    Svc->>LDR: GetEmptyAmongAsync(affectedDayIds)
    Svc->>LDR: RemoveRange(emptyDays) + SaveChangesAsync
    Svc-->>API: Ok
  end
```

`Lesson → LessonDay` cascade is `SetNull` (two users may share a day), so empty `LessonDay` rows are cleaned up explicitly after the cascade.

## Sharing flow

```mermaid
sequenceDiagram
  autonumber
  participant API as LessonPlanShareController
  participant Svc as LessonPlanShareService
  participant LPR as ILessonPlanRepository
  participant UR as IUserRepository
  participant SR as ILessonPlanShareRepository

  API->>Svc: AddShareAsync(planId, email)
  Svc->>LPR: IsOwnerAsync(planId, ownerId)
  alt not owner
    Svc-->>API: NotFound
  else owner
    Svc->>UR: GetByEmailAsync(email)
    alt unknown
      Svc-->>API: NotFound("no user")
    else target == owner
      Svc-->>API: BadRequest
    else
      Svc->>SR: ExistsAsync(planId, target.Id)
      alt already shared
        Svc-->>API: Conflict
      else
        Svc->>SR: Add + SaveChangesAsync
        Svc-->>API: Ok(LessonPlanShareDto)
      end
    end
  end
```

## Document upload + ingest

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant DC as DocumentsController
  participant DS as DocumentService
  participant DR as IDocumentRepository
  participant Storage as IDocumentStorage
  participant Job as JobBackgroundService
  participant RAG as IRagApiClient
  participant AI as Python AI

  User->>DC: POST /api/documents/upload (multipart)
  DC->>DS: UploadAsync(input)
  DS->>DR: Add(Document {Status=Pending}) + SaveChangesAsync
  DS->>Storage: SaveAsync → gs://...
  DS->>DR: doc.StorageUri = gs://...; SaveChangesAsync
  DS-->>DC: Ok(DocumentDto)
  DC-->>User: 202 { document, jobId }

  Note over Job: async — DocumentIngest job
  Job->>RAG: IngestAsync(docId, storageUri, apiKey)
  RAG->>AI: POST /api/rag/ingest
  AI-->>RAG: { chunkCount }
  Job->>DR: doc.IngestionStatus="Ingested"; ChunkCount=N
```

If RAG ingestion fails, the executor sets `IngestionStatus = "Failed"` + truncates the error to `IngestionError`. The document row stays so the user can see what went wrong and re-upload.

## AI hand-off boundary

The deeper details of plan/content/exercise generation live in [../flows/](../flows/). On the .NET side, the executor calls the existing service method, which calls `LessonsAiApiClient` through the resilience pipeline:

```mermaid
sequenceDiagram
  autonumber
  participant Svc as LessonPlanService
  participant Client as LessonsAiApiClient
  participant Res as Resilience handler
  participant Iam as IamAuthHandler
  participant AI as Python AI

  Svc->>Client: GenerateLessonPlanAsync(req)
  Client->>Res: HTTP request
  Res->>Iam: attempt 1
  Iam->>Iam: mint Google ID token (audience = AI URL)
  Iam->>AI: POST + Bearer
  alt happy path
    AI-->>Res: 200 OK
  else transient (5xx, 408, 429, network)
    AI-->>Res: error → backoff 2s
    Res->>Iam: attempt 2 (fresh token)
    Iam->>AI: POST + Bearer
    AI-->>Res: 200 or final error
  else circuit open
    Res--xClient: BrokenCircuitException
  end
  Res-->>Client: result
  Client-->>Svc: AiLessonPlanResponse or null
```

Resilience wraps *outside* `IamAuthHandler` so each retry mints a fresh Google ID token — important when the original is mid-expiry. See [04-infrastructure.md](04-infrastructure.md) for the policy table.
