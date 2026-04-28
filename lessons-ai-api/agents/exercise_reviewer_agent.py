from crewai import Agent, LLM
from agents.utils import _create_agent_from_template


def create_exercise_reviewer(llm: LLM, agent_type: str = "Default") -> Agent:
    """Create a specialized exercise reviewer agent based on the topic type."""
    return _create_agent_from_template(
        llm, 
        "exercise_reviewer", 
        agent_type, 
        backstory_suffix=" You are fair but rigorous in your evaluations."
    )
