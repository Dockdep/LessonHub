using Google.Cloud.Storage.V1;
using LessonsHub.Application.Interfaces;
using LessonsHub.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Writes uploaded files to <c>gs://{GcsBucket}/users/{userId}/{documentId}/{fileName}</c>.
/// The Python RAG service reads the same object back using its own service-account
/// credentials — no bytes ever cross HTTP between the .NET and Python services.
/// </summary>
public class GcsDocumentStorage : IDocumentStorage
{
    private readonly DocumentStorageSettings _settings;
    private readonly StorageClient _client;
    private readonly ILogger<GcsDocumentStorage> _logger;

    public GcsDocumentStorage(DocumentStorageSettings settings, ILogger<GcsDocumentStorage> logger)
    {
        _settings = settings;
        _logger = logger;
        // Application Default Credentials: on Cloud Run this resolves to the
        // workload service account automatically.
        _client = StorageClient.Create();
    }

    public async Task<string> SaveAsync(
        int userId,
        int documentId,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.GcsBucket))
        {
            throw new InvalidOperationException(
                "DocumentStorage:GcsBucket is not configured but Strategy=Gcs.");
        }

        var safeFileName = Path.GetFileName(fileName);
        var objectName = $"users/{userId}/{documentId}/{safeFileName}";

        await _client.UploadObjectAsync(
            bucket: _settings.GcsBucket,
            objectName: objectName,
            contentType: contentType,
            source: content,
            options: null,
            cancellationToken: cancellationToken);

        var uri = $"gs://{_settings.GcsBucket}/{objectName}";
        _logger.LogInformation("Document {DocId} for user {UserId} uploaded to {Uri}", documentId, userId, uri);
        return uri;
    }

    public async Task<bool> DeleteAsync(string storageUri, CancellationToken cancellationToken = default)
    {
        if (!storageUri.StartsWith("gs://"))
        {
            return false;
        }
        // gs://bucket/object/path → split into bucket + object name.
        var rest = storageUri.Substring("gs://".Length);
        var slash = rest.IndexOf('/');
        if (slash <= 0)
        {
            return false;
        }
        var bucket = rest.Substring(0, slash);
        var objectName = rest.Substring(slash + 1);

        try
        {
            await _client.DeleteObjectAsync(bucket, objectName, cancellationToken: cancellationToken);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete GCS object {Uri}", storageUri);
            return false;
        }
    }
}
