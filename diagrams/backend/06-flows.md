# Backend — 06 Flows

End-to-end sequences for the .NET API. AI-orchestrated flows (lesson plan / content / exercise generation) live in [../flows/](../flows/) — those involve the Python service. This file covers .NET-only paths plus the orchestration *boundary* (where .NET calls into Python).

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
  participant DB as Postgres

  User->>UI: click "Sign in with Google"
  UI->>Goog: One-Tap flow
  Goog-->>UI: id_token (JWT)
  UI->>API: POST /api/auth/google { idToken }
  API->>Svc: LoginWithGoogleAsync(req)
  Svc->>GTV: ValidateAsync(idToken)
  GTV->>Goog: GET certs (cached)
  GTV-->>Svc: GoogleTokenPayload(sub, email, name, picture)
  Svc->>Repo: GetByGoogleIdAsync(sub)
  Repo->>DB: SELECT
  DB-->>Repo: User? (null if first-time)
  alt new user
    Svc->>Repo: Add(new User { ... })
    Svc->>Repo: SaveChangesAsync()
    Repo->>DB: INSERT
  end
  Svc->>TS: CreateToken(user)
  TS-->>Svc: JWT
  Svc-->>API: ServiceResult<LoginResponseDto>(token, user)
  API-->>UI: 200 { token, user }
  UI->>UI: localStorage.setItem("token")
```

Subsequent authenticated requests carry `Authorization: Bearer <jwt>`; the JWT bearer middleware validates the signature + claims, populates `HttpContext.User`, and `ICurrentUser` reads `Id` from there.

## Plan list (owned + shared)

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular UI
  participant LDC as LessonDayController
  participant LDS as LessonDayService
  participant CU as ICurrentUser
  participant LPR as ILessonPlanRepository
  participant DB as Postgres

  User->>UI: navigate /lesson-plans
  UI->>LDC: GET /api/lessonday/plans
  LDC->>LDS: GetUserPlansAsync()
  LDS->>CU: Id (claims)
  CU-->>LDS: 42
  LDS->>LPR: GetOwnedWithLessonCountAsync(42)
  LPR->>DB: SELECT * FROM LessonPlans WHERE UserId=42 + Lessons
  DB-->>LPR: rows
  LPR-->>LDS: List<LessonPlan>
  LDS-->>LDC: ServiceResult<List<LessonPlanSummaryDto>>
  LDC-->>UI: 200 [...]
  Note over UI: Then UI also calls /api/lessonplan/shared-with-me<br/>via LessonPlanController
```

## Plan delete (owner-only) with day cleanup

```mermaid
sequenceDiagram
  autonumber
  participant API as LessonPlanController
  participant Svc as LessonPlanService
  participant LPR as ILessonPlanRepository
  participant LDR as ILessonDayRepository
  participant DB as Postgres

  API->>Svc: DeleteAsync(planId)
  Svc->>LPR: GetOwnedWithLessonsAsync(planId, userId)
  LPR-->>Svc: LessonPlan? (with Lessons)
  alt not owner
    Svc-->>API: NotFound
  else found
    Note over Svc: Collect distinct affected day IDs<br/>from plan.Lessons
    Svc->>LPR: Remove(plan)
    Svc->>LPR: SaveChangesAsync<br/>(cascades: lessons, exercises, ...)
    LPR->>DB: DELETE
    Note over Svc: After cascade, some LessonDay rows<br/>may have zero remaining Lessons
    Svc->>LDR: GetEmptyAmongAsync(affectedDayIds)
    LDR-->>Svc: List<LessonDay> (those that ended up empty)
    Svc->>LDR: RemoveRange(emptyDays)
    Svc->>LDR: SaveChangesAsync
    LDR->>DB: DELETE
    Svc-->>API: Ok("Lesson plan deleted")
  end
```

## Plan update (owner-only, lesson reconciliation)

```mermaid
sequenceDiagram
  autonumber
  participant API as LessonPlanController
  participant Svc as LessonPlanService
  participant LPR as ILessonPlanRepository
  participant LR as ILessonRepository

  API->>Svc: UpdateAsync(planId, req)
  Svc->>LPR: GetOwnedWithLessonsAsync(planId, userId)
  LPR-->>Svc: LessonPlan?
  alt not owner
    Svc-->>API: NotFound
  else found
    Note over Svc: Apply plan-level fields<br/>(Name, Topic, Description, Languages...)
    Note over Svc: Compute incoming lesson IDs
    Svc->>LR: RemoveRange(plan.Lessons NOT IN incomingIds)
    loop each incoming lesson DTO
      alt has Id (existing)
        Note over Svc: Mutate existing in-place
      else no Id (new)
        Svc->>LR: Add(new Lesson{...})
      end
    end
    Svc->>LPR: SaveChangesAsync
    Note over Svc: Reload to project the post-save state
    Svc->>LPR: GetOwnedWithLessonsAsync(planId, userId)
    Svc-->>API: Ok(LessonPlanDetailDto)
  end
```

## Sharing flow

```mermaid
sequenceDiagram
  autonumber
  actor Owner
  participant API as LessonPlanShareController
  participant Svc as LessonPlanShareService
  participant LPR as ILessonPlanRepository
  participant UR as IUserRepository
  participant SR as ILessonPlanShareRepository

  Owner->>API: POST /api/lessonplan/{id}/shares { email }
  API->>Svc: AddShareAsync(planId, email)
  Svc->>LPR: IsOwnerAsync(planId, ownerId)
  alt not owner
    Svc-->>API: NotFound("Lesson plan not found.")
  else owner
    Svc->>UR: GetByEmailAsync(email)
    alt unknown email
      Svc-->>API: NotFound("No user found...")
    else known
      alt target == owner
        Svc-->>API: BadRequest("You already own...")
      else
        Svc->>SR: ExistsAsync(planId, target.Id)
        alt already shared
          Svc-->>API: Conflict("Already shared...")
        else
          Svc->>SR: Add(new LessonPlanShare)
          Svc->>SR: SaveChangesAsync
          Svc-->>API: Ok(LessonPlanShareDto)
        end
      end
    end
  end
```

## Document upload + ingest trigger

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular UI
  participant DC as DocumentsController
  participant DS as DocumentService
  participant DR as IDocumentRepository
  participant Storage as IDocumentStorage
  participant UKP as IUserApiKeyProvider
  participant RAG as IRagApiClient
  participant AI as Python AI Service

  User->>UI: upload file
  UI->>DC: POST /api/documents/upload (multipart)
  DC->>DS: UploadAsync(input)
  Note over DS: Validate size ≤ 32 MB
  DS->>DR: Add(new Document { Status = "Pending", StorageUri="" })
  DS->>DR: SaveChangesAsync (gets Id)
  DS->>Storage: SaveAsync(userId, docId, name, stream, contentType)
  Storage-->>DS: storageUri (gs://... or file://...)
  DS->>DR: Update doc.StorageUri
  DS->>DR: SaveChangesAsync
  DS->>UKP: GetCurrentUserKeyAsync()
  UKP-->>DS: user's Gemini API key
  DS->>RAG: IngestAsync(docId, storageUri, isMarkdown, apiKey)
  RAG->>AI: POST /api/rag/ingest
  AI-->>RAG: { chunkCount }
  RAG-->>DS: ingest response
  DS->>DR: doc.IngestionStatus = "Ingested", doc.ChunkCount = N
  DS->>DR: SaveChangesAsync
  DS-->>DC: Ok(DocumentDto)
  DC-->>UI: 200 { document with status }
```

If RAG ingestion fails, the catch block sets `IngestionStatus = "Failed"` + truncates the error to `IngestionError`. The document row stays so the user can see what went wrong (and re-upload).

## Lesson generation hand-off (boundary)

The deeper details of plan/content/exercise generation are in [../flows/](../flows/), but here's the .NET-side hand-off:

```mermaid
sequenceDiagram
  autonumber
  participant API as LessonPlanController
  participant LPS as LessonPlanService
  participant Client as LessonsAiApiClient
  participant Res as Resilience handler
  participant Iam as IamAuthHandler
  participant AI as Python AI Service

  API->>LPS: GenerateAsync(req)
  LPS->>LPS: Build AiLessonPlanRequest from DTO<br/>(LessonType, Topic, Languages, DocumentId, ...)
  LPS->>Client: GenerateLessonPlanAsync(aiRequest)
  Client->>Res: HTTP request enters pipeline
  Res->>Iam: attempt 1
  alt prod (Cloud Run)
    Iam->>Iam: Mint Google ID token<br/>audience = AI service URL
    Iam->>AI: POST /api/lesson-plan/generate<br/>Authorization: Bearer
  else local-dev
    Iam->>AI: POST /api/lesson-plan/generate<br/>no auth header
  end

  alt happy path
    AI-->>Iam: 200 OK
    Iam-->>Res: response
    Res-->>Client: response
  else transient failure (5xx, 408, 429, network)
    AI-->>Iam: error
    Iam-->>Res: error
    Note over Res: classified as transient<br/>backoff 2s + jitter
    Res->>Iam: attempt 2 (retry, fresh token)
    Iam->>AI: POST again
    AI-->>Iam: 200 OK or final error
    Iam-->>Res: result
    Res-->>Client: result
  else circuit open
    Note over Res: BrokenCircuitException — no HTTP attempt
    Res-->>Client: throw
  end

  Client-->>LPS: response (or null on failure)
  LPS->>LPS: Map to LessonPlanResponseDto
  LPS-->>API: ServiceResult.Ok(...) or .Internal/.Timeout
```

The resilience handler wraps **outside** `IamAuthHandler` so each retry mints a fresh Google ID token — important when the original is mid-expiry. See [04-infrastructure.md#resilience-pipeline-microsoftextensionshttpresilience--polly-v8](04-infrastructure.md#resilience-pipeline-microsoftextensionshttpresilience--polly-v8) for the policy table and tuning rationale.

Lazy lesson-content generation works the same way but is triggered by `LessonController.GetLesson` when `lesson.Content` is empty — the user pays the latency on their first read of each lesson. Subsequent reads return the saved markdown directly.
