using LessonsHub.Application.Interfaces;
using LessonsHub.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Writes uploaded files under <c>{LocalBasePath}/users/{userId}/{documentId}/{fileName}</c>.
/// In docker-compose the same directory is mounted into the Python service, so
/// the URI we return resolves on both sides without copying bytes.
/// </summary>
public class LocalDocumentStorage : IDocumentStorage
{
    private readonly DocumentStorageSettings _settings;
    private readonly ILogger<LocalDocumentStorage> _logger;

    public LocalDocumentStorage(DocumentStorageSettings settings, ILogger<LocalDocumentStorage> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        int userId,
        int documentId,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName); // strip path separators
        var dir = Path.Combine(_settings.LocalBasePath, "users", userId.ToString(), documentId.ToString());
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, safeFileName);
        await using (var output = File.Create(fullPath))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        // Build a `file://` URI that's portable to the Python service.
        var uri = new Uri(fullPath).AbsoluteUri;
        _logger.LogInformation("Document {DocId} for user {UserId} saved to {Uri}", documentId, userId, uri);
        return uri;
    }

    public Task<bool> DeleteAsync(string storageUri, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(storageUri, UriKind.Absolute, out var uri) || !uri.IsFile)
        {
            return Task.FromResult(false);
        }
        var path = uri.LocalPath;
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }
        try
        {
            File.Delete(path);
            // Best-effort: prune empty parent directory so old document IDs don't pile up.
            var dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete local document at {Path}", path);
            return Task.FromResult(false);
        }
    }
}
