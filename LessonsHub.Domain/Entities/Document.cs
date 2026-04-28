namespace LessonsHub.Domain.Entities;

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>
    /// Where the file lives. `gs://bucket/path/...` for GCS, `file:///abs/path/...`
    /// for local-disk storage. Opaque to most callers; only the storage layer
    /// and the Python RAG service interpret it.
    /// </summary>
    public string StorageUri { get; set; } = string.Empty;

    /// <summary>
    /// "Pending" while the file is being chunked + embedded by the AI service,
    /// "Ingested" on success, "Failed" if anything went wrong (see IngestionError).
    /// </summary>
    public string IngestionStatus { get; set; } = "Pending";
    public string? IngestionError { get; set; }
    public int? ChunkCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? IngestedAt { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}
