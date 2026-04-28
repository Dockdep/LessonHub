namespace LessonsHub.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty; // "user" or "model"
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Optional Foreign Keys (one will be null, one will be filled)
    public int? LessonId { get; set; }
    public int? ExerciseId { get; set; }
}
