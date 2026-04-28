from crewai import Agent, Task
from pydantic import BaseModel, Field


class QualityCheckResult(BaseModel):
    score: int = Field(..., description="Quality score from 0 to 100. 80+ means acceptable quality.")
    shortcomings: str = Field(..., description="Specific shortcomings found. Write 'None' if score is 80+.")


def create_quality_check_task(
    generation_type: str,
    original_request: str,
    generated_result: str,
    agent: Agent
) -> Task:
    """Create a task to evaluate the quality of generated content against the original request."""
    description = f"""Evaluate the quality of the following generated {generation_type}.

## Original Request
{original_request}

## Generated Result
{generated_result}

## Evaluation Criteria
1. **Completeness** — Does the result fully address everything in the original request?
2. **Accuracy** — Is the content factually correct and relevant to the topic?
3. **Structure** — Is the content well-organized and logically structured?
4. **Educational Value** — Is this useful for learning? Is it clear and understandable?
5. **Relevance** — Does the content stay on topic without unnecessary filler?

## Instructions
- Assign a score from 0 to 100 based on the criteria above.
- If the score is below 80, describe the specific shortcomings that need to be fixed.
- If the score is 80 or above, write "None" for shortcomings.
- Be strict but fair. Do not inflate scores.
- IMPORTANT: Respond with ONLY the score and shortcomings. Do NOT repeat or quote any part of the generated content."""

    return Task(
        description=description,
        expected_output="ONLY a JSON object with 'score' (integer 0-100) and 'shortcomings' (string). No other text.",
        output_pydantic=QualityCheckResult,
        agent=agent
    )
