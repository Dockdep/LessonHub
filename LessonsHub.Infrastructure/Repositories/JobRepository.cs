using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class JobRepository : RepositoryBase, IJobRepository
{
    public JobRepository(LessonsHubDbContext db) : base(db) { }

    public Task<Job?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<Job?> GetForUserAsync(Guid id, int userId, CancellationToken ct = default) =>
        _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId, ct);

    public Task<Job?> FindByIdempotencyKeyAsync(int userId, string type, string idempotencyKey, CancellationToken ct = default) =>
        _db.Jobs.AsNoTracking().FirstOrDefaultAsync(
            j => j.UserId == userId && j.Type == type && j.IdempotencyKey == idempotencyKey,
            ct);

    public Task<List<Job>> ListForUserAsync(int userId, JobStatus? status = null, CancellationToken ct = default)
    {
        var q = _db.Jobs.AsNoTracking().Where(j => j.UserId == userId);
        if (status.HasValue) q = q.Where(j => j.Status == status.Value);
        return q.OrderByDescending(j => j.CreatedAt).ToListAsync(ct);
    }

    public Task<Job?> FindInFlightAsync(int userId, string type, string? relatedEntityType, int? relatedEntityId, CancellationToken ct = default)
    {
        var q = _db.Jobs.AsNoTracking().Where(j =>
            j.UserId == userId
            && j.Type == type
            && (j.Status == JobStatus.Pending || j.Status == JobStatus.Running));

        if (!string.IsNullOrEmpty(relatedEntityType))
            q = q.Where(j => j.RelatedEntityType == relatedEntityType);
        if (relatedEntityId.HasValue)
            q = q.Where(j => j.RelatedEntityId == relatedEntityId.Value);

        // Newest first — if for some reason there are duplicates (idempotency miss),
        // the most recent one is the right thing to track.
        return q.OrderByDescending(j => j.CreatedAt).FirstOrDefaultAsync(ct);
    }

    public Task<List<Job>> ListInFlightForEntityAsync(int userId, string relatedEntityType, int relatedEntityId, CancellationToken ct = default) =>
        _db.Jobs
            .AsNoTracking()
            .Where(j => j.UserId == userId
                        && j.RelatedEntityType == relatedEntityType
                        && j.RelatedEntityId == relatedEntityId
                        && (j.Status == JobStatus.Pending || j.Status == JobStatus.Running))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Job>> ListByStatusAsync(JobStatus status, CancellationToken ct = default) =>
        _db.Jobs.Where(j => j.Status == status).ToListAsync(ct);

    public Task<int> DeleteCompletedBeforeAsync(DateTime cutoff, CancellationToken ct = default) =>
        _db.Jobs
            .Where(j => (j.Status == JobStatus.Completed || j.Status == JobStatus.Failed)
                        && j.CompletedAt != null
                        && j.CompletedAt < cutoff)
            .ExecuteDeleteAsync(ct);

    public void Add(Job job) => _db.Jobs.Add(job);
}
