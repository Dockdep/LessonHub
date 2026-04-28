using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Services;

public interface IJobService
{
    /// <summary>
    /// Persists a Pending job for the current user, hands its Id to the
    /// in-memory queue, and returns the Id. Idempotent: when (currentUser,
    /// type, idempotencyKey) already exists, returns the existing job's Id
    /// without enqueueing again.
    /// </summary>
    /// <param name="payload">Strongly-typed request — JSON-serialized into Job.PayloadJson.</param>
    Task<Guid> EnqueueAsync<TPayload>(
        string type,
        TPayload payload,
        string? idempotencyKey = null,
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        CancellationToken ct = default);

    Task<ServiceResult<JobDto>> GetForCurrentUserAsync(Guid id, CancellationToken ct = default);

    Task<ServiceResult<List<JobDto>>> ListForCurrentUserAsync(JobStatus? status = null, CancellationToken ct = default);
}
