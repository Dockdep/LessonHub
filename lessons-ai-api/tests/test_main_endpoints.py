"""End-to-end tests for the FastAPI endpoints with all heavy services mocked.

We're not exercising CrewAI here — that would require real (paid) LLM calls.
Instead we verify the wiring: the request reaches the right service, the
google_api_key from the body is forwarded, and the response is shaped correctly.
"""
import os
import sys
from unittest.mock import AsyncMock, patch

import pytest
from fastapi.testclient import TestClient

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import main  # noqa: E402
from routes import lessons as lessons_routes  # noqa: E402
from models.responses import (  # noqa: E402
    LessonPlanResponse,
    LessonContentResponse,
    LessonExerciseResponse,
    ExerciseReviewResponse,
    LessonResourcesResponse,
)


@pytest.fixture
def client():
    return TestClient(main.app)


class TestHealthEndpoint:
    def test_returns_healthy(self, client):
        r = client.get("/health")
        assert r.status_code == 200
        assert r.json() == {"status": "healthy"}


class TestLessonPlanEndpoint:
    def test_forwards_googleApiKey_to_service(self, client):
        fake_response = LessonPlanResponse(topic="x", lessons=[])

        with patch.object(lessons_routes.CurriculumService, "generate_plan",
                          new_callable=AsyncMock, return_value=fake_response) as mock_gen:
            r = client.post("/api/lesson-plan/generate", json={
                "lessonType": "Technical",
                "topic": "Python",
                "numberOfLessons": 3,
                "googleApiKey": "user-key-from-dotnet",
            })

        assert r.status_code == 200
        mock_gen.assert_awaited_once()
        kwargs = mock_gen.await_args.kwargs
        assert kwargs["google_api_key"] == "user-key-from-dotnet"
        assert kwargs["plan"].topic == "Python"
        assert kwargs["plan"].agent_type == "Technical"
        assert kwargs["number_of_lessons"] == 3

    def test_response_includes_correlation_id(self, client):
        fake_response = LessonPlanResponse(topic="x", lessons=[])
        with patch.object(lessons_routes.CurriculumService, "generate_plan",
                          new_callable=AsyncMock, return_value=fake_response):
            r = client.post("/api/lesson-plan/generate", json={
                "lessonType": "T", "topic": "x", "googleApiKey": "k"
            })
        assert r.status_code == 200
        assert r.json().get("correlationId")


class TestLessonContentEndpoint:
    def test_forwards_googleApiKey(self, client):
        fake_response = LessonContentResponse(lesson_number=1, lesson_name="L", content="body")
        with patch.object(lessons_routes.ContentService, "generate_content",
                          new_callable=AsyncMock, return_value=fake_response) as mock_gen:
            r = client.post("/api/lesson-content/generate", json={
                "topic": "x", "lessonType": "T", "lessonTopic": "lt", "keyPoints": [],
                "planDescription": "pd", "lessonNumber": 1, "lessonName": "ln",
                "lessonDescription": "ld", "googleApiKey": "borrower-key"
            })
        assert r.status_code == 200
        assert mock_gen.await_args.kwargs["google_api_key"] == "borrower-key"


class TestLessonExerciseEndpoints:
    def test_generate_forwards_googleApiKey(self, client):
        fake_response = LessonExerciseResponse(lesson_number=1, lesson_name="L", exercise="ex")
        with patch.object(lessons_routes.ExerciseService, "generate_exercise",
                          new_callable=AsyncMock, return_value=fake_response) as mock_gen:
            r = client.post("/api/lesson-exercise/generate", json={
                "lessonType": "T", "lessonTopic": "lt", "lessonNumber": 1, "lessonName": "ln",
                "lessonDescription": "ld", "keyPoints": [], "difficulty": "easy",
                "googleApiKey": "k1"
            })
        assert r.status_code == 200
        assert mock_gen.await_args.kwargs["google_api_key"] == "k1"

    def test_retry_forwards_googleApiKey(self, client):
        fake_response = LessonExerciseResponse(lesson_number=1, lesson_name="L", exercise="ex")
        with patch.object(lessons_routes.ExerciseService, "retry_exercise",
                          new_callable=AsyncMock, return_value=fake_response) as mock_gen:
            r = client.post("/api/lesson-exercise/retry", json={
                "lessonType": "T", "lessonTopic": "lt", "lessonNumber": 1, "lessonName": "ln",
                "lessonDescription": "ld", "keyPoints": [], "difficulty": "easy", "review": "r",
                "googleApiKey": "k2"
            })
        assert r.status_code == 200
        assert mock_gen.await_args.kwargs["google_api_key"] == "k2"


class TestExerciseReviewEndpoint:
    def test_forwards_googleApiKey(self, client):
        fake_response = ExerciseReviewResponse(accuracy_level=80, exam_review="ok")
        with patch.object(lessons_routes.ExerciseService, "review_exercise",
                          new_callable=AsyncMock, return_value=fake_response) as mock_gen:
            r = client.post("/api/exercise-review/check", json={
                "lessonType": "T", "lessonContent": "lc", "exerciseContent": "ec",
                "difficulty": "easy", "answer": "a", "googleApiKey": "k"
            })
        assert r.status_code == 200
        assert mock_gen.await_args.kwargs["google_api_key"] == "k"


class TestLessonResourcesEndpoint:
    def test_forwards_googleApiKey(self, client):
        fake_response = LessonResourcesResponse(
            lesson_name="ln", topic="x", videos=[], books=[], documentation=[]
        )
        with patch.object(lessons_routes.ResearchService, "generate_resources",
                          new_callable=AsyncMock, return_value=fake_response) as mock_gen:
            r = client.post("/api/lesson-resources/generate", json={
                "lessonType": "T", "topic": "x", "lessonName": "ln", "lessonTopic": "lt",
                "lessonDescription": "ld", "googleApiKey": "k"
            })
        assert r.status_code == 200
        assert mock_gen.await_args.kwargs["google_api_key"] == "k"


class TestErrorHandling:
    def test_value_error_returns_400(self, client):
        async def boom(**kwargs):
            raise ValueError("bad input")

        with patch.object(lessons_routes.CurriculumService, "generate_plan", side_effect=boom):
            r = client.post("/api/lesson-plan/generate", json={
                "lessonType": "T", "topic": "x", "googleApiKey": "k"
            })
        assert r.status_code == 400
        assert "bad input" in r.json()["detail"]

    def test_unexpected_exception_returns_500(self):
        # The general exception handler kicks in; TestClient re-raises by default
        # so we need a client with raise_server_exceptions disabled.
        client = TestClient(main.app, raise_server_exceptions=False)

        async def boom(**kwargs):
            raise RuntimeError("kaboom")

        with patch.object(lessons_routes.CurriculumService, "generate_plan", side_effect=boom):
            r = client.post("/api/lesson-plan/generate", json={
                "lessonType": "T", "topic": "x", "googleApiKey": "k"
            })
        assert r.status_code == 500
