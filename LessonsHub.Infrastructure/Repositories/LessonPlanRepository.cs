using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class LessonPlanRepository : RepositoryBase, ILessonPlanRepository
{
    public LessonPlanRepository(LessonsHubDbContext db) : base(db) { }

    public Task<bool> IsOwnerAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlans.AnyAsync(lp => lp.Id == planId && lp.UserId == userId, ct);

    public Task<bool> HasReadAccessAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlans.AnyAsync(lp =>
            lp.Id == planId &&
            (lp.UserId == userId || lp.Shares.Any(s => s.UserId == userId)), ct);

    public Task<LessonPlan?> GetOwnedAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlans.FirstOrDefaultAsync(lp => lp.Id == planId && lp.UserId == userId, ct);

    public Task<LessonPlan?> GetOwnedWithLessonsAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlans
            .Include(lp => lp.Lessons)
            .FirstOrDefaultAsync(lp => lp.Id == planId && lp.UserId == userId, ct);

    public Task<LessonPlan?> GetForReadAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlans.AsNoTracking().FirstOrDefaultAsync(lp =>
            lp.Id == planId &&
            (lp.UserId == userId || lp.Shares.Any(s => s.UserId == userId)), ct);

    public Task<LessonPlan?> GetForReadWithLessonsAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlans
            .AsNoTracking()
            .Include(lp => lp.Lessons)
            .Include(lp => lp.User)
            .FirstOrDefaultAsync(lp =>
                lp.Id == planId &&
                (lp.UserId == userId || lp.Shares.Any(s => s.UserId == userId)), ct);

    public Task<List<LessonPlan>> GetSharedWithUserAsync(int userId, CancellationToken ct = default) =>
        _db.LessonPlanShares
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.LessonPlan).ThenInclude(lp => lp!.Lessons)
            .Include(s => s.LessonPlan).ThenInclude(lp => lp!.User)
            .Select(s => s.LessonPlan!)
            .ToListAsync(ct);

    public Task<List<LessonPlan>> GetOwnedWithLessonCountAsync(int userId, CancellationToken ct = default) =>
        _db.LessonPlans
            .AsNoTracking()
            .Include(lp => lp.Lessons)
            .Where(lp => lp.UserId == userId)
            .ToListAsync(ct);

    public void Add(LessonPlan plan) => _db.LessonPlans.Add(plan);

    public void Remove(LessonPlan plan) => _db.LessonPlans.Remove(plan);
}
