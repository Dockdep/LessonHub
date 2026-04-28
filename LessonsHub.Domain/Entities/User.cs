namespace LessonsHub.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string? GoogleApiKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<LessonPlan> LessonPlans { get; set; } = new();
}
