namespace LessonsHub.Application.Models.Responses;

public class RagIngestResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
}
