namespace LessonsHub.Application.Models.Requests;

/// <summary>
/// HTTP-agnostic projection of an uploaded file. Controllers adapt their
/// IFormFile (or any other transport) into this so the service stays free of
/// ASP.NET concerns.
/// </summary>
public sealed record UploadDocumentInput(
    string FileName,
    string ContentType,
    long Length,
    Stream Content);
