# Flow — Lesson Plan Generation (Default)

The simplest path: no framework grounding, no language toggle. Used for non-technical, non-language plans (e.g. history, business, philosophy).

> **Source files**: [routes/lessons.py:generate_lesson_plan](../../lessons-ai-api/routes/lessons.py), [services/curriculum_service.py](../../lessons-ai-api/services/curriculum_service.py), [crews/curriculum_crew.py](../../lessons-ai-api/crews/curriculum_crew.py), [tasks/lesson_plan_tasks.py](../../lessons-ai-api/tasks/lesson_plan_tasks.py), [templates/tasks/lesson_plan_Default.jinja2](../../lessons-ai-api/templates/tasks/lesson_plan_Default.jinja2).

## End-to-end

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular
  participant Net as .NET API
  participant LPS as LessonPlanService
  participant Client as LessonsAiApiClient
  participant Route as routes/lessons.py
  participant CS as CurriculumService
  participant Crew as run_curriculum_crew
  participant CD as curriculum_designer_Default
  participant LLM as Plan LLM (Gemini Pro)
  participant QC as run_quality_check

  User->>UI: fill form (lessonType=Default)
  UI->>Net: POST /api/lessonplan/generate<br/>X-Idempotency-Key: uuid
  Net->>LPS: ValidateGenerateAsync(req)
  LPS-->>Net: ServiceResult.Ok
  Net->>Net: JobService.EnqueueAsync<br/>(LessonPlanGenerate, payload)
  Net-->>UI: 202 { jobId }
  UI->>Net: WS subscribe /hubs/generation
  Note over Net: JobBackgroundService picks up jobId
  Net->>LPS: GenerateAsync(req) (in BG scope)
  LPS->>Client: GenerateLessonPlanAsync(AiLessonPlanRequest)
  Client->>Route: POST /api/lesson-plan/generate
  Route->>Route: build PlanContext<br/>(language = req.language or req.nativeLanguage)
  Route->>CS: generate_plan(plan, number_of_lessons, ...)
  CS->>Crew: run_curriculum_crew(llm, plan, ...)

  Note over Crew: agent_type != "Technical" → skip framework analyzer<br/>document_id is None → skip RAG outline

  loop attempt = 0..max_quality_retries
    Crew->>Crew: build curriculum agent + task<br/>(template: lesson_plan_Default.jinja2)
    Crew->>LLM: invoke
    LLM-->>Crew: lesson plan markdown
    Crew->>QC: run_quality_check(generation_type="lesson plan", ...)
    QC->>LLM: validator critique
    LLM-->>QC: { score, shortcomings }
    alt passed or last attempt
      Crew-->>CS: LessonPlanResponse
      Note right of Crew: usage[] tracks every LLM call
    else
      Note over Crew: Append shortcomings to plan.description<br/>retry next iteration
    end
  end

  CS-->>Route: response
  Route-->>Client: JSON
  Client-->>LPS: response
  LPS->>LPS: Map to LessonPlanResponseDto
  LPS-->>Net: ServiceResult.Ok(response)
  Net->>Net: Job.ResultJson = JSON(response)<br/>Status = Completed
  Net->>UI: SignalR JobUpdated<br/>(via user-{userId} group)
  UI->>UI: parsePlanResult(event), render plan<br/>user clicks "Save to Library"
```

This is the same SignalR job pattern every AI-generation endpoint follows — see [backend/04-infrastructure.md#realtime--job-pipeline-signalr--background-worker](../backend/04-infrastructure.md#realtime--job-pipeline-signalr--background-worker) for the executor + queue + hub plumbing. Other flow docs in this folder focus on the AI-side detail (CrewAI agents + tasks + Python services) and abstract the .NET-side hand-off as `Net → AI → Net → SignalR`; the envelope is identical.

## Task prompt (Default)

[templates/tasks/lesson_plan_Default.jinja2](../../lessons-ai-api/templates/tasks/lesson_plan_Default.jinja2) renders with these context vars:

```jinja
{% if language %}Write all content in {{ language }}.{% endif %}
Topic: {{ topic }}
{% if number_of_lessons %}
Required Lessons: {{ number_of_lessons }}
{% endif %}
Additional Context/User Level: {{ description }}
```

The `_document_context.jinja2` partial is included but renders empty when `document_context` is `""` (which it is for plain Default plans without an attached document).

## Quality retry feedback

If the validator returns `score < 80`, the crew appends the shortcomings to `plan.description`:

```text
[QUALITY FEEDBACK - Attempt 1]: Lesson 3 doesn't have a clear learning outcome; revise.
```

Next iteration, the writer sees this in the description field of its prompt and (hopefully) addresses it. After `max_quality_retries`, the crew gives up and returns the last attempt regardless.

## What the user sees

- The frontend's `LessonPlan` form returns a `LessonPlanResponse` with `topic`, `lessons[]`, plus optional `qualityCheck` and `usage[]`.
- The user reviews + edits the generated lessons inline, then clicks **Save to Library** (`POST /api/lessonplan/save`) which persists the plan via `LessonPlanService.SaveAsync`.
- A separate flow (the "lazy content generation" hit on first lesson read) generates each lesson's body — see [lesson-content-default.md](lesson-content-default.md).
