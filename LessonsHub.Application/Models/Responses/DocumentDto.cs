namespace LessonsHub.Application.Models.Responses;

public class DocumentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string IngestionStatus { get; set; } = "Pending";
    public string? IngestionError { get; set; }
    public int? ChunkCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? IngestedAt { get; set; }
}
