using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Interfaces;

/// <summary>
/// HTTP client for the Python RAG endpoints (separate concern from
/// <see cref="ILessonsAiApiClient"/> which owns lesson generation).
/// Both clients hit the same Python service today; if RAG ever splits out
/// into its own service, only this interface's implementation changes.
/// </summary>
public interface IRagApiClient
{
    Task<RagIngestResponse> IngestAsync(RagIngestRequest request, CancellationToken cancellationToken = default);
    Task<RagSearchResponse> SearchAsync(RagSearchRequest request, CancellationToken cancellationToken = default);
}
