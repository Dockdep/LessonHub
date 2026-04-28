"""Tests for the documentation pre-fetch pipeline.

We mock DDG and httpx so tests don't hit the network. We also stub the
Postgres cache (`doc_cache.get`/`put`) so tests don't need a database.

Pipeline under test:
    queries (from analyzer) → cache lookup → DDG → trafilatura → cache write
"""
import os
import sys
from unittest.mock import AsyncMock, patch

import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from tools import documentation_search as ds  # noqa: E402


# ---------------- format_docs_for_prompt + format_sources_section ----------------

class TestFormatters:
    def test_format_docs_for_prompt_returns_empty_for_no_results(self):
        assert ds.format_docs_for_prompt([]) == ""

    def test_format_docs_for_prompt_includes_each_doc(self):
        out = ds.format_docs_for_prompt([
            {"url": "https://a.com", "title": "Doc A", "content_excerpt": "body A"},
            {"url": "https://b.com", "title": "Doc B", "content_excerpt": "body B"},
        ])
        assert "Doc A" in out
        assert "https://a.com" in out
        assert "body A" in out
        assert "Doc B" in out

    def test_format_sources_dedupes_and_returns_markdown_list(self):
        out = ds.format_sources_section([
            {"url": "https://a.com", "title": "A"},
            {"url": "https://a.com", "title": "A"},  # duplicate
            {"url": "https://b.com", "title": "B"},
        ])
        assert out.count("https://a.com") == 1
        assert out.count("https://b.com") == 1
        assert "## Sources" in out

    def test_format_sources_empty_for_empty_input(self):
        assert ds.format_sources_section([]) == ""


# ---------------- _query_cache_key ----------------

class TestQueryCacheKey:
    def test_normalises_case_and_whitespace(self):
        assert ds._query_cache_key("  Angular Pipes  ") == ds._query_cache_key("angular pipes")

    def test_distinct_queries_produce_distinct_keys(self):
        assert ds._query_cache_key("angular pipes") != ds._query_cache_key("angular signals")


# ---------------- _run_one_query ----------------

@pytest.mark.asyncio
async def test_run_one_query_returns_cached_when_available():
    cached = [{"url": "https://fastapi.tiangolo.com", "title": "Cached", "content_excerpt": "..."}]
    with patch.object(ds.doc_cache, "get", new=AsyncMock(return_value=cached)) as mget, \
         patch.object(ds, "_ddg_search", new=AsyncMock()) as mddg, \
         patch.object(ds.doc_cache, "put", new=AsyncMock()) as mput:
        out = await ds._run_one_query("fastapi site:fastapi.tiangolo.com", bypass_cache=False)

    assert out == cached
    mget.assert_awaited_once()
    mddg.assert_not_awaited()
    mput.assert_not_awaited()


@pytest.mark.asyncio
async def test_run_one_query_skips_cache_when_bypass_true():
    with patch.object(ds.doc_cache, "get", new=AsyncMock(return_value=[{"x": 1}])) as mget, \
         patch.object(ds, "_ddg_search", new=AsyncMock(return_value=[
             {"href": "https://fastapi.tiangolo.com", "title": "FastAPI", "body": "snippet"}
         ])) as mddg, \
         patch.object(ds, "_fetch_page_content", new=AsyncMock(return_value="full page")) as mfetch, \
         patch.object(ds.doc_cache, "put", new=AsyncMock()) as mput:
        out = await ds._run_one_query("fastapi site:fastapi.tiangolo.com", bypass_cache=True)

    mget.assert_not_awaited()  # cache lookup skipped
    mddg.assert_awaited_once()
    mfetch.assert_awaited_once()
    mput.assert_awaited_once()
    assert out[0]["url"] == "https://fastapi.tiangolo.com"
    assert out[0]["content_excerpt"] == "full page"


@pytest.mark.asyncio
async def test_run_one_query_returns_empty_on_ddg_failure():
    """Graceful degradation — empty list, no crash, nothing cached."""
    with patch.object(ds.doc_cache, "get", new=AsyncMock(return_value=None)), \
         patch.object(ds, "_ddg_search", new=AsyncMock(return_value=[])), \
         patch.object(ds.doc_cache, "put", new=AsyncMock()) as mput:
        out = await ds._run_one_query("fastapi site:fastapi.tiangolo.com", bypass_cache=False)

    assert out == []
    mput.assert_not_awaited()  # don't pollute cache with empty result


@pytest.mark.asyncio
async def test_run_one_query_falls_back_to_snippet_when_fetch_fails():
    with patch.object(ds.doc_cache, "get", new=AsyncMock(return_value=None)), \
         patch.object(ds, "_ddg_search", new=AsyncMock(return_value=[
             {"href": "https://docs.example.com", "title": "Docs", "body": "snippet body"}
         ])), \
         patch.object(ds, "_fetch_page_content", new=AsyncMock(return_value="")), \
         patch.object(ds.doc_cache, "put", new=AsyncMock()):
        out = await ds._run_one_query("anything official documentation", bypass_cache=False)

    assert out[0]["content_excerpt"] == "snippet body"


# ---------------- search_for_queries orchestration ----------------

@pytest.mark.asyncio
async def test_search_for_queries_runs_each_query_concurrently():
    async def fake_one(query, bypass_cache):
        return [{"url": f"https://{query.split()[0]}.example", "title": query, "content_excerpt": ""}]

    with patch.object(ds, "_run_one_query", side_effect=fake_one):
        out = await ds.search_for_queries([
            "fastapi site:fastapi.tiangolo.com",
            "postgres site:postgresql.org",
        ])

    assert len(out) == 2
    titles = [r["title"] for r in out]
    assert "fastapi site:fastapi.tiangolo.com" in titles
    assert "postgres site:postgresql.org" in titles


@pytest.mark.asyncio
async def test_search_for_queries_handles_empty_input():
    out = await ds.search_for_queries([])
    assert out == []


@pytest.mark.asyncio
async def test_search_for_queries_strips_blanks():
    """Blank/whitespace-only queries are filtered out before reaching DDG."""
    captured: list[str] = []

    async def fake_one(query, bypass_cache):
        captured.append(query)
        return []

    with patch.object(ds, "_run_one_query", side_effect=fake_one):
        await ds.search_for_queries(["", "  ", "real query"])

    assert captured == ["real query"]


# ---------------- Cache helpers (without a real database) ----------------

@pytest.mark.asyncio
async def test_doc_cache_returns_none_when_database_url_blank(monkeypatch):
    from tools import doc_cache
    from config import settings

    monkeypatch.setattr(settings, "database_url", "")
    assert await doc_cache.get("anykey") is None
    # put is silent on no-config
    await doc_cache.put("anykey", [{"x": 1}])  # should not raise


# ---------------- Settings ----------------

def test_default_ttl_is_30_days():
    """Documentation cache TTL is 30 days so monthly refresh is the norm."""
    from config import settings
    assert settings.doc_cache_ttl_days == 30
