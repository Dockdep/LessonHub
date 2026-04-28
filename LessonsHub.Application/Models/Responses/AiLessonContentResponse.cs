using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Responses;

public class AiLessonContentResponse : IModelUsageCarrier
{
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("lessonNumber")]
    public int LessonNumber { get; set; }

    [JsonPropertyName("lessonName")]
    public string LessonName { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public List<ModelUsage> Usage { get; set; } = new();
}
