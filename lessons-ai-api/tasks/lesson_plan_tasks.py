from crewai import Agent, Task
from pydantic import BaseModel, Field
from typing import List

from models.contexts import PlanContext


class Lesson(BaseModel):
    lessonNumber: int = Field(..., description="The sequential number of the lesson.")
    name: str = Field(..., description="A catchy and descriptive title.")
    shortDescription: str = Field(..., description="2-3 sentences summarizing the goals.")
    lessonTopic: str = Field(..., description="The specific sub-topic covered.")
    keyPoints: List[str] = Field(..., description="The 3-5 specific bullets to cover.")


class LessonPlan(BaseModel):
    topic: str
    lessons: List[Lesson]


from factories.template_manager import TemplateManager
from config import settings


def create_lesson_plan_task_with_count(
    plan: PlanContext,
    number_of_lessons: int,
    agent: Agent,
    document_context: str = "",
) -> Task:
    """Create a task to generate a lesson plan with a specific number of lessons."""
    language = plan.language or settings.default_language
    tm = TemplateManager()
    template_path = tm.get_task_template(
        "lesson_plan",
        plan.agent_type if plan.agent_type in ["Technical", "Language"] else "Default",
    )

    description_rendered = tm.render(
        template_path,
        topic=plan.topic,
        number_of_lessons=number_of_lessons,
        description=plan.description,
        language=language,
        native_language=plan.native_language,
        language_to_learn=plan.language_to_learn,
        use_native_language=plan.use_native_language,
        document_context=document_context,
    )

    return Task(
        description=description_rendered.strip(),
        expected_output=f"A structured lesson plan with exactly {number_of_lessons} lessons including key points and learning outcomes.",
        output_pydantic=LessonPlan,
        agent=agent,
    )


def create_lesson_plan_task_auto_count(
    plan: PlanContext,
    agent: Agent,
    document_context: str = "",
) -> Task:
    """Create a task to generate a lesson plan with AI-determined number of lessons."""
    language = plan.language or settings.default_language
    tm = TemplateManager()
    template_path = tm.get_task_template(
        "lesson_plan",
        plan.agent_type if plan.agent_type in ["Technical", "Language"] else "Default",
    )

    description_rendered = tm.render(
        template_path,
        topic=plan.topic,
        number_of_lessons=None,
        description=plan.description,
        language=language,
        native_language=plan.native_language,
        language_to_learn=plan.language_to_learn,
        use_native_language=plan.use_native_language,
        document_context=document_context,
    )

    return Task(
        description=description_rendered.strip(),
        expected_output="A comprehensive, naturally sequenced lesson plan with key points and learning outcomes.",
        output_pydantic=LessonPlan,
        agent=agent,
    )
