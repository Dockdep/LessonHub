using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Application.Services;

public sealed class DocumentService : IDocumentService
{
    // Hard upper bound on accepted file size to keep memory + Cloud Run
    // request body limits predictable.
    public const long MaxUploadBytes = 32L * 1024 * 1024;

    private readonly IDocumentRepository _docs;
    private readonly IDocumentStorage _storage;
    private readonly IRagApiClient _rag;
    private readonly IUserApiKeyProvider _keyProvider;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository docs,
        IDocumentStorage storage,
        IRagApiClient rag,
        IUserApiKeyProvider keyProvider,
        ICurrentUser currentUser,
        ILogger<DocumentService> logger)
    {
        _docs = docs;
        _storage = storage;
        _rag = rag;
        _keyProvider = keyProvider;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ServiceResult<List<DocumentDto>>> ListAsync(CancellationToken ct = default)
    {
        var docs = await _docs.ListForUserAsync(_currentUser.Id, ct);
        return ServiceResult<List<DocumentDto>>.Ok(docs.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<DocumentDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var doc = await _docs.GetForUserAsync(id, _currentUser.Id, ct);
        if (doc == null) return ServiceResult<DocumentDto>.NotFound();
        return ServiceResult<DocumentDto>.Ok(ToDto(doc));
    }

    public async Task<ServiceResult<DocumentDto>> UploadAsync(UploadDocumentInput input, CancellationToken ct = default)
    {
        if (input.Length == 0)
            return ServiceResult<DocumentDto>.BadRequest("No file provided.");
        if (input.Length > MaxUploadBytes)
            return ServiceResult<DocumentDto>.BadRequest($"File exceeds {MaxUploadBytes / (1024 * 1024)} MB limit.");

        var userId = _currentUser.Id;

        // Insert the row first so we get an Id to use in the storage path.
        var doc = new Document
        {
            Name = input.FileName,
            ContentType = string.IsNullOrWhiteSpace(input.ContentType) ? "application/octet-stream" : input.ContentType,
            SizeBytes = input.Length,
            StorageUri = string.Empty,
            IngestionStatus = "Pending",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };
        _docs.Add(doc);
        await _docs.SaveChangesAsync(ct);

        try
        {
            doc.StorageUri = await _storage.SaveAsync(
                userId, doc.Id, input.FileName, input.Content, doc.ContentType, ct);
            await _docs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document {DocId} to storage", doc.Id);
            // Storage failed → roll back the row so we don't leave a broken Pending entry.
            _docs.Remove(doc);
            await _docs.SaveChangesAsync(CancellationToken.None);
            return ServiceResult<DocumentDto>.Internal("Failed to save document.");
        }

        // Ingestion is now a background job (DocumentIngestExecutor). Caller
        // gets the doc back with status="Pending"; the SignalR hub pushes the
        // status transition once the executor finishes.
        return ServiceResult<DocumentDto>.Ok(ToDto(doc));
    }

    public async Task<ServiceResult> ValidateIngestAsync(int documentId, CancellationToken ct = default)
    {
        var doc = await _docs.GetForUserAsync(documentId, _currentUser.Id, ct);
        if (doc == null) return ServiceResult.NotFound();
        if (string.IsNullOrWhiteSpace(doc.StorageUri))
            return ServiceResult.BadRequest("Document has not been written to storage yet.");
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<DocumentDto>> IngestAsync(int documentId, CancellationToken ct = default)
    {
        var validation = await ValidateIngestAsync(documentId, ct);
        if (!validation.IsSuccess)
            return new ServiceResult<DocumentDto>(default, validation.Error, validation.Message);

        var doc = (await _docs.GetForUserAsync(documentId, _currentUser.Id, ct))!;

        try
        {
            var apiKey = await _keyProvider.GetCurrentUserKeyAsync();
            var ingest = await _rag.IngestAsync(new RagIngestRequest
            {
                DocumentId = doc.Id.ToString(),
                DocumentUri = doc.StorageUri,
                IsMarkdown = LooksLikeMarkdown(doc.ContentType, doc.Name),
                GoogleApiKey = apiKey,
            }, ct);

            doc.IngestionStatus = "Ingested";
            doc.ChunkCount = ingest.ChunkCount;
            doc.IngestedAt = DateTime.UtcNow;
            await _docs.SaveChangesAsync(ct);
            _logger.LogInformation("Document {DocId} ingested with {Count} chunks", doc.Id, ingest.ChunkCount);
            return ServiceResult<DocumentDto>.Ok(ToDto(doc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for document {DocId}", doc.Id);
            doc.IngestionStatus = "Failed";
            doc.IngestionError = Truncate(ex.Message, 2000);
            await _docs.SaveChangesAsync(CancellationToken.None);
            return ServiceResult<DocumentDto>.Internal($"Ingestion failed: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var doc = await _docs.GetForUserAsync(id, _currentUser.Id, ct);
        if (doc == null) return ServiceResult.NotFound();

        if (!string.IsNullOrWhiteSpace(doc.StorageUri))
        {
            try { await _storage.DeleteAsync(doc.StorageUri, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Storage delete failed for {DocId}", id); }
        }

        _docs.Remove(doc);
        await _docs.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static bool LooksLikeMarkdown(string contentType, string fileName)
    {
        if (contentType.Contains("markdown", StringComparison.OrdinalIgnoreCase)) return true;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".md" or ".markdown" or ".docx" or ".epub" or ".mobi" or ".azw" or ".azw3";
    }

    private static string? Truncate(string? value, int max)
        => value == null ? null : (value.Length <= max ? value : value.Substring(0, max));

    private static DocumentDto ToDto(Document d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        ContentType = d.ContentType,
        SizeBytes = d.SizeBytes,
        IngestionStatus = d.IngestionStatus,
        IngestionError = d.IngestionError,
        ChunkCount = d.ChunkCount,
        CreatedAt = d.CreatedAt,
        IngestedAt = d.IngestedAt,
    };
}
