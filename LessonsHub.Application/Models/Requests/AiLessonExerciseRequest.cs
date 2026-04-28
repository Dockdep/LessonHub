using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Requests;

public class AiLessonExerciseRequest : IAiRequestWithApiKey, IAiTechnicalRequest
{
    [JsonPropertyName("lessonType")]
    public string LessonType { get; set; } = string.Empty;

    [JsonPropertyName("lessonTopic")]
    public string LessonTopic { get; set; } = string.Empty;

    [JsonPropertyName("lessonNumber")]
    public int LessonNumber { get; set; }

    [JsonPropertyName("lessonName")]
    public string LessonName { get; set; } = string.Empty;

    [JsonPropertyName("lessonDescription")]
    public string LessonDescription { get; set; } = string.Empty;

    [JsonPropertyName("keyPoints")]
    public List<string> KeyPoints { get; set; } = new();

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("nativeLanguage")]
    public string? NativeLanguage { get; set; }

    /// <summary>Language lessons only — target language being studied.</summary>
    [JsonPropertyName("languageToLearn")]
    public string? LanguageToLearn { get; set; }

    /// <summary>Language lessons only — when true, render in native; when false, immerse in target.</summary>
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

    /// <summary>Optional source document — when set, its embedded chunks are
    /// used to ground the exercise in RAG context. Independent of LessonType.</summary>
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
}
