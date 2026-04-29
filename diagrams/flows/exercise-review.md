# Flow — Exercise Review (Submit Answer)

The user submits an answer; the AI scores it 0–100 and writes a markdown review. The score lives on `ExerciseAnswer.AccuracyLevel`; the review on `ExerciseAnswer.ReviewText`.

> **Source files**: [LessonsHub.Application/Services/ExerciseService.cs](../../LessonsHub.Application/Services/ExerciseService.cs), [routes/lessons.py](../../lessons-ai-api/routes/lessons.py), [crews/review_crew.py](../../lessons-ai-api/crews/review_crew.py), [tasks/exercise_review_tasks.py](../../lessons-ai-api/tasks/exercise_review_tasks.py).

## End-to-end

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular
  participant Net as .NET API
  participant Job as JobBackgroundService
  participant Route as routes/lessons.py
  participant Crew as run_exercise_review_crew
  participant LLM as Review LLM

  User->>UI: type answer + Submit
  UI->>Net: POST /api/lesson/exercise/{id}/check (X-Idempotency-Key)
  Net-->>UI: 202 { jobId }
  Net->>Job: pick up; verify caller owns the exercise
  Job->>Route: POST /api/exercise-review/check
  Route->>Crew: run_exercise_review_crew

  loop quality retry
    Crew->>LLM: render exercise_review_{type}.jinja2 + invoke
    LLM-->>Crew: ExerciseReviewResult { accuracyLevel, examReview }
  end

  Route-->>Job: response (accuracyLevel, examReview)
  Job->>Net: Add ExerciseAnswer; SignalR JobUpdated
```

## Caller-must-own-exercise

[ExerciseRepository.GetForUserWithLessonAsync](../../LessonsHub.Infrastructure/Repositories/ExerciseRepository.cs) joins on `Exercise.UserId == currentUserId`, so the exercise is found only if the caller owns it. A borrower cannot submit answers to the owner's exercises (and vice versa).

## Pydantic structured output

[tasks/exercise_review_tasks.py](../../lessons-ai-api/tasks/exercise_review_tasks.py) uses CrewAI's `output_pydantic` parameter:

```python
class ExerciseReviewResult(BaseModel):
    accuracyLevel: int = Field(..., description="Score from 0 to 100 ...")
    examReview: str = Field(..., description="Detailed feedback ...")
```

CrewAI parses the agent's output into the model automatically. Parse failures retry up to the agent's `max_iter`.

## Per-type templates

| Type | Distinguishing instruction |
| --- | --- |
| Default | "Explain what's right and wrong" |
| Technical | "Check for bugs, edge cases, idiomatic usage" |
| Language | "Check grammar, spelling, vocabulary, sentence structure, natural expression" — review in `native_language` if `useNativeLanguage`, else `language_to_learn` |

The Language template branches the same way as the plan/content templates — native (helpful for low-CEFR) vs target (immersive correction).

## Retry button heuristic

The frontend renders a `Retry` button beneath answers with `accuracyLevel < 80`. See [exercise-retry.md](exercise-retry.md) for what happens when the user clicks it.

## What's stored

`ExerciseAnswer` row: `UserResponse`, `SubmittedAt`, `AccuracyLevel`, `ReviewText`, `ExerciseId`. Plus `AiRequestLog` rows. The frontend renders the review markdown beneath the user's answer with a colored badge (red <50, amber 50-79, green ≥80).
