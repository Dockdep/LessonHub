from crewai import Agent, Task

from models.contexts import ExerciseSpec, LessonContext, PlanContext
from factories.template_manager import TemplateManager


def _create_base_exercise_task(
    plan: PlanContext,
    lesson: LessonContext,
    spec: ExerciseSpec,
    agent: Agent,
    document_context: str = "",
) -> Task:
    """
    Internal helper to build the task description for both new and retry exercises.
    spec.review is None for the first attempt and a feedback string on retry.
    """
    tm = TemplateManager()

    template_path = tm.get_task_template(
        "exercise_generation",
        plan.agent_type if plan.agent_type in ["Technical", "Language"] else "Default",
    )

    description = tm.render(
        template_path,
        review_content=spec.review,
        lesson_number=lesson.number,
        lesson_name=lesson.name,
        lesson_topic=lesson.topic,
        lesson_description=lesson.description,
        key_points=', '.join(lesson.key_points),
        comment=spec.comment,
        difficulty=spec.difficulty,
        native_language=plan.native_language or plan.language,
        language_to_learn=plan.language_to_learn,
        use_native_language=plan.use_native_language,
        previous_lesson=lesson.previous,
        next_lesson=lesson.next,
        document_context=document_context,
    )

    return Task(
        description=description.strip(),
        expected_output=(
            f"A Markdown document with a {spec.difficulty}-level {plan.agent_type} "
            f"exercise including a '### Your Response' section."
        ),
        agent=agent,
    )


def create_exercise_generation_task(
    plan: PlanContext,
    lesson: LessonContext,
    spec: ExerciseSpec,
    agent: Agent,
    document_context: str = "",
) -> Task:
    """Creates a standard exercise generation task."""
    # Defensive: ensure no leftover review on the first-attempt path.
    spec_no_review = ExerciseSpec(
        difficulty=spec.difficulty, comment=spec.comment, review=None
    )
    return _create_base_exercise_task(plan, lesson, spec_no_review, agent, document_context)


def create_exercise_retry_task(
    plan: PlanContext,
    lesson: LessonContext,
    spec: ExerciseSpec,
    agent: Agent,
    document_context: str = "",
) -> Task:
    """Creates a retry exercise task based on previous feedback. spec.review must be set."""
    return _create_base_exercise_task(plan, lesson, spec, agent, document_context)
