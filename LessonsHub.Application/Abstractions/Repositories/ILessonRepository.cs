using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface ILessonRepository : IRepository
{
    Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Lesson?> GetWithPlanAsync(int id, CancellationToken ct = default);
    Task<Lesson?> GetWithDetailsAsync(int id, int forUserId, CancellationToken ct = default);
    Task<Lesson?> GetWithLessonAndPlanForExerciseAsync(int exerciseId, CancellationToken ct = default);
    Task<List<Lesson>> GetByPlanAsync(int planId, CancellationToken ct = default);
    Task<(Lesson? previous, Lesson? next)> GetAdjacentAsync(int? planId, int lessonNumber, CancellationToken ct = default);
    void Add(Lesson lesson);
    void RemoveRange(IEnumerable<Lesson> lessons);
}
