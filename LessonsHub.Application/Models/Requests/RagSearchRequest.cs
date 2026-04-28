namespace LessonsHub.Application.Models.Requests;

public class RagSearchRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
    public string GoogleApiKey { get; set; } = string.Empty;
}
