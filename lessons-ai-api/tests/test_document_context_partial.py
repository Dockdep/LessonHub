"""Verify each per-type task template includes the shared document-context
partial. The partial is the only way RAG-grounded text reaches the agent
prompt now that the Document lesson type has been removed — if a template
loses its `{% include %}` line, the document grounding silently disappears
even though the .NET request still sets documentId."""
from crewai import Agent

from agents.lesson_planner_agent import create_curriculum_agent
from agents.content_writer_agent import create_content_writer
from agents.exercise_creator_agent import create_exercise_creator
from config import create_plan_llm
from models.contexts import ExerciseSpec, LessonContext, PlanContext
from tasks.lesson_plan_tasks import create_lesson_plan_task_with_count
from tasks.content_generation_tasks import create_content_generation_task
from tasks.exercise_generation_tasks import (
    create_exercise_generation_task,
    create_exercise_retry_task,
)


# Marker text from `templates/_document_context.jinja2`. If a template stops
# including the partial, this string won't appear in the rendered prompt.
PARTIAL_MARKER = "Source Document — Use as Primary Source of Information"

# A blob the partial echoes back verbatim so we can confirm the content
# (not just the framing) reached the prompt.
DOC_BODY = "### Chapter 1\n\nThe quick brown fox jumps over the lazy dog."


def _agent() -> Agent:
    """A lightweight agent stub. crewai.Task() requires an agent reference,
    but we never run the crew — we only inspect the rendered description."""
    return create_curriculum_agent(create_plan_llm(api_key="fake"), "Default")


class TestLessonPlanTemplate:
    """Plan-level template covers Default + Technical + Language variants
    via the agent_type parameter."""

    def test_partial_renders_when_document_context_set(self):
        for agent_type in ("Default", "Technical", "Language"):
            plan = PlanContext(
                topic="topic", description="desc", agent_type=agent_type, language="English"
            )
            task = create_lesson_plan_task_with_count(
                plan=plan, number_of_lessons=3, agent=_agent(),
                document_context=DOC_BODY,
            )
            assert PARTIAL_MARKER in task.description, f"missing in {agent_type}"
            assert DOC_BODY in task.description, f"body missing in {agent_type}"

    def test_partial_is_silent_when_no_document(self):
        plan = PlanContext(
            topic="topic", description="desc", agent_type="Default", language="English"
        )
        task = create_lesson_plan_task_with_count(
            plan=plan, number_of_lessons=3, agent=_agent(),
        )
        # No document attached → no source-of-truth framing leaks into the prompt.
        assert PARTIAL_MARKER not in task.description


class TestContentTemplate:
    def test_partial_renders_when_document_context_set(self):
        writer = create_content_writer(create_plan_llm(api_key="fake"), "Default")
        for agent_type in ("Default", "Technical", "Language"):
            plan = PlanContext(topic="topic", description="pd", agent_type=agent_type)
            lesson = LessonContext(
                number=1, name="ln", topic="lt", description="ld", key_points=["a", "b"]
            )
            task = create_content_generation_task(
                plan=plan, lesson=lesson, agent=writer, document_context=DOC_BODY,
            )
            assert PARTIAL_MARKER in task.description, f"missing in {agent_type}"
            assert DOC_BODY in task.description

    def test_partial_silent_without_document(self):
        writer = create_content_writer(create_plan_llm(api_key="fake"), "Default")
        plan = PlanContext(topic="topic", description="pd", agent_type="Default")
        lesson = LessonContext(
            number=1, name="ln", topic="lt", description="ld", key_points=["a"]
        )
        task = create_content_generation_task(plan=plan, lesson=lesson, agent=writer)
        assert PARTIAL_MARKER not in task.description


class TestExerciseTemplate:
    def test_partial_renders_for_generate_and_retry(self):
        creator = create_exercise_creator(create_plan_llm(api_key="fake"), "Default")
        lesson = LessonContext(
            number=1, name="ln", topic="lt", description="ld", key_points=["a"]
        )
        # Generate
        gen = create_exercise_generation_task(
            plan=PlanContext(agent_type="Technical"),
            lesson=lesson,
            spec=ExerciseSpec(difficulty="medium"),
            agent=creator,
            document_context=DOC_BODY,
        )
        assert PARTIAL_MARKER in gen.description
        assert DOC_BODY in gen.description

        # Retry path uses the same _create_base_exercise_task so should
        # also include the partial.
        retry = create_exercise_retry_task(
            plan=PlanContext(agent_type="Language"),
            lesson=lesson,
            spec=ExerciseSpec(difficulty="hard", review="be more concise"),
            agent=creator,
            document_context=DOC_BODY,
        )
        assert PARTIAL_MARKER in retry.description
        assert DOC_BODY in retry.description
