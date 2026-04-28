using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Services.Executors;

/// <summary>
/// TEMP — Phase-0 sanity test only. Sleeps for the requested seconds (clamped),
/// then returns the original message back. Lets us verify the full pipeline
/// (controller → DB → queue → background service → hub) without needing real
/// AI integration. Delete in Phase 2 once a real executor is proven working.
/// </summary>
public sealed class EchoTestExecutor : IJobExecutor
{
    public const string TypeName = "_TestEcho";

    public string Type => TypeName;

    public async Task<object?> ExecuteAsync(Job job, CancellationToken ct)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<EchoPayload>(job.PayloadJson)
                      ?? new EchoPayload("(empty)", 1);
        var seconds = Math.Clamp(payload.SleepSeconds, 0, 30);
        await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        return new { echoed = payload.Message, sleptSeconds = seconds, completedAt = DateTime.UtcNow };
    }
}

public sealed record EchoPayload(string Message, int SleepSeconds);
