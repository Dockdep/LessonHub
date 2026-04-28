from config import create_exercise_llm, create_review_llm
from crews.exercise_crew import run_exercise_crew, run_exercise_retry_crew
from crews.review_crew import run_exercise_review_crew
from models.contexts import ExerciseSpec, LessonContext, PlanContext
from models.responses import LessonExerciseResponse, ExerciseReviewResponse


class ExerciseService:
    @staticmethod
    async def generate_exercise(
        plan: PlanContext,
        lesson: LessonContext,
        spec: ExerciseSpec,
        google_api_key: str | None = None,
    ) -> LessonExerciseResponse:
        llm = create_exercise_llm(api_key=google_api_key)
        return await run_exercise_crew(
            llm=llm,
            plan=plan,
            lesson=lesson,
            spec=spec,
            google_api_key=google_api_key,
        )

    @staticmethod
    async def retry_exercise(
        plan: PlanContext,
        lesson: LessonContext,
        spec: ExerciseSpec,
        google_api_key: str | None = None,
    ) -> LessonExerciseResponse:
        """spec.review must be set."""
        llm = create_exercise_llm(api_key=google_api_key)
        return await run_exercise_retry_crew(
            llm=llm,
            plan=plan,
            lesson=lesson,
            spec=spec,
            google_api_key=google_api_key,
        )

    @staticmethod
    async def review_exercise(
        plan: PlanContext,
        lesson_content: str,
        exercise_content: str,
        difficulty: str,
        answer: str,
        google_api_key: str | None = None,
    ) -> ExerciseReviewResponse:
        llm = create_review_llm(api_key=google_api_key)
        return await run_exercise_review_crew(
            llm=llm,
            plan=plan,
            lesson_content=lesson_content,
            exercise_content=exercise_content,
            difficulty=difficulty,
            answer=answer,
            google_api_key=google_api_key,
        )
