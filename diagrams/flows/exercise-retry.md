# Flow — Exercise Retry

After a user submits an answer and gets a low score, they can retry. The retry crew receives the prior `review` text so the new exercise targets the specific weaknesses identified.

> **Source files**: [LessonsHub.Application/Services/ExerciseService.cs](../../LessonsHub.Application/Services/ExerciseService.cs) (`RetryAsync`), [routes/lessons.py:retry_lesson_exercise](../../lessons-ai-api/routes/lessons.py), [crews/exercise_crew.py:run_exercise_retry_crew](../../lessons-ai-api/crews/exercise_crew.py), [tasks/exercise_generation_tasks.py:create_exercise_retry_task](../../lessons-ai-api/tasks/exercise_generation_tasks.py).

## End-to-end

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular
  participant LC as LessonController
  participant ES as ExerciseService (.NET)
  participant Client as LessonsAiApiClient
  participant Route as routes/lessons.py
  participant Py as ExerciseService (Python)
  participant Crew as run_exercise_retry_crew
  participant EC as exercise_creator
  participant LLM as Exercise LLM
  participant QC as run_quality_check

  Note over User: After receiving an Average/Hard score on previous answer
  User->>UI: click "Retry"
  UI->>UI: open GenerateExerciseDialog with prior review prefilled
  User->>UI: confirm difficulty + comment
  UI->>LC: POST /api/lesson/{id}/retry-exercise?<br/>difficulty=...&comment=...&review=...

  LC->>ES: RetryAsync(id, difficulty, comment, review)
  ES->>ES: read access check + lesson.Content non-empty
  ES->>Client: RetryLessonExerciseAsync(AiExerciseRetryRequest)
  Client->>Route: POST /api/lesson-exercise/retry
  Route->>Py: retry_exercise(plan, lesson, spec=ExerciseSpec(difficulty, comment, review))
  Py->>Crew: run_exercise_retry_crew

  Note over Crew: spec.review is set (vs None on initial gen)<br/>Template branches on review_content

  loop attempt = 0..max_quality_retries
    Crew->>Crew: build task with review_content = spec.review
    Crew->>LLM: invoke (sees REMEDIAL block)
    LLM-->>Crew: new exercise markdown
    Crew->>QC: run_quality_check
    alt passed or last
      Crew-->>Py: LessonExerciseResponse
    else
      Note over Crew: append shortcomings to spec.comment; retry
    end
  end

  Py-->>Route: response
  Route-->>Client: JSON
  Client-->>ES: response
  ES->>ES: persist new Exercise (UserId = currentUserId)
  ES-->>LC: Ok(ExerciseDto)
  LC-->>UI: 200
```

## Template branching on `review_content`

[exercise_generation_*.jinja2](../../lessons-ai-api/templates/tasks/) shares the same skeleton between generate and retry — the difference is the `{% if review_content %}` block at the top:

```jinja
{% if review_content %}
You are creating a **REMEDIAL** exercise based on a previous failure.

## PREVIOUS PERFORMANCE REVIEW
The student attempted this previously and received this feedback:
> {{ review_content }}

**CRITICAL INSTRUCTION**:
- Analyze the review above.
- Create a **NEW** exercise that targets the specific weaknesses identified.
- Do NOT repeat the same exercise.
{% else %}
You are creating a **NEW** single exercise for a course lesson.
{% endif %}
```

The "do not repeat" instruction is the key — without it, LLMs sometimes regenerate near-duplicates of the failed exercise.

## What "review" contains

```mermaid
flowchart LR
  classDef bx fill:#fff3e0

  prev[Previous ExerciseAnswer]:::bx
  rev[ReviewText:<br/>"You missed the conjugation in past tense.<br/>Specifically the irregular form of 'ir'..."]:::bx
  retry[Retry exercise template]:::bx

  prev --> rev --> retry
```

The `review` is the AI's prior critique — a markdown explanation of what the user got right and what they missed. Passing this to the retry agent gives it concrete targets.

## How it differs from initial generation

```mermaid
flowchart LR
  classDef same fill:#e8f5e9
  classDef diff fill:#ffe0e0

  init[Initial generate]:::same
  retry[Retry]:::same

  init --> i_qc[Quality loop]:::same
  retry --> r_qc[Quality loop]:::same
  init --> i_doc[RAG doc context]:::same
  retry --> r_doc[RAG doc context]:::same

  init --> i_template[Template: review_content empty]:::diff
  retry --> r_template[Template: REMEDIAL block injected<br/>+ explicit "do not repeat" rule]:::diff

  init --> i_save["Save Exercise<br/>no link to prior"]:::diff
  retry --> r_save["Save Exercise<br/>also no DB link to prior"]:::diff
```

There's intentionally no FK linking the new exercise back to the failed one. From the model's perspective each `Exercise` is independent — the user can attempt them in any order, and the retry context is purely for the AI's prompt.

## When users see this flow

The frontend shows a "Retry" button on `ExerciseAnswer` cards where the AI's `accuracyLevel < 80`. Clicking it opens the same `GenerateExerciseDialog` with the review pre-filled in a hidden field. The user can adjust difficulty or comment, then submit.

## Failure modes

- **Review is too vague** — sometimes the prior review is just "Mostly correct" with no specifics. The retry exercise will be similar to the original since there's nothing to target. Mitigated by: a stronger prompt to the reviewer agent in the review-flow template, asking for specific shortcomings.
- **User keeps retrying past mastery** — no rate limit, but each retry costs an LLM call. Could grow into runaway cost for a single user; not currently capped.
- **Comment + review conflict** — user provides a fresh comment that contradicts the review (e.g. "make it easier" but review says "user understood the basics fine"). The agent generally weights the comment higher (it's the user's direct intent). Acceptable.
