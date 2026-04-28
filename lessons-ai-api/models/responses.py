from pydantic import BaseModel, Field


class ModelUsage(BaseModel):
    """Token usage record for a single model call."""
    request_type: str = Field(..., alias="requestType")
    input_tokens: int = Field(..., alias="inputTokens")
    output_tokens: int = Field(..., alias="outputTokens")
    model_name: str = Field(..., alias="modelName")
    provider: str
    latency_ms: int = Field(..., alias="latencyMs")
    finish_reason: str = Field(..., alias="finishReason")
    is_success: bool = Field(..., alias="isSuccess")

    model_config = {"populate_by_name": True, "by_alias": True}


class QualityCheck(BaseModel):
    """Quality check result for generated content."""
    score: int = Field(..., description="Quality score from 0 to 100")
    passed: bool = Field(..., description="Whether the quality check passed (score >= threshold)")
    shortcomings: str | None = Field(None, description="Description of shortcomings if any")
    retries: int = Field(0, description="Number of regeneration attempts made")

    model_config = {"populate_by_name": True, "by_alias": True}


class LessonItem(BaseModel):
    """A single lesson in a lesson plan."""
    lesson_number: int = Field(..., alias="lessonNumber")
    name: str
    short_description: str = Field(..., alias="shortDescription")
    lesson_topic: str = Field(..., alias="lessonTopic")
    key_points: list[str] = Field(..., alias="keyPoints")

    model_config = {"populate_by_name": True, "by_alias": True}


class LessonPlanResponse(BaseModel):
    """Response model for a generated lesson plan."""
    correlation_id: str | None = Field(None, alias="correlationId")
    topic: str
    lessons: list[LessonItem]
    quality_check: QualityCheck | None = Field(None, alias="qualityCheck")
    usage: list[ModelUsage] = Field(default_factory=list)

    model_config = {"populate_by_name": True, "by_alias": True}


class LessonContentResponse(BaseModel):
    """Response model for generated lesson content."""
    correlation_id: str | None = Field(None, alias="correlationId")
    lesson_number: int = Field(..., alias="lessonNumber")
    lesson_name: str = Field(..., alias="lessonName")
    content: str
    quality_check: QualityCheck | None = Field(None, alias="qualityCheck")
    usage: list[ModelUsage] = Field(default_factory=list)

    model_config = {"populate_by_name": True, "by_alias": True}


class LessonExerciseResponse(BaseModel):
    """Response model for generated lesson exercises."""
    correlation_id: str | None = Field(None, alias="correlationId")
    lesson_number: int = Field(..., alias="lessonNumber")
    lesson_name: str = Field(..., alias="lessonName")
    exercise: str
    quality_check: QualityCheck | None = Field(None, alias="qualityCheck")
    usage: list[ModelUsage] = Field(default_factory=list)

    model_config = {"populate_by_name": True, "by_alias": True}


class ExerciseReviewResponse(BaseModel):
    """Response model for exercise review/evaluation."""
    correlation_id: str | None = Field(None, alias="correlationId")
    accuracy_level: int = Field(..., alias="accuracyLevel", description="Score from 0 to 100")
    exam_review: str = Field(..., alias="examReview", description="Detailed feedback")
    quality_check: QualityCheck | None = Field(None, alias="qualityCheck")
    usage: list[ModelUsage] = Field(default_factory=list)

    model_config = {"populate_by_name": True, "by_alias": True}


class VideoItem(BaseModel):
    """A YouTube video recommendation."""
    title: str
    channel: str
    description: str
    url: str

    model_config = {"populate_by_name": True, "by_alias": True}


class BookItem(BaseModel):
    """A book recommendation with structured fields."""
    author: str
    book_name: str = Field(..., alias="bookName")
    chapter_number: int | None = Field(None, alias="chapterNumber")
    chapter_name: str | None = Field(None, alias="chapterName")
    description: str

    model_config = {"populate_by_name": True, "by_alias": True}


class DocumentationItem(BaseModel):
    """A documentation resource recommendation."""
    name: str
    section: str | None = None
    description: str
    url: str

    model_config = {"populate_by_name": True, "by_alias": True}


class LessonResourcesResponse(BaseModel):
    """Response model for lesson resources (videos, books, and docs)."""
    correlation_id: str | None = Field(None, alias="correlationId")
    lesson_name: str = Field(..., alias="lessonName")
    topic: str
    videos: list[VideoItem]
    books: list[BookItem]
    documentation: list[DocumentationItem]
    quality_check: QualityCheck | None = Field(None, alias="qualityCheck")
    usage: list[ModelUsage] = Field(default_factory=list)

    model_config = {"populate_by_name": True, "by_alias": True}


# -- RAG (Phase 1) ----------------------------------------------------------

class RagIngestResponse(BaseModel):
    """Outcome of an ingest call — how the document landed in pgvector."""
    document_id: str = Field(..., alias="documentId")
    chunk_count: int = Field(..., alias="chunkCount")

    model_config = {"populate_by_name": True, "by_alias": True}


class RagSearchHit(BaseModel):
    """A single chunk returned from a search."""
    chunk_index: int = Field(..., alias="chunkIndex")
    header_path: str = Field("", alias="headerPath")
    text: str
    score: float = Field(..., description="Cosine similarity, 0..1; higher is more similar.")

    model_config = {"populate_by_name": True, "by_alias": True}


class RagSearchResponse(BaseModel):
    document_id: str = Field(..., alias="documentId")
    hits: list[RagSearchHit] = Field(default_factory=list)

    model_config = {"populate_by_name": True, "by_alias": True}
