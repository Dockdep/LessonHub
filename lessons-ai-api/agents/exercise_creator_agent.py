from crewai import Agent, LLM
from agents.utils import _create_agent_from_template


def create_exercise_creator(llm: LLM, agent_type: str = "Default") -> Agent:
    """Create a specialized exercise creator agent based on the topic type."""
    return _create_agent_from_template(
        llm, 
        "exercise_creator", 
        agent_type, 
        backstory_suffix=" You ensure exercises are progressively challenging and clearly worded."
    )
