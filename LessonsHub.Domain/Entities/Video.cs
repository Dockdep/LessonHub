namespace LessonsHub.Domain.Entities;

public class Video
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    // Relationship
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}
