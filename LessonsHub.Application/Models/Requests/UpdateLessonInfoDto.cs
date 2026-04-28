namespace LessonsHub.Application.Models.Requests;

public class UpdateLessonInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LessonTopic { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
}
