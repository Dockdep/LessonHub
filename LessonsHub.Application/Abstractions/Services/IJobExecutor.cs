using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Services;

/// <summary>
/// Strategy for one Job.Type. The background service resolves the right
/// executor from the registry, opens a fresh DI scope per job, and calls
/// ExecuteAsync. The executor returns the JSON result body — the framework
/// handles status transitions, persistence, and the SignalR push.
/// </summary>
public interface IJobExecutor
{
    /// <summary>The Job.Type discriminator this executor handles.</summary>
    string Type { get; }

    /// <summary>
    /// Runs the work. Throw on failure; the framework catches and marks the
    /// job Failed. Return value is JSON-serialized into Job.ResultJson.
    /// </summary>
    Task<object?> ExecuteAsync(Job job, CancellationToken ct);
}

/// <summary>Resolves an executor by Job.Type. Registered as singleton.</summary>
public interface IJobExecutorRegistry
{
    IJobExecutor Resolve(string type);
}
