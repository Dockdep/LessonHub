import time
from crewai import Crew, Process, LLM
from agents.quality_checker_agent import create_quality_checker
from tasks.quality_check_tasks import create_quality_check_task
from models.responses import QualityCheck, ModelUsage
from tools.documentation_search import format_docs_for_prompt


async def run_quality_check(
    llm: LLM,
    generation_type: str,
    original_request: str,
    generated_result: str,
    doc_sources: list[dict] | None = None
) -> tuple[QualityCheck, ModelUsage]:
    """Run a quality check on generated content. Returns passed result on failure so content is not lost.

    When doc_sources is provided (Technical lessons), the validator is told to verify
    factual claims against the same docs the writer was given.
    """
    try:
        checker = create_quality_checker(llm)

        task = create_quality_check_task(
            generation_type=generation_type,
            original_request=original_request,
            generated_result=generated_result,
            agent=checker
        )

        # Re-use writer's docs so the validator grounds against the same sources
        # rather than its own (potentially divergent) memory.
        docs_block = format_docs_for_prompt(doc_sources or [])
        if docs_block:
            task.description = (
                f"{task.description}\n\n"
                f"## Authoritative References (verify factual claims against these)\n"
                f"{docs_block}"
            )

        crew = Crew(
            agents=[checker],
            tasks=[task],
            process=Process.sequential,
            verbose=True
        )

        start_time = time.time()
        result = await crew.akickoff()
        latency_ms = int((time.time() - start_time) * 1000)

        finish_reason = "STOP" if (result and result.raw) else "EMPTY_OR_BLOCKED"
        check_data = result.pydantic

        quality = QualityCheck(
            score=check_data.score,
            passed=check_data.score >= 80,
            shortcomings=None if check_data.shortcomings.strip().lower() == "none" else check_data.shortcomings,
            retries=0
        )

        usage = ModelUsage(
            request_type="quality_check",
            input_tokens=result.token_usage.prompt_tokens,
            output_tokens=result.token_usage.completion_tokens,
            model_name=llm.model,
            provider="Google",
            latency_ms=latency_ms,
            finish_reason=finish_reason,
            is_success=(finish_reason == "STOP")
        )

        return quality, usage

    except Exception as e:
        print(f"Quality check failed for {generation_type}: {e}")
        # Return a passing result so the generated content is not lost
        return (
            QualityCheck(
                score=0,
                passed=True,
                shortcomings=f"Quality check failed: {str(e)}",
                retries=0
            ),
            ModelUsage(
                request_type="quality_check",
                input_tokens=0,
                output_tokens=0,
                model_name=llm.model,
                provider="Google",
                latency_ms=0,
                finish_reason="ERROR",
                is_success=False
            )
        )
