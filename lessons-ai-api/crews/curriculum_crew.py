import time
from crewai import Crew, Process, LLM

from agents.lesson_planner_agent import create_curriculum_agent
from tasks.lesson_plan_tasks import (
    create_lesson_plan_task_with_count,
    create_lesson_plan_task_auto_count,
)
from models.contexts import PlanContext
from models.responses import LessonPlanResponse, LessonItem, ModelUsage
from config import create_quality_llm, settings
from crews.quality_crew import run_quality_check
from crews.framework_analysis_crew import analyze_for_search_queries
from tools.documentation_search import format_docs_for_prompt, search_for_queries
from tools.rag_store import list_chunks
from tools.document_context import format_outline_for_plan


async def run_curriculum_crew(
    llm: LLM,
    plan: PlanContext,
    number_of_lessons: int | None,
    google_api_key: str | None = None,
    bypass_doc_cache: bool = False,
) -> LessonPlanResponse:
    """Execute a crew to generate a lesson plan with quality checking."""
    all_usage = []
    quality_llm = create_quality_llm(api_key=google_api_key)

    # For Technical lessons: ask an analyzer agent what to search for, then
    # run those queries against DDG. Same docs are passed to the validator so
    # it grounds its accuracy check against the same sources.
    docs: list[dict] = []
    if plan.agent_type == "Technical":
        analyzer_llm = create_quality_llm(api_key=google_api_key)
        queries = await analyze_for_search_queries(analyzer_llm, plan, lesson=None)
        if queries:
            docs = await search_for_queries(queries, bypass_cache=bypass_doc_cache)

    # If the user attached a source document, fetch its outline once and pass
    # it as document_context to the task template — orthogonal to lesson type,
    # so any of Default/Technical/Language can be document-grounded.
    document_context = ""
    if plan.document_id:
        chunks = await list_chunks(plan.document_id)
        document_context = format_outline_for_plan(chunks)

    original_request = (
        f"Generate a {plan.agent_type} lesson plan for topic: '{plan.topic}'. "
        f"Number of lessons: {number_of_lessons or 'auto-determine'}. "
        f"Description: {plan.description or 'N/A'}. Language: {plan.language or 'English'}."
    )

    # description gets feedback appended between attempts; mutate a copy.
    current_plan = PlanContext(
        topic=plan.topic,
        description=plan.description,
        agent_type=plan.agent_type,
        language=plan.language,
        document_id=plan.document_id,
    )

    for attempt in range(settings.max_quality_retries + 1):
        curriculum_designer = create_curriculum_agent(llm, plan.agent_type)

        if number_of_lessons and number_of_lessons > 0:
            task = create_lesson_plan_task_with_count(
                plan=current_plan,
                number_of_lessons=number_of_lessons,
                agent=curriculum_designer,
                document_context=document_context,
            )
        else:
            task = create_lesson_plan_task_auto_count(
                plan=current_plan,
                agent=curriculum_designer,
                document_context=document_context,
            )

        # Append fetched framework docs to the agent's task description. The
        # document outline is rendered inline by the task template (via
        # _document_context.jinja2), not appended afterwards.
        docs_block = format_docs_for_prompt(docs)
        if docs_block:
            task.description = f"{task.description}\n\n{docs_block}"

        crew = Crew(
            agents=[curriculum_designer],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        all_usage.append(ModelUsage(
            request_type="lesson_plan",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP"),
        ))

        plan_data = result.pydantic
        generated_result = str(result)

        # Quality check — pass docs so the validator grounds against the same sources.
        quality, quality_usage = await run_quality_check(
            llm=quality_llm,
            generation_type="lesson plan",
            original_request=original_request,
            generated_result=generated_result,
            doc_sources=docs,
        )
        quality.retries = attempt
        all_usage.append(quality_usage)

        if quality.passed or attempt == settings.max_quality_retries:
            lessons = [
                LessonItem(
                    lesson_number=lesson.lessonNumber,
                    name=lesson.name,
                    short_description=lesson.shortDescription,
                    lesson_topic=lesson.lessonTopic,
                    key_points=lesson.keyPoints,
                )
                for lesson in plan_data.lessons
            ]
            return LessonPlanResponse(
                topic=plan_data.topic,
                lessons=lessons,
                quality_check=quality,
                usage=all_usage,
            )

        # Append shortcomings to description for next attempt
        current_plan.description = (
            f"{current_plan.description}\n\n"
            f"[QUALITY FEEDBACK - Attempt {attempt + 1}]: {quality.shortcomings}"
        )
