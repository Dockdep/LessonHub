using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface IJobRepository : IRepository
{
    /// <summary>For the background worker — load by Id without owner check.</summary>
    Task<Job?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>For controllers — caller-scoped fetch. Returns null if not owned.</summary>
    Task<Job?> GetForUserAsync(Guid id, int userId, CancellationToken ct = default);

    /// <summary>Idempotency probe: returns existing job when (userId, type, key) already exists.</summary>
    Task<Job?> FindByIdempotencyKeyAsync(int userId, string type, string idempotencyKey, CancellationToken ct = default);

    /// <summary>Used by the in-flight UI banner. Status filter is optional.</summary>
    Task<List<Job>> ListForUserAsync(int userId, JobStatus? status = null, CancellationToken ct = default);

    /// <summary>Startup recovery: re-enqueue Pending jobs left by a previous instance.</summary>
    Task<List<Job>> ListByStatusAsync(JobStatus status, CancellationToken ct = default);

    /// <summary>Cleanup helper: jobs that finished long enough ago to drop.</summary>
    Task<int> DeleteCompletedBeforeAsync(DateTime cutoff, CancellationToken ct = default);

    void Add(Job job);
}
