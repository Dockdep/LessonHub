from crewai import Agent, Task
from pydantic import BaseModel, Field

from models.contexts import PlanContext


class ExerciseReviewResult(BaseModel):
    accuracyLevel: int = Field(..., description="Score from 0 to 100 indicating how correctly the exercise was solved")
    examReview: str = Field(..., description="Detailed feedback explaining mistakes, correct answers, and suggestions")


from factories.template_manager import TemplateManager
from config import settings


def create_exercise_review_task(
    plan: PlanContext,
    lesson_content: str,
    exercise_content: str,
    difficulty: str,
    answer: str,
    agent: Agent,
) -> Task:
    """Create a task to evaluate a user's exercise response."""
    language = plan.language or settings.default_language
    tm = TemplateManager()
    template_path = tm.get_task_template(
        "exercise_review",
        plan.agent_type if plan.agent_type in ["Technical", "Language"] else "Default",
    )

    description_rendered = tm.render(
        template_path,
        lesson_content=lesson_content,
        exercise_content=exercise_content,
        difficulty=difficulty,
        answer=answer,
        language=language,
        native_language=plan.native_language,
        language_to_learn=plan.language_to_learn,
        use_native_language=plan.use_native_language,
    )

    return Task(
        description=description_rendered.strip(),
        expected_output="A structured evaluation, an accuracy score (0-100), and Markdown document containing detailed review feedback.",
        output_pydantic=ExerciseReviewResult,
        agent=agent,
    )
