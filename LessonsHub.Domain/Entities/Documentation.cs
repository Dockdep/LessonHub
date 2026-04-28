namespace LessonsHub.Domain.Entities;

public class Documentation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    // Relationship
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}
