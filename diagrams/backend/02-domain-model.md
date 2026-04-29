# Backend — 02 Domain Model

Entities under [LessonsHub.Domain/Entities/](../../LessonsHub.Domain/Entities/) — POCOs only, no behaviour, no external deps. EF relationships are configured in [LessonsHubDbContext.OnModelCreating](../../LessonsHub.Infrastructure/Data/LessonsHubDbContext.cs).

> Cross-tier ER view is in [03-database.md](../03-database.md). This file zooms in on the C# class shape.

## Class diagram

```mermaid
classDiagram
  class User {
    +int Id
    +string GoogleId
    +string Email
    +string GoogleApiKey
  }
  class LessonPlan {
    +int Id
    +string Name
    +string Topic
    +string NativeLanguage
    +string LanguageToLearn
    +bool UseNativeLanguage
    +int? UserId
    +int? DocumentId
  }
  class Lesson {
    +int Id
    +int LessonNumber
    +string Name
    +string Content
    +string LessonType
    +List~string~ KeyPoints
    +bool IsCompleted
    +int LessonPlanId
    +int? LessonDayId
  }
  class LessonDay {
    +int Id
    +DateTime Date
    +int UserId
  }
  class LessonPlanShare {
    +int LessonPlanId
    +int UserId
  }
  class Exercise {
    +int Id
    +string ExerciseText
    +string Difficulty
    +int LessonId
    +int UserId
  }
  class ExerciseAnswer {
    +int Id
    +int? AccuracyLevel
    +string ReviewText
    +int ExerciseId
  }
  class Document {
    +int Id
    +string StorageUri
    +string IngestionStatus
    +int? ChunkCount
    +int UserId
  }
  class Job {
    +Guid Id
    +int UserId
    +string Type
    +int Status
    +string PayloadJson
    +string ResultJson
    +string IdempotencyKey
    +string RelatedEntityType
    +int RelatedEntityId
  }

  User "1" --> "*" LessonPlan
  User "1" --> "*" LessonDay
  User "1" --> "*" Document
  User "1" --> "*" Job
  LessonPlan "1" --> "*" Lesson
  LessonPlan "1" --> "*" LessonPlanShare
  LessonPlan "0..1" --> "0..1" Document
  Lesson "1" --> "*" Exercise
  Lesson "*" --> "0..1" LessonDay
  Exercise "1" --> "*" ExerciseAnswer
```

Per-lesson resource entities (`Video`, `Book`, `Documentation`) and `ChatMessage` are omitted from the diagram for clarity — they're plain `LessonId`-keyed children. `AiRequestLog` is per-call cost-tracking, written by [AiCostLogger.cs](../../LessonsHub.Infrastructure/Services/AiCostLogger.cs).

## Key invariants

- **Per-user `Exercise` and `ExerciseAnswer`**. When a borrower (a user the plan was shared with) generates an exercise on a shared lesson, the new row is theirs, not the owner's. `Exercise.UserId` is required.
- **Per-user `LessonDay`**. The calendar entry is the user's, not the plan's. `Lesson.LessonDayId` is shared across users — practically only the plan owner can assign/unassign, so this is consistent today; a future per-user-assignment redesign would split it.
- **`GoogleApiKey` is per-user**. Every AI call routes through it via `IUserApiKeyProvider`, so users pay for their own generation.
- **Sharing is read-only by convention**. `LessonService.UpdateAsync` and `RegenerateContentAsync` are owner-only; borrowers can read content and generate their own exercises.
- **Three language fields on `LessonPlan`**. `NativeLanguage` is used universally; `LanguageToLearn` and `UseNativeLanguage` apply only when `LessonType == "Language"`. Native mode = explanations in mother tongue with target-language examples; immersive = entire lesson in target language.
- **`Document.StorageUri` is opaque** (`gs://bucket/path` in prod). Only the storage layer interprets it.

## Document lifecycle

```mermaid
stateDiagram-v2
  [*] --> Pending: uploaded + saved to GCS
  Pending --> Ingested: chunked + embedded
  Pending --> Failed: ingestion error
  Failed --> Pending: re-ingest (deletes old chunks)
  Ingested --> Pending: re-ingest
  Ingested --> [*]: deleted
  Failed --> [*]: deleted
```

## Cascade behaviour

The cascading set is centred on `LessonPlan` — deleting a plan tears down `Lessons` → `Exercises`/`Videos`/`Books`/`Documentation` and the `LessonPlanShare` rows. `Lesson.LessonDayId` cascade is `SetNull` (two users may share a day), so empty `LessonDay` rows are cleaned up explicitly by `LessonPlanService.DeleteAsync`. `Document` deletion uses `SetNull` on `LessonPlan.DocumentId` — losing the source doc doesn't take the plan down with it.
