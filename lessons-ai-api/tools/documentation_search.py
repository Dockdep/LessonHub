"""Web search + page fetch used to ground Technical lessons.

The framework analyzer agent (in crews/framework_analysis_crew.py) decides
*what* to search for — this module just runs the queries it produces:

    queries (from analyzer) → DDG search → trafilatura page fetch
                            → cap content length → cache → return

Each query is cached independently in `tools.doc_cache` (Postgres-backed,
30-day TTL by default). Any failure short-circuits to `[]` so the upstream
crew can proceed ungrounded rather than crash.

Returns shape: `[{url, title, content_excerpt}, ...]`. Consumed by
`format_docs_for_prompt` and `format_sources_section` in this same file.
"""
from __future__ import annotations

import asyncio
import logging
from typing import Any
from urllib.parse import urlparse

import httpx
import trafilatura
from ddgs import DDGS

from config import settings
from tools import doc_cache

logger = logging.getLogger(__name__)

USER_AGENT = "LessonsHub-DocFetch/1.0 (+https://github.com/lessonshub)"


def _query_cache_key(query: str) -> str:
    """Cache key per analyzer-produced query. Trim + lowercase only — we trust
    the analyzer to dedupe semantically before reaching this layer."""
    return f"q|{query.strip().lower()}"


async def _ddg_search(query: str) -> list[dict[str, str]]:
    """Run a DDG search in a thread (the lib is sync). One retry with 2s
    backoff on rate-limit."""
    def _do_search() -> list[dict[str, str]]:
        with DDGS() as ddgs:
            return list(ddgs.text(query, max_results=settings.doc_search_max_results_per_query))

    for attempt in range(2):
        try:
            return await asyncio.to_thread(_do_search)
        except Exception as e:
            msg = str(e).lower()
            if attempt == 0 and ("rate" in msg or "ratelimit" in msg or "202" in msg or "timeout" in msg):
                logger.info("DDG rate-limited, retrying in 2s: %s", e)
                await asyncio.sleep(2)
                continue
            logger.warning("DDG search failed for %r: %s", query, e)
            return []
    return []


async def _fetch_page_content(url: str) -> str:
    """Fetch a URL and extract its main content with trafilatura. Returns ''
    on any failure."""
    try:
        async with httpx.AsyncClient(
            timeout=settings.doc_fetch_timeout_seconds,
            headers={"User-Agent": USER_AGENT},
            follow_redirects=True,
        ) as client:
            resp = await client.get(url)
            if resp.status_code >= 400:
                return ""
            extracted = trafilatura.extract(resp.text, url=url, favor_recall=True) or ""
            return extracted[: settings.doc_page_max_chars]
    except Exception as e:
        logger.info("page fetch failed for %s: %s", url, e)
        return ""


async def _run_one_query(query: str, bypass_cache: bool) -> list[dict[str, Any]]:
    """End-to-end for a single query: cache → DDG → concurrent page fetch → cache write."""
    key = _query_cache_key(query)

    if not bypass_cache:
        cached = await doc_cache.get(key)
        if cached is not None:
            logger.info("doc cache HIT for %s", key)
            return cached

    raw_results = await _ddg_search(query)
    if not raw_results:
        return []

    pages = await asyncio.gather(
        *(_fetch_page_content(r.get("href") or r.get("link") or "") for r in raw_results),
        return_exceptions=False,
    )

    enriched: list[dict[str, Any]] = []
    for r, content in zip(raw_results, pages):
        url = r.get("href") or r.get("link") or ""
        if not url:
            continue
        enriched.append({
            "url": url,
            "title": r.get("title", "") or urlparse(url).netloc,
            "content_excerpt": content or (r.get("body", "") or ""),
        })

    if enriched:
        await doc_cache.put(key, enriched)

    return enriched


async def search_for_queries(
    queries: list[str],
    bypass_cache: bool = False,
) -> list[dict[str, Any]]:
    """Run all analyzer-produced queries concurrently and flatten the results.

    Returns [] when the input is empty so callers can render conditionally.
    """
    queries = [q.strip() for q in (queries or []) if q and q.strip()]
    if not queries:
        return []
    results = await asyncio.gather(*(_run_one_query(q, bypass_cache) for q in queries))
    flat: list[dict[str, Any]] = []
    for r in results:
        flat.extend(r)
    return flat


def format_sources_section(results: list[dict[str, Any]]) -> str:
    """Render a Markdown 'Sources' section listing the URLs the agent saw.

    Returns '' when the results list is empty so the caller can append unconditionally.
    """
    if not results:
        return ""
    lines = ["", "## Sources", ""]
    seen: set[str] = set()
    for r in results:
        url = r.get("url", "")
        if not url or url in seen:
            continue
        seen.add(url)
        title = (r.get("title") or url).strip()
        lines.append(f"- [{title}]({url})")
    return "\n".join(lines) + "\n"


def format_docs_for_prompt(results: list[dict[str, Any]]) -> str:
    """Render fetched documentation as a single block to inject into an agent prompt.

    Returns '' when the results list is empty so the caller can render conditionally.
    """
    if not results:
        return ""
    blocks: list[str] = ["## Reference Documentation (use these, not your training data)"]
    for r in results:
        title = (r.get("title") or "").strip() or r.get("url", "")
        url = r.get("url", "")
        excerpt = (r.get("content_excerpt") or "").strip()
        blocks.append(f"\n### {title}\nSource: {url}\n\n{excerpt}")
    return "\n".join(blocks)
