using System.Text.Json;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

public sealed record DocumentIngestPayload(int DocumentId);

public sealed class DocumentIngestExecutor : IJobExecutor
{
    private readonly IDocumentService _docs;
    public DocumentIngestExecutor(IDocumentService docs) { _docs = docs; }

    public string Type => JobType.DocumentIngest;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<DocumentIngestPayload>(job.PayloadJson)
                      ?? throw new InvalidOperationException("Empty payload for DocumentIngest job.");

        var result = await _docs.IngestAsync(payload.DocumentId, ct);
        if (!result.IsSuccess)
            throw new ApplicationException(result.Message ?? $"Ingest failed: {result.Error}");
        return result.Value;
    }
}
