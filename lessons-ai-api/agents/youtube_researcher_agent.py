from crewai import Agent, LLM
from tools.youtube_search_tool import search_youtube_videos


def create_youtube_researcher(llm: LLM) -> Agent:
    """Create a YouTube researcher agent for finding relevant video tutorials."""
    return Agent(
        role="YouTube Educational Content Researcher",
        goal="Find the most relevant and high-quality YouTube video tutorials that complement the lesson content",
        backstory="""You are an expert at finding educational content on YouTube. You know
the most reputable educational channels and content creators across various subjects.
You understand what makes a good tutorial video - clear explanations, good production
quality, and accurate information. You prioritize videos from established educators
and official channels.""",
        tools=[search_youtube_videos],
        verbose=True,
        max_iter=3,
        allow_delegation=False,
        llm=llm
    )
