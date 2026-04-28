using System.Text.Json;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Mappers;
using LessonsHub.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LessonsHub.Infrastructure.Realtime;

/// <summary>
/// Pumps the in-memory queue. For each Id: opens a fresh DI scope (so EF
/// + scoped services get clean lifetimes), loads the Job, dispatches to the
/// appropriate executor, persists the result/error, and pushes a JobEvent
/// to the user's SignalR group.
///
/// Startup recovery (StartAsync, before the loop): re-enqueues any rows
/// left in Pending by a previous instance; flips orphaned Running rows to
/// Failed (we can't resume mid-flight without checkpointing).
/// </summary>
public sealed class JobBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IJobQueue _queue;
    private readonly ILogger<JobBackgroundService> _logger;

    public JobBackgroundService(
        IServiceProvider services,
        IJobQueue queue,
        ILogger<JobBackgroundService> logger)
    {
        _services = services;
        _queue = queue;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        await RecoverInflightAsync(ct);
        await base.StartAsync(ct);
    }

    private async Task RecoverInflightAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Pending: previous instance died before pulling them out of the queue.
        // Safe to re-enqueue.
        var pending = await jobs.ListByStatusAsync(JobStatus.Pending, ct);
        foreach (var p in pending)
            await _queue.EnqueueAsync(p.Id, ct);

        if (pending.Count > 0)
            _logger.LogInformation("Recovered {Count} pending jobs from previous run", pending.Count);

        // Running: previous instance died mid-execution. Without checkpointing
        // we can't safely resume; mark Failed so the user gets a clean signal
        // (and can retry from the UI).
        var orphans = await jobs.ListByStatusAsync(JobStatus.Running, ct);
        foreach (var orphan in orphans)
        {
            orphan.Status = JobStatus.Failed;
            orphan.Error = "Worker stopped before completion. Please retry.";
            orphan.CompletedAt = DateTime.UtcNow;
        }
        if (orphans.Count > 0)
        {
            await jobs.SaveChangesAsync(ct);
            _logger.LogWarning("Marked {Count} orphaned Running jobs as Failed", orphans.Count);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var jobId in _queue.ReadAllAsync(ct))
        {
            try
            {
                await ProcessAsync(jobId, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing job {JobId}", jobId);
            }
        }
    }

    private async Task ProcessAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var registry = scope.ServiceProvider.GetRequiredService<IJobExecutorRegistry>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<GenerationHub>>();

        var job = await jobs.GetAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} disappeared between enqueue and dequeue", jobId);
            return;
        }

        // Idempotency: if we re-enqueued during recovery and it's already done, skip.
        if (job.Status is JobStatus.Completed or JobStatus.Failed)
            return;

        job.Status = JobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await jobs.SaveChangesAsync(ct);
        await PushAsync(hub, job, ct);

        try
        {
            var executor = registry.Resolve(job.Type);
            var result = await executor.ExecuteAsync(job, ct);

            job.Status = JobStatus.Completed;
            job.ResultJson = result is null ? null : JsonSerializer.Serialize(result);
            job.CompletedAt = DateTime.UtcNow;
            await jobs.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Service stopping — leave Running so startup recovery picks it up
            // on next boot. (It'll be marked Failed there since we can't resume.)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} (Type={Type}) failed", job.Id, job.Type);
            job.Status = JobStatus.Failed;
            job.Error = Truncate(ex.Message, 4000);
            job.CompletedAt = DateTime.UtcNow;
            await jobs.SaveChangesAsync(ct);
        }

        await PushAsync(hub, job, ct);
    }

    private static Task PushAsync(IHubContext<GenerationHub> hub, Job job, CancellationToken ct) =>
        hub.Clients
            .Group(GenerationHub.GroupForUser(job.UserId))
            .SendAsync("JobUpdated", JobMapper.ToEvent(job), ct);

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max);
}
