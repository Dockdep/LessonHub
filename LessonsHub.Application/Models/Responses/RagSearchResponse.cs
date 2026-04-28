namespace LessonsHub.Application.Models.Responses;

public class RagSearchResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public List<RagSearchHit> Hits { get; set; } = new();
}

public class RagSearchHit
{
    public int ChunkIndex { get; set; }
    public string HeaderPath { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
}
