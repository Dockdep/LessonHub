from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware

from routes import lessons_router, rag_router
from tools.doc_cache import init_schema as init_doc_cache_schema
from tools.rag_store import init_schema as init_rag_schema


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Owned schemas (this service is the source of truth for these tables).
    await init_doc_cache_schema()
    await init_rag_schema()
    yield


app = FastAPI(title="LessonsHub AI API", lifespan=lifespan)

# Configure CORS for .NET backend
app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "https://localhost:7121",
        "http://localhost:5000",
        "http://localhost:3000",
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.exception_handler(ValueError)
async def value_error_handler(request, exc: ValueError):
    """Handle validation errors."""
    return JSONResponse(status_code=400, content={"detail": str(exc)})


@app.exception_handler(Exception)
async def general_exception_handler(request, exc: Exception):
    """Handle unexpected errors."""
    print(f"Unexpected error: {exc}")
    import traceback
    traceback.print_exc()
    return JSONResponse(status_code=500, content={"detail": "An unexpected technical error occurred."})


app.include_router(lessons_router)
app.include_router(rag_router)


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy"}


# ==============================================================================
# HOW TO RUN THIS
# ==============================================================================
# Terminal Command:
# uvicorn main:app --reload --port 8000
