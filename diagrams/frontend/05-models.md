# Frontend — 05 Models

TypeScript interfaces under [lessonshub-ui/src/app/models/](../../lessonshub-ui/src/app/models/). Field names use camelCase matching the .NET API's serializer; date fields are ISO 8601 strings.

## Class diagram

```mermaid
classDiagram
  class LessonPlanRequest {
    +string lessonType
    +string planName
    +number? numberOfDays
    +string topic
    +string description
    +string? nativeLanguage
    +string? languageToLearn
    +boolean? useNativeLanguage
    +boolean? bypassDocCache
    +number? documentId
  }

  class LessonPlanResponse {
    +string planName
    +string topic
    +GeneratedLesson[] lessons
  }

  class LessonPlanSummary {
    +number id
    +string name
    +string topic
    +string description
    +string createdDate
    +number lessonsCount
    +boolean isOwner
    +string? ownerName
  }

  class LessonPlanDetail {
    +number id
    +string name
    +string topic
    +string description
    +string? nativeLanguage
    +string? languageToLearn
    +boolean? useNativeLanguage
    +PlanLesson[] lessons
    +boolean isOwner
  }

  class Lesson {
    +number id
    +number lessonNumber
    +string name
    +string content
    +string lessonType
    +string[] keyPoints
    +boolean isCompleted
    +number lessonPlanId
    +number? lessonDayId
    +Exercise[] exercises
    +Video[] videos
    +Book[] books
    +Documentation[] documentation
    +boolean isOwner
  }

  class Exercise {
    +number id
    +string exerciseText
    +string difficulty
    +number lessonId
    +ExerciseAnswer[] answers
  }

  class ExerciseAnswer {
    +number id
    +string userResponse
    +string submittedAt
    +number? accuracyLevel
    +string? reviewText
  }

  class LessonDay {
    +number id
    +string date
    +AssignedLesson[] lessons
  }

  class Document {
    +number id
    +string name
    +string contentType
    +number sizeBytes
    +IngestionStatus ingestionStatus
    +string? ingestionError
    +number? chunkCount
    +string createdAt
    +string? ingestedAt
  }

  class IngestionStatus {
    <<enum>>
    Pending
    Ingested
    Failed
  }

  class JobDto {
    +string id
    +string type
    +string status
    +string? payloadJson
    +string? resultJson
    +string? error
    +string? relatedEntityType
    +number? relatedEntityId
    +string createdAt
  }

  LessonPlanResponse --> GeneratedLesson
  LessonPlanDetail --> PlanLesson
  Lesson --> Exercise
  Lesson --> Video
  Lesson --> Book
  Lesson --> Documentation
  Exercise --> ExerciseAnswer
  Document --> IngestionStatus
```

## Per-file inventory

- [lesson-plan.model.ts](../../lessonshub-ui/src/app/models/lesson-plan.model.ts) — `LessonPlanRequest`, `LessonPlanResponse`, `GeneratedLesson`, plus `LESSON_TYPES = ['Technical', 'Language', 'Default']`.
- [lesson.model.ts](../../lessonshub-ui/src/app/models/lesson.model.ts) — `Lesson`, `Exercise`, `ExerciseAnswer`, `Video`, `Book`, `Documentation`, `ChatMessage`, `UpdateLessonInfo`, plus `DIFFICULTIES = ['Easy', 'Average', 'Hard', 'Very hard']`.
- [lesson-day.model.ts](../../lessonshub-ui/src/app/models/lesson-day.model.ts) — `LessonDay`, `AssignedLesson`, `AvailableLesson`, `LessonPlanSummary`, `LessonPlanDetail`, `LessonPlanShareItem`, `AddShareRequest`, `UpdateLessonPlanRequest`, `UpdateLessonRequest`, `AssignLessonRequest`, `PlanLesson`.
- [document.model.ts](../../lessonshub-ui/src/app/models/document.model.ts) — `Document`, `IngestionStatus` enum.
- [job.model.ts](../../lessonshub-ui/src/app/models/job.model.ts) — `JobDto`, `JobEvent`, `JobStatus` enum (mirrors the .NET enum).

## Conventions

- Interface fields are **camelCase**, matching the JSON wire format.
- Date fields are **strings** (ISO 8601 from the server). Components parse them with `new Date(...)` only for display formatting.
- Optional fields use `?:` not `| undefined` — keeps call sites cleaner.
- Enum-like sets (`LESSON_TYPES`, `DIFFICULTIES`) are exported as `const` arrays, not TS enums, to avoid runtime overhead. Real enums are reserved for value sets that flow over the wire (`IngestionStatus`, `JobStatus`).
- `Lesson.isOwner` and `LessonPlanDetail.isOwner` drive the permission UI: owner-only buttons (regenerate, edit, complete, share, delete) are gated on these.
