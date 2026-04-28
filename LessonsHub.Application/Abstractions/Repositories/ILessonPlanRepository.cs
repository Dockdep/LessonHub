using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface ILessonPlanRepository : IRepository
{
    Task<bool> IsOwnerAsync(int planId, int userId, CancellationToken ct = default);
    Task<bool> HasReadAccessAsync(int planId, int userId, CancellationToken ct = default);
    Task<LessonPlan?> GetOwnedAsync(int planId, int userId, CancellationToken ct = default);
    Task<LessonPlan?> GetOwnedWithLessonsAsync(int planId, int userId, CancellationToken ct = default);
    Task<LessonPlan?> GetForReadAsync(int planId, int userId, CancellationToken ct = default);
    Task<LessonPlan?> GetForReadWithLessonsAsync(int planId, int userId, CancellationToken ct = default);
    Task<List<LessonPlan>> GetSharedWithUserAsync(int userId, CancellationToken ct = default);
    Task<List<LessonPlan>> GetOwnedWithLessonCountAsync(int userId, CancellationToken ct = default);
    void Add(LessonPlan plan);
    void Remove(LessonPlan plan);
}
