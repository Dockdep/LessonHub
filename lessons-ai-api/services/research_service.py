from config import create_research_llm
from crews.research_crew import run_resources_crew
from models.contexts import LessonContext, PlanContext
from models.responses import LessonResourcesResponse


class ResearchService:
    @staticmethod
    async def generate_resources(
        plan: PlanContext,
        lesson: LessonContext,
        google_api_key: str | None = None,
    ) -> LessonResourcesResponse:
        llm = create_research_llm(api_key=google_api_key)
        return await run_resources_crew(
            llm=llm,
            plan=plan,
            lesson=lesson,
            google_api_key=google_api_key,
        )
