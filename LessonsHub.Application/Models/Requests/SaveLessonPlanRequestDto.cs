using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Models.Requests;

public class SaveLessonPlanRequestDto
{
    public LessonPlanResponseDto LessonPlan { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string? LessonType { get; set; }
    public string? NativeLanguage { get; set; }

    /// <summary>Language lessons only — target language being studied.</summary>
    public string? LanguageToLearn { get; set; }

    /// <summary>Language lessons only — when true, render in native; when false, immerse in target. Default true.</summary>
    public bool UseNativeLanguage { get; set; } = true;

    /// <summary>Optional FK to the source Document (Document lesson type).</summary>
    public int? DocumentId { get; set; }
}
