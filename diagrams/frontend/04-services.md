# Frontend — 04 Services

10 services + 1 in-memory state store ([LessonDataStore](../../lessonshub-ui/src/app/services/lesson-data.store.ts)). 8 are HTTP-facing, 1 ([RealtimeService](../../lessonshub-ui/src/app/services/realtime.service.ts)) owns the SignalR connection, and 1 ([JobsService](../../lessonshub-ui/src/app/services/jobs.service.ts)) is the central client for the `/api/jobs/*` surface — every async-generation endpoint goes through it.

> **Source files**: [lessonshub-ui/src/app/services/](../../lessonshub-ui/src/app/services/).

## SignalR job pipeline (`RealtimeService` + `JobsService`)

All AI-generation calls are async. The browser POSTs, gets `202 + jobId`, subscribes to the hub, then renders progress + final result as `JobEvent`s arrive. `JobsService.postAndStream` collapses that whole sequence — POST + subscribe + filter on terminal status + throw on failure — into one call so each AI-facing service method stays at ~3 lines.

```mermaid
sequenceDiagram
  participant Comp as Component
  participant Svc as TS service<br/>(LessonPlanService etc.)
  participant Jobs as JobsService
  participant API as .NET API
  participant RT as RealtimeService
  participant Hub as GenerationHub

  Comp->>Svc: generateLessonPlan(req)
  Svc->>Jobs: postAndStream(url, req)
  Jobs->>API: POST /generate<br/>X-Idempotency-Key: uuid
  API-->>Jobs: 202 + { jobId }
  Jobs->>Jobs: subscribeToExistingJob(jobId)
  Jobs->>API: GET /api/jobs/{id} (race-guard poll)
  API-->>Jobs: JobDto (Pending|Running)
  Jobs->>RT: subscribe(jobId)
  RT->>Hub: ensureConnection() (lazy)
  Hub-->>RT: WS open + JobUpdated stream
  loop until terminal
    Hub-->>RT: JobUpdated (Status=Running)
    RT-->>Jobs: JobEvent
    Jobs-->>Svc: JobEvent
    Svc-->>Comp: JobEvent (component shows phased status)
  end
  Hub-->>RT: JobUpdated (Status=Completed, result=JSON)
  RT-->>Jobs: JobEvent
  Jobs-->>Svc: JobEvent
  Svc-->>Comp: parsed DTO via parsePlanResult / parseExerciseResult / parseLessonResult
```

Each AI-touching TS service (`LessonPlanService`, `LessonService`, `DocumentService`) returns `Observable<JobEvent>` from its generate-style methods. Components filter for `JobStatus.Completed`, parse `event.result` (JSON string) into the expected DTO via the service's `parse___Result` helper, and update their state. `JobStatus.Failed` causes the observable to error with the server's message. JWT is forwarded on the WS handshake via `accessTokenFactory: () => this.auth.getToken()`; the .NET side accepts `?access_token=…` only on `/hubs/*` paths.

### `JobsService` API

| Method | Purpose |
| --- | --- |
| `postAndStream<TBody>(url, body, opts?)` | The standard "fire-and-forget then stream" entry point. Auto-injects `X-Idempotency-Key`. Returns `Observable<JobEvent>` that emits per status transition, errors on `Failed`. |
| `subscribeToExistingJob(jobId)` | Resume tracking by id. Polls `GET /api/jobs/{id}` first to handle the race where the executor finished between page load and the WS handshake (emits a synthetic `Completed`/`Failed` event in that case). |
| `findInFlight(type, entityType?, entityId?)` | Single-job page-load probe. Returns the matching `JobDto` or `null`. |
| `listInFlightForEntity(entityType, entityId)` | All jobs the user has on one entity. Detail pages call this on load to repaint every active banner with one query. |
| `get(jobId)` | Polling fallback (used internally by `subscribeToExistingJob`). |

### In-flight recovery (revisit-after-navigation)

Jobs are decoupled from the UI lifecycle: the BG worker keeps running regardless of subscribers. So if the user navigates away mid-generation and comes back:

- **Lesson plan generate** — `LessonPlan.ngOnInit` calls `findInFlight('LessonPlanGenerate')`. If found, banner re-attaches and stream resumes. If the job already **completed** while the user was away, the result is also persisted to `localStorage['lessonshub:pendingPlan']` (24h TTL) so the form repopulates and the user can still hit Save without re-paying for generation.
- **Lesson detail** (content gen / regen / exercise gen / retry) — `LessonDetail.loadLesson` calls `listInFlightForEntity('Lesson', id)` (one query per page load) and dispatches each returned job to the matching banner via a `resumeJobByType` switch.
- **Other endpoints** — exercise review and document ingest don't restore banners on revisit (low value; data still lands correctly, page reload shows new state).

## Service → endpoint map

```mermaid
flowchart LR
  classDef svc fill:#e8f5e9,color:#1a1a1a
  classDef store fill:#bbdefb,color:#1a1a1a
  classDef api fill:#e3f2fd,color:#1a1a1a

  AuthSvc[AuthService]:::svc --> A1[/POST /api/auth/google/]:::api
  Profile[UserProfileService]:::svc --> P1[/GET /api/user/profile/]:::api
  Profile --> P2[/PUT /api/user/profile/]:::api
  Plan[LessonPlanService]:::svc --> LP1[/POST /api/lessonplan/generate/]:::api
  Plan --> LP2[/POST /api/lessonplan/save/]:::api
  Lesson[LessonService]:::svc --> L1[/GET /api/lesson/&lt;id&gt;/]:::api
  Lesson --> L2[/PUT /api/lesson/&lt;id&gt;/]:::api
  Lesson --> L3[/POST /api/lesson/&lt;id&gt;/regenerate-content/]:::api
  Lesson --> L4[/PATCH /api/lesson/&lt;id&gt;/complete/]:::api
  Lesson --> L5[/GET /api/lesson/&lt;id&gt;/siblings/]:::api
  Lesson --> L6[/POST /api/lesson/&lt;id&gt;/generate-exercise/]:::api
  Lesson --> L7[/POST /api/lesson/&lt;id&gt;/retry-exercise/]:::api
  Lesson --> L8[/POST /api/lesson/exercise/&lt;id&gt;/check/]:::api
  Day[LessonDayService]:::svc --> D1[/GET /api/lessonday/plans/]:::api
  Day --> D2[/GET /api/lessonplan/&lt;id&gt;/]:::api
  Day --> D3[/DELETE /api/lessonplan/&lt;id&gt;/]:::api
  Day --> D4[/PUT /api/lessonplan/&lt;id&gt;/]:::api
  Day --> D5[/GET /api/lessonday/&lt;year&gt;/&lt;month&gt;/]:::api
  Day --> D6[/POST /api/lessonday/assign/]:::api
  Day --> D7[/DELETE /api/lessonday/unassign/&lt;id&gt;/]:::api
  Day --> D8[/GET /api/lessonday/date/&lt;date&gt;/]:::api
  Day --> D9[/GET /api/lessonday/plans/&lt;id&gt;/lessons/]:::api
  Doc[DocumentService]:::svc --> D10[/GET /api/documents/]:::api
  Doc --> D11[/GET /api/documents/&lt;id&gt;/]:::api
  Doc --> D12[/POST /api/documents/upload/]:::api
  Doc --> D13[/DELETE /api/documents/&lt;id&gt;/]:::api
  Share[LessonPlanShareService]:::svc --> S1[/GET /api/lessonplan/shared-with-me/]:::api
  Share --> S2[/GET /api/lessonplan/&lt;id&gt;/shares/]:::api
  Share --> S3[/POST /api/lessonplan/&lt;id&gt;/shares/]:::api
  Share --> S4[/DELETE /api/lessonplan/&lt;id&gt;/shares/&lt;userId&gt;/]:::api
  Notify[NotificationService]:::svc

  Store[LessonDataStore]:::store --> Day
  Store --> Share
```

## `LessonDataStore`

The cross-component cache. Owns four signals and provides cache-aware loaders.

```mermaid
classDiagram
  class LessonDataStore {
    -plans signal~LessonPlanSummary[]~
    -sharedPlans signal~LessonPlanSummary[]~
    -lessonDays signal~LessonDay[]~
    -todayLessons signal~AssignedLesson[]~
    -isLoadingPlans signal~boolean~
    -isLoadingSchedule signal~boolean~
    -planCount computed~number~
    +loadPlans() void
    +invalidatePlans() void
    +refreshPlans() void
    +loadSharedPlans() void
    +refreshSharedPlans() void
    +loadSchedule(year, month) void
    +refreshSchedule() void
    +loadToday(dateStr) void
    +refreshToday(dateStr) void
    +onScheduleChanged() void
    +onPlanChanged() void
  }
```

`loadX` checks the signal — if already populated and not stale, it's a no-op. `refreshX` is the explicit "I just mutated something, fetch again" call. The mutators (`onScheduleChanged`, `onPlanChanged`) reset the relevant signals so the next `loadX` call re-fetches.

## Per-service class summaries

### `AuthService` ([services/auth.service.ts](../../lessonshub-ui/src/app/services/auth.service.ts))

```mermaid
classDiagram
  class AuthService {
    -platformId Object
    -tokenSignal signal~string?~
    -userSignal signal~LoginUser?~
    +isLoggedIn() boolean
    +loginWithGoogle(idToken) Observable~LoginResponseDto~
    +logout() void
    +getToken() string?
    +getUser() LoginUser?
    +token computed~string?~
    +user computed~LoginUser?~
  }
```

`isLoggedIn()` decodes the JWT, checks `exp`, and returns false if expired. SSR-aware: returns `false` when running on the server (no `localStorage`).

### `LessonPlanService` ([services/lesson-plan.service.ts](../../lessonshub-ui/src/app/services/lesson-plan.service.ts))

```typescript
class LessonPlanService {
  generateLessonPlan(request: LessonPlanRequest): Observable<LessonPlanResponse>
  saveLessonPlan(
    plan, description, lessonType,
    nativeLanguage?, documentId?, languageToLearn?, useNativeLanguage?
  ): Observable<any>
}
```

The save signature carries the trio of language fields so the component can send only the relevant ones (Language → all three, Default/Technical → just `nativeLanguage`).

### `LessonService` ([services/lesson.service.ts](../../lessonshub-ui/src/app/services/lesson.service.ts))

```typescript
class LessonService {
  getLessonById(id): Observable<Lesson>
  generateExercise(id, difficulty, comment?): Observable<Exercise>
  retryExercise(id, difficulty, comment?, review): Observable<Exercise>
  submitExerciseAnswer(exerciseId, answer): Observable<ExerciseAnswer>
  updateLesson(id, info: UpdateLessonInfo): Observable<Lesson>
  regenerateContent(id, bypassDocCache): Observable<Lesson>
  completeLesson(id): Observable<Lesson>
  getSiblingLessonIds(id): Observable<SiblingLessons>
}
```

### `LessonDayService` ([services/lesson-day.service.ts](../../lessonshub-ui/src/app/services/lesson-day.service.ts))

```typescript
class LessonDayService {
  getLessonPlans(): Observable<LessonPlanSummary[]>
  getLessonPlanDetail(id): Observable<LessonPlanDetail>
  deleteLessonPlan(id): Observable<void>
  updateLessonPlan(id, request: UpdateLessonPlanRequest): Observable<LessonPlanDetail>
  getAvailableLessons(planId): Observable<AvailableLesson[]>
  getLessonDaysByMonth(year, month): Observable<LessonDay[]>
  assignLesson(request: AssignLessonRequest): Observable<void>
  unassignLesson(lessonId): Observable<void>
  getLessonDayByDate(date): Observable<LessonDay?>
}
```

### `DocumentService` ([services/document.service.ts](../../lessonshub-ui/src/app/services/document.service.ts))

```typescript
class DocumentService {
  list(): Observable<Document[]>
  get(id): Observable<Document>
  upload(file: File): Observable<UploadProgress>  // emits {progress: 0-100} or {document: Document}
  delete(id): Observable<void>
}
```

`upload` returns `Observable<UploadProgress>` instead of `Observable<Document>` because it surfaces `HttpEventType.UploadProgress` events for the progress bar.

### `LessonPlanShareService` ([services/lesson-plan-share.service.ts](../../lessonshub-ui/src/app/services/lesson-plan-share.service.ts))

```typescript
class LessonPlanShareService {
  getSharedWithMe(): Observable<LessonPlanSummary[]>
  getShares(planId): Observable<LessonPlanShareItem[]>
  addShare(planId, request: AddShareRequest): Observable<LessonPlanShareItem>
  removeShare(planId, userId): Observable<void>
}
```

### `UserProfileService` ([services/user-profile.service.ts](../../lessonshub-ui/src/app/services/user-profile.service.ts))

```typescript
class UserProfileService {
  getProfile(): Observable<UserProfile>
  updateProfile(request: UpdateUserProfileRequest): Observable<UserProfile>
}
```

### `NotificationService` ([services/notification.service.ts](../../lessonshub-ui/src/app/services/notification.service.ts))

Local-state only — emits toasts that the root `App` component renders. No HTTP.

```typescript
class NotificationService {
  success(message: string): void  // auto-dismiss 4s
  error(message: string): void    // auto-dismiss 6s
  clear(): void
  current = signal<Notification | null>(null);
}
```

## Service injection patterns

All services are `@Injectable({ providedIn: 'root' })` — single shared instance per app.

Components inject via the constructor or `inject()`:

```typescript
constructor(private lessonPlanService: LessonPlanService) {}
// or
private docs = inject(DocumentService);
```

Modern code prefers `inject()` (used in functional guards + standalone components), but constructor injection still works fine for class components.

## Error handling pattern

Components subscribe with `next` + `error` callbacks. The error path typically:

1. Sets a local `error` signal to display in the template.
2. Calls `notify.error('user-friendly message: ' + (err.error?.message || err.message))`.
3. Resets any loading signals.

There's no global error interceptor — errors surface where the call originated.
