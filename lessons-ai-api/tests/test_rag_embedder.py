"""Verify the embedder slices large input lists into batches the Gemini
BatchEmbedContents API will accept (max 100 per request)."""
from types import SimpleNamespace

import pytest

from tools import rag_embedder
from tools.rag_embedder import MAX_BATCH_SIZE, embed_documents


class _FakeAioModels:
    def __init__(self):
        self.calls: list[int] = []  # records the size of each batch

    async def embed_content(self, *, model, contents, config):
        self.calls.append(len(contents))
        # Return one fake vector per input — matches the real shape enough.
        return SimpleNamespace(
            embeddings=[SimpleNamespace(values=[0.0] * 4) for _ in contents]
        )


class _FakeClient:
    def __init__(self, *_, **__):
        self.aio = SimpleNamespace(models=_FakeAioModels())


@pytest.fixture
def fake_genai(monkeypatch):
    """Replace google.genai.Client so we can observe the request batching
    without making any real API calls."""
    holder: dict[str, _FakeClient] = {}

    def factory(*args, **kwargs):
        client = _FakeClient()
        holder["client"] = client
        return client

    monkeypatch.setattr(rag_embedder.genai, "Client", factory)
    return holder


async def test_embed_documents_splits_into_batches_of_at_most_100(fake_genai):
    """A 250-chunk book should produce 3 calls of sizes 100, 100, 50."""
    texts = [f"chunk {i}" for i in range(250)]

    vectors = await embed_documents(texts, api_key="fake-key")

    assert len(vectors) == 250  # one vector per input, order preserved
    assert fake_genai["client"].aio.models.calls == [100, 100, 50]


async def test_embed_documents_under_limit_makes_single_call(fake_genai):
    """Small books shouldn't pay for unnecessary round-trips."""
    texts = [f"chunk {i}" for i in range(MAX_BATCH_SIZE)]

    await embed_documents(texts, api_key="fake-key")

    assert fake_genai["client"].aio.models.calls == [MAX_BATCH_SIZE]


async def test_embed_documents_empty_input_makes_no_api_call(fake_genai):
    """Don't even spin up a client when there's nothing to embed."""
    vectors = await embed_documents([], api_key="fake-key")

    assert vectors == []
    assert "client" not in fake_genai  # genai.Client was never instantiated
