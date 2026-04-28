using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class LessonDayRepository : RepositoryBase, ILessonDayRepository
{
    public LessonDayRepository(LessonsHubDbContext db) : base(db) { }

    public Task<List<LessonDay>> GetByMonthAsync(int userId, DateTime startUtc, DateTime endUtcExclusive, CancellationToken ct = default) =>
        _db.LessonDays
            .AsNoTracking()
            .Include(ld => ld.Lessons).ThenInclude(l => l.LessonPlan)
            .Where(ld => ld.UserId == userId && ld.Date >= startUtc && ld.Date < endUtcExclusive)
            .OrderBy(ld => ld.Date)
            .ToListAsync(ct);

    public Task<LessonDay?> GetByDateAsync(int userId, DateTime dateUtc, CancellationToken ct = default) =>
        _db.LessonDays.FirstOrDefaultAsync(ld => ld.UserId == userId && ld.Date == dateUtc, ct);

    public Task<LessonDay?> GetByDateWithLessonsAsync(int userId, DateTime dateUtc, CancellationToken ct = default)
    {
        var nextDay = dateUtc.AddDays(1);
        return _db.LessonDays
            .AsNoTracking()
            .Include(ld => ld.Lessons).ThenInclude(l => l.LessonPlan)
            .Where(ld => ld.UserId == userId && ld.Date >= dateUtc && ld.Date < nextDay)
            .FirstOrDefaultAsync(ct);
    }

    public Task<LessonDay?> GetByIdWithLessonsAsync(int dayId, CancellationToken ct = default) =>
        _db.LessonDays
            .Include(ld => ld.Lessons)
            .FirstOrDefaultAsync(ld => ld.Id == dayId, ct);

    public Task<List<LessonDay>> GetEmptyAmongAsync(IEnumerable<int> dayIds, CancellationToken ct = default)
    {
        var ids = dayIds.ToList();
        return _db.LessonDays
            .Where(ld => ids.Contains(ld.Id) && !ld.Lessons.Any())
            .ToListAsync(ct);
    }

    public void Add(LessonDay day) => _db.LessonDays.Add(day);

    public void Remove(LessonDay day) => _db.LessonDays.Remove(day);

    public void RemoveRange(IEnumerable<LessonDay> days) => _db.LessonDays.RemoveRange(days);
}
