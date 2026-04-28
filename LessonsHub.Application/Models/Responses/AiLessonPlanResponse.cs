using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Responses;

public class AiLessonPlanResponse : IModelUsageCarrier
{
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("lessons")]
    public List<AiLessonItem> Lessons { get; set; } = new();

    [JsonPropertyName("usage")]
    public List<ModelUsage> Usage { get; set; } = new();
}

public class AiLessonItem
{
    [JsonPropertyName("lessonNumber")]
    public int LessonNumber { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("shortDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("lessonTopic")]
    public string LessonTopic { get; set; } = string.Empty;

    [JsonPropertyName("keyPoints")]
    public List<string> KeyPoints { get; set; } = new();
}
