namespace LessonsHub.Application.Models.Requests;

public class UpdateLessonPlanRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NativeLanguage { get; set; }

    /// <summary>Language lessons only — target language being studied.</summary>
    public string? LanguageToLearn { get; set; }

    /// <summary>Language lessons only — when true, render in native; when false, immerse in target. Default true.</summary>
    public bool UseNativeLanguage { get; set; } = true;

    public List<UpdateLessonDto> Lessons { get; set; } = new();
}

public class UpdateLessonDto
{
    public int? Id { get; set; }
    public int LessonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LessonTopic { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
}
