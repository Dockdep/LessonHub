from config import create_content_llm
from crews.content_crew import run_content_crew
from models.contexts import LessonContext, PlanContext
from models.responses import LessonContentResponse


class ContentService:
    @staticmethod
    async def generate_content(
        plan: PlanContext,
        lesson: LessonContext,
        google_api_key: str | None = None,
        bypass_doc_cache: bool = False,
    ) -> LessonContentResponse:
        llm = create_content_llm(api_key=google_api_key)
        return await run_content_crew(
            llm=llm,
            plan=plan,
            lesson=lesson,
            google_api_key=google_api_key,
            bypass_doc_cache=bypass_doc_cache,
        )
