using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Requests;

public class AiLessonResourcesRequest : IAiRequestWithApiKey, IAiTechnicalRequest
{
    [JsonPropertyName("lessonType")]
    public string LessonType { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("lessonName")]
    public string LessonName { get; set; } = string.Empty;

    [JsonPropertyName("lessonTopic")]
    public string LessonTopic { get; set; } = string.Empty;

    [JsonPropertyName("lessonDescription")]
    public string LessonDescription { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("googleApiKey")]
    public string? GoogleApiKey { get; set; }

    [JsonPropertyName("bypassDocCache")]
    public bool BypassDocCache { get; set; }
}
