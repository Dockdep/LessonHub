import os
import sys
import yaml
import pytest
from unittest.mock import MagicMock

# Add project root to path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from factories.template_manager import TemplateManager

def test_exercise_generation_rendering():
    """Test that exercise generation templates render correctly."""
    tm = TemplateManager()
    
    contexts = [
        {"agent_type": "Technical", "review_content": None},
        {"agent_type": "Language", "review_content": "Some feedback", "native_language": "Spanish"},
        {"agent_type": "Default", "review_content": None, "comment": "Add more challenge"}
    ]
    
    for ctx in contexts:
        agent_type = ctx["agent_type"]
        template_path = tm.get_task_template("exercise_generation", agent_type if agent_type in ["Technical", "Language"] else "Default")
        
        rendered = tm.render(
            template_path,
            review_content=ctx["review_content"],
            lesson_number=1,
            lesson_name="Test Lesson",
            lesson_topic="Testing",
            lesson_description="Testing templates",
            key_points="Point A, Point B",
            comment=ctx.get("comment"),
            difficulty="Hard",
            native_language=ctx.get("native_language")
        )
        assert len(rendered) > 0
        assert "Test Lesson" in rendered

def test_agent_creation_logic():
    """Test the decentralized agent creation logic with mocked dependencies."""

    import agents.utils
    import agents.lesson_planner_agent as planner
    import agents.content_writer_agent as writer

    class MockAgent:
        def __init__(self, role, goal, backstory, **kwargs):
            self.role = role
            self.goal = goal
            self.backstory = backstory

    def mock_create_agent_from_template(llm, agent_role, agent_type, backstory_suffix=""):
        tm = TemplateManager()
        template_path = tm.get_agent_template(agent_role, agent_type if agent_type in ["Technical", "Language"] else "Default")
        rendered = tm.render(template_path)
        config_data = yaml.safe_load(rendered)

        return MockAgent(
            role=config_data.get('role', ''),
            goal=config_data.get('goal', ''),
            backstory=config_data.get('backstory', '') + backstory_suffix
        )

    # Patch every module that imported the helper directly via `from agents.utils import _create_agent_from_template`.
    # If we only patch agents.utils, modules that already bound a local reference at import time keep using the original.
    originals = {
        "utils": agents.utils._create_agent_from_template,
        "planner": getattr(planner, "_create_agent_from_template", None),
        "writer": getattr(writer, "_create_agent_from_template", None),
    }
    agents.utils._create_agent_from_template = mock_create_agent_from_template
    if originals["planner"] is not None:
        planner._create_agent_from_template = mock_create_agent_from_template
    if originals["writer"] is not None:
        writer._create_agent_from_template = mock_create_agent_from_template

    try:
        llm = None
        curriculum_agent = planner.create_curriculum_agent(llm, "Technical")
        assert "Technical" in curriculum_agent.role

        content_agent = writer.create_content_writer(llm, "Technical")
        assert "Technical" in content_agent.role or "Senior" in content_agent.role
    finally:
        agents.utils._create_agent_from_template = originals["utils"]
        if originals["planner"] is not None:
            planner._create_agent_from_template = originals["planner"]
        if originals["writer"] is not None:
            writer._create_agent_from_template = originals["writer"]
