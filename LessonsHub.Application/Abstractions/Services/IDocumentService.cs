using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface IDocumentService
{
    Task<ServiceResult<List<DocumentDto>>> ListAsync(CancellationToken ct = default);
    Task<ServiceResult<DocumentDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Persists the Document row, writes the file to storage, and returns the
    /// DTO with IngestionStatus="Pending". Does NOT call RAG ingest — the
    /// controller enqueues a DocumentIngest job afterward and the executor
    /// invokes IngestAsync below in the background.
    /// </summary>
    Task<ServiceResult<DocumentDto>> UploadAsync(UploadDocumentInput input, CancellationToken ct = default);

    /// <summary>Pre-flight check the controller runs before enqueueing the ingest job.</summary>
    Task<ServiceResult> ValidateIngestAsync(int documentId, CancellationToken ct = default);

    /// <summary>Background ingest: calls Python /api/rag/ingest and updates the Document's status.</summary>
    Task<ServiceResult<DocumentDto>> IngestAsync(int documentId, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
