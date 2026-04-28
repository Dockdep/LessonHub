from config import create_plan_llm
from crews.curriculum_crew import run_curriculum_crew
from models.contexts import PlanContext
from models.responses import LessonPlanResponse


class CurriculumService:
    @staticmethod
    async def generate_plan(
        plan: PlanContext,
        number_of_lessons: int | None,
        google_api_key: str | None = None,
        bypass_doc_cache: bool = False,
    ) -> LessonPlanResponse:
        llm = create_plan_llm(api_key=google_api_key)
        return await run_curriculum_crew(
            llm=llm,
            plan=plan,
            number_of_lessons=number_of_lessons,
            google_api_key=google_api_key,
            bypass_doc_cache=bypass_doc_cache,
        )
