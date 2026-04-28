using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Requests;

public class AiLessonPlanRequest : IAiRequestWithApiKey, IAiTechnicalRequest
{
	[JsonPropertyName("lessonType")]
	public string LessonType { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("numberOfLessons")]
    public int? NumberOfLessons { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

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

    /// <summary>Optional source document — when set, its embedded chunks are
    /// used to ground the plan in RAG context. Independent of LessonType.</summary>
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
}
