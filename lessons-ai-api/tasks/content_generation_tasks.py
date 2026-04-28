from crewai import Agent, Task

from models.contexts import LessonContext, PlanContext
from factories.template_manager import TemplateManager
from config import settings


def create_content_generation_task(
    plan: PlanContext,
    lesson: LessonContext,
    agent: Agent,
    document_context: str = "",
) -> Task:
    """Create a task to generate detailed lesson content based on agent type."""
    language = plan.language or settings.default_language
    tm = TemplateManager()
    template_path = tm.get_task_template(
        "content_generation",
        plan.agent_type if plan.agent_type in ["Technical", "Language"] else "Default",
    )

    description_rendered = tm.render(
        template_path,
        topic=plan.topic,
        lesson_topic=lesson.topic,
        key_points=', '.join(lesson.key_points),
        plan_description=plan.description,
        lesson_number=lesson.number,
        lesson_name=lesson.name,
        lesson_description=lesson.description,
        language=language,
        native_language=plan.native_language,
        language_to_learn=plan.language_to_learn,
        use_native_language=plan.use_native_language,
        previous_lesson=lesson.previous,
        next_lesson=lesson.next,
        document_context=document_context,
    )

    return Task(
        description=description_rendered.strip(),
        expected_output=f"A comprehensive Markdown document for lesson {lesson.number}.",
        agent=agent,
    )
