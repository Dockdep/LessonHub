from crewai import Agent, LLM


def create_resource_researcher(llm: LLM, agent_type: str = "Default") -> Agent:
    configs = {
        "Technical": {
            "role": "Senior Technical Librarian",
            "goal": "Locate official documentation (MSDN, MDN, etc.), RFCs, and industry-standard textbooks.",
            "backstory": "You focus on the 'Canonical' sources. You prioritize official docs over blog posts. "
                         "You are an expert at navigating O'Reilly, Manning, and official technical repositories."
        },
        "Language": {
            "role": "Linguistic Resource Curator",
            "goal": "Identify standard textbooks (like Cambridge 'In Use' series) and authentic language corpora.",
            "backstory": "You know the gold standards of language learning. You focus on resources that "
                         "provide high-quality drills, listening materials, and clear grammar references."
        },
        "Default": {
            "role": "Expert Academic Researcher",
            "goal": "Curate a mix of classic textbooks and top-rated digital learning resources.",
            "backstory": "You have an encyclopedic knowledge of general education materials. You find the "
                         "most highly-regarded books and courses for any given subject."
        }
    }

    config = configs.get(agent_type, configs["Default"])

    return Agent(
        role=config["role"],
        goal=config["goal"],
        backstory=config["backstory"] + " You provide precise citations for maximum student utility.",
        verbose=True,
        max_iter=3,
        allow_delegation=False,
        llm=llm
    )