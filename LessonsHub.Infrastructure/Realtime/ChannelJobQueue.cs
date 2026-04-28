using System.Threading.Channels;
using LessonsHub.Application.Abstractions.Services;

namespace LessonsHub.Infrastructure.Realtime;

/// <summary>
/// Unbounded channel — producers (controllers) never block. The single
/// background-service consumer pulls one Id at a time and drives execution.
/// </summary>
public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(jobId, ct);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);
}
