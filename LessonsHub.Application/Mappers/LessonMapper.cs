using LessonsHub.Application.Models.Responses;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Mappers;

/// <summary>
/// Pure mapping helpers from Domain entities to API DTOs.
/// No data-access, no DI. Caller passes the current user id so we can stamp
/// IsOwner without leaking auth concerns into the entity itself.
/// </summary>
public static class LessonMapper
{
    public static LessonDetailDto ToDetailDto(this Lesson lesson, int currentUserId) => new()
    {
        Id = lesson.Id,
        LessonNumber = lesson.LessonNumber,
        Name = lesson.Name,
        ShortDescription = lesson.ShortDescription,
        Content = lesson.Content,
        LessonType = lesson.LessonType,
        LessonTopic = lesson.LessonTopic,
        KeyPoints = lesson.KeyPoints ?? new List<string>(),
        IsCompleted = lesson.IsCompleted,
        CompletedAt = lesson.CompletedAt,
        LessonPlanId = lesson.LessonPlanId,
        LessonDayId = lesson.LessonDayId,

        Exercises = lesson.Exercises?.Select(e => e.ToDto()).ToList() ?? new(),
        ChatHistory = lesson.ChatHistory?.Select(c => c.ToDto()).ToList() ?? new(),
        Videos = lesson.Videos?.Select(v => v.ToDto()).ToList() ?? new(),
        Books = lesson.Books?.Select(b => b.ToDto()).ToList() ?? new(),
        Documentation = lesson.Documentation?.Select(d => d.ToDto()).ToList() ?? new(),

        IsOwner = lesson.LessonPlan?.UserId == currentUserId,
        OwnerName = lesson.LessonPlan?.User?.Name
    };

    public static ExerciseDto ToDto(this Exercise exercise) => new()
    {
        Id = exercise.Id,
        ExerciseText = exercise.ExerciseText,
        Difficulty = exercise.Difficulty,
        LessonId = exercise.LessonId,
        Answers = exercise.Answers?.Select(a => a.ToDto()).ToList() ?? new(),
        ChatHistory = exercise.ChatHistory?.Select(c => c.ToDto()).ToList() ?? new()
    };

    public static ExerciseAnswerDto ToDto(this ExerciseAnswer answer) => new()
    {
        Id = answer.Id,
        UserResponse = answer.UserResponse,
        SubmittedAt = answer.SubmittedAt,
        AccuracyLevel = answer.AccuracyLevel,
        ReviewText = answer.ReviewText,
        ExerciseId = answer.ExerciseId
    };

    public static ChatMessageDto ToDto(this ChatMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Text = message.Text,
        CreatedAt = message.CreatedAt,
        LessonId = message.LessonId,
        ExerciseId = message.ExerciseId
    };

    public static VideoDto ToDto(this Video video) => new()
    {
        Id = video.Id,
        Title = video.Title,
        Channel = video.Channel,
        Description = video.Description,
        Url = video.Url,
        LessonId = video.LessonId
    };

    public static BookDto ToDto(this Book book) => new()
    {
        Id = book.Id,
        Author = book.Author,
        BookName = book.BookName,
        ChapterNumber = book.ChapterNumber,
        ChapterName = book.ChapterName,
        Description = book.Description,
        LessonId = book.LessonId
    };

    public static DocumentationDto ToDto(this Documentation doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        Section = doc.Section,
        Description = doc.Description,
        Url = doc.Url,
        LessonId = doc.LessonId
    };
}
