namespace LessonsHub.Application.Models.Requests;

public class LessonPlanRequestDto
{
    public string LessonType { get; set; } = string.Empty;
    public string LessonTopic { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int? NumberOfDays { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NativeLanguage { get; set; }

    /// <summary>Language lessons only — target language being studied.</summary>
    public string? LanguageToLearn { get; set; }

    /// <summary>Language lessons only — when true, render in native; when false, immerse in target. Default true.</summary>
    public bool UseNativeLanguage { get; set; } = true;

    public bool BypassDocCache { get; set; }

    /// <summary>Optional source document — when set, its content is used as
    /// RAG ground-truth for this plan. Independent of LessonType.</summary>
    public int? DocumentId { get; set; }
}
