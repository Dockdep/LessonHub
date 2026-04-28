using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface ILessonPlanShareRepository : IRepository
{
    Task<List<LessonPlanShare>> GetByPlanAsync(int planId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int planId, int userId, CancellationToken ct = default);
    Task<LessonPlanShare?> GetAsync(int planId, int userId, CancellationToken ct = default);
    void Add(LessonPlanShare share);
    void Remove(LessonPlanShare share);
}
