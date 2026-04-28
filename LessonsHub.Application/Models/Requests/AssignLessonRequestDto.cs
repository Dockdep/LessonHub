namespace LessonsHub.Application.Models.Requests;

public class AssignLessonRequestDto
{
    public int LessonId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public string DayDescription { get; set; } = string.Empty;
}
