"""Unit tests for the RAG chunker. No DB, no API calls."""
from tools.rag_chunker import chunk_text


def test_empty_input_returns_empty_list():
    assert chunk_text("") == []
    assert chunk_text("   \n  ") == []


def test_short_text_becomes_single_chunk():
    chunks = chunk_text("Just one sentence.", is_markdown=False)
    assert len(chunks) == 1
    assert chunks[0].text == "Just one sentence."
    assert chunks[0].header_path == ""
    assert chunks[0].chunk_index == 0


def test_markdown_split_uses_headings_as_header_path():
    text = """# Chapter 1

Intro paragraph.

## Section A

Content of section A.

## Section B

Content of section B.
"""
    chunks = chunk_text(text, is_markdown=True)
    paths = [c.header_path for c in chunks]
    assert "Chapter 1" in paths
    assert "Chapter 1 > Section A" in paths
    assert "Chapter 1 > Section B" in paths


def test_long_section_gets_window_split_with_overlap():
    """A section longer than chunk_words should split into multiple chunks
    that share `overlap_words` words at their boundaries."""
    long_body = " ".join(f"word{i}" for i in range(1500))  # 1500 words
    chunks = chunk_text(long_body, is_markdown=False, chunk_words=500, overlap_words=50)

    # 1500 words, 500-word chunks with 450-word stride → 4 chunks (500, 500, 500, 50)
    # Last chunk may be shorter; main check is multiple chunks were produced.
    assert len(chunks) >= 3
    # Boundary words should overlap by exactly `overlap_words`: the last 50
    # words of chunk N are the first 50 words of chunk N+1.
    first_words = chunks[0].text.split()
    second_words = chunks[1].text.split()
    assert first_words[-50:] == second_words[:50]


def test_overlap_must_be_smaller_than_chunk_size():
    import pytest
    long_body = " ".join(f"word{i}" for i in range(1000))
    with pytest.raises(ValueError):
        chunk_text(long_body, is_markdown=False, chunk_words=100, overlap_words=100)


def test_chunk_indices_are_sequential_and_unique():
    text = """# A

""" + " ".join(f"word{i}" for i in range(800)) + """

## B

""" + " ".join(f"other{i}" for i in range(800))
    chunks = chunk_text(text, is_markdown=True, chunk_words=300, overlap_words=30)
    indices = [c.chunk_index for c in chunks]
    assert indices == list(range(len(chunks)))


def test_plain_text_treated_as_single_section_when_no_headings():
    chunks = chunk_text("Plain text. No headings here.", is_markdown=True)
    assert len(chunks) == 1
    assert chunks[0].header_path == ""
