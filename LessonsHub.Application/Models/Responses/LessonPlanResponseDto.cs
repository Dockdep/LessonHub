namespace LessonsHub.Application.Models.Responses;

public class LessonPlanResponseDto
{
    public string PlanName { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public List<GeneratedLessonDto> Lessons { get; set; } = new();
}

public class GeneratedLessonDto
{
    public int LessonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LessonTopic { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
}
