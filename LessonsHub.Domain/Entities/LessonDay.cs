namespace LessonsHub.Domain.Entities;

public class LessonDay
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User? User { get; set; }

    // One Day can have multiple Lessons (which belong to various Topics)
    public List<Lesson> Lessons { get; set; } = new();
}
