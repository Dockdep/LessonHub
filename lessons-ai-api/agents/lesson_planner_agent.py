from crewai import Agent, LLM
from agents.utils import _create_agent_from_template


def create_curriculum_agent(llm: LLM, agent_type: str = "Default") -> Agent:
    """Create a curriculum designer agent based on type."""
    return _create_agent_from_template(llm, "curriculum_designer", agent_type)


# Backward compatibility aliases
def create_curriculum_designer(llm: LLM) -> Agent:
    """Create a default curriculum designer agent."""
    return create_curriculum_agent(llm, "Default")


def create_technical_architect(llm: LLM) -> Agent:
    """Create a technical curriculum architect agent."""
    return create_curriculum_agent(llm, "Technical")


def create_language_architect(llm: LLM) -> Agent:
    """Create a linguistic curriculum architect agent."""
    return create_curriculum_agent(llm, "Language")
