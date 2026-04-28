using System.Text.Json;
using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Mappers;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services;

public sealed class JobService : IJobService
{
    private readonly IJobRepository _jobs;
    private readonly IJobQueue _queue;
    private readonly ICurrentUser _currentUser;

    public JobService(IJobRepository jobs, IJobQueue queue, ICurrentUser currentUser)
    {
        _jobs = jobs;
        _queue = queue;
        _currentUser = currentUser;
    }

    public async Task<Guid> EnqueueAsync<TPayload>(
        string type,
        TPayload payload,
        string? idempotencyKey = null,
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUser.Id;

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _jobs.FindByIdempotencyKeyAsync(userId, type, idempotencyKey, ct);
            if (existing is not null)
                return existing.Id;
        }

        var job = new Job
        {
            UserId = userId,
            Type = type,
            Status = JobStatus.Pending,
            PayloadJson = JsonSerializer.Serialize(payload),
            IdempotencyKey = idempotencyKey,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
        };

        _jobs.Add(job);
        await _jobs.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(job.Id, ct);

        return job.Id;
    }

    public async Task<ServiceResult<JobDto>> GetForCurrentUserAsync(Guid id, CancellationToken ct = default)
    {
        var job = await _jobs.GetForUserAsync(id, _currentUser.Id, ct);
        if (job is null)
            return ServiceResult<JobDto>.NotFound("Job not found.");
        return ServiceResult<JobDto>.Ok(JobMapper.ToDto(job));
    }

    public async Task<ServiceResult<List<JobDto>>> ListForCurrentUserAsync(JobStatus? status = null, CancellationToken ct = default)
    {
        var jobs = await _jobs.ListForUserAsync(_currentUser.Id, status, ct);
        return ServiceResult<List<JobDto>>.Ok(jobs.Select(JobMapper.ToDto).ToList());
    }

    public async Task<ServiceResult<JobDto?>> FindInFlightForCurrentUserAsync(string type, string? relatedEntityType = null, int? relatedEntityId = null, CancellationToken ct = default)
    {
        var job = await _jobs.FindInFlightAsync(_currentUser.Id, type, relatedEntityType, relatedEntityId, ct);
        return ServiceResult<JobDto?>.Ok(job is null ? null : JobMapper.ToDto(job));
    }

    public async Task<ServiceResult<List<JobDto>>> ListInFlightForEntityAsync(string relatedEntityType, int relatedEntityId, CancellationToken ct = default)
    {
        var jobs = await _jobs.ListInFlightForEntityAsync(_currentUser.Id, relatedEntityType, relatedEntityId, ct);
        return ServiceResult<List<JobDto>>.Ok(jobs.Select(JobMapper.ToDto).ToList());
    }
}
