using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface IExerciseRepository : IRepository
{
    Task<Exercise?> GetForUserWithLessonAsync(int exerciseId, int userId, CancellationToken ct = default);
    void Add(Exercise exercise);
}
