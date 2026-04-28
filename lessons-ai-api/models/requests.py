from typing import List
from pydantic import BaseModel, Field


class AdjacentLesson(BaseModel):
    """Summary of a neighbouring lesson for continuity context."""
    name: str
    description: str

    model_config = {"populate_by_name": True}


class LessonPlanRequest(BaseModel):
    """Request model for generating a lesson plan."""
    lesson_type: str = Field(..., alias="lessonType")
    topic: str
    number_of_lessons: int | None = Field(None, alias="numberOfLessons")
    description: str | None = None
    language: str | None = Field(None, description="Default/Technical: language to write lesson content in. For Language lessons, see native_language / language_to_learn.")
    native_language: str | None = Field(None, alias="nativeLanguage", description="Language lessons: user's mother tongue.")
    language_to_learn: str | None = Field(None, alias="languageToLearn", description="Language lessons: target language being studied.")
    use_native_language: bool = Field(True, alias="useNativeLanguage", description="Language lessons: when True, render in native; when False, immerse in language_to_learn.")
    correlation_id: str | None = Field(None, alias="correlationId")
    google_api_key: str | None = Field(None, alias="googleApiKey")
    bypass_doc_cache: bool = Field(False, alias="bypassDocCache")
    document_id: str | None = Field(None, alias="documentId", description="Optional .NET Documents.Id whose embedded chunks ground the plan; orthogonal to lesson_type.")

    model_config = {"populate_by_name": True}


class LessonContentRequest(BaseModel):
    """Request model for generating lesson content."""
    topic: str
    lesson_type: str = Field(..., alias="lessonType")
    lesson_topic: str = Field(..., alias="lessonTopic")
    key_points: List[str]= Field(..., alias="keyPoints")
    plan_description: str = Field(..., alias="planDescription")
    lesson_number: int = Field(..., alias="lessonNumber")
    lesson_name: str = Field(..., alias="lessonName")
    lesson_description: str = Field(..., alias="lessonDescription")
    language: str | None = Field(None, description="Default/Technical: rendering language. For Language lessons, see native_language / language_to_learn.")
    native_language: str | None = Field(None, alias="nativeLanguage")
    language_to_learn: str | None = Field(None, alias="languageToLearn")
    use_native_language: bool = Field(True, alias="useNativeLanguage")
    previous_lesson: AdjacentLesson | None = Field(None, alias="previousLesson")
    next_lesson: AdjacentLesson | None = Field(None, alias="nextLesson")
    correlation_id: str | None = Field(None, alias="correlationId")
    google_api_key: str | None = Field(None, alias="googleApiKey")
    bypass_doc_cache: bool = Field(False, alias="bypassDocCache")
    document_id: str | None = Field(None, alias="documentId")

    model_config = {"populate_by_name": True}


class LessonExerciseRequest(BaseModel):
    """Request model for generating lesson exercises."""
    lesson_type: str = Field(..., alias="lessonType")
    lesson_topic: str = Field(..., alias="lessonTopic")
    lesson_number: int = Field(..., alias="lessonNumber")
    lesson_name: str = Field(..., alias="lessonName")
    lesson_description: str = Field(..., alias="lessonDescription")
    key_points: List[str] = Field(..., alias="keyPoints")
    difficulty: str = Field(..., description="Exercise difficulty: easy, medium, or hard")
    comment: str | None = Field(None, description="Optional comment from the user to guide exercise generation")
    native_language: str | None = Field(None, alias="nativeLanguage", description="Language lessons: user's mother tongue.")
    language_to_learn: str | None = Field(None, alias="languageToLearn", description="Language lessons: target language.")
    use_native_language: bool = Field(True, alias="useNativeLanguage", description="Language lessons: render in native vs. immersive in target.")
    previous_lesson: AdjacentLesson | None = Field(None, alias="previousLesson")
    next_lesson: AdjacentLesson | None = Field(None, alias="nextLesson")
    correlation_id: str | None = Field(None, alias="correlationId")
    google_api_key: str | None = Field(None, alias="googleApiKey")
    bypass_doc_cache: bool = Field(False, alias="bypassDocCache")
    document_id: str | None = Field(None, alias="documentId")

    model_config = {"populate_by_name": True}


class ExerciseReviewRequest(BaseModel):
    """Request model for reviewing/checking an exercise response."""
    lesson_type: str = Field(..., alias="lessonType")
    lesson_content: str = Field(..., alias="lessonContent")
    exercise_content: str = Field(..., alias="exerciseContent")
    difficulty: str = Field(..., description="Exercise difficulty: easy, medium, or hard")
    answer: str = Field(..., description="The student's answer to the exercise")
    language: str | None = Field(None, description="Default/Technical: language for review feedback. For Language lessons, see native_language / language_to_learn.")
    native_language: str | None = Field(None, alias="nativeLanguage")
    language_to_learn: str | None = Field(None, alias="languageToLearn")
    use_native_language: bool = Field(True, alias="useNativeLanguage")
    correlation_id: str | None = Field(None, alias="correlationId")
    google_api_key: str | None = Field(None, alias="googleApiKey")
    bypass_doc_cache: bool = Field(False, alias="bypassDocCache")

    model_config = {"populate_by_name": True}


class ExerciseRetryRequest(BaseModel):
    """Request model for generating a new exercise based on a previous exercise review."""
    lesson_type: str = Field(..., alias="lessonType")
    lesson_topic: str = Field(..., alias="lessonTopic")
    lesson_number: int = Field(..., alias="lessonNumber")
    lesson_name: str = Field(..., alias="lessonName")
    lesson_description: str = Field(..., alias="lessonDescription")
    key_points: List[str] = Field(..., alias="keyPoints")
    difficulty: str = Field(..., description="Exercise difficulty: easy, medium, or hard")
    review: str = Field(..., description="The review/feedback from the previous exercise attempt")
    comment: str | None = Field(None, description="Optional comment from the user to guide exercise generation")
    native_language: str | None = Field(None, alias="nativeLanguage", description="Language lessons: user's mother tongue.")
    language_to_learn: str | None = Field(None, alias="languageToLearn", description="Language lessons: target language.")
    use_native_language: bool = Field(True, alias="useNativeLanguage", description="Language lessons: render in native vs. immersive in target.")
    previous_lesson: AdjacentLesson | None = Field(None, alias="previousLesson")
    next_lesson: AdjacentLesson | None = Field(None, alias="nextLesson")
    correlation_id: str | None = Field(None, alias="correlationId")
    google_api_key: str | None = Field(None, alias="googleApiKey")
    bypass_doc_cache: bool = Field(False, alias="bypassDocCache")
    document_id: str | None = Field(None, alias="documentId")

    model_config = {"populate_by_name": True}


class LessonResourcesRequest(BaseModel):
    """Request model for finding lesson resources (videos and books/docs)."""
    lesson_type: str = Field(..., alias="lessonType")
    topic: str
    lesson_name: str = Field(..., alias="lessonName")
    lesson_topic: str = Field(..., alias="lessonTopic")
    lesson_description: str = Field(..., alias="lessonDescription")
    language: str | None = Field(None, description="Language to write resource descriptions in (e.g. 'English', 'Spanish')")
    correlation_id: str | None = Field(None, alias="correlationId")
    google_api_key: str | None = Field(None, alias="googleApiKey")
    bypass_doc_cache: bool = Field(False, alias="bypassDocCache")

    model_config = {"populate_by_name": True}


# -- RAG (Phase 1) ----------------------------------------------------------

class RagIngestRequest(BaseModel):
    """Chunk + embed + store a document. The .NET service has already saved
    the file somewhere (GCS or local), and tells us where via `document_uri`."""
    document_id: str = Field(..., alias="documentId", description=".NET's Documents.Id, used as the partition key in pgvector.")
    document_uri: str = Field(..., alias="documentUri", description="gs://... or file:// URI the Python service can read directly.")
    is_markdown: bool = Field(True, alias="isMarkdown", description="When True, chunker splits on markdown headings first.")
    google_api_key: str = Field(..., alias="googleApiKey", description="User's Gemini API key — they pay for embedding their own docs.")

    model_config = {"populate_by_name": True}


class RagSearchRequest(BaseModel):
    """Find the top-k most relevant chunks of a document for a query."""
    document_id: str = Field(..., alias="documentId")
    query: str
    top_k: int = Field(5, alias="topK", ge=1, le=50)
    google_api_key: str = Field(..., alias="googleApiKey")

    model_config = {"populate_by_name": True}
