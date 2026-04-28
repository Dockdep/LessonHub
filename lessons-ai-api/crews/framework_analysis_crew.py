"""One-shot crew: lesson context -> list of search queries.

Tiny by design. No quality-loop wrapping (the output is internal — the writer
sees the eventual search results, not these queries directly), no docs
injection, no retries beyond what CrewAI does internally.
"""
import logging
from crewai import Crew, Process, LLM

from agents.framework_analyzer_agent import create_framework_analyzer
from tasks.framework_analysis_tasks import create_framework_analysis_task
from models.contexts import LessonContext, PlanContext

logger = logging.getLogger(__name__)

# Hard cap so a runaway analyzer can't trigger 50 DDG calls.
MAX_QUERIES = 5


async def analyze_for_search_queries(
    llm: LLM,
    plan: PlanContext,
    lesson: LessonContext | None,
) -> list[str]:
    """Run the analyzer and return its search queries (or [] on any failure).

    Failing soft is intentional — if the analyzer breaks, content generation
    proceeds ungrounded rather than crashing.
    """
    try:
        analyzer = create_framework_analyzer(llm)
        task = create_framework_analysis_task(plan=plan, lesson=lesson, agent=analyzer)
        crew = Crew(
            agents=[analyzer],
            tasks=[task],
            process=Process.sequential,
            verbose=True,
        )
        result = await crew.akickoff()
        analysis = result.pydantic
        if analysis is None or not getattr(analysis, "search_queries", None):
            return []

        # Dedupe, trim, lowercase, cap.
        seen: set[str] = set()
        out: list[str] = []
        for q in analysis.search_queries:
            cleaned = (q or "").strip()
            if not cleaned:
                continue
            key = cleaned.lower()
            if key in seen:
                continue
            seen.add(key)
            out.append(cleaned)
            if len(out) >= MAX_QUERIES:
                break
        return out
    except Exception as e:
        logger.warning("framework analysis failed, proceeding ungrounded: %s", e)
        return []
