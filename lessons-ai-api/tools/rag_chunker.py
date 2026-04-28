"""Text → chunks with simple structure-awareness.

Strategy:
  - For markdown: split on `#`/`##`/`###` headings first, then sub-split any
    section that's still too long.
  - For plain text: sliding window over paragraphs.
  - Target chunk size measured in words (rough proxy for tokens; gemini tokens
    are ~0.75 words on average). 500 words ≈ 600-700 tokens — well under the
    embedding model's 2048 limit, with headroom for noisy content.
  - Overlap between adjacent chunks: 50 words. Helps retrieval when a query
    matches a concept that crosses a chunk boundary.

Output preserves a `header_path` for each chunk so callers can show where in
the source document a result came from.
"""
from __future__ import annotations

import re
from dataclasses import dataclass

DEFAULT_CHUNK_WORDS = 500
DEFAULT_OVERLAP_WORDS = 50

_MD_HEADING_RE = re.compile(r"^(#{1,6})\s+(.+?)\s*$", re.MULTILINE)


@dataclass
class Chunk:
    text: str
    header_path: str  # e.g. "Chapter 1 > Section 2"
    chunk_index: int


def chunk_text(
    source_text: str,
    *,
    is_markdown: bool = True,
    chunk_words: int = DEFAULT_CHUNK_WORDS,
    overlap_words: int = DEFAULT_OVERLAP_WORDS,
) -> list[Chunk]:
    """Split source text into Chunks. Returns empty list for empty input."""
    if not source_text or not source_text.strip():
        return []

    if is_markdown and _MD_HEADING_RE.search(source_text):
        sections = _split_markdown_by_headings(source_text)
    else:
        sections = [("", source_text)]

    chunks: list[Chunk] = []
    for header_path, section_text in sections:
        for sub_text in _window_split(section_text, chunk_words, overlap_words):
            chunks.append(
                Chunk(
                    text=sub_text.strip(),
                    header_path=header_path,
                    chunk_index=len(chunks),
                )
            )
    return [c for c in chunks if c.text]


def _split_markdown_by_headings(text: str) -> list[tuple[str, str]]:
    """Split markdown into (header_path, section_body) tuples.

    Each section's body is the text BETWEEN one heading and the next heading
    of the same or higher level. The header_path tracks ancestry, e.g. an
    `## Section` under `# Chapter` gets `"Chapter > Section"`.
    """
    matches = list(_MD_HEADING_RE.finditer(text))
    if not matches:
        return [("", text)]

    sections: list[tuple[str, str]] = []
    # Preamble before first heading (if any).
    preamble = text[: matches[0].start()].strip()
    if preamble:
        sections.append(("", preamble))

    # Track active heading stack: list of (level, title).
    stack: list[tuple[int, str]] = []

    for i, m in enumerate(matches):
        level = len(m.group(1))
        title = m.group(2).strip()
        # Pop deeper headings off the stack.
        while stack and stack[-1][0] >= level:
            stack.pop()
        stack.append((level, title))

        body_start = m.end()
        body_end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        body = text[body_start:body_end].strip()

        header_path = " > ".join(t for _, t in stack)
        if body:
            sections.append((header_path, body))

    return sections


def _window_split(text: str, chunk_words: int, overlap_words: int) -> list[str]:
    """Slide a window of `chunk_words` words across text with overlap."""
    words = text.split()
    if len(words) <= chunk_words:
        return [text] if text.strip() else []

    if overlap_words >= chunk_words:
        raise ValueError("overlap_words must be smaller than chunk_words")

    step = chunk_words - overlap_words
    chunks: list[str] = []
    for start in range(0, len(words), step):
        end = start + chunk_words
        chunk_words_slice = words[start:end]
        if not chunk_words_slice:
            break
        chunks.append(" ".join(chunk_words_slice))
        if end >= len(words):
            break
    return chunks
