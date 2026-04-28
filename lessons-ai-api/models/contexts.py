"""Internal context dataclasses passed through the task -> crew -> service stack.

These are NOT HTTP DTOs (those live in models/requests.py with aliases for
camelCase JSON). The boundary at main.py constructs these from the validated
Pydantic request models so lower layers don't depend on FastAPI/Pydantic.

Three groupings, picked from the natural clusters in the existing call sites:

- PlanContext  — what plan/course this lesson lives in
- LessonContext — the specific lesson being generated/retried/etc.
- ExerciseSpec — per-exercise generation knobs (difficulty + comment + retry review)
"""
from dataclasses import dataclass, field
from typing import List, Optional

from models.requests import AdjacentLesson


@dataclass
class PlanContext:
    """Plan/course-level fields that surround the current lesson.

    Language fields:
    - For Default/Technical: only `language` is meaningful — the language the
      lesson is written in. `language_to_learn` and `use_native_language` are
      ignored.
    - For Language lessons: `native_language` is the user's mother tongue,
      `language_to_learn` is the target language. `use_native_language`
      decides which one the lesson is rendered in. `language` is set at the
      boundary in main.py to the rendering language so existing templates
      that consume `{{ language }}` keep working.
    """
    topic: str = ""
    description: str = ""
    agent_type: str = "Default"  # "Technical" | "Language" | "Default"
    language: Optional[str] = None  # rendering language for the lesson output
    native_language: Optional[str] = None  # Language lessons only
    language_to_learn: Optional[str] = None  # Language lessons only
    use_native_language: bool = True  # Language lessons only; default native-mode
    document_id: Optional[str] = None


@dataclass
class LessonContext:
    """Identifies a specific lesson and its place in the sequence."""
    number: int = 0
    name: str = ""
    topic: str = ""
    description: str = ""
    key_points: List[str] = field(default_factory=list)
    previous: Optional[AdjacentLesson] = None
    next: Optional[AdjacentLesson] = None


@dataclass
class ExerciseSpec:
    """Exercise generation parameters. `review` is set on retry, None otherwise."""
    difficulty: str = "medium"
    comment: Optional[str] = None
    review: Optional[str] = None
