namespace LessonsHub.Domain.Entities;

public class Book
{
    public int Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public int? ChapterNumber { get; set; }
    public string? ChapterName { get; set; }
    public string Description { get; set; } = string.Empty;

    // Relationship
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}
