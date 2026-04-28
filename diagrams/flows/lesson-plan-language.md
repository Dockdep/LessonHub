# Flow — Lesson Plan Generation (Language)

For language-learning plans. Three language fields shape the prompt:

- `nativeLanguage` — the user's mother tongue.
- `languageToLearn` — the target language.
- `useNativeLanguage` — boolean toggle deciding whether the lesson is rendered in the native tongue (with target-language examples) or fully immersive in the target language.

> **Source files**: [routes/lessons.py:_resolve_language](../../lessons-ai-api/routes/lessons.py), [crews/curriculum_crew.py](../../lessons-ai-api/crews/curriculum_crew.py), [tasks/lesson_plan_tasks.py](../../lessons-ai-api/tasks/lesson_plan_tasks.py), [templates/tasks/lesson_plan_Language.jinja2](../../lessons-ai-api/templates/tasks/lesson_plan_Language.jinja2).

## Language resolution at the boundary

```mermaid
flowchart TD
  start{lesson_type<br/>== Language?}
  yes{use_native_language?}
  no_lang[language = req.language or req.nativeLanguage]
  use_native[language = req.nativeLanguage]
  use_target[language = req.languageToLearn]

  start -- Yes --> yes
  start -- No --> no_lang
  yes -- True --> use_native
  yes -- False --> use_target
```

The resulting `language` is what gets bound to `{{ language }}` in templates — the *rendering* language. `nativeLanguage` and `languageToLearn` are passed through separately so templates can branch on them and refer to both explicitly.

## End-to-end

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant UI as Angular
  participant Route as routes/lessons.py
  participant CS as CurriculumService
  participant Crew as run_curriculum_crew
  participant CD as curriculum_designer_Language
  participant LLM as Plan LLM
  participant QC as run_quality_check

  User->>UI: lessonType=Language<br/>nativeLanguage=English<br/>languageToLearn=Spanish<br/>useNativeLanguage=true
  UI->>Route: POST /api/lesson-plan/generate
  Route->>Route: _resolve_language → "English"<br/>(use_native_language=true)
  Route->>CS: generate_plan(PlanContext<br/>language="English",<br/>native_language="English",<br/>language_to_learn="Spanish",<br/>use_native_language=true)
  CS->>Crew: run_curriculum_crew

  Note over Crew: agent_type != "Technical" → skip framework analyzer

  loop attempt = 0..max_quality_retries
    Crew->>Crew: render lesson_plan_Language.jinja2<br/>with all 4 language fields
    Crew->>LLM: invoke
    LLM-->>Crew: plan in English (lessons explained in English,<br/>covering Spanish topics)
    Crew->>QC: run_quality_check
    alt passed or last
      Crew-->>CS: LessonPlanResponse
    end
  end

  CS-->>Route: response
  Route-->>UI: 200
```

## Template branching ([lesson_plan_Language.jinja2](../../lessons-ai-api/templates/tasks/lesson_plan_Language.jinja2))

```jinja
{% if use_native_language %}
Write the lesson plan in {{ native_language }}. The student is studying
**{{ language_to_learn }}**, but explanations and lesson titles must be in
{{ native_language }}.
{% else %}
Write the lesson plan in **{{ language_to_learn }}** (immersive mode).
The student wants to be exposed to {{ language_to_learn }} from the start.
{% endif %}

Topic: {{ topic }}
Native language: {{ native_language }}
Language being studied: {{ language_to_learn }}
{% if number_of_lessons %}
Required Lessons: {{ number_of_lessons }}
{% endif %}
Additional Context/User Level: {{ description }}

Instructions:
1. **Determine Baseline**: Based on the context, identify the learner's
   starting point in {{ language_to_learn }}.
2. **Curate Skeleton**: ...
3. **Logical Sequencing**: Focus on 'Structure → Usage → Conversational Context'.
```

The branching is at the *top* of the prompt so the LLM sees the language directive before any other instructions.

## Mode comparison

```mermaid
flowchart LR
  classDef nat fill:#e8f5e9
  classDef imm fill:#fff3e0

  subgraph native[useNativeLanguage = true]
    direction TB
    nat_in[Topic: Spanish A2]:::nat
    nat_in --> nat_pl[Plan written in English]:::nat
    nat_pl --> nat_les[Lessons titled in English]:::nat
    nat_les --> nat_ex[Lesson body has Spanish examples,<br/>English explanations]:::nat
  end

  subgraph immersive[useNativeLanguage = false]
    direction TB
    imm_in[Topic: Spanish A2]:::imm
    imm_in --> imm_pl[Plan written in Spanish]:::imm
    imm_pl --> imm_les[Lessons titled in Spanish]:::imm
    imm_les --> imm_ex[Lesson body fully in Spanish,<br/>simple level-appropriate language]:::imm
  end
```

Native mode is the safer default — beginners benefit from explanations they fully understand. Immersive mode works for higher-CEFR levels where the student can already parse the target language well enough to learn *in* it.

## Per-lesson follow-up

After the plan is saved, lesson content generation uses the same three language fields per-lesson — see [lesson-content-language.md](lesson-content-language.md). The branching is consistent across the curriculum and content templates.

## Failure modes

- **`useNativeLanguage=false` with very low CEFR** — the LLM may still fall back to English because the target-language vocabulary at A1 is too limited to express plan-level concepts. The plan ends up bilingual. Acceptable; the user can re-prompt with native mode.
- **Missing `nativeLanguage` or `languageToLearn`** — Pydantic accepts them as optional, the template renders the literal placeholder text "(not provided)". The lesson plan still generates but with weaker context. The Angular form requires both for `lessonType=Language`, so this only happens via direct API calls.
- **Same value in both fields** — e.g. nativeLanguage=Spanish, languageToLearn=Spanish. Not validated; the LLM produces a confused plan. Edge case not worth special-casing.
