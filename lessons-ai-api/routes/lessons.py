"""Lesson-generation endpoints — plan, content, exercise (generate / retry / review),
resources. All six share the `_resolve_language` boundary helper that picks the
rendering language from the per-type language fields on the request."""
import uuid
from fastapi import APIRouter

from models.requests import (
    LessonPlanRequest, LessonContentRequest, LessonExerciseRequest,
    ExerciseRetryRequest, ExerciseReviewRequest, LessonResourcesRequest,
)
from models.responses import (
    LessonPlanResponse, LessonContentResponse, LessonExerciseResponse,
    ExerciseReviewResponse, LessonResourcesResponse,
)
from models.contexts import ExerciseSpec, LessonContext, PlanContext
from services import CurriculumService, ContentService, ExerciseService, ResearchService

router = APIRouter()


def _resolve_language(
    lesson_type: str,
    language: str | None,
    native_language: str | None,
    language_to_learn: str | None,
    use_native_language: bool,
) -> str | None:
    """Pick the rendering language for the lesson output based on lesson type.

    For Language lessons the rendering language is one of (native, target)
    chosen by the user's `useNativeLanguage` flag. For Default/Technical the
    plain `language` field is the answer; we fall back to `native_language`
    for backwards compatibility (the Angular form sends `nativeLanguage` for
    every lesson type today).
    """
    if lesson_type == "Language":
        return native_language if use_native_language else language_to_learn
    return language or native_language


@router.post("/api/lesson-plan/generate", response_model=LessonPlanResponse)
async def generate_lesson_plan(request: LessonPlanRequest) -> LessonPlanResponse:
    """Generates a lesson plan with a specified or auto-determined number of lessons."""
    plan = PlanContext(
        topic=request.topic,
        description=request.description or "",
        agent_type=request.lesson_type,
        language=_resolve_language(
            request.lesson_type, request.language, request.native_language,
            request.language_to_learn, request.use_native_language,
        ),
        native_language=request.native_language,
        language_to_learn=request.language_to_learn,
        use_native_language=request.use_native_language,
        document_id=request.document_id,
    )
    response = await CurriculumService.generate_plan(
        plan=plan,
        number_of_lessons=request.number_of_lessons,
        google_api_key=request.google_api_key,
        bypass_doc_cache=request.bypass_doc_cache,
    )
    response.correlation_id = request.correlation_id or str(uuid.uuid4())
    return response


@router.post("/api/lesson-content/generate", response_model=LessonContentResponse)
async def generate_lesson_content(request: LessonContentRequest) -> LessonContentResponse:
    """Generates detailed educational content for a specific lesson."""
    plan = PlanContext(
        topic=request.topic,
        description=request.plan_description,
        agent_type=request.lesson_type,
        language=_resolve_language(
            request.lesson_type, request.language, request.native_language,
            request.language_to_learn, request.use_native_language,
        ),
        native_language=request.native_language,
        language_to_learn=request.language_to_learn,
        use_native_language=request.use_native_language,
        document_id=request.document_id,
    )
    lesson = LessonContext(
        number=request.lesson_number,
        name=request.lesson_name,
        topic=request.lesson_topic,
        description=request.lesson_description,
        key_points=request.key_points,
        previous=request.previous_lesson,
        next=request.next_lesson,
    )
    response = await ContentService.generate_content(
        plan=plan,
        lesson=lesson,
        google_api_key=request.google_api_key,
        bypass_doc_cache=request.bypass_doc_cache,
    )
    response.correlation_id = request.correlation_id or str(uuid.uuid4())
    return response


@router.post("/api/lesson-exercise/generate", response_model=LessonExerciseResponse)
async def generate_lesson_exercise(request: LessonExerciseRequest) -> LessonExerciseResponse:
    """Generates practical exercises for a specific lesson based on its content."""
    plan = PlanContext(
        agent_type=request.lesson_type,
        language=_resolve_language(
            request.lesson_type, None, request.native_language,
            request.language_to_learn, request.use_native_language,
        ),
        native_language=request.native_language,
        language_to_learn=request.language_to_learn,
        use_native_language=request.use_native_language,
        document_id=request.document_id,
    )
    lesson = LessonContext(
        number=request.lesson_number,
        name=request.lesson_name,
        topic=request.lesson_topic,
        description=request.lesson_description,
        key_points=request.key_points,
        previous=request.previous_lesson,
        next=request.next_lesson,
    )
    spec = ExerciseSpec(difficulty=request.difficulty, comment=request.comment)
    response = await ExerciseService.generate_exercise(
        plan=plan,
        lesson=lesson,
        spec=spec,
        google_api_key=request.google_api_key,
    )
    response.correlation_id = request.correlation_id or str(uuid.uuid4())
    return response


@router.post("/api/lesson-exercise/retry", response_model=LessonExerciseResponse)
async def retry_lesson_exercise(request: ExerciseRetryRequest) -> LessonExerciseResponse:
    """Generates a new exercise based on feedback from a previous exercise review."""
    plan = PlanContext(
        agent_type=request.lesson_type,
        language=_resolve_language(
            request.lesson_type, None, request.native_language,
            request.language_to_learn, request.use_native_language,
        ),
        native_language=request.native_language,
        language_to_learn=request.language_to_learn,
        use_native_language=request.use_native_language,
        document_id=request.document_id,
    )
    lesson = LessonContext(
        number=request.lesson_number,
        name=request.lesson_name,
        topic=request.lesson_topic,
        description=request.lesson_description,
        key_points=request.key_points,
        previous=request.previous_lesson,
        next=request.next_lesson,
    )
    spec = ExerciseSpec(
        difficulty=request.difficulty,
        comment=request.comment,
        review=request.review,
    )
    response = await ExerciseService.retry_exercise(
        plan=plan,
        lesson=lesson,
        spec=spec,
        google_api_key=request.google_api_key,
    )
    response.correlation_id = request.correlation_id or str(uuid.uuid4())
    return response


@router.post("/api/exercise-review/check", response_model=ExerciseReviewResponse)
async def check_exercise_review(request: ExerciseReviewRequest) -> ExerciseReviewResponse:
    """Evaluates a student's exercise response and provides accuracy score and feedback."""
    plan = PlanContext(
        agent_type=request.lesson_type,
        language=_resolve_language(
            request.lesson_type, request.language, request.native_language,
            request.language_to_learn, request.use_native_language,
        ),
        native_language=request.native_language,
        language_to_learn=request.language_to_learn,
        use_native_language=request.use_native_language,
    )
    response = await ExerciseService.review_exercise(
        plan=plan,
        lesson_content=request.lesson_content,
        exercise_content=request.exercise_content,
        difficulty=request.difficulty,
        answer=request.answer,
        google_api_key=request.google_api_key,
    )
    response.correlation_id = request.correlation_id or str(uuid.uuid4())
    return response


@router.post("/api/lesson-resources/generate", response_model=LessonResourcesResponse)
async def generate_lesson_resources(request: LessonResourcesRequest) -> LessonResourcesResponse:
    """Finds YouTube videos and books/documentation for a specific lesson."""
    plan = PlanContext(
        topic=request.topic,
        agent_type=request.lesson_type,
        language=request.language,
    )
    lesson = LessonContext(
        name=request.lesson_name,
        topic=request.lesson_topic,
        description=request.lesson_description,
    )
    response = await ResearchService.generate_resources(
        plan=plan,
        lesson=lesson,
        google_api_key=request.google_api_key,
    )
    response.correlation_id = request.correlation_id or str(uuid.uuid4())
    return response
