namespace LessonsHub.Application.Abstractions.Services;

/// <summary>
/// In-process FIFO of job IDs awaiting execution. The current impl wraps
/// a System.Threading.Channels.Channel — single instance, no fanout.
/// Cloud Run runs this with --max-instances=1; if we ever scale out we'll
/// need a Redis-backed implementation.
/// </summary>
public interface IJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct = default);
}
