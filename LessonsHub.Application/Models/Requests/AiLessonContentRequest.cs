using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Requests;

public class AiLessonContentRequest : IAiRequestWithApiKey, IAiTechnicalRequest
{
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("lessonType")]
    public string LessonType { get; set; } = string.Empty;

    [JsonPropertyName("lessonTopic")]
    public string LessonTopic { get; set; } = string.Empty;

    [JsonPropertyName("keyPoints")]
    public List<string> KeyPoints { get; set; } = new();

    [JsonPropertyName("planDescription")]
    public string PlanDescription { get; set; } = string.Empty;

    [JsonPropertyName("lessonNumber")]
    public int LessonNumber { get; set; }

    [JsonPropertyName("lessonName")]
    public string LessonName { get; set; } = string.Empty;

    [JsonPropertyName("lessonDescription")]
    public string LessonDescription { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("nativeLanguage")]
    public string? NativeLanguage { get; set; }

    [JsonPropertyName("languageToLearn")]
    public string? LanguageToLearn { get; set; }

    [JsonPropertyName("useNativeLanguage")]
    public bool UseNativeLanguage { get; set; } = true;

    [JsonPropertyName("previousLesson")]
    public AdjacentLesson? PreviousLesson { get; set; }

    [JsonPropertyName("nextLesson")]
    public AdjacentLesson? NextLesson { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("googleApiKey")]
    public string? GoogleApiKey { get; set; }

    [JsonPropertyName("bypassDocCache")]
    public bool BypassDocCache { get; set; }

    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
}
