from crewai import Agent, Task
from pydantic import BaseModel, Field
from typing import List

from models.contexts import LessonContext, PlanContext


class FrameworkAnalysis(BaseModel):
    """Structured output: the search queries the writer should ground on."""
    search_queries: List[str] = Field(
        ...,
        description="3-5 concrete web search queries, each biased toward official documentation.",
    )


def create_framework_analysis_task(
    plan: PlanContext,
    lesson: LessonContext | None,
    agent: Agent,
) -> Task:
    """Build a one-shot task that returns search queries to ground a Technical lesson.

    `lesson=None` is the plan-level path (curriculum crew). With a lesson set, the
    queries narrow to the specific lesson's topic + key points (content crew).
    """
    lesson_block = ""
    if lesson is not None:
        key_points = ", ".join(lesson.key_points) if lesson.key_points else "(none provided)"
        lesson_block = (
            f"\n\n## Specific Lesson\n"
            f"- Lesson #{lesson.number}: {lesson.name}\n"
            f"- Lesson topic: {lesson.topic}\n"
            f"- Lesson description: {lesson.description}\n"
            f"- Key points: {key_points}\n"
        )

    description = f"""You are about to write educational material on the topic below. \
Before you write anything, list the web search queries you would run to find official \
documentation for the frameworks and concepts you will need.

## Plan
- Topic: {plan.topic}
- Description: {plan.description}
- Language for the lesson output: {plan.language or "English"}{lesson_block}

## Rules for Your Queries
1. Return between 1 and 5 queries — fewer is fine if the topic is small.
2. Each query MUST target official documentation. When you know the framework's \
official doc host (e.g. angular.dev, react.dev, fastapi.tiangolo.com, vuejs.org, \
docs.python.org, doc.rust-lang.org, go.dev, developer.mozilla.org, \
typescriptlang.org, kubernetes.io), include `site:<host>` in the query.
3. If you don't know a canonical host for a particular framework, append \
`official documentation` to the query instead.
4. Pick the SPECIFIC sub-topic the lesson covers, not the framework as a whole. \
Bad: `"angular site:angular.dev"`. Good: `"angular standalone components site:angular.dev"`.
5. No duplicate queries. Lowercase preferred.
6. If the topic is non-technical or you can't think of any reasonable doc query, \
return an empty list.

Return JSON with one field `search_queries`: a list of strings.
"""

    return Task(
        description=description,
        expected_output="ONLY a JSON object with one field `search_queries` (list of strings). No other text.",
        output_pydantic=FrameworkAnalysis,
        agent=agent,
    )
