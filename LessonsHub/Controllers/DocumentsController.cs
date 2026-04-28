using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Services;
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

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        (await _documents.ListAsync()).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) =>
        (await _documents.GetAsync(id)).ToActionResult();

    [HttpPost("upload")]
    [RequestSizeLimit(DocumentService.MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null)
            return BadRequest(new { message = "No file provided." });

        await using var stream = file.OpenReadStream();
        var input = new UploadDocumentInput(
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length,
            stream);

        return (await _documents.UploadAsync(input, cancellationToken)).ToActionResult();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _documents.DeleteAsync(id, cancellationToken);
        // Preserve original 204 No Content semantics for delete success.
        return result.IsSuccess ? NoContent() : result.ToActionResult();
    }
}
