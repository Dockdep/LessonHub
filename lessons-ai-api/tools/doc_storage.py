"""Document content reader.

Two locations the bytes can live:
  - gs://bucket/path/...    → Google Cloud Storage (production)
  - file:///abs/path/...    → local filesystem (docker-compose dev)
  - /abs/path/...           → local filesystem (no scheme; treated as file://)

The .NET service decides where to write a document and tells us the URI.
We read the bytes from there, then dispatch to a format-specific extractor
based on the file extension.

Supported formats:
  - .md / .markdown / .txt / source-code → UTF-8 decode
  - .pdf                                  → pypdf
  - .docx                                 → python-docx (preserves heading levels)
  - .epub                                 → ebooklib + BeautifulSoup (preserves chapter titles)
  - .mobi / .azw / .azw3                  → mobi (unpacks to EPUB, then reuses EPUB extractor)
"""
from __future__ import annotations

import logging
from pathlib import Path
from urllib.parse import urlparse
from urllib.request import url2pathname

from tools import rag_extractors

logger = logging.getLogger(__name__)

# Extensions whose extractor returns plain text we should decode straight away.
_TEXT_EXTS = {".md", ".markdown", ".txt", ".html", ".htm", ".rst"}


async def read_document(uri: str) -> str:
    """Return the document contents as text. Raises on unreadable URI / unknown
    binary format that fails extraction."""
    raw = _read_raw(uri)
    ext = _extract_extension(uri).lower()

    if ext == ".pdf":
        return rag_extractors.extract_pdf(raw)
    if ext == ".docx":
        return rag_extractors.extract_docx(raw)
    if ext == ".epub":
        return rag_extractors.extract_epub(raw)
    if ext in (".mobi", ".azw", ".azw3"):
        return rag_extractors.extract_mobi(raw)

    # Treat anything else as text; replace bad bytes rather than failing
    # outright so source-code files etc. still ingest. Normalise CRLF→LF
    # so downstream code (chunker) sees consistent line endings regardless
    # of where the file was authored.
    if ext in _TEXT_EXTS or ext == "":
        return raw.decode("utf-8", errors="replace").replace("\r\n", "\n").replace("\r", "\n")

    # Unknown extension — try UTF-8 anyway, but log so we notice if a real
    # binary format slipped through unhandled.
    logger.warning("Unknown extension %r for %s; decoding as UTF-8", ext, uri)
    return raw.decode("utf-8", errors="replace").replace("\r\n", "\n").replace("\r", "\n")


def _read_raw(uri: str) -> bytes:
    """Fetch the URI's contents as raw bytes, ready for an extractor."""
    parsed = urlparse(uri)
    scheme = parsed.scheme.lower()

    if scheme == "gs":
        return _read_gcs(parsed.netloc, parsed.path.lstrip("/"))
    if scheme == "file":
        return _read_local(url2pathname(parsed.path))
    # No scheme, OR a single-letter "scheme" that's actually a Windows drive
    # letter (urlparse mis-detects `D:\foo\bar` as scheme=`d`).
    if scheme == "" or len(scheme) == 1:
        return _read_local(uri)

    raise ValueError(f"Unsupported document URI scheme '{scheme}': {uri}")


def _extract_extension(uri: str) -> str:
    """Get the lowercase extension including the dot (e.g. `.pdf`).

    Strips any URL noise so we always see the file's actual suffix even when
    URIs include query strings.
    """
    parsed = urlparse(uri)
    path = parsed.path or uri
    return Path(path).suffix


def _read_local(path: str) -> bytes:
    p = Path(path)
    if not p.exists():
        raise FileNotFoundError(f"Document not found at {path}")
    return p.read_bytes()


def _read_gcs(bucket: str, blob_name: str) -> bytes:
    """Read a GCS object using Application Default Credentials.

    On Cloud Run, ADC resolves to the workload service account automatically.
    Locally, use `gcloud auth application-default login` to authenticate.
    """
    # Lazy import — google-cloud-storage is only needed in prod / when reading
    # a `gs://` URI and we don't want the import cost on every dev startup.
    from google.cloud import storage  # type: ignore[import-not-found]

    client = storage.Client()
    blob = client.bucket(bucket).blob(blob_name)
    return blob.download_as_bytes()
