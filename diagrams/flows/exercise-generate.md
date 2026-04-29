# Flow — Exercise Generation

Anyone with read access (owner or borrower) can generate an exercise on a lesson. Each exercise is per-user — borrowers get their own, not the owner's.

> **Source files**: [LessonsHub.Application/Services/ExerciseService.cs](../../LessonsHub.Application/Services/ExerciseService.cs), [routes/lessons.py](../../lessons-ai-api/routes/lessons.py), [services/exercise_service.py](../../lessons-ai-api/services/exercise_service.py), [crews/exercise_crew.py](../../lessons-ai-api/crews/exercise_crew.py).

## End-to-end

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular
  participant Net as .NET API
  participant Job as JobBackgroundService
  participant Route as routes/lessons.py
  participant Crew as run_exercise_crew
  participant LLM as Exercise LLM
  participant QC as run_quality_check

  User->>UI: GenerateExerciseDialog (difficulty, comment)
  UI->>Net: POST /api/lesson/{id}/generate-exercise (X-Idempotency-Key)
  Net-->>UI: 202 { jobId }
  Net->>Job: pick up
  Job->>Route: POST /api/lesson-exercise/generate
  Route->>Crew: run_exercise_crew(plan, lesson, spec)

  opt document_id present and api_key
    Crew->>Crew: embed query + rag_search top-k → document_context
  end

  loop quality retry
    Crew->>LLM: render exercise_generation_{type}.jinja2 + invoke
    LLM-->>Crew: exercise markdown
    Crew->>QC: run_quality_check
    alt passed
      Crew-->>Route: LessonExerciseResponse
    else
      Crew->>Crew: append shortcomings to spec.comment, retry
    end
  end

  Route-->>Job: response
  Job->>Net: persist new Exercise (UserId = caller); SignalR JobUpdated
```

## Per-user tagging

When a borrower generates an exercise on a shared lesson, the new `Exercise` row gets *their* `UserId`. `LessonMapper.ToDetailDto(lesson, userId)` filters `Exercises` to only those matching `userId`, so each user's exercises are private to them.

## Inputs

| Field | Notes |
| --- | --- |
| `difficulty` | Frontend constrains to `easy / medium / hard / very-hard`; backend accepts any free-text. Rendered verbatim into `**Difficulty**: {{ difficulty }}`. |
| `comment` | Optional free-form guidance — "focus on edge cases", "translation task, not multiple-choice", etc. Rendered conditionally in the prompt. |

## Per-type templates

| Type | Behaviour |
| --- | --- |
| Default | Open-ended exercise structure. |
| Technical | Demands code + clear acceptance criteria. |
| Language | Branches on `use_native_language` — native mode: instructions in native + source-text in target; immersive: both in target. Strict no-scaffolding rule (no hints / vocabulary lists in the source text). |

All three templates require the output to end with `### Your Response` so the UI can render a clean answer-input field below the question.

## Prerequisite

The .NET service rejects with `400 BadRequest("Lesson content must be generated first.")` if `lesson.Content` is empty — without it, the AI has nothing to base the exercise on.

## What's stored

`Exercise` row with `Text`, `Difficulty`, `LessonId`, `UserId`, plus `AiRequestLog` rows for the writer + quality validator.
