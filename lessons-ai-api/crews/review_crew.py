import time
from crewai import Crew, Process, LLM

from agents.exercise_reviewer_agent import create_exercise_reviewer
from tasks.exercise_review_tasks import create_exercise_review_task
from models.contexts import PlanContext
from models.responses import ExerciseReviewResponse, ModelUsage
from config import create_quality_llm, settings
from crews.quality_crew import run_quality_check


async def run_exercise_review_crew(
    llm: LLM,
    plan: PlanContext,
    lesson_content: str,
    exercise_content: str,
    difficulty: str,
    answer: str,
    google_api_key: str | None = None,
) -> ExerciseReviewResponse:
    """Execute a crew to evaluate a student's exercise response with quality checking."""
    all_usage = []
    quality_llm = create_quality_llm(api_key=google_api_key)

    original_request = (
        f"Review a student's {difficulty} {plan.agent_type} exercise answer. "
        f"Exercise: {exercise_content[:200]}... "
        f"Student answer: {answer[:200]}... "
        f"Language: {plan.language or 'English'}."
    )

    for attempt in range(settings.max_quality_retries + 1):
        exercise_reviewer = create_exercise_reviewer(llm, plan.agent_type)

        task = create_exercise_review_task(
            plan=plan,
            lesson_content=lesson_content,
            exercise_content=exercise_content,
            difficulty=difficulty,
            answer=answer,
            agent=exercise_reviewer,
        )

        crew = Crew(
            agents=[exercise_reviewer],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        all_usage.append(ModelUsage(
            request_type="exercise_review",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP"),
        ))

        review_data = result.pydantic
        generated_result = f"Accuracy: {review_data.accuracyLevel}/100\nReview: {review_data.examReview}"

        # Quality check
        quality, quality_usage = await run_quality_check(
            llm=quality_llm,
            generation_type="exercise review",
            original_request=original_request,
            generated_result=generated_result,
        )
        quality.retries = attempt
        all_usage.append(quality_usage)

        if quality.passed or attempt == settings.max_quality_retries:
            return ExerciseReviewResponse(
                accuracy_level=review_data.accuracyLevel,
                exam_review=review_data.examReview,
                quality_check=quality,
                usage=all_usage,
            )
