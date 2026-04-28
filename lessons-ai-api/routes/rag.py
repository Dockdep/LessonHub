"""RAG endpoints — embedding + vector search backed by pgvector.

Distinct from the lesson endpoints: separate dependencies (rag_store,
rag_chunker, rag_embedder, doc_storage) and a different stakeholder shape
(the .NET Documents flow, not the lesson generation flow).
"""
from fastapi import APIRouter

from models.requests import RagIngestRequest, RagSearchRequest
from models.responses import RagIngestResponse, RagSearchResponse, RagSearchHit
from tools.doc_storage import read_document
from tools.rag_chunker import chunk_text
from tools.rag_embedder import embed_documents, embed_query
from tools.rag_store import upsert_chunks, search as rag_search

router = APIRouter()


@router.post("/api/rag/ingest", response_model=RagIngestResponse)
async def rag_ingest(request: RagIngestRequest) -> RagIngestResponse:
    """Read a document from its URI, chunk + embed, store in pgvector.

    Replaces all existing chunks for the same document_id, so this is also the
    re-ingest path when a user edits a book's source.
    """
    source_text = await read_document(request.document_uri)
    chunks = chunk_text(source_text, is_markdown=request.is_markdown)
    if not chunks:
        return RagIngestResponse(document_id=request.document_id, chunk_count=0)

    embeddings = await embed_documents(
        [c.text for c in chunks],
        api_key=request.google_api_key,
    )
    count = await upsert_chunks(request.document_id, chunks, embeddings)
    return RagIngestResponse(document_id=request.document_id, chunk_count=count)


@router.post("/api/rag/search", response_model=RagSearchResponse)
async def rag_search_endpoint(request: RagSearchRequest) -> RagSearchResponse:
    """Top-k cosine-similarity chunks for a query, scoped to one document."""
    query_vec = await embed_query(request.query, api_key=request.google_api_key)
    hits_raw = await rag_search(request.document_id, query_vec, top_k=request.top_k)
    hits = [
        RagSearchHit(
            chunk_index=h["chunk_index"],
            header_path=h["header_path"],
            text=h["text"],
            score=h["score"],
        )
        for h in hits_raw
    ]
    return RagSearchResponse(document_id=request.document_id, hits=hits)
