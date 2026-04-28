"""Tests for config._resolve_api_key and the create_*_llm factories.

The factories instantiate `crewai.LLM`, which we patch out so we can assert how
the resolver chooses between caller-supplied and env-configured keys without
contacting any real provider.
"""
import os
import sys
from unittest.mock import patch

import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import config  # noqa: E402
from config import _resolve_api_key  # noqa: E402


class TestResolveApiKey:
    def test_returns_caller_key_when_provided(self):
        config.settings.google_api_key = "env-key"
        assert _resolve_api_key("caller-key") == "caller-key"

    def test_falls_back_to_env_when_caller_key_missing(self):
        config.settings.google_api_key = "env-key"
        assert _resolve_api_key(None) == "env-key"
        assert _resolve_api_key("") == "env-key"

    def test_raises_when_neither_caller_nor_env_key_present(self):
        config.settings.google_api_key = ""
        with pytest.raises(ValueError, match="Google API key"):
            _resolve_api_key(None)

    def test_caller_key_wins_over_env(self):
        config.settings.google_api_key = "env-key"
        assert _resolve_api_key("caller-wins") == "caller-wins"


class TestCreateLlmFactories:
    """Each factory must thread the resolved api_key into the LLM constructor."""

    @pytest.mark.parametrize("factory_name,settings_model_attr,settings_temp_attr", [
        ("create_plan_llm", "plan_model", "plan_temperature"),
        ("create_content_llm", "content_model", "content_temperature"),
        ("create_exercise_llm", "exercise_model", "exercise_temperature"),
        ("create_review_llm", "review_model", "review_temperature"),
        ("create_research_llm", "research_model", "research_temperature"),
        ("create_quality_llm", "quality_model", "quality_temperature"),
    ])
    def test_factory_passes_caller_key_to_llm(self, factory_name, settings_model_attr, settings_temp_attr):
        with patch.object(config, "LLM") as mock_llm:
            getattr(config, factory_name)(api_key="user-key-123")
            mock_llm.assert_called_once_with(
                model=getattr(config.settings, settings_model_attr),
                api_key="user-key-123",
                temperature=getattr(config.settings, settings_temp_attr),
            )

    def test_factory_falls_back_to_env_when_no_caller_key(self):
        config.settings.google_api_key = "env-fallback"
        with patch.object(config, "LLM") as mock_llm:
            config.create_plan_llm()
            kwargs = mock_llm.call_args.kwargs
            assert kwargs["api_key"] == "env-fallback"

    def test_factory_raises_when_no_key_anywhere(self):
        config.settings.google_api_key = ""
        with patch.object(config, "LLM"):
            with pytest.raises(ValueError, match="Google API key"):
                config.create_plan_llm()
