using LessonsHub.Application.Abstractions.Services;

namespace LessonsHub.Infrastructure.Realtime;

public sealed class JobExecutorRegistry : IJobExecutorRegistry
{
    private readonly IReadOnlyDictionary<string, IJobExecutor> _byType;

    public JobExecutorRegistry(IEnumerable<IJobExecutor> executors)
    {
        _byType = executors.ToDictionary(e => e.Type, e => e, StringComparer.Ordinal);
    }

    public IJobExecutor Resolve(string type)
    {
        if (_byType.TryGetValue(type, out var exec))
            return exec;
        throw new InvalidOperationException($"No IJobExecutor registered for type '{type}'.");
    }
}
