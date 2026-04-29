# Frontend — 06 Flows

Component-level user flows. AI-orchestrated detail (what happens *inside* a `generate` call once it leaves the UI) is in [../ai/03-services-and-crews.md](../ai/03-services-and-crews.md) and [../backend/06-flows.md](../backend/06-flows.md).

## Login (Google One Tap)

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant L as Login component
  participant Goog as Google One Tap
  participant Auth as AuthService
  participant API as .NET API
  participant Router

  User->>L: visit /login
  L->>Goog: render One Tap
  Goog-->>L: id_token (callback)
  L->>Auth: loginWithGoogle(idToken)
  Auth->>API: POST /api/auth/google
  API-->>Auth: { token, user }
  Auth->>Auth: localStorage.setItem auth_token + tokenSignal.set
  Auth-->>L: ok
  L->>Router: navigate('/today')
```

## Generate + save a lesson plan

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant LP as LessonPlan component
  participant LPS as LessonPlanService
  participant Jobs as JobsService
  participant API as .NET API

  User->>LP: pick lessonType, fill form
  alt Language
    Note over LP: also set nativeLanguage, languageToLearn, useNativeLanguage
  end
  User->>LP: click Generate Plan
  LP->>LPS: generateLessonPlan(request)
  LPS->>Jobs: postAndStream(url, request)
  Jobs->>API: POST /api/lessonplan/generate
  API-->>Jobs: 202 { jobId }
  loop until terminal
    Jobs-->>LP: JobEvent (Pending → Running → Completed)
  end
  LP->>LP: parsePlanResult(event), generatedPlan.set
  LP->>LP: persist to localStorage 24h TTL
  User->>LP: review, edit names, click Save
  LP->>LPS: saveLessonPlan(...)
  LPS->>API: POST /api/lessonplan/save
  API-->>LPS: { lessonPlanId }
  LP->>LP: notify.success + clear localStorage
```

The localStorage backup means the user can navigate away and return without re-paying for generation. On revisit, `LessonPlan.ngOnInit` calls `JobsService.findInFlight('LessonPlanGenerate')` to resume an in-flight job, or reads `localStorage['lessonshub:pendingPlan']` for an already-completed-but-unsaved one.

## Lesson detail + content generation

```mermaid
sequenceDiagram
  actor User
  participant LD as LessonDetail
  participant Jobs as JobsService
  participant LS as LessonService
  participant API as .NET API

  User->>LD: navigate /lesson/42
  LD->>API: GET /api/lesson/42
  API-->>LD: LessonDetailDto (Content may be empty)
  LD->>Jobs: listInFlightForEntity('Lesson', 42)
  Jobs-->>LD: any active jobs (content gen, exercise gen, …)
  LD->>LD: repaint banners
  alt Content empty
    User->>LD: click Generate Content
    LD->>LS: generateContent(42)
    LS->>Jobs: postAndStream
    Jobs-->>LD: streaming events, final Completed → reload lesson
  end
```

Sibling navigation (`prev`/`next`) subscribes to `route.paramMap` so the URL change reloads without a full re-render.

## Generate exercise + submit answer

```mermaid
sequenceDiagram
  actor User
  participant LD as LessonDetail
  participant Dlg as GenerateExerciseDialog
  participant LS as LessonService
  participant Jobs as JobsService

  User->>LD: click Generate Exercise
  LD->>Dlg: open
  Dlg-->>LD: { difficulty, comment }
  LD->>LS: generateExercise(lessonId, difficulty, comment)
  LS->>Jobs: postAndStream
  Jobs-->>LD: Completed → parse → push exercise

  User->>LD: type answer + Submit
  LD->>LS: submitExerciseAnswer(exerciseId, answer)
  LS->>Jobs: postAndStream
  Jobs-->>LD: Completed → parse review → append to exercise.answers[]
```

Exercises are tagged server-side with the *caller's* `userId` — borrowers (people the plan was shared with) get their own exercises, not the owner's.

## Share a plan

```mermaid
sequenceDiagram
  actor Owner
  participant SD as ShareDialog
  participant SS as LessonPlanShareService
  participant API as .NET API

  Owner->>SD: open
  SD->>SS: getShares(planId)
  SS->>API: GET /api/lessonplan/{id}/shares
  API-->>SD: existing shares list
  Owner->>SD: type email + Share
  SD->>SS: addShare(planId, { email })
  SS->>API: POST /api/lessonplan/{id}/shares
  alt unknown email
    API-->>SD: 404 → notify.error
  else conflict
    API-->>SD: 409 → notify.error
  else ok
    API-->>SD: LessonPlanShareDto → push to list
  end
```

## Schedule a lesson

```mermaid
sequenceDiagram
  actor User
  participant LDays as LessonDays
  participant LDS as LessonDayService
  participant API as .NET API
  participant Store as LessonDataStore

  User->>LDays: pick date in calendar
  LDays->>LDS: getLessonDayByDate(date)
  LDays->>LDS: getAvailableLessons(planId)
  User->>LDays: click Assign on a lesson
  LDays->>LDS: assignLesson({ lessonId, date, dayName, dayDescription })
  LDS->>API: POST /api/lessonday/assign
  API-->>LDS: 200
  LDays->>Store: onScheduleChanged()
```

## Upload a document

```mermaid
sequenceDiagram
  actor User
  participant Docs as Documents
  participant DS as DocumentService
  participant API as .NET API

  User->>Docs: choose file + upload
  Docs->>DS: upload(file)
  DS->>API: POST /api/documents/upload (multipart)
  loop progress events
    API-->>DS: HttpEventType.UploadProgress
    DS-->>Docs: { progress: 0-100 }
  end
  API-->>DS: 202 { document, jobId }
  DS-->>Docs: document
  Note over Docs: SignalR job pipeline drives ingest —<br/>document row updates from Pending → Ingested
```

If ingestion fails, the document still appears in the list with `status: "Failed"` and an `ingestionError` — the user can re-upload.
