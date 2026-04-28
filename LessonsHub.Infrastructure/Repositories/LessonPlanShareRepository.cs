using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class LessonPlanShareRepository : RepositoryBase, ILessonPlanShareRepository
{
    public LessonPlanShareRepository(LessonsHubDbContext db) : base(db) { }

    public Task<List<LessonPlanShare>> GetByPlanAsync(int planId, CancellationToken ct = default) =>
        _db.LessonPlanShares
            .Where(s => s.LessonPlanId == planId)
            .Include(s => s.User)
            .OrderBy(s => s.User!.Email)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlanShares.AnyAsync(s => s.LessonPlanId == planId && s.UserId == userId, ct);

    public Task<LessonPlanShare?> GetAsync(int planId, int userId, CancellationToken ct = default) =>
        _db.LessonPlanShares.FirstOrDefaultAsync(s => s.LessonPlanId == planId && s.UserId == userId, ct);

    public void Add(LessonPlanShare share) => _db.LessonPlanShares.Add(share);

    public void Remove(LessonPlanShare share) => _db.LessonPlanShares.Remove(share);
}
