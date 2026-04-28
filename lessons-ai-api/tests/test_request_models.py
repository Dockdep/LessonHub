"""Tests for the FastAPI request models — alias mapping (camelCase ↔ snake_case)
and the new google_api_key plumbing."""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models.requests import (  # noqa: E402
    LessonPlanRequest,
    LessonContentRequest,
    LessonExerciseRequest,
    ExerciseReviewRequest,
    ExerciseRetryRequest,
    LessonResourcesRequest,
)


class TestLessonPlanRequest:
    def test_accepts_camelCase_aliases_from_dotnet_client(self):
        req = LessonPlanRequest.model_validate({
            "lessonType": "Technical",
            "topic": "Python",
            "numberOfLessons": 5,
            "googleApiKey": "user-key",
        })
        assert req.lesson_type == "Technical"
        assert req.number_of_lessons == 5
        assert req.google_api_key == "user-key"

    def test_google_api_key_optional_default_none(self):
        req = LessonPlanRequest.model_validate({"lessonType": "T", "topic": "x"})
        assert req.google_api_key is None


class TestAllRequestModelsHaveGoogleApiKey:
    """Every AI request model needs the new `google_api_key` field so the .NET
    client can attach the per-user key on every call."""

    @staticmethod
    def _minimal_kwargs(model_cls):
        # Different request models require different fields; build minimal valid input.
        if model_cls is LessonPlanRequest:
            return {"lessonType": "T", "topic": "x"}
        if model_cls is LessonContentRequest:
            return {
                "topic": "x", "lessonType": "T", "lessonTopic": "lt", "keyPoints": [],
                "planDescription": "pd", "lessonNumber": 1, "lessonName": "ln", "lessonDescription": "ld"
            }
        if model_cls is LessonExerciseRequest:
            return {
                "lessonType": "T", "lessonTopic": "lt", "lessonNumber": 1, "lessonName": "ln",
                "lessonDescription": "ld", "keyPoints": [], "difficulty": "easy"
            }
        if model_cls is ExerciseReviewRequest:
            return {
                "lessonType": "T", "lessonContent": "lc", "exerciseContent": "ec",
                "difficulty": "easy", "answer": "a"
            }
        if model_cls is ExerciseRetryRequest:
            return {
                "lessonType": "T", "lessonTopic": "lt", "lessonNumber": 1, "lessonName": "ln",
                "lessonDescription": "ld", "keyPoints": [], "difficulty": "easy", "review": "r"
            }
        if model_cls is LessonResourcesRequest:
            return {
                "lessonType": "T", "topic": "x", "lessonName": "ln", "lessonTopic": "lt",
                "lessonDescription": "ld"
            }
        raise AssertionError(f"unknown model {model_cls}")

    @staticmethod
    def _all_models():
        return [
            LessonPlanRequest, LessonContentRequest, LessonExerciseRequest,
            ExerciseReviewRequest, ExerciseRetryRequest, LessonResourcesRequest,
        ]

    def test_each_model_accepts_googleApiKey_alias(self):
        for cls in self._all_models():
            kwargs = self._minimal_kwargs(cls) | {"googleApiKey": "k"}
            req = cls.model_validate(kwargs)
            assert req.google_api_key == "k", f"{cls.__name__} did not capture googleApiKey"

    def test_each_model_defaults_googleApiKey_to_none(self):
        for cls in self._all_models():
            req = cls.model_validate(self._minimal_kwargs(cls))
            assert req.google_api_key is None, f"{cls.__name__} default for google_api_key should be None"
