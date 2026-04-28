from crewai import Agent, Task
from pydantic import BaseModel, Field
from typing import List

from models.contexts import LessonContext, PlanContext
from config import settings

YOUTUBE_VIDEOS_LIMIT = settings.youtube_videos_limit
BOOKS_LIMIT = settings.books_limit
DOCUMENTATION_LIMIT = settings.documentation_limit


class YouTubeVideo(BaseModel):
    title: str = Field(..., description="Video title from search results")
    channel: str = Field(..., description="Channel name from search results")
    description: str = Field(..., description="Why this video is relevant to the lesson")
    url: str = Field(..., description="Real YouTube video URL")


class YouTubeSearchResult(BaseModel):
    videos: List[YouTubeVideo] = Field(..., description="List of relevant YouTube videos")


class Book(BaseModel):
    author: str = Field(..., description="Author name")
    bookName: str = Field(..., description="Book title")
    chapterNumber: int | None = Field(None, description="Chapter number if applicable")
    chapterName: str | None = Field(None, description="Chapter name if applicable")
    description: str = Field(..., description="Why this book is relevant to the lesson")


class Documentation(BaseModel):
    name: str = Field(..., description="Documentation name")
    section: str | None = Field(None, description="Section or topic name")
    description: str = Field(..., description="Why this documentation is relevant")
    url: str = Field(..., description="Official documentation URL")


class ResourceSearchResult(BaseModel):
    books: List[Book] = Field(..., description="List of relevant books")
    documentation: List[Documentation] = Field(..., description="List of relevant documentation")


from factories.template_manager import TemplateManager


def create_youtube_research_task(
    plan: PlanContext,
    lesson: LessonContext,
    agent: Agent,
) -> Task:
    """Create a task to find relevant YouTube videos for a lesson."""
    language = plan.language or settings.default_language
    tm = TemplateManager()
    description_rendered = tm.render(
        "tasks/resource_research_youtube.jinja2",
        topic=plan.topic,
        lesson_name=lesson.name,
        lesson_topic=lesson.topic,
        lesson_description=lesson.description,
        limit=YOUTUBE_VIDEOS_LIMIT,
        language=language,
    )

    return Task(
        description=description_rendered.strip(),
        expected_output=f"A structured result with exactly {YOUTUBE_VIDEOS_LIMIT} YouTube videos including title, channel, description, and URL.",
        output_pydantic=YouTubeSearchResult,
        agent=agent,
    )


def create_resource_research_task(
    plan: PlanContext,
    lesson: LessonContext,
    agent: Agent,
) -> Task:
    """Create a task to find relevant books and documentation for a lesson."""
    language = plan.language or settings.default_language
    tm = TemplateManager()
    template_path = tm.get_task_template(
        "resource_research",
        plan.agent_type if plan.agent_type in ["Technical", "Language"] else "Default",
    )

    description_rendered = tm.render(
        template_path,
        topic=plan.topic,
        lesson_name=lesson.name,
        lesson_topic=lesson.topic,
        lesson_description=lesson.description,
        books_limit=BOOKS_LIMIT,
        documentation_limit=DOCUMENTATION_LIMIT,
        language=language,
    )

    return Task(
        description=description_rendered.strip(),
        expected_output=f"A structured result with exactly {BOOKS_LIMIT} books and {DOCUMENTATION_LIMIT} documentation resources.",
        output_pydantic=ResourceSearchResult,
        agent=agent,
    )
