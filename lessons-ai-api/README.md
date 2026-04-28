# LessonsHub AI API

A production-ready **FastAPI** application powered by **CrewAI** that generates complete educational content pipelines — from lesson plans to exercises, reviews, and learning resources — using Google Gemini as the underlying LLM.

Designed to be used as a standalone microservice consumed by a .NET backend (or any HTTP client). See [DOTNET_INTEGRATION.md](DOTNET_INTEGRATION.md) for C# integration examples.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Agent Types](#agent-types)
- [Agents](#agents)
- [Tasks](#tasks)
- [API Endpoints](#api-endpoints)
- [Key Libraries](#key-libraries)
- [Project Structure](#project-structure)
- [Setup](#setup)
- [Running with Docker](#running-with-docker)
- [Extending the Template](#extending-the-template)

---

## Architecture Overview

This project follows the **CrewAI multi-agent pattern**:

```
HTTP Request → FastAPI Endpoint → Crew Function → Agent + Task → LLM → Response
```

Each API endpoint spins up a **Crew** — a coordinated group of AI agents assigned specific tasks. The crew runs sequentially, and the final output is parsed and returned as a typed Pydantic response.

### Core Concepts

| Concept | Role |
|---|---|
| **Agent** | An AI persona with a role, goal, and backstory. Shapes how the LLM responds. |
| **Task** | A prompt template assigned to an agent. Defines what to produce and the expected output format. |
| **Crew** | Orchestrates one or more agents and tasks, runs them in sequence, and returns the result. |
| **Tool** | An external capability an agent can call (e.g., YouTube search). |
| **AgentFactory** | A factory class that produces typed agents based on `agent_type`. |
| **TaskConfig** | A registry of style hints and instructions injected into task prompts per `agent_type`. |

---

## Agent Types

Every agent in this system is parameterized by `agent_type`, allowing the same endpoint to behave differently based on the course category.

| `agent_type` | Use Case |
|---|---|
| `Technical` | Programming, software engineering, system design |
| `Language` | Foreign language learning, linguistics |
| `Default` | General education — science, history, business, etc. |

The `agent_type` is passed in every request and flows through the entire pipeline, customizing agent personas, task prompts, and evaluation criteria.

---

## Agents

All agents are created via `AgentFactory` ([factories/agent_factory.py](factories/agent_factory.py)) and exposed through thin wrapper functions in the `agents/` directory.

### Curriculum Designer (`agents/lesson_planner_agent.py`)

Creates structured lesson plans with progressive learning paths.

| Type | Role | Focus |
|---|---|---|
| `Technical` | Technical Curriculum Architect | Prerequisite → Feature → Implementation sequencing |
| `Language` | Linguistic Curriculum Architect | Grammar in Use methodology, SLA principles |
| `Default` | Expert Curriculum Designer | Cognitive load management, topic sequencing |

### Content Writer (`agents/content_writer_agent.py`)

Writes the full body of each lesson in Markdown.

| Type | Role | Focus |
|---|---|---|
| `Technical` | Senior Technical Author | Code-heavy, explains "the why", architecture context |
| `Language` | ESL Content Specialist | Realistic dialogues, Meaning-Form-Use framework |
| `Default` | Expert Educational Content Creator | Feynman Technique, analogies, real-world application |

### Exercise Creator (`agents/exercise_creator_agent.py`)

Generates interactive exercises based on lesson key points.

| Type | Role | Focus |
|---|---|---|
| `Technical` | Senior Technical Exercise Designer | Coding challenges, debugging tasks, real-world scenarios |
| `Language` | Language Exercise Specialist | Translation, dialogue completion, sentence transformation |
| `Default` | Expert Exercise Creator | Comprehension checks, critical thinking prompts |

### Exercise Reviewer (`agents/exercise_reviewer_agent.py`)

Evaluates student answers and returns a structured score with feedback.

| Type | Role | Focus |
|---|---|---|
| `Technical` | Senior Code Reviewer & Technical Examiner | Correctness, edge cases, best practices |
| `Language` | Language Proficiency Examiner | Grammar, vocabulary, fluency, natural expression |
| `Default` | Expert Exercise Evaluator | Correctness, completeness, clarity |

### YouTube Researcher (`agents/youtube_researcher_agent.py`)

Finds relevant YouTube tutorial videos for a lesson using a custom search tool. Type-agnostic — the same agent is used for all lesson types.

### Resource Researcher (`agents/resource_researcher_agent.py`)

Curates books and official documentation for a lesson. Configured inline (not via `AgentFactory`) with type-specific focus on canonical sources.

| Type | Role | Focus |
|---|---|---|
| `Technical` | Senior Technical Librarian | MSDN, MDN, RFCs, O'Reilly, Manning |
| `Language` | Linguistic Resource Curator | Cambridge "In Use" series, language corpora |
| `Default` | Expert Academic Researcher | Highly-rated textbooks, digital learning resources |

---

## Tasks

Tasks are prompt templates that define **what** an agent must produce. They live in the `tasks/` directory.

### Lesson Plan Tasks (`tasks/lesson_plan_tasks.py`)

Two variants depending on whether the number of lessons is specified:

- **`create_lesson_plan_task_with_count`** — Plans exactly N lessons
- **`create_lesson_plan_task_auto_count`** — Lets the AI decide the appropriate number

Both output a structured `LessonPlan` Pydantic model with `planName`, `topic`, and a list of `Lesson` objects (each with `lessonNumber`, `name`, `shortDescription`, `lessonTopic`, `keyPoints`).

### Content Generation Tasks (`tasks/content_generation_tasks.py`)

Dispatches to one of three specialized task creators based on `agent_type`:

| Function | Prompt Persona | Output Structure |
|---|---|---|
| `_create_technical_content_task` | Senior Technical Author & Software Architect | Introduction → Core Concepts → Implementation → Best Practices → Summary |
| `_create_language_content_task` | Expert Linguist & Native-Level Instructor | Introduction + Cultural Note → Grammar & Vocabulary → Scenarios & Dialogues → Common Pitfalls → Summary |
| `_create_default_content_task` | Professional Educator & Course Creator | Introduction → Core Concepts → Real-World Application → Common Misconceptions → Summary |

All three return an extensive Markdown document with no exercises.

### Exercise Generation Tasks (`tasks/exercise_generation_tasks.py`)

Uses a shared `_create_base_exercise_task` helper that builds the prompt dynamically:

- **`create_exercise_generation_task`** — Fresh exercise based on lesson key points
- **`create_exercise_retry_task`** — Remedial exercise based on a previous exercise review (targets identified weaknesses)

Both support optional `comment` (user guidance) and `native_language` (for Language type — injected into the Language exercise protocol to specify the source translation language).

Output always ends with a `### Your Response` section prompting the student to submit their answer.

### Exercise Review Task (`tasks/exercise_review_tasks.py`)

Evaluates a student's answer against the exercise and lesson content.

- Returns an `accuracyLevel` (0–100 integer) and `examReview` (detailed Markdown feedback)
- Scoring bands: 100 (perfect) → 75–99 (minor issues) → 50–74 (significant gaps) → 25–49 (major errors) → 0–24 (incorrect)
- Criteria are customized per `agent_type` via `TaskConfig.get_exercise_review_hint()`

### Resource Research Tasks (`tasks/resource_research_tasks.py`)

Two tasks run sequentially in the resources crew:

- **`create_youtube_research_task`** — Uses the `search_youtube_videos` tool; returns real video URLs
- **`create_resource_research_task`** — Curates books and official documentation; no external tools

---

## API Endpoints

Base URL: `http://localhost:8000`

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/lesson-plan/generate` | Generate a lesson plan |
| `POST` | `/api/lesson-content/generate` | Generate detailed lesson content |
| `POST` | `/api/lesson-exercise/generate` | Generate an exercise for a lesson |
| `POST` | `/api/lesson-exercise/retry` | Generate a new exercise based on review feedback |
| `POST` | `/api/exercise-review/check` | Evaluate a student's exercise response |
| `POST` | `/api/lesson-resources/generate` | Find YouTube videos, books, and documentation |
| `GET` | `/health` | Health check |

### POST `/api/lesson-plan/generate`

**Request:**
```json
{
  "lessonType": "Technical",
  "planName": "Python Basics",
  "topic": "Python Programming",
  "numberOfLessons": 5,
  "description": "Beginner-friendly course covering Python fundamentals"
}
```

**Response:**
```json
{
  "planName": "Python Basics",
  "topic": "Python Programming",
  "lessons": [
    {
      "lessonNumber": 1,
      "name": "Introduction to Python",
      "shortDescription": "Getting started with Python and the development environment.",
      "lessonTopic": "Python setup and basics",
      "keyPoints": ["Install Python 3.12", "Set up a virtual environment", "Write your first script"]
    }
  ]
}
```

### POST `/api/lesson-content/generate`

**Request:**
```json
{
  "planName": "Python Basics",
  "topic": "Python Programming",
  "lessonType": "Technical",
  "lessonTopic": "Python setup and basics",
  "keyPoints": ["Install Python 3.12", "Set up a virtual environment", "Write your first script"],
  "planDescription": "Beginner-friendly course covering Python fundamentals",
  "lessonNumber": 1,
  "lessonName": "Introduction to Python",
  "lessonDescription": "Learn what Python is and set up your development environment"
}
```

**Response:**
```json
{
  "lessonNumber": 1,
  "lessonName": "Introduction to Python",
  "content": "## Introduction to Python\n\n..."
}
```

### POST `/api/lesson-exercise/generate`

**Request:**
```json
{
  "lessonType": "Technical",
  "lessonTopic": "Python setup and basics",
  "lessonNumber": 1,
  "lessonName": "Introduction to Python",
  "lessonDescription": "Learn what Python is and set up your development environment",
  "keyPoints": ["Install Python 3.12", "Set up a virtual environment", "Write your first script"],
  "difficulty": "easy",
  "comment": null,
  "nativeLanguage": null
}
```

**Response:**
```json
{
  "lessonNumber": 1,
  "lessonName": "Introduction to Python",
  "exercise": "## Exercise: Virtual Environment Setup\n\n..."
}
```

### POST `/api/lesson-exercise/retry`

Generates a **new, different** exercise targeting weaknesses from the previous attempt.

**Request:**
```json
{
  "lessonType": "Technical",
  "lessonTopic": "Python setup and basics",
  "lessonNumber": 1,
  "lessonName": "Introduction to Python",
  "lessonDescription": "Learn what Python is and set up your development environment",
  "keyPoints": ["Install Python 3.12", "Set up a virtual environment", "Write your first script"],
  "difficulty": "easy",
  "review": "Score: 40/100. The student confused pip with conda and could not activate the virtual environment.",
  "comment": null,
  "nativeLanguage": null
}
```

**Response:** Same shape as exercise generate.

### POST `/api/exercise-review/check`

**Request:**
```json
{
  "lessonType": "Technical",
  "lessonContent": "## Introduction to Python\n\n...",
  "exerciseContent": "## Exercise: Hello World\n\n...",
  "difficulty": "easy",
  "answer": "print('Hello, World!')"
}
```

**Response:**
```json
{
  "accuracyLevel": 95,
  "examReview": "**Excellent work!** Your solution is correct...\n\n..."
}
```

### POST `/api/lesson-resources/generate`

**Request:**
```json
{
  "lessonType": "Technical",
  "topic": "Python Programming",
  "lessonName": "Introduction to Python",
  "lessonTopic": "Python setup and basics",
  "lessonDescription": "Learn what Python is and set up your development environment"
}
```

**Response:**
```json
{
  "lessonName": "Introduction to Python",
  "topic": "Python Programming",
  "videos": [{ "title": "...", "channel": "...", "description": "...", "url": "..." }],
  "books": [{ "author": "...", "bookName": "...", "chapterNumber": 1, "chapterName": "...", "description": "..." }],
  "documentation": [{ "name": "...", "section": "...", "description": "...", "url": "..." }]
}
```

---

## Key Libraries

| Library | Version | Purpose |
|---|---|---|
| **[FastAPI](https://fastapi.tiangolo.com/)** | 0.128.0 | HTTP API framework — routing, validation, OpenAPI docs |
| **[CrewAI](https://docs.crewai.com/)** | 1.9.3 | Multi-agent orchestration framework |
| **[Pydantic](https://docs.pydantic.dev/)** | 2.11.10 | Request/response models and structured LLM output parsing |
| **[python-dotenv](https://pypi.org/project/python-dotenv/)** | 1.1.1 | Loads environment variables from `.env` |
| **[uvicorn](https://www.uvicorn.org/)** | 0.40.0 | ASGI server to run the FastAPI app |
| **[google-generativeai](https://ai.google.dev/)** | 0.8.6 | Google Gemini LLM SDK (used via CrewAI's LLM wrapper) |
| **[langchain-google-genai](https://python.langchain.com/)** | 4.2.0 | LangChain ↔ Gemini bridge (CrewAI dependency) |
| **[chromadb](https://www.trychroma.com/)** | 1.1.1 | Vector store (CrewAI agent memory, optional) |
| **[opentelemetry-sdk](https://opentelemetry.io/)** | 1.34.1 | Observability / tracing (CrewAI dependency) |

### Why CrewAI?

CrewAI provides a clean abstraction over raw LLM prompting:

- **Agent personas** — role, goal, and backstory shape the LLM's tone and expertise without manual prompt engineering
- **Pydantic output parsing** — `output_pydantic=MyModel` on a Task gives you structured, typed results with no regex or JSON cleanup
- **Tool integration** — agents can call external tools (e.g., YouTube search) mid-task
- **Sequential processes** — multiple agents can work in order, each building on the previous result

### Why Google Gemini?

Configured in [config.py](config.py) via `LLM(model="gemini/gemini-3-flash-preview")`. To switch to a different provider (OpenAI, Anthropic, etc.), change the `model` string and `api_key` — the rest of the codebase is provider-agnostic.

---

## Project Structure

```
lessons-ai-api/
│
├── main.py                          # FastAPI app — all endpoints defined here
├── config.py                        # LLM factory, environment config, resource limits
│
├── agents/                          # Thin wrappers around AgentFactory
│   ├── lesson_planner_agent.py      # Curriculum designer (plans lessons)
│   ├── content_writer_agent.py      # Content writer (writes lesson body)
│   ├── exercise_creator_agent.py    # Exercise creator (generates exercises)
│   ├── exercise_reviewer_agent.py   # Exercise reviewer (evaluates answers)
│   ├── youtube_researcher_agent.py  # YouTube researcher (finds tutorial videos)
│   └── resource_researcher_agent.py # Resource researcher (finds books & docs)
│
├── tasks/                           # Prompt templates — one file per domain
│   ├── lesson_plan_tasks.py         # Lesson plan prompts (with/without count)
│   ├── content_generation_tasks.py  # Content prompts (3 variants by agent_type)
│   ├── exercise_generation_tasks.py # Exercise prompts (new + retry)
│   ├── exercise_review_tasks.py     # Exercise evaluation prompt
│   └── resource_research_tasks.py   # YouTube + book/doc research prompts
│
├── crews/
│   └── lesson_crew.py               # Crew orchestration — wires agents + tasks + Crew
│
├── factories/
│   ├── agent_factory.py             # AgentFactory — typed agent creation with configs
│   └── task_config.py               # TaskConfig — style hints & instructions registry
│
├── models/
│   ├── requests.py                  # Pydantic request models for all endpoints
│   └── responses.py                 # Pydantic response models for all endpoints
│
├── tools/
│   └── youtube_search_tool.py       # Custom CrewAI tool for YouTube search
│
├── Dockerfile                       # Container definition
├── .dockerignore
├── requirements.txt
├── .env                             # Local secrets (not committed)
├── postman_collection.json          # Importable Postman collection for all endpoints
└── DOTNET_INTEGRATION.md            # C# models, client, and usage examples
```

---

## Setup

### Prerequisites

- Python 3.12
- A [Google AI API key](https://aistudio.google.com/app/apikey)

### 1. Create a virtual environment

```bash
# Windows
py -3.12 -m venv .venv
.venv\Scripts\activate

# Linux / macOS
python3.12 -m venv .venv
source .venv/bin/activate
```

### 2. Install dependencies

```bash
pip install -r requirements.txt
```

### 3. Configure environment

Create a `.env` file in the project root:

```env
GOOGLE_API_KEY=your_google_api_key_here

# Optional — override resource limits for the resources endpoint
YOUTUBE_VIDEOS_LIMIT=2
BOOKS_LIMIT=2
DOCUMENTATION_LIMIT=1
```

### 4. Run the server

```bash
uvicorn main:app --reload --port 8000
```

The API will be available at `http://localhost:8000`.
Interactive docs (Swagger UI) are available at `http://localhost:8000/docs`.

---

## Running with Docker

```bash
# Build the image
docker build -t lessons-ai-api .

# Run — pass environment variables at runtime (never bake secrets into the image)
docker run -p 8000:8000 --env-file .env lessons-ai-api
```

Or with an explicit key:

```bash
docker run -p 8000:8000 -e GOOGLE_API_KEY=your_key lessons-ai-api
```

---

## Extending the Template

This project is structured to be easy to fork and adapt for any AI content generation use case.

### Add a new agent type

1. Add a new config entry in `AgentFactory` ([factories/agent_factory.py](factories/agent_factory.py)) for each registry (`CURRICULUM_CONFIGS`, `CONTENT_WRITER_CONFIGS`, etc.)
2. Add matching style hints in `TaskConfig` ([factories/task_config.py](factories/task_config.py))
3. No changes needed in agents, tasks, or endpoints — they all read `agent_type` dynamically

### Add a new endpoint

1. Add a request model to [models/requests.py](models/requests.py)
2. Add a response model to [models/responses.py](models/responses.py) (if needed)
3. Create a task function in `tasks/`
4. Create a crew function in [crews/lesson_crew.py](crews/lesson_crew.py)
5. Register the endpoint in [main.py](main.py)

### Switch LLM provider

Edit `create_llm()` in [config.py](config.py):

```python
# OpenAI
return LLM(model="gpt-4o", api_key=os.getenv("OPENAI_API_KEY"))

# Anthropic
return LLM(model="claude-opus-4-6", api_key=os.getenv("ANTHROPIC_API_KEY"))
```

### Add a custom tool

1. Create a new file in `tools/` and define a function decorated with `@tool` from CrewAI
2. Pass it to the agent's `tools=[...]` list in the relevant agent factory method

---

## CORS

Pre-configured in [main.py](main.py) for:

- `https://localhost:7121` (.NET HTTPS dev server)
- `http://localhost:5000` (.NET HTTP dev server)
- `http://localhost:3000` (React / Next.js dev server)

Add additional origins to the `allow_origins` list as needed.
