import re
import json
from pydantic_settings import BaseSettings, SettingsConfigDict
from crewai import LLM

class Settings(BaseSettings):
    """Application settings using Pydantic Settings."""
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    google_api_key: str = ""
    youtube_videos_limit: int = 2
    books_limit: int = 2
    documentation_limit: int = 1
    default_language: str = "English"

    # Per-task model configuration
    plan_model: str = "gemini/gemini-3.1-pro-preview"
    plan_temperature: float = 0.5
    content_model: str = "gemini/gemini-3-flash-preview"
    content_temperature: float = 0.5
    exercise_model: str = "gemini/gemini-3-flash-preview"
    exercise_temperature: float = 0.5
    review_model: str = "gemini/gemini-3-flash-preview"
    review_temperature: float = 0.5
    research_model: str = "gemini/gemini-3-flash-preview"
    research_temperature: float = 0.5

    # Quality check configuration
    quality_model: str = "gemini/gemini-3.1-flash-lite-preview"
    quality_temperature: float = 0.3
    min_quality_score: int = 80
    max_quality_retries: int = 2

    # Documentation search / cache configuration
    # Postgres connection string used only by the doc-cache helpers (asyncpg format).
    # Optional: when blank the cache is bypassed and every search hits DDG live.
    database_url: str = ""
    # Cache lifetime for analyzer-driven query results.
    # The user can force-refresh via the "Use fresh documentation" button per request.
    doc_cache_ttl_days: int = 30
    doc_search_max_results_per_query: int = 3
    doc_page_max_chars: int = 3500
    doc_fetch_timeout_seconds: int = 8

    # RAG — number of document chunks injected into the content writer's prompt
    # per lesson. Higher = more grounding, more tokens. 5 is a balanced default.
    rag_top_k_per_lesson: int = 5

# Global settings instance
settings = Settings()

def _resolve_api_key(api_key: str | None) -> str:
    """Use the caller-provided key, falling back to the env-configured key for dev."""
    resolved = api_key or settings.google_api_key
    if not resolved:
        raise ValueError("Google API key is missing. Set it in your user profile.")
    return resolved

def create_plan_llm(api_key: str | None = None) -> LLM:
    """Create LLM for lesson plan generation."""
    return LLM(model=settings.plan_model, api_key=_resolve_api_key(api_key), temperature=settings.plan_temperature)

def create_content_llm(api_key: str | None = None) -> LLM:
    """Create LLM for lesson content generation."""
    return LLM(model=settings.content_model, api_key=_resolve_api_key(api_key), temperature=settings.content_temperature)

def create_exercise_llm(api_key: str | None = None) -> LLM:
    """Create LLM for exercise generation."""
    return LLM(model=settings.exercise_model, api_key=_resolve_api_key(api_key), temperature=settings.exercise_temperature)

def create_review_llm(api_key: str | None = None) -> LLM:
    """Create LLM for exercise review."""
    return LLM(model=settings.review_model, api_key=_resolve_api_key(api_key), temperature=settings.review_temperature)

def create_research_llm(api_key: str | None = None) -> LLM:
    """Create LLM for resource research."""
    return LLM(model=settings.research_model, api_key=_resolve_api_key(api_key), temperature=settings.research_temperature)

def create_quality_llm(api_key: str | None = None) -> LLM:
    """Create LLM for quality checking."""
    return LLM(model=settings.quality_model, api_key=_resolve_api_key(api_key), temperature=settings.quality_temperature)


def parse_json_response(response: str) -> dict:
    """
    Parse JSON from LLM response, handling markdown code blocks.
    """
    # Remove markdown code blocks if present
    cleaned = re.sub(r'```json\s*', '', response)
    cleaned = re.sub(r'```\s*', '', cleaned)
    return json.loads(cleaned.strip())
