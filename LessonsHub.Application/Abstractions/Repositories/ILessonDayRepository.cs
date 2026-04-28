using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface ILessonDayRepository : IRepository
{
    Task<List<LessonDay>> GetByMonthAsync(int userId, DateTime startUtc, DateTime endUtcExclusive, CancellationToken ct = default);
    Task<LessonDay?> GetByDateAsync(int userId, DateTime dateUtc, CancellationToken ct = default);
    Task<LessonDay?> GetByDateWithLessonsAsync(int userId, DateTime dateUtc, CancellationToken ct = default);
    Task<LessonDay?> GetByIdWithLessonsAsync(int dayId, CancellationToken ct = default);
    Task<List<LessonDay>> GetEmptyAmongAsync(IEnumerable<int> dayIds, CancellationToken ct = default);
    void Add(LessonDay day);
    void Remove(LessonDay day);
    void RemoveRange(IEnumerable<LessonDay> days);
}
