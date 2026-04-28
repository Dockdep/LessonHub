"""Format RAG search results for injection into agent prompts.

When a user attaches a document to a lesson plan / content / exercise
generation request, the crew pre-fetches relevant content from that
document and passes it to the task template via `document_context`. The
shared `templates/_document_context.jinja2` partial wraps it with a
"primary source of information" instruction.

This module owns just the formatting; the actual fetching lives in
`tools.rag_store`.
"""
from __future__ import annotations

# Cap how much of each chunk's text we paste into the prompt at PLAN time.
# At plan time we want broad coverage (many headers), not deep content.
PLAN_PREVIEW_CHARS = 300


def format_outline_for_plan(chunks: list[dict]) -> str:
    """Render a document's chunks as a structured outline + previews for the
    curriculum agent. Returns '' for an empty list so the caller can pass
    unconditionally as `document_context`.

    The outer "use as primary source" framing is added by the
    `_document_context.jinja2` partial; this helper produces just the body.
    """
    if not chunks:
        return ""

    lines: list[str] = []
    last_path = None
    for c in chunks:
        path = c.get("header_path") or "(unstructured)"
        if path != last_path:
            lines.append(f"### {path}")
            last_path = path
        preview = (c.get("text") or "").strip().replace("\n", " ")
        if len(preview) > PLAN_PREVIEW_CHARS:
            preview = preview[:PLAN_PREVIEW_CHARS] + "…"
        lines.append(f"- chunk {c.get('chunk_index')}: {preview}")
    return "\n".join(lines) + "\n"


def format_chunks_for_lesson(hits: list[dict]) -> str:
    """Render search hits as a reference block for content/exercise agents.
    Returns '' for an empty list. The "primary source" framing is added by
    the `_document_context.jinja2` partial."""
    if not hits:
        return ""

    blocks: list[str] = []
    for h in hits:
        path = h.get("header_path") or "(unstructured)"
        score = h.get("score", 0.0)
        text = (h.get("text") or "").strip()
        blocks.append(f"### {path} (relevance {score:.2f})\n\n{text}")
    return "\n\n".join(blocks) + "\n"
