namespace LessonsHub.Domain.Entities;

public class ExerciseAnswer
{
    public int Id { get; set; }
    public string UserResponse { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? AccuracyLevel { get; set; }
    public string? ReviewText { get; set; }

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
}
