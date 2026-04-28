using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Tests.TestSupport;

/// <summary>
/// No-op IJobService for controller tests that don't exercise the enqueue
/// path. EnqueueAsync returns a deterministic Guid for assertion purposes;
/// other methods return NotFound. Use real wiring (ChannelJobQueue + DB) in
/// integration tests that actually drive the job pipeline.
/// </summary>
public sealed class TestJobService : IJobService
{
    public Guid LastEnqueuedId { get; private set; }
    public string? LastEnqueuedType { get; private set; }

    public Task<Guid> EnqueueAsync<TPayload>(
        string type,
        TPayload payload,
        string? idempotencyKey = null,
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        CancellationToken ct = default)
    {
        LastEnqueuedType = type;
        LastEnqueuedId = Guid.NewGuid();
        return Task.FromResult(LastEnqueuedId);
    }

    public Task<ServiceResult<JobDto>> GetForCurrentUserAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(ServiceResult<JobDto>.NotFound("TestJobService stub."));

    public Task<ServiceResult<List<JobDto>>> ListForCurrentUserAsync(JobStatus? status = null, CancellationToken ct = default) =>
        Task.FromResult(ServiceResult<List<JobDto>>.Ok(new List<JobDto>()));

    public Task<ServiceResult<JobDto?>> FindInFlightForCurrentUserAsync(string type, string? relatedEntityType = null, int? relatedEntityId = null, CancellationToken ct = default) =>
        Task.FromResult(ServiceResult<JobDto?>.Ok(null));

    public Task<ServiceResult<List<JobDto>>> ListInFlightForEntityAsync(string relatedEntityType, int relatedEntityId, CancellationToken ct = default) =>
        Task.FromResult(ServiceResult<List<JobDto>>.Ok(new List<JobDto>()));
}
