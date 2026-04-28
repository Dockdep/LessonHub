import os
from jinja2 import Environment, FileSystemLoader, select_autoescape

class TemplateManager:
    """Central manager for loading and rendering Jinja2 templates."""
    
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(TemplateManager, cls).__new__(cls)
            # Initialize Jinja2 environment
            template_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'templates')
            cls._instance.env = Environment(
                loader=FileSystemLoader(template_dir),
                autoescape=select_autoescape(['html', 'xml', 'jinja2']),
                trim_blocks=True,
                lstrip_blocks=True
            )
        return cls._instance

    def render(self, template_path: str, **kwargs) -> str:
        """Render a template with the given context."""
        template = self.env.get_template(template_path)
        return template.render(**kwargs)

    @classmethod
    def get_task_template(cls, task_name: str, agent_type: str = "Default") -> str:
        """Helper to get a task-specific template path."""
        return f"tasks/{task_name}_{agent_type}.jinja2"

    @classmethod
    def get_agent_template(cls, agent_role: str, agent_type: str = "Default") -> str:
        """Helper to get an agent-specific template path."""
        return f"agents/{agent_role}_{agent_type}.jinja2"
