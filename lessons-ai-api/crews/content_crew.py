import time
from crewai import Crew, Process, LLM

from agents.content_writer_agent import create_content_writer
from tasks.content_generation_tasks import create_content_generation_task
from models.contexts import LessonContext, PlanContext
from models.responses import LessonContentResponse, ModelUsage
from config import create_quality_llm, settings
from crews.quality_crew import run_quality_check
from crews.framework_analysis_crew import analyze_for_search_queries
from tools.documentation_search import (
    format_docs_for_prompt,
    format_sources_section,
    search_for_queries,
)
from tools.rag_embedder import embed_query
from tools.rag_store import search as rag_search
from tools.document_context import format_chunks_for_lesson


async def run_content_crew(
    llm: LLM,
    plan: PlanContext,
    lesson: LessonContext,
    google_api_key: str | None = None,
    bypass_doc_cache: bool = False,
) -> LessonContentResponse:
    """Execute a crew to generate lesson content with quality checking."""
    all_usage = []
    quality_llm = create_quality_llm(api_key=google_api_key)

    # For Technical lessons: ask an analyzer agent what to search for, then
    # run those queries against DDG. Same docs are passed to the validator so
    # it grounds its accuracy check against the same sources.
    docs: list[dict] = []
    if plan.agent_type == "Technical":
        analyzer_llm = create_quality_llm(api_key=google_api_key)
        queries = await analyze_for_search_queries(analyzer_llm, plan, lesson=lesson)
        if queries:
            docs = await search_for_queries(queries, bypass_cache=bypass_doc_cache)

    # If the user attached a source document, RAG-search it for the most
    # relevant chunks for this specific lesson and pass them as
    # document_context — orthogonal to lesson type, so any of
    # Default/Technical/Language can be document-grounded.
    document_context = ""
    if plan.document_id and google_api_key:
        query_text = " ".join(filter(None, [lesson.topic, lesson.name, lesson.description]))
        if query_text.strip():
            try:
                query_vec = await embed_query(query_text, api_key=google_api_key)
                hits = await rag_search(plan.document_id, query_vec, top_k=settings.rag_top_k_per_lesson)
                document_context = format_chunks_for_lesson(hits)
            except Exception:
                # Fail open — the writer can still produce content from the lesson plan.
                document_context = ""

    original_request = (
        f"Generate {plan.agent_type} lesson content for topic: '{plan.topic}'. "
        f"Lesson #{lesson.number}: '{lesson.name}' — {lesson.description}. "
        f"Key points: {', '.join(lesson.key_points)}. Language: {plan.language or 'English'}."
    )

    # The plan description gets quality feedback appended between attempts;
    # we mutate a copy rather than the caller's PlanContext.
    current_plan = PlanContext(
        topic=plan.topic,
        description=plan.description,
        agent_type=plan.agent_type,
        language=plan.language,
        document_id=plan.document_id,
    )

    for attempt in range(settings.max_quality_retries + 1):
        content_writer = create_content_writer(llm, plan.agent_type)

        task = create_content_generation_task(
            plan=current_plan,
            lesson=lesson,
            agent=content_writer,
            document_context=document_context,
        )

        # Inject pre-fetched framework docs. Document chunks are rendered
        # inline by the task template (via _document_context.jinja2).
        docs_block = format_docs_for_prompt(docs)
        if docs_block:
            task.description = f"{task.description}\n\n{docs_block}"

        crew = Crew(
            agents=[content_writer],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        all_usage.append(ModelUsage(
            request_type="content_generation",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP"),
        ))

        generated_content = str(result)

        # Quality check — pass docs so the validator grounds against the same sources.
        quality, quality_usage = await run_quality_check(
            llm=quality_llm,
            generation_type="lesson content",
            original_request=original_request,
            generated_result=generated_content,
            doc_sources=docs,
        )
        quality.retries = attempt
        all_usage.append(quality_usage)

        if quality.passed or attempt == settings.max_quality_retries:
            # Append a Sources section listing the docs the writer was given.
            final_content = generated_content + format_sources_section(docs)
            return LessonContentResponse(
                lesson_number=lesson.number,
                lesson_name=lesson.name,
                content=final_content,
                quality_check=quality,
                usage=all_usage,
            )

        # Append shortcomings to plan description for next attempt
        current_plan.description = (
            f"{current_plan.description}\n\n"
            f"[QUALITY FEEDBACK - Attempt {attempt + 1}]: {quality.shortcomings}"
        )
