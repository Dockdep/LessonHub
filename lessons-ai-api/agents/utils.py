import yaml
from jinja2 import TemplateNotFound
from crewai import Agent, LLM
from factories.template_manager import TemplateManager


def _resolve_template(tm: TemplateManager, agent_role: str, agent_type: str) -> str:
    """Pick a template path for (agent_role, agent_type), falling back to
    Default if the requested type's template doesn't exist.

    Most lesson types have full template coverage (Technical, Language), but
    a few agent_roles (quality_checker, resource_researcher,
    youtube_researcher) don't have type-specific personas — those just use
    Default regardless of the lesson's type. Document grounding is no longer
    a separate type; it's layered on via document_context in the task render.
    """
    candidates = []
    if agent_type in ("Technical", "Language"):
        candidates.append(tm.get_agent_template(agent_role, agent_type))
    candidates.append(tm.get_agent_template(agent_role, "Default"))

    for path in candidates:
        try:
            tm.env.get_template(path)
            return path
        except TemplateNotFound:
            continue
    raise TemplateNotFound(
        f"No template found for agent_role={agent_role!r}, agent_type={agent_type!r}"
    )


def _create_agent_from_template(
    llm: LLM,
    agent_role: str,
    agent_type: str,
    backstory_suffix: str = ""
) -> Agent:
    """Internal utility to create an agent from a Jinja2 template and YAML/JSON config."""
    tm = TemplateManager()
    template_path = _resolve_template(tm, agent_role, agent_type)
    rendered = tm.render(template_path)

    # Load config from rendered YAML
    config_data = yaml.safe_load(rendered)

    return Agent(
        role=config_data.get('role', ''),
        goal=config_data.get('goal', ''),
        backstory=config_data.get('backstory', '') + backstory_suffix,
        verbose=True,
        max_iter=3,
        allow_delegation=False,
        llm=llm
    )
