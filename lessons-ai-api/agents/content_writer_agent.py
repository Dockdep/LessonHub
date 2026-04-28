from crewai import Agent, LLM
from agents.utils import _create_agent_from_template


def create_content_writer(llm: LLM, agent_type: str = "Default") -> Agent:
    """Create a specialized content writer agent based on the topic type."""
    return _create_agent_from_template(
        llm,
        "content_writer",
        agent_type,
        backstory_suffix=" You focus on clarity and cognitive load management."
    )
