namespace LessonsHub.Application.Models.Requests;

public class RagIngestRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentUri { get; set; } = string.Empty;
    public bool IsMarkdown { get; set; } = true;
    public string GoogleApiKey { get; set; } = string.Empty;
}
