from crewai import Agent, LLM


def create_framework_analyzer(llm: LLM) -> Agent:
    """Create an agent that produces search queries for grounding a Technical lesson.

    Single persona — not parameterised by lesson type. Only invoked for Technical
    lessons; other types skip this step entirely.
    """
    return Agent(
        role="Technical Documentation Researcher",
        goal=(
            "Read a lesson topic + description and produce a short list of "
            "concrete web search queries that would surface official documentation "
            "for the frameworks/libraries the lesson will need."
        ),
        backstory=(
            "You are a senior developer who, before writing about a topic, looks up "
            "the official documentation. You know that vendor docs (angular.dev, "
            "react.dev, fastapi.tiangolo.com, ...) are far more reliable than blog "
            "posts or Stack Overflow. When you write a search query, you anchor it "
            "to the official site with `site:` whenever you know the canonical host, "
            "and you pick the specific sub-topic the lesson is about (not the "
            "framework as a whole). You return JUST the queries — no commentary."
        ),
        verbose=True,
        max_iter=2,
        allow_delegation=False,
        llm=llm,
    )
