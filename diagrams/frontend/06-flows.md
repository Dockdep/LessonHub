# Frontend — 06 Flows

Component-level user flows. AI-orchestrated flows (what happens *inside* a `generate` call once it leaves the UI) are in [../flows/](../flows/).

## Login (Google One Tap)

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant L as Login component
  participant Goog as Google One Tap
  participant Auth as AuthService
  participant API as .NET API
  participant LS as localStorage
  participant Router

  User->>L: visit /login
  L->>Goog: render One Tap button
  User->>Goog: click + select account
  Goog-->>L: id_token (callback)
  L->>Auth: loginWithGoogle(idToken)
  Auth->>API: POST /api/auth/google { idToken }
  API-->>Auth: { token, user }
  Auth->>LS: setItem('token', jwt)
  Auth->>Auth: tokenSignal.set(jwt)
  Auth->>Auth: userSignal.set(user)
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
  participant API as .NET API
  participant AI as Python AI
  participant Store as LessonDataStore

  User->>LP: pick lessonType, fill topic, …
  alt lessonType == Language
    Note over LP: also fill nativeLanguage, languageToLearn,<br/>useNativeLanguage toggle
  end
  alt sourceDocument selected
    Note over LP: documentId set on request
  end
  User->>LP: click "Generate Plan"
  LP->>LPS: generateLessonPlan(request)
  LPS->>API: POST /api/lessonplan/generate
  API->>AI: POST /api/lesson-plan/generate
  AI-->>API: LessonPlanResponse
  API-->>LPS: response
  LPS-->>LP: response
  LP->>LP: generatedPlan.set(response)
  User->>LP: review lessons, edit names, click "Save to Library"
  LP->>LPS: saveLessonPlan(plan, ...)
  LPS->>API: POST /api/lessonplan/save
  API-->>LPS: { lessonPlanId }
  LPS-->>LP: ok
  LP->>Store: onPlanChanged()
  LP->>LP: notify.success("Plan saved!")
```

The component calls `Store.onPlanChanged()` so the next visit to `/lesson-plans` re-fetches.

## Generate exercise (any user, plan-shared OK)

```mermaid
sequenceDiagram
  actor User
  participant LD as LessonDetail component
  participant Dlg as GenerateExerciseDialog
  participant LS as LessonService
  participant API as .NET API

  User->>LD: click "Generate Exercise"
  LD->>Dlg: open dialog
  Dlg-->>User: ask difficulty + comment
  User->>Dlg: submit
  Dlg-->>LD: { difficulty, comment }
  LD->>LS: generateExercise(lessonId, difficulty, comment)
  LS->>API: POST /api/lesson/{id}/generate-exercise
  API-->>LS: ExerciseDto
  LS-->>LD: exercise
  LD->>LD: lesson().exercises.push(exercise)
  LD->>LD: notify.success
```

The exercise is tagged server-side with the *caller's* `userId` — borrowers (people the plan was shared with) get their own exercise, not the owner's.

## Submit answer + receive review

```mermaid
sequenceDiagram
  actor User
  participant LD as LessonDetail component
  participant LS as LessonService
  participant API as .NET API
  participant AI as Python AI

  User->>LD: type answer, click "Submit"
  LD->>LS: submitExerciseAnswer(exerciseId, answer)
  LS->>API: POST /api/lesson/exercise/{id}/check
  API->>AI: POST /api/exercise-review/check
  AI-->>API: ExerciseReviewResponse { accuracyLevel, examReview }
  API-->>LS: ExerciseAnswerDto
  LS-->>LD: answer
  LD->>LD: append to exercise.answers[]
```

The review is rendered as markdown beneath the user's answer.

## Regenerate lesson content (owner-only)

```mermaid
sequenceDiagram
  actor Owner
  participant LD as LessonDetail
  participant Dlg as RegenerateLessonDialog
  participant LS as LessonService
  participant API as .NET API
  participant AI as Python AI

  Owner->>LD: click "Regenerate" (visible only if isOwner)
  LD->>Dlg: open
  Dlg-->>Owner: ask bypassDocCache + comment
  Owner->>Dlg: submit
  Dlg-->>LD: { bypassDocCache, comment }
  LD->>LS: regenerateContent(id, bypassDocCache)
  LS->>API: POST /api/lesson/{id}/regenerate-content?bypassDocCache=true
  API->>AI: POST /api/lesson-content/generate
  AI-->>API: new content
  API->>API: lesson.Content = content; SaveChanges
  API-->>LS: updated LessonDetailDto
  LS-->>LD: lesson
  LD->>LD: lesson.set(updated)
```

## Share a plan

```mermaid
sequenceDiagram
  actor Owner
  participant LPD as LessonPlanDetail
  participant SD as ShareDialog
  participant SS as LessonPlanShareService
  participant API as .NET API

  Owner->>LPD: click "Share"
  LPD->>SD: open with planId
  SD->>SS: getShares(planId)
  SS->>API: GET /api/lessonplan/{id}/shares
  API-->>SS: list
  SS-->>SD: existing shares
  Owner->>SD: type email + click "Share"
  SD->>SS: addShare(planId, { email })
  SS->>API: POST /api/lessonplan/{id}/shares
  alt unknown email
    API-->>SS: 404
    SS-->>SD: error → notify.error("No user...")
  else conflict
    API-->>SS: 409
    SS-->>SD: error → notify.error("Already shared")
  else ok
    API-->>SS: LessonPlanShareDto
    SS-->>SD: ok → push to list
  end
  Owner->>SD: click 🗑 next to a share
  SD->>SS: removeShare(planId, userId)
  SS->>API: DELETE /api/lessonplan/{id}/shares/{userId}
  API-->>SS: 200
  SS-->>SD: ok → remove from list
```

## Schedule a lesson on a date

```mermaid
sequenceDiagram
  actor User
  participant LDays as LessonDays
  participant LDS as LessonDayService
  participant API as .NET API
  participant Store as LessonDataStore

  User->>LDays: pick date in calendar
  LDays->>LDS: getLessonDayByDate(date)
  LDS->>API: GET /api/lessonday/date/{date}
  API-->>LDS: LessonDayDto?
  LDS-->>LDays: existing day or null
  User->>LDays: pick a plan
  LDays->>LDS: getAvailableLessons(planId)
  LDS->>API: GET /api/lessonday/plans/{id}/lessons
  API-->>LDS: AvailableLesson[]
  LDS-->>LDays: lessons (with isAssigned flag)
  User->>LDays: click "Assign" on a lesson
  LDays->>LDS: assignLesson({ lessonId, date, dayName, dayDescription })
  LDS->>API: POST /api/lessonday/assign
  API->>API: upsert LessonDay; set lesson.LessonDayId
  API-->>LDS: 200
  LDS-->>LDays: ok
  LDays->>Store: onScheduleChanged()
```

## Upload a document

```mermaid
sequenceDiagram
  actor User
  participant Docs as Documents component
  participant DS as DocumentService
  participant API as .NET API
  participant AI as Python AI (RAG)

  User->>Docs: choose file, click upload
  Docs->>DS: upload(file)
  DS->>API: POST /api/documents/upload (multipart, with progress)
  API-->>DS: HttpEventType.UploadProgress (multiple)
  loop while progress events
    DS-->>Docs: { progress: 0-100 }
    Docs->>Docs: uploadProgress.set(n)
  end
  API->>API: write to GCS / local FS
  API->>AI: POST /api/rag/ingest
  AI->>AI: chunk + embed + upsert to pgvector
  AI-->>API: { chunkCount }
  API-->>DS: HttpEventType.Response { document with status: "Ingested" }
  DS-->>Docs: { document }
  Docs->>Docs: documents().push(document); isUploading.set(false)
  Docs->>Docs: notify.success
```

If the AI ingestion fails, the document still appears in the list with `status: "Failed"` and an `ingestionError` — the user can re-upload.

## Update profile (Gemini API key)

```mermaid
sequenceDiagram
  actor User
  participant Pr as Profile component
  participant UPS as UserProfileService
  participant API as .NET API

  User->>Pr: paste Gemini API key, click Save
  Pr->>UPS: updateProfile({ googleApiKey })
  UPS->>API: PUT /api/user/profile
  API->>API: Update User.GoogleApiKey; SaveChanges
  API-->>UPS: UserProfileDto
  UPS-->>Pr: profile
  Pr->>Pr: notify.success
```

After this, every AI call routed through `IUserApiKeyProvider` uses the new key.
