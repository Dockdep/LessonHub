namespace LessonsHub.Application.Models.Responses;

/// <summary>
/// Wire-format representation of a Lesson returned by LessonController.
/// Decoupled from the Lesson entity so the API surface can evolve without
/// touching persistence shape (and vice-versa).
/// </summary>
public class LessonDetailDto
{
    public int Id { get; set; }
    public int LessonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string LessonTopic { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int LessonPlanId { get; set; }
    public int? LessonDayId { get; set; }

    public List<ExerciseDto> Exercises { get; set; } = new();
    public List<ChatMessageDto> ChatHistory { get; set; } = new();
    public List<VideoDto> Videos { get; set; } = new();
    public List<BookDto> Books { get; set; } = new();
    public List<DocumentationDto> Documentation { get; set; } = new();

    /// <summary>True iff the caller owns the lesson plan this lesson belongs to.</summary>
    public bool IsOwner { get; set; }
    /// <summary>Name of the plan owner (for the "Shared by X" pill in the SPA).</summary>
    public string? OwnerName { get; set; }
}

public class ExerciseDto
{
    public int Id { get; set; }
    public string ExerciseText { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int LessonId { get; set; }
    public List<ExerciseAnswerDto> Answers { get; set; } = new();
    public List<ChatMessageDto> ChatHistory { get; set; } = new();
}

public class ExerciseAnswerDto
{
    public int Id { get; set; }
    public string UserResponse { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? AccuracyLevel { get; set; }
    public string? ReviewText { get; set; }
    public int ExerciseId { get; set; }
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? LessonId { get; set; }
    public int? ExerciseId { get; set; }
}

public class VideoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int LessonId { get; set; }
}

public class BookDto
{
    public int Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public int? ChapterNumber { get; set; }
    public string? ChapterName { get; set; }
    public string Description { get; set; } = string.Empty;
    public int LessonId { get; set; }
}

public class DocumentationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int LessonId { get; set; }
}
