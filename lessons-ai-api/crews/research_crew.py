import time
from crewai import Crew, Process, LLM

from agents.youtube_researcher_agent import create_youtube_researcher
from agents.resource_researcher_agent import create_resource_researcher
from tasks.resource_research_tasks import (
    create_youtube_research_task,
    create_resource_research_task,
)
from models.contexts import LessonContext, PlanContext
from models.responses import (
    LessonResourcesResponse, VideoItem, BookItem, DocumentationItem, ModelUsage,
)
from config import create_quality_llm, settings
from crews.quality_crew import run_quality_check


async def run_resources_crew(
    llm: LLM,
    plan: PlanContext,
    lesson: LessonContext,
    google_api_key: str | None = None,
) -> LessonResourcesResponse:
    """Execute a crew to find YouTube videos and books/documentation with quality checking."""
    all_usage = []
    quality_llm = create_quality_llm(api_key=google_api_key)

    original_request = (
        f"Find YouTube videos and books/documentation for {plan.agent_type} lesson: '{lesson.name}'. "
        f"Topic: {plan.topic}. Lesson topic: {lesson.topic}. "
        f"Description: {lesson.description}. Language: {plan.language or 'English'}."
    )

    for attempt in range(settings.max_quality_retries + 1):
        youtube_researcher = create_youtube_researcher(llm)
        resource_researcher = create_resource_researcher(llm, plan.agent_type)

        youtube_task = create_youtube_research_task(
            plan=plan,
            lesson=lesson,
            agent=youtube_researcher,
        )

        resource_task = create_resource_research_task(
            plan=plan,
            lesson=lesson,
            agent=resource_researcher,
        )

        crew = Crew(
            agents=[youtube_researcher, resource_researcher],
            tasks=[youtube_task, resource_task],
            process=Process.sequential,
            verbose=True,
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        all_usage.append(ModelUsage(
            request_type="resource_research",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP"),
        ))

        # Access validated Pydantic objects directly
        youtube_data = youtube_task.output.pydantic
        resource_data = resource_task.output.pydantic

        videos = [
            VideoItem(
                title=video.title,
                channel=video.channel,
                description=video.description,
                url=video.url,
            )
            for video in youtube_data.videos
        ]

        books = [
            BookItem(
                author=book.author,
                book_name=book.bookName,
                chapter_number=book.chapterNumber,
                chapter_name=book.chapterName,
                description=book.description,
            )
            for book in resource_data.books
        ]

        documentation = [
            DocumentationItem(
                name=doc.name,
                section=doc.section,
                description=doc.description,
                url=doc.url,
            )
            for doc in resource_data.documentation
        ]

        generated_result = (
            f"Videos: {[v.title for v in videos]}\n"
            f"Books: {[b.book_name for b in books]}\n"
            f"Documentation: {[d.name for d in documentation]}"
        )

        # Quality check
        quality, quality_usage = await run_quality_check(
            llm=quality_llm,
            generation_type="lesson resources",
            original_request=original_request,
            generated_result=generated_result,
        )
        quality.retries = attempt
        all_usage.append(quality_usage)

        if quality.passed or attempt == settings.max_quality_retries:
            return LessonResourcesResponse(
                lesson_name=lesson.name,
                topic=plan.topic,
                videos=videos,
                books=books,
                documentation=documentation,
                quality_check=quality,
                usage=all_usage,
            )
