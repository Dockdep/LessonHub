using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Application.Services;
using LessonsHub.Application.Services.Executors;
using LessonsHub.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LessonsHub.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;
    private readonly IJobService _jobs;

    public DocumentsController(IDocumentService documents, IJobService jobs)
    {
        _documents = documents;
        _jobs = jobs;
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        (await _documents.ListAsync()).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) =>
        (await _documents.GetAsync(id)).ToActionResult();

    /// <summary>
    /// Synchronously persists the file to storage, then enqueues a
    /// DocumentIngest job and returns the document with status="Pending"
    /// plus the job id. The browser subscribes to the SignalR hub for
    /// the ingest result.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(DocumentService.MaxUploadBytes)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (file == null)
            return BadRequest(new { message = "No file provided." });

        await using var stream = file.OpenReadStream();
        var input = new UploadDocumentInput(
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length,
            stream);

        var uploadResult = await _documents.UploadAsync(input, cancellationToken);
        if (!uploadResult.IsSuccess) return uploadResult.ToActionResult();

        var doc = uploadResult.Value!;
        var jobId = await _jobs.EnqueueAsync(
            JobType.DocumentIngest,
            new DocumentIngestPayload(doc.Id),
            idempotencyKey: idempotencyKey,
            relatedEntityType: "Document",
            relatedEntityId: doc.Id,
            ct: cancellationToken);

        return Accepted(new UploadAcceptedResponse(doc, jobId));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _documents.DeleteAsync(id, cancellationToken);
        // Preserve original 204 No Content semantics for delete success.
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }
}

/// <summary>
/// Upload responds with both the freshly-stored Document (so the UI can show
/// it in the list immediately) and the ingest jobId (so the UI can subscribe
/// for status transitions).
/// </summary>
public sealed record UploadAcceptedResponse(DocumentDto Document, Guid JobId);
