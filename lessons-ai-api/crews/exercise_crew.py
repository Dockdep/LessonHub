import time
from crewai import Crew, Process, LLM

from agents.exercise_creator_agent import create_exercise_creator
from tasks.exercise_generation_tasks import create_exercise_generation_task, create_exercise_retry_task
from models.contexts import ExerciseSpec, LessonContext, PlanContext
from models.responses import LessonExerciseResponse, ModelUsage
from config import create_quality_llm, settings
from crews.quality_crew import run_quality_check
from tools.rag_embedder import embed_query
from tools.rag_store import search as rag_search
from tools.document_context import format_chunks_for_lesson


async def _fetch_document_context(
    plan: PlanContext,
    lesson: LessonContext,
    google_api_key: str | None,
) -> str:
    """RAG-search the attached document for chunks relevant to this lesson.

    Returns '' if no document is attached, no API key is available, or the
    search fails. Failing open keeps exercise generation working even when
    embedding rate-limits or transient pgvector errors occur.
    """
    if not (plan.document_id and google_api_key):
        return ""
    query_text = " ".join(filter(None, [lesson.topic, lesson.name, lesson.description]))
    if not query_text.strip():
        return ""
    try:
        query_vec = await embed_query(query_text, api_key=google_api_key)
        hits = await rag_search(plan.document_id, query_vec, top_k=settings.rag_top_k_per_lesson)
        return format_chunks_for_lesson(hits)
    except Exception:
        return ""


async def run_exercise_crew(
    llm: LLM,
    plan: PlanContext,
    lesson: LessonContext,
    spec: ExerciseSpec,
    google_api_key: str | None = None,
) -> LessonExerciseResponse:
    """Execute a crew to generate an exercise with quality checking."""
    all_usage = []
    quality_llm = create_quality_llm(api_key=google_api_key)

    document_context = await _fetch_document_context(plan, lesson, google_api_key)

    original_request = (
        f"Generate a {spec.difficulty} {plan.agent_type} exercise for lesson #{lesson.number}: '{lesson.name}'. "
        f"Topic: {lesson.topic}. Key points: {', '.join(lesson.key_points)}. "
        f"Comment: {spec.comment or 'N/A'}."
    )

    # spec.comment gets quality feedback appended between attempts; mutate a
    # copy so the caller's spec stays clean.
    current_spec = ExerciseSpec(
        difficulty=spec.difficulty, comment=spec.comment, review=spec.review
    )

    for attempt in range(settings.max_quality_retries + 1):
        exercise_creator = create_exercise_creator(llm, plan.agent_type)

        task = create_exercise_generation_task(
            plan=plan,
            lesson=lesson,
            spec=current_spec,
            agent=exercise_creator,
            document_context=document_context,
        )

        crew = Crew(
            agents=[exercise_creator],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        all_usage.append(ModelUsage(
            request_type="exercise_generation",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP"),
        ))

        generated_exercise = str(result)

        # Quality check
        quality, quality_usage = await run_quality_check(
            llm=quality_llm,
            generation_type="exercise",
            original_request=original_request,
            generated_result=generated_exercise,
        )
        quality.retries = attempt
        all_usage.append(quality_usage)

        if quality.passed or attempt == settings.max_quality_retries:
            return LessonExerciseResponse(
                lesson_number=lesson.number,
                lesson_name=lesson.name,
                exercise=generated_exercise,
                quality_check=quality,
                usage=all_usage,
            )

        # Append shortcomings to comment for next attempt
        current_spec.comment = (
            f"{current_spec.comment or ''}\n\n"
            f"[QUALITY FEEDBACK - Attempt {attempt + 1}]: {quality.shortcomings}"
        ).strip()


async def run_exercise_retry_crew(
    llm: LLM,
    plan: PlanContext,
    lesson: LessonContext,
    spec: ExerciseSpec,
    google_api_key: str | None = None,
) -> LessonExerciseResponse:
    """Execute a crew to generate a new exercise based on feedback, with quality checking.
    spec.review must be set."""
    all_usage = []
    quality_llm = create_quality_llm(api_key=google_api_key)

    document_context = await _fetch_document_context(plan, lesson, google_api_key)

    original_request = (
        f"Regenerate a {spec.difficulty} {plan.agent_type} exercise for lesson #{lesson.number}: '{lesson.name}'. "
        f"Topic: {lesson.topic}. Key points: {', '.join(lesson.key_points)}. "
        f"Previous review: {spec.review}. Comment: {spec.comment or 'N/A'}."
    )

    current_spec = ExerciseSpec(
        difficulty=spec.difficulty, comment=spec.comment, review=spec.review
    )

    for attempt in range(settings.max_quality_retries + 1):
        exercise_creator = create_exercise_creator(llm, plan.agent_type)

        task = create_exercise_retry_task(
            plan=plan,
            lesson=lesson,
            spec=current_spec,
            agent=exercise_creator,
            document_context=document_context,
        )

        crew = Crew(
            agents=[exercise_creator],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        all_usage.append(ModelUsage(
            request_type="exercise_retry",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP"),
        ))

        generated_exercise = str(result)

        # Quality check
        quality, quality_usage = await run_quality_check(
            llm=quality_llm,
            generation_type="exercise retry",
            original_request=original_request,
            generated_result=generated_exercise,
        )
        quality.retries = attempt
        all_usage.append(quality_usage)

        if quality.passed or attempt == settings.max_quality_retries:
            return LessonExerciseResponse(
                lesson_number=lesson.number,
                lesson_name=lesson.name,
                exercise=generated_exercise,
                quality_check=quality,
                usage=all_usage,
            )

        # Append shortcomings to comment for next attempt
        current_spec.comment = (
            f"{current_spec.comment or ''}\n\n"
            f"[QUALITY FEEDBACK - Attempt {attempt + 1}]: {quality.shortcomings}"
        ).strip()
