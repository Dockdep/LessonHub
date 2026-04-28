namespace LessonsHub.Domain.Entities;

public class LessonPlanShare
{
    public int Id { get; set; }

    public int LessonPlanId { get; set; }
    public LessonPlan? LessonPlan { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
}
