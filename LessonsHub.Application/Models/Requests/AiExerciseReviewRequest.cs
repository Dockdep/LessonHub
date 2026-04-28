using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Requests;

public class AiExerciseReviewRequest : IAiRequestWithApiKey, IAiTechnicalRequest
{
    [JsonPropertyName("lessonType")]
    public string LessonType { get; set; } = string.Empty;

    [JsonPropertyName("lessonContent")]
    public string LessonContent { get; set; } = string.Empty;

    [JsonPropertyName("exerciseContent")]
    public string ExerciseContent { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("nativeLanguage")]
    public string? NativeLanguage { get; set; }

    [JsonPropertyName("languageToLearn")]
    public string? LanguageToLearn { get; set; }

    [JsonPropertyName("useNativeLanguage")]
    public bool UseNativeLanguage { get; set; } = true;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("googleApiKey")]
    public string? GoogleApiKey { get; set; }

    [JsonPropertyName("bypassDocCache")]
    public bool BypassDocCache { get; set; }
}
