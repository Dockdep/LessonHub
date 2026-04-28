"""Extract plain text from binary book/document formats.

Each function takes raw bytes and returns extracted text. They aim to never
raise on per-page/per-element failures — those get logged and we keep going,
returning whatever we successfully got.

Where the source format has structure (headings, chapter titles), the
extractor synthesizes markdown headings so the chunker can preserve that
structure when chunking. Plain-text formats (PDF) get no synthesis.
"""
from __future__ import annotations

import io
import logging
import shutil
import tempfile
from pathlib import Path

logger = logging.getLogger(__name__)


def extract_pdf(data: bytes) -> str:
    """Best-effort text extraction from a PDF. Layout structure is lost.

    Tries pypdf first (fast, lightweight). Falls back to pdfminer.six when
    pypdf can't even open the file — most often because the PDF was
    post-processed by a security/DRM shell (e.g. 3-Heights) that left
    metadata strings in an encoding pypdf decodes as UTF-8 and chokes on.
    """
    text = _extract_pdf_pypdf(data)
    if text.strip():
        return text

    text = _extract_pdf_pdfminer(data)
    if text.strip():
        return text

    raise ValueError(
        "Could not extract any text from PDF. The file may be encrypted, "
        "image-only (scanned), or use a non-standard encoding."
    )


def _extract_pdf_pypdf(data: bytes) -> str:
    from pypdf import PdfReader

    try:
        reader = PdfReader(io.BytesIO(data))
        page_count = len(reader.pages)
    except Exception as e:
        logger.warning("pypdf could not open PDF (%s); will try pdfminer fallback", e)
        return ""

    pages: list[str] = []
    for i in range(page_count):
        try:
            text = reader.pages[i].extract_text() or ""
            if text.strip():
                pages.append(text)
        except Exception as e:
            logger.warning("PDF page %d extract failed: %s", i, e)
    return "\n\n".join(pages)


def _extract_pdf_pdfminer(data: bytes) -> str:
    try:
        from pdfminer.high_level import extract_text  # type: ignore[import-not-found]
    except ImportError:
        logger.error("pdfminer.six unavailable; cannot fall back from pypdf")
        return ""

    try:
        return extract_text(io.BytesIO(data)) or ""
    except Exception as e:
        logger.error("pdfminer fallback also failed: %s", e)
        return ""


def extract_docx(data: bytes) -> str:
    """Word document — preserves heading levels as markdown #/##/###..."""
    import docx  # type: ignore[import-not-found]

    document = docx.Document(io.BytesIO(data))
    parts: list[str] = []
    for para in document.paragraphs:
        text = (para.text or "").strip()
        if not text:
            continue
        style = (para.style.name or "").lower()
        if style.startswith("heading"):
            # "Heading 1" → "# ...", "Heading 2" → "## ...", etc.
            try:
                level = int(style.split()[-1])
            except (ValueError, IndexError):
                level = 1
            parts.append(f"{'#' * min(max(level, 1), 6)} {text}")
        else:
            parts.append(text)
    return "\n\n".join(parts)


def extract_epub(data: bytes) -> str:
    """EPUB — each chapter becomes a `# Title` section followed by its text."""
    from ebooklib import epub, ITEM_DOCUMENT  # type: ignore[import-not-found]
    from bs4 import BeautifulSoup

    # ebooklib reads from a file path, so we stage the bytes in a tempfile.
    tmp_path = _bytes_to_temp(data, suffix=".epub")
    try:
        book = epub.read_epub(tmp_path)
    finally:
        Path(tmp_path).unlink(missing_ok=True)

    chapters: list[str] = []
    for item in book.get_items_of_type(ITEM_DOCUMENT):
        try:
            soup = BeautifulSoup(item.get_content(), "html.parser")
        except Exception as e:
            logger.warning("EPUB chapter parse failed: %s", e)
            continue
        title_tag = soup.find(["h1", "h2", "h3"])
        title = title_tag.get_text(strip=True) if title_tag else ""
        text = soup.get_text(separator="\n", strip=True)
        if not text:
            continue
        if title:
            chapters.append(f"# {title}\n\n{text}")
        else:
            chapters.append(text)
    return "\n\n".join(chapters)


def extract_mobi(data: bytes) -> str:
    """MOBI / AZW / AZW3 — the `mobi` lib unpacks to an EPUB; we then reuse
    the EPUB extractor to get structured text out."""
    import mobi  # type: ignore[import-not-found]

    tmp_path = _bytes_to_temp(data, suffix=".mobi")
    try:
        tempdir, output_path = mobi.extract(tmp_path)
        try:
            output = Path(output_path)
            if output.suffix.lower() == ".epub":
                return extract_epub(output.read_bytes())
            # If the lib produced something else (HTML/PDF/...), best-effort
            # decode as text — at least the user gets searchable content.
            return output.read_text(encoding="utf-8", errors="replace")
        finally:
            shutil.rmtree(tempdir, ignore_errors=True)
    finally:
        Path(tmp_path).unlink(missing_ok=True)


def _bytes_to_temp(data: bytes, *, suffix: str) -> str:
    """Write bytes to a NamedTemporaryFile and return its path. Caller deletes."""
    fd = tempfile.NamedTemporaryFile(suffix=suffix, delete=False)
    try:
        fd.write(data)
    finally:
        fd.close()
    return fd.name
