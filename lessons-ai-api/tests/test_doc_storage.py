"""Tests for doc_storage. Local-FS path is exercised here; GCS is mocked out
in the rare cases where extractors need it."""
import io
import zipfile

import pytest

from tools.doc_storage import read_document


@pytest.mark.asyncio
async def test_read_local_file_no_scheme(tmp_path):
    p = tmp_path / "book.md"
    p.write_text("# Hello\n\nbody", encoding="utf-8")
    text = await read_document(str(p))
    assert text == "# Hello\n\nbody"


@pytest.mark.asyncio
async def test_read_local_file_with_file_scheme(tmp_path):
    p = tmp_path / "doc.txt"
    p.write_text("plain text content", encoding="utf-8")
    text = await read_document(p.as_uri())  # file:///...
    assert text == "plain text content"


@pytest.mark.asyncio
async def test_unsupported_scheme_raises():
    with pytest.raises(ValueError, match="Unsupported"):
        await read_document("http://example.com/foo")


@pytest.mark.asyncio
async def test_missing_local_file_raises():
    with pytest.raises(FileNotFoundError):
        await read_document("/nonexistent/path/does/not/exist.md")


@pytest.mark.asyncio
async def test_unknown_extension_falls_back_to_utf8(tmp_path):
    """Source code / other text formats should still decode."""
    p = tmp_path / "script.py"
    p.write_text("def hello():\n    return 'world'\n", encoding="utf-8")
    text = await read_document(str(p))
    assert "def hello" in text
    assert "world" in text


@pytest.mark.asyncio
async def test_pdf_extension_dispatches_to_pdf_extractor(tmp_path, monkeypatch):
    """We don't ship a real PDF in tests — just verify the dispatcher routes
    `.pdf` to extract_pdf rather than UTF-8 decoding (which would fail on
    real binary PDFs)."""
    p = tmp_path / "book.pdf"
    p.write_bytes(b"\x00\x01\x02not-actually-utf8\xff")

    called_with: dict[str, bytes] = {}

    def fake_extract_pdf(data: bytes) -> str:
        called_with["data"] = data
        return "EXTRACTED PDF TEXT"

    from tools import rag_extractors
    monkeypatch.setattr(rag_extractors, "extract_pdf", fake_extract_pdf)

    text = await read_document(str(p))
    assert text == "EXTRACTED PDF TEXT"
    assert called_with["data"] == p.read_bytes()


@pytest.mark.asyncio
async def test_docx_extension_dispatches_to_docx_extractor(tmp_path, monkeypatch):
    p = tmp_path / "doc.docx"
    p.write_bytes(b"PK\x03\x04fake-docx-bytes")  # DOCX is a zip; we don't need a real one here

    def fake_extract_docx(data: bytes) -> str:
        return "EXTRACTED DOCX TEXT"

    from tools import rag_extractors
    monkeypatch.setattr(rag_extractors, "extract_docx", fake_extract_docx)

    text = await read_document(str(p))
    assert text == "EXTRACTED DOCX TEXT"
